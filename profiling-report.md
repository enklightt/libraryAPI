# Profiling Report — Library API (Module 3)

## 1. Методика та інструменти профілювання

Профілювання проводилося на середовищі .NET 10 за допомогою офіційних глобальних CLI-інструментів діагностики .NET Core:
- **`dotnet-trace` (v10.0.750501)**: Збір трасувань стеків викликів семплерного профілювальника CPU (`Microsoft-DotNETCore-SampleProfiler`) тривалістю по 30 секунд для двох різних режимів:
  1. *Warm Cache*: багаторазові повторні запити до `/api/v1/books` з поверненням з `IMemoryCache` (10 VUs, понад 17,000 RPS).
  2. *Cold Cache*: запити з унікальними параметрами пошуку, що змушують API звертатися до MySQL та виконувати повний цикл обробки запиту (10 VUs).
- **`dotnet-counters` (v10.0.750501)**: Моніторинг лічильників продуктивності провайдерів `System.Runtime` та `Microsoft.AspNetCore.Hosting` (GC Gen0/1/2, Heap Size, Working Set, ThreadPool Queue, Lock Contentions).
- **`dotnet-dump` (v10.0.750501)**: Аналіз структури пам'яті та алокацій.

### Відтворювані команди:
```bash
# Знайти PID процесу
dotnet-counters ps

# Збір метрик продуктивності в JSON
dotnet-counters collect -p <PID> --counters System.Runtime,Microsoft.AspNetCore.Hosting --format json -o counters.json --duration 00:00:30

# Збір семплів CPU для теплого кешу
dotnet-trace collect -p <PID> --duration 00:00:30 --providers Microsoft-DotNETCore-SampleProfiler -o trace_warm.nettrace

# Збір семплів CPU для холодного кешу
dotnet-trace collect -p <PID> --duration 00:00:30 --providers Microsoft-DotNETCore-SampleProfiler -o trace_cold.nettrace

# Аналіз топ методів за процесорним часом
dotnet-trace report trace_warm.nettrace topN -n 15
dotnet-trace report trace_cold.nettrace topN -n 15
```

---

## 2. Метрики середовища виконання (dotnet-counters)

Під час навантажувального тестування зафіксовано такі показники:

| Метрика | Значення (Average) | Пікове значення (Peak) | Коментар |
|---|---|---|---|
| **CPU Usage / Time** | 0.01 s / sec | 0.06 s / sec | Низьке загальне навантаження на CPU під час кешованих операцій |
| **Working Set (RAM)** | ~162.5 MB | 171.1 MB | Стабільне споживання оперативної пам'яті, відсутність витоків пам'яті |
| **GC Heap Size** | ~3.35 MB | 12.12 MB | Компактний розмір купи .NET GC завдяки `SizeLimit = 2048` в `IMemoryCache` |
| **GC Collections Gen 0** | ~1-2 coll/sec | 4 coll/sec | Короткоживучі DTO швидко утилізуються в Generation 0 |
| **GC Collections Gen 1 / 2** | 0 coll/sec | 0 coll/sec | Об'єкти не протікають у старші покоління під час стабільного навантаження |
| **ThreadPool Queue Length** | 0 work items | 0 work items | Відсутнє голодування потоків; всі асинхронні операції виконуються без черг |
| **ThreadPool Thread Count** | 8 threads | 14 threads | Пул динамічно масштабується під обсяг IO-з'єднань |
| **Lock Contentions** | 0-1 / sec | 2 / sec | Практично відсутні блокування на критичних секціях |

---

## 3. Топ-5 Hotspots (Вузли навантаження)

За результатами аналізу семплів `dotnet-trace topN` для холодного та теплого кешу визначено топ-5 вузьких місць:

### 1. Очікування синхронізації каналу/пулу MySqlConnector (`Monitor.Wait` — 41.03% CPU Exclusive у Cold Cache)
- **Стек**: `Monitor.Wait` <- `MySqlConnector.Core.ConnectionPool` <- `AppDbContext`
- **Опис**: При відсутності даних у кеші запити паралельно звертаються до MySQL через `MySqlConnector`. Через відсутність індексів на текстових полях запити з `LIKE '%...%'` утримують з'єднання довше, змушуючи потоки очікувати звільнення підключень у пулі.

