# libraryAPI — цифрова бібліотека

REST API для онлайн-бібліотеки: каталог книг, читання PDF/текстів, прогрес читання, відгуки, вибране, «хочу прочитати», гейміфікація, друзі, AI-чат про книгу (Groq). + статичний фронтенд `wwwroot/index.html` (LibraFlow).

База: `api/v1/...`, Swagger в Development.

## Можливості

- **Книги:** пошук, фільтр за жанром, сортування, пагінація, CRUD (admin/manager), завантаження PDF, проксі PDF, текст з Project Gutenberg / Gutendex
- **Auth:** register / login / refresh, JWT + Refresh-токени (ротація), BCrypt, ролі `admin, customer, librarian, manager`
- **Читач:** `favorites`, `want-to-read` (toggle), `reading-progress` (upsert, % , статуси `reading|finished`, `recent`, `activity` heatmap по днях)
- **Соціальне:** `reviews` (1 на юзер-книгу, перерахунок рейтингу), `friends` (заявки pending/accept/reject, профіль, спільні книги, пошук)
- **Гейміфікація:** 5 ачивок (`first_book`, `read_5_books`, `bookworm`, `night_reader`, `favorite_collector`), титули
- **AI:** `POST books/{id}/chat` та `/chat/stream` (SSE) — Groq `llama-3.3-70b-versatile`, відповіді українською

## Стек

`ASP.NET Core net10.0`, `EF Core 9 + Pomelo.EntityFrameworkCore.MySql 9 (MySQL 8.0)`, `JwtBearer 10.0.7`, `BCrypt.Net-Next 4.1.0`, `Swashbuckle 10.1.7`

Архітектура: `Controllers -> Services (Scoped) -> AppDbContext`, `Middleware/ExceptionMiddleware`, сідери `DatabaseSeeder` + `TestDataSeeder` (тільки Dev).

## Швидкий старт

Вимоги: `.NET 10 SDK`, `MySQL 8.0`, `dotnet-ef` (опційно).

```bash
git clone <repo-url>
cd libraryAPI

# 1. БД
mysql -u root -p -e "CREATE DATABASE library_db CHARACTER SET utf8mb4;"

# 2. Конфігурація (НЕ кладіть секрети в appsettings.json)
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=127.0.0.1;Port=3306;Database=library_db;User=lib_user;Password=***"
dotnet user-secrets set "JwtSettings:Secret" "<мінімум 32 символи, 256 біт>"
dotnet user-secrets set "GroqSettings:ApiKey" "<gsk_...>"

# або через env:
# ConnectionStrings__DefaultConnection, JwtSettings__Secret, GroqSettings__ApiKey

# 3. Міграції
dotnet ef database update

# 4. Запуск
dotnet run
```

URLs (див. `Properties/launchSettings.json`): `https://localhost:7043`, `http://localhost:5043`. Swagger: `https://localhost:7043/swagger`. Фронт: `https://localhost:7043/` (`UseDefaultFiles + UseStaticFiles`).

### Конфіг `appsettings.json`

```json
{
  "ConnectionStrings": { "DefaultConnection": "Server=...;Database=library_db;User=...;Password=...;" },
  "JwtSettings": { "Secret": "...", "AccessTokenExpiryMinutes": 60, "RefreshTokenExpiryDays": 7 },
  "GroqSettings": { "ApiKey": "...", "Model": "llama-3.3-70b-versatile" }
}
```

## База даних (MySQL 8.0, EF Core, `Data/AppDbContext.cs`)

Всі таблиці `snake_case`. Створюються через `dotnet ef database update`. `DatabaseSeeder` сідить ролі + жанри.

| Таблиця | Поля | Зв'язки / нотатки |
|---|---|---|
| `roles` | `Id int PK AI`, `Name` | 1—* `users`. Сід: 1 admin, 2 customer, 3 librarian, 4 manager |
| `users` | `Id Guid PK`, `Name`, `Email unique`, `Password (BCrypt)`, `AvatarUrl?`, `RoleId=2 FK`, `IsActive`, `EquippedTitle?`, `CreatedAt/UpdatedAt` | `Role`, `Reviews`, `Favorites`, `WantToReads`, `RefreshTokens`, `Friends` |
| `refresh_tokens` | `Id int PK AI`, `UserId FK`, `Token`, `ExpiresAt`, `CreatedAt` | каскад від `users`, ротація при refresh, чистка прострочених |
| `genres` | `Id Guid PK`, `Name` | 1—* `books` (nullable). Сід: Фантастика, Детектив, Роман, Наукова література, Історія, Поезія |
| `books` | `Id Guid PK`, `Title`, `Author`, `Isbn unique`, `GenreId? FK`, `TotalCopies/AvailableCopies`, `PdfUrl?`, `ImageUrl?`, `Description?`, `Quote?`, `Pages?`, `Rating? decimal`, `GutenbergId?`, `SourceUrl?`, `IsActive`, `CreatedAt/UpdatedAt` | `Genre`, `Reviews`, `Favorites`, `WantToReads`. Видалення чистить пов'язані вручну в `BookService` |
| `reviews` | `Id Guid PK`, `BookId FK`, `UserId FK`, `Rating 1-5`, `Text? <=2000`, `CreatedAt/UpdatedAt` | unique `{BookId,UserId}` — 1 відгук на книгу, перераховує `books.Rating=AVG` |
| `favorites` | `UserId PK/FK`, `BookId PK/FK`, `AddedAt` | композитний PK, каскад з обох боків |
| `want_to_read` | `UserId PK/FK`, `BookId PK/FK`, `CreatedAt` | композитний PK, toggle-логіка |
| `reading_progress` | `Id Guid PK`, `UserId FK`, `BookId FK`, `CurrentPage`, `TotalPages`, `ProgressPercent`, `Status reading|finished`, `LastReadAt/CreatedAt/UpdatedAt` | unique `{UserId,BookId}`, `>=100% => finished`, пише в `daily_activity` |
| `daily_activity` | `Id Guid PK`, `UserId FK`, `Date (день)`, `PagesRead` | heatmap `activity?weeks=12`, добиваються нулі |
| `friends` | `Id Guid PK`, `UserId FK`, `FriendUserId FK`, `CreatedAt` | самопосилання на `users` 2x, зберігається в обидва боки при accept |
| `friend_requests` | `Id Guid PK`, `FromUserId FK`, `ToUserId FK`, `Status pending|accepted`, `CreatedAt/UpdatedAt` | reject/cancel = видалення рядка |

