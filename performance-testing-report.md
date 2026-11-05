# Performance Testing Report — Library API (Module 3)

## 1. Загальний огляд та конфігурація
- **Платформа**: ASP.NET Core 10, Kestrel HTTP Server.
- **База даних**: MySQL Server 8/9.5 (локально, порт 3306), Entity Framework Core 9 (Pomelo).
- **Кешування**: In-Memory Cache (`IMemoryCache`), `X-Cache: HIT|MISS`, TTL: `BookListSeconds = 30s`, `BookSeconds = 60s`, `GenresSeconds = 300s`.
- **Rate Limiting**: `RateLimiter` з лімітом 10 запитів/хв для `/api/v1/auth/login` та 60 запитів/хв для операцій запису.
- **Інструмент тестування**: Grafana k6 v2.3.0.

---

## 2. Відтворювані команди запуску
API сервер запускається на `http://localhost:5043`:
```bash
dotnet run --urls "http://localhost:5043"
```

Запуск тестових сценаріїв:
```bash
# Димовий тест (Smoke)
k6 run --summary-export performance-results/books-smoke.json load-tests/books-smoke.js

# Навантажувальний тест (Load)
k6 run --summary-export performance-results/books-load.json load-tests/books-load.js

# Стрес-тест (Stress)
k6 run --summary-export performance-results/books-stress.json load-tests/books-stress.js

# Перевірка Rate Limiting на авторизацію (10/хв)
k6 run --summary-export performance-results/auth-rate-limit.json load-tests/auth-rate-limit.js

# Змішане навантаження (Mixed)
k6 run --summary-export performance-results/mixed.json load-tests/mixed.js

# Порівняння Cold Cache vs Warm Cache
k6 run --summary-export performance-results/cache-comparison.json load-tests/cache-comparison.js
```

---

## 3. Зведена таблиця метрик основних сценаріїв

| Сценарій | Тривалість / VU | Кількість запитів | RPS | p50 Latency | p95 Latency | p99 Latency / Max | Error Rate |
|---|---|---|---|---|---|---|---|
| **books-smoke.js** | 30s / 1 VU | 30 | 0.98 req/s | 1.84 ms | 8.87 ms | Max: 356.28 ms | 0.00% |
| **books-load.js** | 3m / 0→50→100 VU | 103,144 | 572.92 req/s | 1.04 ms | 3.44 ms | Max: 76.12 ms | 0.00% |
| **books-stress.js** | 2m10s / до 300 VU | 344,172 | 2,647.30 req/s | 0.65 ms | 2.86 ms | Max: 105.55 ms | 0.00% |
| **auth-rate-limit.js**| 15 запитів / 1 VU | 15 | 9.08 req/s | 3.74 ms | 29.36 ms | Max: 78.13 ms | 0% помилок логіки (перші 10 — не 429, з 11-го — строго 429) |
| **mixed.js** | 2m / до 50 VU | 35,160 | 292.97 req/s | 1.08 ms | 3.54 ms | Max: 56.00 ms | 0.00% |

> **Примітка щодо `auth-rate-limit.js`**: Перші 10 запитів повертають HTTP 401 (невірний пароль, запит успішно оброблено контролером), а запити 11–15 повертають HTTP 429 (Too Many Requests), що підтверджує 100% коректне спрацювання `SlidingWindowLimiter` на рівні middleware.

---

## 4. Порівняльний аналіз: Cold Cache vs. Warm Cache

Сценарій `load-tests/cache-comparison.js` провів два ізольованих цикли по 30 секунд з 5 VU:
1. **Cold Cache (`X-Cache: MISS`)**: Кожен запит містив випадковий унікальний пошуковий параметр, форсуючи повне проходження крізь пайплайн ASP.NET Core до БД MySQL (генерація SQL через EF Core, виконання `SELECT ... FROM books WHERE title LIKE ... OR author LIKE ...`).
2. **Warm Cache (`X-Cache: HIT`)**: Запити зверталися до попередньо прогрітого ключа списку книг з пам'яті.

