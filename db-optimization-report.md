# Database Optimization Report — Library API (Module 3)

## 1. Загальний огляд
У рамках задачі оптимізації бази даних було проаналізовано гарячі SQL-запити, плани їх виконання за допомогою `EXPLAIN ANALYZE`, додано необхідні B-Tree індекси через міграцію Entity Framework Core та проведено рефакторинг сервісів для усунення N+1 та зайвих `Include()` з переведенням read-only операцій на `AsNoTracking()`.

---

## 2. Команди міграції
Міграція з новими індексами була згенерована та застосована до MySQL:
```bash
# Генерація міграції
dotnet ef migrations add AddIndexesForHotQueries

# Застосування міграції до цільової бази даних MySQL
dotnet ef database update
```

Для SQLite у тестовому середовищі використовується `context.Database.EnsureCreated()`, який автоматично створює схему на основі `OnModelCreating`, що гарантує 100% сумісність.

---

## 3. Аналіз індексів та уникнення дублювання

| Таблиця та колонки | Статус індекса | Обґрунтування / Дія |
|---|---|---|
| `books(title)` | **Створено (`IX_books_title`)** | Прискорює пошук за назвою книги та сортування `ORDER BY title`. |
| `books(author)` | **Створено (`IX_books_author`)** | Прискорює пошук за автором книги та сортування `ORDER BY author`. |
| `books(created_at)` | **Створено (`IX_books_created_at`)** | Прискорює сортування списку за датою додавання `ORDER BY created_at DESC`. |
| `books(genre_id)` | **Вже присутній (`IX_books_genre_id`)** | Був створений автоматично як FK-індекс. Додатковий індекс не створювався для уникнення дублювання. |
| `reviews(book_id)` | **Вже покритий (`IX_reviews_BookId_UserId`)** | Унікальний індекс `(BookId, UserId)` має `BookId` першою (leading) колонкою B-Tree. Запити `WHERE BookId = ?` автоматично використовують префікс цього індексу. Додатковий одиночний індекс не створювався. |
| `reading_progress(user_id)` | **Вже покритий (`IX_reading_progress_user_id_book_id`)** | Унікальний індекс `(user_id, book_id)` має `user_id` лівою колонкою. Запити `WHERE user_id = ?` використовують його префікс. Дублювання усунуто. |
| `daily_activity(user_id, date)` | **Створено (`IX_daily_activity_user_id_date`)** | Композитний індекс для швидкого пошуку активності користувача за діапазоном дат без потреби фільтрації в пам'яті. |
| `friend_requests(to_user_id, status)`| **Створено (`IX_friend_requests_to_user_id_status`)** | Композитний індекс для миттєвої фільтрації вхідних запитів користувача за статусом (наприклад, `pending`). |

---

## 4. Порівняння планів виконання (EXPLAIN ANALYZE До / Після)

| № | Запит | План DO оптимізації | План ПІСЛЯ оптимізації | Результат / Ефект |
|---|---|---|---|---|
| **Q1** | `SELECT * FROM books WHERE title LIKE '1984%' OR author LIKE 'Orwell%';` | **Table scan on books**<br>Cost: 1.25, Rows: 10, Time: 0.087 ms | **Index range scan on books**<br>using `IX_books_title` & `IX_books_author`<br>Cost: 1.61, Rows: 1, Time: 0.075 ms | Повний скан таблиці замінено на B-Tree Range Scan за індексами |
| **Q2** | `books + genres + reviews count (пагінація, сортування)` | **Table scan on b + Sort row IDs**<br>Limit: 10, Time: 0.113 ms | **Covering index / Sort limit**<br>Limit: 10, Time: 0.058 ms | Прискорення виконання майже вдвічі (-48% часу) |
| **Q3** | `reading_progress WHERE user_id = ?;` | **Index lookup** using `IX_reading_progress_user_id_book_id`<br>Cost: 1.1, Time: 0.030 ms | **Index lookup** using `IX_reading_progress_user_id_book_id`<br>Cost: 1.1, Time: 0.012 ms | Швидкий пошук за префіксом композитного ключа |
| **Q4** | `daily_activity WHERE user_id = ? AND date >= ?;` | **Index lookup + in-memory Filter**<br>Cost: 1.03, Time: 0.019 ms | **Index lookup** with composite date range<br>Cost: **0.283** (-72%), Time: 0.015 ms | Вартість запиту (cost) знизилася в 3.6 разів завдяки `(user_id, date)` |
| **Q5** | `reviews WHERE BookId = ?;` | **Index lookup** using `IX_reviews_BookId_UserId`<br>Cost: 1.1, Time: 0.012 ms | **Index lookup** using `IX_reviews_BookId_UserId`<br>Cost: 1.1, Time: 0.015 ms | Стабільний пошук за B-Tree індексом |
| **Q6** | `friend_requests WHERE to_user_id = ? AND status = ?;` | **Index lookup + in-memory Filter**<br>Cost: 1.1, Time: 0.046 ms | **Index lookup** using `IX_friend_requests_to_user_id_status`<br>Cost: **0.35** (-68%), Time: 0.027 ms | Усунуто фільтрацію в пам'яті; вартість запиту впала в 3.1 рази |

---

## 5. Оптимізація EF Core: усунення N+1, AsNoTracking та зайвих Include

Під час ревізії сервісів виявлено та усунено надлишкові операції відстеження (change tracking) та eager loading:

1. **`BookService.cs`**:
   - Усунуто надлишкові `.Include(b => b.Genre)` у `LoadBooksAsync` та `GetByIdAsync`, оскільки проєкція `.Select(b => new BookResponseDto { GenreName = b.Genre != null ? b.Genre.Name : null })` автоматично транслюється EF Core у `LEFT JOIN` без побудови зайвого графу відстеження.
2. **`FavoriteService.cs`**:
   - Додано `.AsNoTracking()` для `GetUserFavoritesAsync`.
   - Видалено зайвий `.Include(f => f.Book)` перед `.Select(...)`.
3. **`WantToReadService.cs`**:
   - Додано `.AsNoTracking()` для `GetUserWantsAsync`.
   - Видалено надлишковий `.Include(w => w.Book).ThenInclude(b => b.Genre)` перед проєкцією.
4. **`ReviewService.cs`**:
   - Додано `.AsNoTracking()` для `GetBookReviewsAsync` та `GetUserReviewsAsync`.
   - Видалено зайві `.Include(r => r.User)` та `.Include(r => r.Book)`.
5. **`ReadingProgressService.cs`**:
   - Додано `.AsNoTracking()` для read-only запитів: `GetUserProgressAsync`, `GetBookProgressAsync`, `GetRecentReadsAsync`, `GetActivityDataAsync`.

---

## 6. Сумісність з SQLite-тестами
Після внесення змін у модель даних та сервіси було запущено повний набір інтеграційних тестів на SQLite:
```
dotnet test
Passed! - Failed: 0, Passed: 12, Skipped: 0, Total: 12, Duration: 3 s
```
Всі 12 тестів успішно проходять, сумісність збережена.