ER-зв'язки: `Role 1-* User 1-* (Review|Favorite|WantToRead|ReadingProgress|DailyActivity|RefreshToken) *-1 Book`; `Genre 1-* Book`; `User *-* User` через `friends` + `friend_requests`.

### Тестові акаунти (тільки Development, `TestDataSeeder`)

| Роль | Email | Пароль |
|---|---|---|
| admin | admin@library.com | admin123 |
| customer | ivan@example.com | password123 |
| customer | maria@example.com | password123 |
| librarian | librarian@library.com | librarian123 |

Сідиться: 4 ролі, 6 жанрів, 9 книг, прогрес Івана/Марії, `DailyActivity` за 84 дні.

## API (скорочено)

Auth: `Authorization: Bearer <accessToken>`

```
POST api/v1/auth/register | login | refresh
GET  api/v1/books?page&search&genreId&sortBy&sortOrder
GET  api/v1/books/{id} | GET {id}/pdf | GET {id}/text
POST api/v1/books (admin,manager) | PUT {id} | DELETE {id} | POST {id}/pdf
POST api/v1/books/{id}/chat [Authorize] | POST {id}/chat/stream (SSE)

GET  api/v1/favorites [Authorize] | POST {bookId} | DELETE {bookId} | GET check/{bookId}
GET  api/v1/want-to-read [Authorize] | GET {bookId} | POST {bookId} (toggle)

GET  api/v1/books/{bookId}/reviews
POST api/v1/books/{bookId}/reviews [Authorize] (create-or-update)
DELETE api/v1/books/{bookId}/reviews [Authorize]
GET  api/v1/reviews/my [Authorize]
DELETE api/v1/books/{bookId}/reviews/{reviewId} (admin,manager)

POST api/v1/reading-progress [Authorize] {bookId,currentPage,totalPages,status?}
GET  api/v1/reading-progress/my | book/{bookId} | recent?count=5 | activity?weeks=12

GET  api/v1/gamification/achievements | titles [Authorize]
GET  api/v1/friends | POST request | GET requests|requests/sent
POST api/v1/friends/requests/{id}/accept|reject | DELETE requests/{id} | DELETE {friendUserId}
GET  api/v1/friends/{id}/profile|favorites|reading | GET search?q= [Authorize]

GET  api/v1/users/me [Authorize] | PUT me/title
GET  api/v1/users (admin,manager) | PUT {id}/role (admin) | PUT {id}/toggle-active
GET  api/v1/genres
```

Повний перелік маршрутів і контрактів: [docs/API.md](docs/API.md). Деталі прогресу читання: `READING_PROGRESS.md`, Swagger у Development, приклади запитів: `libraryAPI.http`.

## Матеріали командної роботи

- [Проєктування бази даних](docs/database-design.md) — сутності, зв'язки, ключі та обмеження.
- [Tasks + Kanban](docs/tasks-kanban.md) — статуси пунктів модуля і подальші командні дії.
- [Git branch workflow](docs/git-workflow.md) — гілки, коміти, review та безпечна інтеграція.
- [Code review](docs/code-review.md) — результат перевірки feature-коміту і відкритий ризик міграції.
- Git-репозиторій містить `main`, `develop` і `feature/validation`. Поточний робочий контекст цього завдання — `feature/validation`; не пушити та не зливати її в `main`.

## Структура

```
Controllers/ Auth,Books,Favorites,WantToRead,Reviews,ReadingProgress,Gamification,Friends,Users,Genres
Services/ *Service + I*Service (Auth,Book,BookText,BookChat,Favorite,WantToRead,Review,ReadingProgress,Gamification,Friend,UserAdmin)
Models/ User,Role,RefreshToken,Genre,Book,Review,Favorite,ReadingProgress,WantToRead,DailyActivity,Friend,FriendRequest
DTOs/ CreateBook,UpdateBook,BookResponse,PagedResponse,Register,Login,RefreshTokenRequest,Review,ReadingProgress,Favorite,Achievement,Friend
Data/AppDbContext.cs (таблиці snake_case, композитні PK Favorite/WantToRead, unique Review/Progress)
Migrations/ Middleware/ExceptionMiddleware.cs
DatabaseSeeder.cs TestDataSeeder.cs UpdateBooksData.cs wwwroot/
```

## Корисні команди

```bash
dotnet ef migrations add <Name>
dotnet ef database update
dotnet build | dotnet watch run
```

## Нотатки безпеки

Перед продом: винести секрети з `appsettings.json` в secrets/env + ротувати, замінити `CORS AllowAnyOrigin` на `WithOrigins()`, ховати `ex.Message` в prod, додати `UseHttpsRedirection`, JWT `Issuer/Audience`, least-privilege юзера БД замість `root`.
