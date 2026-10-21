# Module 3 — завдання для тіммейта (5 задач)

Привіт! Я вже зробив свою половину Module 3. Твоя половина — продуктивність,
профілювання, БД, reliability та перевірка кешування. Нижче — готовий промт і
точні кроки. Будь ласка, **на кожну задачу створи окрему гілку від
`feature/module3-sonarqube` і зроби окремий коміт**. Не мержити в `develop` без
перевірки.

## 0. Підготовка

```bash
git fetch
git checkout feature/module3-sonarqube   # базова гілка з усім кодом Module 3
dotnet build
dotnet test
```

Що вже є в коді (не переписуй з нуля):

- Кешування: `IMemoryCache`, `X-Cache: HIT|MISS`, TTL у `appsettings.json`,
  інвалідація після create/update/delete.
- Resilience: named `HttpClient` з timeout + retry + circuit breaker
  (`Infrastructure/HttpClientExtensions.cs`), `/health` з перевіркою БД,
  `ExceptionMiddleware`.
- Rate limiting: 10/хв на auth, 60/хв на write; вимкнений у `Testing`.
- API v1 і v2, інтеграційні тести `LibraryAPI.Module3Tests` на SQLite.
- SonarQube: 0 bugs, 0 vulnerabilities, 0 debt; coverage 34.4%.

Локальне середовище: macOS, .NET 10 SDK, Docker Desktop, MySQL 8.
Секрети — тільки в `.env` (не комітити). Запуск: `dotnet run` (5043/7043).
Swagger: `http://localhost:5043/swagger`. SonarQube: `http://localhost:9000`.

---

## 1. Performance Testing

**Гілка:** `feature/module3-performance-testing`  
**Коміт:** `test(perf): add load testing scenarios and results`

Зроби:

1. Встанови k6 (`brew install k6`) або використай наявний `/usr/sbin/ab`.
2. Створи `load-tests/`:
   - `books-smoke.js` — 1 VU, 30s, перевірка 200.
   - `books-load.js` — ramp 0→50→100 VU, 2–3 хв, GET `/api/v1/books`.
   - `books-stress.js` — до 300 VU, знайти точку деградації.
   - `auth-rate-limit.js` — перевірити 429 після 10 login/хв.
   - `mixed.js` — 80% GET books, 15% GET book by id, 5% genres.
3. Для кожного сценарію зафіксуй: RPS, p50/p95/p99 latency, error rate,
   кількість запитів. Окремо — cold cache vs warm cache (`X-Cache`).
4. Для `/api/v1/books` порівняй `pageSize=10` і `pageSize=100`, різні `sortBy`.
5. Збережи сирі результати в `performance-results/` (`.txt`/`.json`, без
   гігабайтних файлів) і напиши `performance-testing-report.md`.

Критерії: є відтворювані команди, таблиця метрик, висновок про bottleneck і
рекомендації. Не комітити `node_modules`, великі логи, `.env`.

---

## 2. Profiling

**Гілка:** `feature/module3-profiling`  
**Коміт:** `perf(profiling): capture CPU/GC traces and document hotspots`

Зроби:

1. Встанови інструменти:
   ```bash
   dotnet tool install --global dotnet-trace
   dotnet tool install --global dotnet-counters
   dotnet tool install --global dotnet-dump
   ```
2. Запусти API та прогрій ендпоінти (можна k6 з попередньої задачі).
3. Збери:
   - `dotnet-counters monitor -p <pid> --counters System.Runtime,Microsoft.AspNetCore.Hosting`
   - `dotnet-trace collect -p <pid> --duration 00:00:30 --providers Microsoft-DotNETCore-SampleProfiler`
   - окремо профіль під навантаженням на `/api/v1/books` (кеш warm/cold).
4. Проаналізуй топ CPU-методів, GC gen0/1/2, thread pool queue, винятки.
   Зверни увагу на EF Core запити, серіалізацію JSON, `Include`, алокації в
   `BookService`/`BookChatService`.
5. Напиши `profiling-report.md`: методика, графіки/таблиці, топ-5 hotspots,
   до/після оптимізації, що саме змінив (якщо змінював код — окремий коміт).

Критерії: трасування відтворюване, є числа, висновки підтверджені вимірами.
Не комітити `.nettrace`/`.dmp` — додай їх у `.gitignore`, якщо потрібно.

---