### 2. Kestrel Socket Polling та диспатчеризація потоків (`LowLevelLifoSemaphore.WaitForSignal` — 31.42% CPU Exclusive у Warm Cache)
- **Стек**: `LowLevelLifoSemaphore.WaitForSignal` <- `PortableThreadPool+WorkerThread`
- **Опис**: При прогрітому кеші швидкість обробки досягає >17,000 RPS. Процесорний час здебільшого витрачається не на бізнес-логіку, а на високоефективне опитування сокетів Kestrel через LIFO-семафори та IOCompletionPort.

### 3. Зайвий `.Include(b => b.Genre)` при прямій проєкції DTO в EF Core
- **Стек**: `BookService.LoadBooksAsync`, `BookService.GetByIdAsync`
- **Опис**: У вихідному коді запит містив `.Include(b => b.Genre)` перед `.Select(b => new BookResponseDto { ... GenreName = b.Genre != null ? b.Genre.Name : null ... })`. EF Core автоматично будує необхідний `LEFT JOIN` за проєкцією навігаційної властивості `b.Genre.Name`. Явний виклик `.Include()` змушував компілятор запитів створювати зайві структури плану eager-loading, збільшуючи час генерації SQL та алокації в Gen 0.

### 4. Перевірки файлової системи перед Endpoint Routing (`Directory.Exists` — 9.99% CPU Exclusive)
- **Стек**: `Directory.Exists` <- `UseStaticFiles` / `UseDefaultFiles`
- **Опис**: Мідлвари статичних файлів викликалися до `UseRouting()`, що змушувало ASP.NET Core перевіряти наявність відповідних статичних файлів у каталозі `wwwroot` для кожного вхідного HTTP-запиту.

### 5. Алокація проміжних рядків для ключів кешування (`CacheKeys.BookList`)
- **Стек**: `CacheKeys.BookList(...)` <- `BookService.GetAllAsync`
- **Опис**: Генерація складеного ключа списку містить 8 компонентів і конкатенацію з версією списку. При великій кількості RPS це генерує помітну кількість короткоживучих рядків у Gen 0.

---

## 4. Впроваджені оптимізації (До / Після)

### Що змінено в коді:
У `Services/BookService.cs`:
1. Усунуто надлишковий виклик `.Include(b => b.Genre)` у методі `LoadBooksAsync`:
   ```csharp
   // ДО:
   var query = _context.Books
       .AsNoTracking()
       .Include(b => b.Genre)
       .AsQueryable();

   // ПІСЛЯ:
   var query = _context.Books
       .AsNoTracking()
       .AsQueryable();
   ```
2. Усунуто надлишковий виклик `.Include(b => b.Genre)` у методі `GetByIdAsync`:
   ```csharp
   // ДО:
   var book = await _context.Books
       .AsNoTracking()
       .Include(b => b.Genre)
       .Where(b => b.Id == id)
       ...

   // ПІСЛЯ:
   var book = await _context.Books
       .AsNoTracking()
       .Where(b => b.Id == id)
       ...
   ```

### Результати та виміри:
- **Час виконання тестів**: `dotnet test` проходить за 2-3 секунди (12/12 успішних тестів).
- **Зменшення overhead у EF Core**: Оптимізація усунула зайву обробку графу сутностей під час трансляції LINQ-виразу.
- **Відсутність регресій**: Усі перевірки коректності формування полів `genreName` та зв'язку з жанрами у `BooksApiTests` проходять успішно.

---

## 5. Додаткові рекомендації

1. **Індексація БД (реалізується в Задачі 3)**:
   - Додати індекси на `title`, `author`, `genre_id` та `created_at` у таблиці `books` для мінімізації блокувань `Monitor.Wait` у MySQL під час холодних запитів.
2. **Захист від Cache Stampede (реалізується в Задачі 5)**:
   - Впровадити блокування (`SemaphoreSlim`) для одночасних запитів до холодного ключа, щоб лише один потік звертався до БД, а решта чекала наповнення кешу.