| Метрика | Cold Cache (MISS, з MySQL) | Warm Cache (HIT, IMemoryCache) | Різниця / Прискорення |
|---|---|---|---|
| **Виконано запитів за 30с** | 19,991 | 331,261 | **+1557% (у ~16.5 разів більше)** |
| **Пропускна здатність (RPS)** | ~666 req/s | ~11,042 req/s | **x16.5 throughput** |
| **Середній час відповіді** | 7.42 ms | 0.45 ms | **Зменшення затримки у 16.5 разів** |
| **p95 Latency** | 14.80 ms | 0.72 ms | **x20.5 швидше на 95 перцентилі** |
| **Error Rate** | 0.00% | 0.00% | 0.00% |

**Висновок**: In-memory кешування знімає практично все навантаження на пул підключень до MySQL для гарячих сторінок, знижуючи p95 до субмілісекундних значень (< 1 ms).

---

## 5. Порівняння параметрів запиту (/api/v1/books)

Було протестовано вплив розміру сторінки (`pageSize=10` vs `pageSize=100`) та різних варіантів сортування (`sortBy=title`, `author`, `rating`, `created`):

| Конфігурація | Пропускна здатність (RPS) | Середній час (avg) | p90 Latency | p95 Latency | Max Latency |
|---|---|---|---|---|---|
| `pageSize=10, sortBy=title` | 11,287 req/s | 0.38 ms | 0.64 ms | 0.76 ms | 15.79 ms |
| `pageSize=100, sortBy=title` | 11,567 req/s | 0.37 ms | 0.62 ms | 0.72 ms | 16.25 ms |
| `pageSize=10, sortBy=author` | 11,693 req/s | 0.36 ms | 0.60 ms | 0.71 ms | 31.51 ms |
| `pageSize=10, sortBy=rating` | 11,903 req/s | 0.35 ms | 0.59 ms | 0.69 ms | 12.81 ms |
| `pageSize=10, sortBy=created` | 11,563 req/s | 0.36 ms | 0.61 ms | 0.72 ms | 19.37 ms |

**Спостереження**: 
- Завдяки `CacheKeys.BookList(...)`, різні комбінації сортування та розміру сторінки мають незалежні ключі кешу.
- При повторних запитах кешована відповідь віддається однаково швидко (~0.36-0.38 ms) незалежно від поля сортування.
- При `pageSize=100` об'єм JSON-пакету зростає в ~10 разів, що призводить до більшого використання пам'яті серіалізатором, проте на локальному середовищі з Kestrel суттєвої деградації затримки не виявлено.

---

## 6. Виявлені вузькі місця (Bottlenecks) та рекомендації

1. **Cold Cache Throughput (База даних як первинний bottleneck)**:
   - При 100% холодних запитах пропускна здатність падає з 11,000+ RPS до ~660 RPS. Вузьким місцем стає виконання SQL-запитів з `LIKE %...%` у MySQL без відповідних індексів або повнотекстового пошуку, а також навантаження на connection pool.
   - *Рекомендація*: Додати композитні та покриваючі індекси (реалізовано в задачі 3), а також захист від Cache Stampede (реалізовано в задачі 5).

2. **Rate Limiting**:
   - `RateLimiter` відмінно захищає `/api/v1/auth/login` (відсікає на 11-му запиті з HTTP 429), запобігаючи brute-force атакам на обчислювально важкий хеш `BCrypt`.

3. **In-Memory Cache Size Limit**:
   - Наразі розмір кешу обмежений `SizeLimit = 2048` у `Program.cs`. Кожен запис має `Size = 1`. При великій кількості унікальних комбінацій фільтрів старі записи будуть витіснятися (LRU eviction).
   - *Рекомендація*: Моніторити `Gen0/Gen1 GC` та розмір пам'яті процесу під час профілювання (задача 2).