## 3. Оптимізація БД

**Гілка:** `feature/module3-db-optimization`  
**Коміт:** `perf(db): add indexes and optimize hot queries`

Зроби:

1. Увімкни MySQL slow query log або перевір `EXPLAIN ANALYZE` для:
   - `SELECT ... FROM books WHERE title LIKE ... OR author LIKE ...`
   - books + genres + reviews count (пагінація, сортування);
   - `reading_progress WHERE user_id = ?`;
   - `daily_activity WHERE user_id = ? AND date >= ?`;
   - `reviews WHERE book_id = ?`;
   - `friend_requests WHERE to_user_id = ? AND status = ?`.
2. Запропонуй та додай індекси через EF migration:
   - `books(title)`, `books(author)`, `books(genre_id)`, `books(created_at)`;
   - `reviews(book_id)`, `reading_progress(user_id)`,
     `daily_activity(user_id, date)`, `friend_requests(to_user_id, status)`.
   Уникай дублювання вже наявних індексів.
3. Перевір план до/після, зафіксуй `EXPLAIN` і час виконання.
4. Перевір N+1 та зайві `Include`; використовуй `AsNoTracking` для read-only.
5. Напиши `db-optimization-report.md` з таблицею до/після та командами
   `dotnet ef migrations add ...`, `dotnet ef database update`.

Критерії: міграція застосовується, тести проходять, є виміри до/після.
Не ламати сумісність з SQLite-тестами.

---

## 4. Reliability

**Гілка:** `feature/module3-reliability`  
**Коміт:** `test(reliability): verify retries, timeouts and fallbacks`

У коді вже є resilience-інфраструктура. Тобі треба її **перевірити й довести**:

1. Перевір `/health` при живій і недоступній БД (зупини MySQL у Docker або
   зміни порт у тимчасовому конфігу).
2. Перевір поведінку Groq/PDF/Gutenberg при:
   - таймауті;
   - 500/503;
   - відкритому circuit breaker.
   Використай локальний mock HTTP-сервер (можна `WireMock.Net` або простий
   `HttpListener`) і переконайся, що retry не дублює небезпечні запити.
3. Додай інтеграційні/unit-тести в `LibraryAPI.Module3Tests`:
   - middleware повертає 404/400/403/500 без stack trace;
   - health check `Degraded`/`Unhealthy`;
   - timeout/circuit breaker повертає контрольовану помилку.
4. Перевір graceful shutdown і коректне скасування запитів (`RequestAborted`).
5. Напиши `reliability-report.md`: сценарії відмови, очікувана/фактична
   поведінка, логи, висновки.

Критерії: є тести, які падають без resilience і проходять з ним; немає
необроблених винятків у логах; звіт з відтворюваними кроками.

---

## 5. Caching

**Гілка:** `feature/module3-caching`  
**Коміт:** `perf(cache): verify cache hit ratio, TTL and invalidation`

Кешування вже реалізовано в `BookService`/`GenreService`. Треба:

1. Перевір `X-Cache: HIT/MISS` для:
   - списку книг (різні `page/pageSize/search/sort`);
   - книги за id;
   - жанрів.
2. Перевір TTL: після `BookListSeconds`/`BookSeconds`/`GenresSeconds` знову MISS.
3. Перевір інвалідацію: create/update/delete книги → список і картка книги
   оновлюються, старі ключі не повертаються.
4. Перевір behavior при вимкненому кеші (`Caching__Enabled=false`).
5. Оціни cache stampede: одночасні 50 запитів на холодний ключ. Якщо треба —
   додай `SemaphoreSlim`/`Lazy<Task<T>>` і тест.
6. Додай тести в `LibraryAPI.Module3Tests`, напиши `caching-report.md` з
   hit ratio до/після, latency, обмеженнями (in-memory, per-instance).

Критерії: тести на HIT/MISS/TTL/invalidation, виміряний ефект, немає stale
даних після мутацій.

---

## Формат здачі

Для кожної задачі:

```bash
git checkout feature/module3-sonarqube
git checkout -b feature/module3-<task>
# ... робота ...
git add <файли>
git commit -m "<повідомлення з прикладу вище>"
git push -u origin feature/module3-<task>
```

У PR/повідомленні вкажи: що зроблено, як перевірити, які метрики до/після,
які файли змінені. Не комітити `.env`, токени, трасування, логи, `bin/obj`.
