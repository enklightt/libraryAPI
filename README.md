# LibraryAPI

## Призначення

LibraryAPI — це backend-застосунок для електронної бібліотеки, який надає REST API для управління книгами, жанрами, користувачами та пов'язаною діяльністю (рецензії, обране, список "хочу прочитати", прогрес читання). База: `api/v1/...` та `api/v2/books`, Swagger в Development.

Основні можливості:

- реєстрація та автентифікація користувачів (JWT, refresh-токени);
- CRUD-операції для книг та жанрів;
- завантаження, зберігання та видача PDF-файлів книг (локально або за зовнішнім посиланням);
- отримання тексту книги;
- чат з AI-асистентом про конкретну книгу (у тому числі у режимі стрімінгу);
- рецензії, оцінки, обране, список "хочу прочитати", прогрес читання.

## Структура проєкту

```
libraryAPI/
├── Controllers/          # API-контролери (обробка HTTP-запитів)
├── Models/                # Моделі даних (сутності бази даних)
├── DTOs/                  # Data Transfer Objects — моделі для запитів/відповідей API
├── Data/
│   └── AppDbContext.cs    # Контекст бази даних (EF Core)
├── Services/               # Бізнес-логіка (сервісний шар)
├── Middleware/             # Проміжне ПЗ (обробка помилок, автентифікація тощо)
├── Migrations/             # Міграції бази даних (EF Core)
├── Properties/
│   └── launchSettings.json # Налаштування запуску проєкту
├── wwwroot/                 # Статичні файли, у т.ч. завантажені PDF книг
├── DatabaseSeeder.cs        # Наповнення бази початковими даними
├── TestDataSeeder.cs        # Наповнення бази тестовими даними
├── UpdateBooksData.cs       # Допоміжний скрипт оновлення даних книг
├── add_books.sql            # SQL-скрипт для додавання книг
├── appsettings.json          # Базова конфігурація (без секретів)
├── appsettings.Development.json # Конфігурація для середовища розробки
├── Program.cs                # Точка входу, конфігурація застосунку та DI
└── libraryAPI.csproj         # Файл проєкту (.NET SDK, залежності)
```

## Технології

- **.NET / ASP.NET Core** — фреймворк для побудови веб-API
- **Entity Framework Core** — ORM для роботи з базою даних
- **Pomelo.EntityFrameworkCore.MySql** — провайдер EF Core для MySQL
- **MySQL** — реляційна база даних
- **JWT (JSON Web Tokens)** — автентифікація та авторизація
- **BCrypt.Net** — хешування паролів
- **Swashbuckle (Swagger)** — документація та тестування API
- **Groq API** — інтеграція з мовною моделлю для чату про книги

## Залежності

Основні NuGet-пакети (див. `libraryAPI.csproj`):

| Пакет | Призначення |
|---|---|
| `BCrypt.Net-Next` | Хешування та перевірка паролів |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | Автентифікація через JWT |
| `Microsoft.EntityFrameworkCore` | ORM для роботи з базою даних |
| `Microsoft.EntityFrameworkCore.Tools` | Інструменти для міграцій EF Core |
| `Pomelo.EntityFrameworkCore.MySql` | Провайдер EF Core для MySQL |
| `Swashbuckle.AspNetCore` | Генерація Swagger-документації |

## Запуск проєкту

### Попередні вимоги

- Встановлений .NET SDK відповідної версії (див. `TargetFramework` у `libraryAPI.csproj`)
- Встановлений та запущений сервер MySQL (перевірити активну службу можна командою `Get-Service -Name "MySQL*"` у PowerShell)
- Клієнт MySQL для виконання SQL-команд (консольний `mysql`, або графічний інструмент, наприклад MySQL Workbench)

### 1. Клонування репозиторію

```bash
git clone <посилання-на-репозиторій>
cd libraryAPI
```

### 2. Створення бази даних та користувача MySQL

Підключіться до MySQL під root-користувачем:

```bash
mysql -u root -p
```

У консолі MySQL виконайте:

```sql
CREATE DATABASE library_db CHARACTER SET utf8mb4;
CREATE USER 'lib_user'@'localhost' IDENTIFIED BY 'ваш_пароль';
GRANT ALL PRIVILEGES ON library_db.* TO 'lib_user'@'localhost';
FLUSH PRIVILEGES;
```

### 3. Налаштування конфігурації (.env)

Секрети зберігаються лише в `.env` (файл у `.gitignore`) або в env-змінних:

```bash
cp .env.example .env
# відредагуйте .env: ConnectionStrings__DefaultConnection,
# JwtSettings__Secret (мінімум 32 символи), GroqSettings__ApiKey
```

.env автоматично завантажується через DotNetEnv.
Альтернатива для CI/prod — змінні середовища ASP.NET Core:
ConnectionStrings__DefaultConnection, JwtSettings__Secret, GroqSettings__ApiKey

Перевірити збережені значення можна командою:

```bash
dotnet user-secrets list
```

### 4. Застосування міграцій бази даних

```bash
dotnet ef database update
```

Ця команда створить усі необхідні таблиці в базі `library_db` відповідно до поточних міграцій проєкту.

### 5. Запуск проєкту

```bash
dotnet run
```

Після запуску API буде доступне за адресою, вказаною в `Properties/launchSettings.json` (`applicationUrl`). Swagger-документація доступна за замовчуванням за адресою `/swagger`.

### Типові проблеми при запуску

- **`Access denied for user`** — невірний пароль або відсутні права користувача MySQL; перевірити командою `SHOW GRANTS FOR 'lib_user'@'localhost';` у консолі MySQL.
- **`mysql: The term 'mysql' is not recognized`** — консольний клієнт MySQL не доданий у PATH; викликати через повний шлях до `mysql.exe` або скористатися MySQL Workbench.
- **`Unable to create a 'DbContext'... Format of the initialization string does not conform to specification`** — помилка у форматі рядка підключення; перевірити `.env` файл.

URLs (див. `Properties/launchSettings.json`): `https://localhost:7043`, `http://localhost:5043`. Swagger: `https://localhost:7043/swagger`. Фронт: `https://localhost:7043/` (`UseDefaultFiles + UseStaticFiles`).

### Конфіг

Секрети зберігаються лише в `.env` (файл у `.gitignore`) або в env-змінних.
`appsettings.json` містить безпечні defaults: CORS origins, TTL кешу, ліміти
rate limiting, параметри resilience.

```bash
# .env (приклад)
ConnectionStrings__DefaultConnection=Server=127.0.0.1;Port=3306;Database=library_db;User=lib_user;Password=***
JwtSettings__Secret=<random 32+ chars>
GroqSettings__ApiKey=gsk_***
```

> **Безпека:** якщо секрети колись потрапляли в Git-історію — змініть пароль
> БД, JWT secret і ключ Groq. Історію можна почистити `git filter-repo`, але це
> окрема операція з командою.

## Module 3 — якість, продуктивність, безпека

| Задача | Стан | Де дивитися |
|---|---|---|
| SonarQube | ✅ 0 bugs, 0 vulnerabilities, 0 debt, coverage 34.4% | `sonarqube-report.md`, `sonar-project.properties` |
| Рефакторинг | ✅ сервіси/DTO/константи/exception middleware | `refactoring-report.md` |
| Performance Testing | 🤝 тіммейт | `TEAMMATE-TASKS.md` |
| Profiling | 🤝 тіммейт | `TEAMMATE-TASKS.md` |
| Оптимізація БД | 🤝 тіммейт | `TEAMMATE-TASKS.md` |
| Reliability | ✅ timeout/retry/circuit breaker, `/health`, безпечні помилки | `Infrastructure/HttpClientExtensions.cs`, `Middleware/ExceptionMiddleware.cs` |
| Захист API | ✅ access control для друзів, path traversal guard, CORS allow-list, валідація DTO | `Services/FriendService.cs`, `Services/BookPdfService.cs` |
| API Versioning | ✅ `api/v1`, `api/v2/books`, `api-supported-versions`, Swagger v1/v2 | `Controllers/BooksV2Controller.cs`, `Program.cs` |
| Rate Limiting | ✅ 10/хв auth, 60/хв write, `Retry-After` | `Program.cs`, `Infrastructure/RateLimitKeyProvider.cs` |
| Caching | ✅ IMemoryCache + `X-Cache`, TTL, інвалідація | `Services/BookService.cs`, `Services/GenreService.cs` |
| `.env` | ✅ DotNetEnv, `.env.example`, секрети прибрано з appsettings | `.env.example`, `Program.cs` |

### Корисні команди

```bash
dotnet build
dotnet test

# покриття + Sonar
dotnet test --settings coverlet.runsettings \
  --collect:"XPlat Code Coverage" --results-directory TestResults
dotnet-sonarscanner begin /k:"libraryAPI" \
  /d:sonar.host.url="http://localhost:9000" /d:sonar.token="$SONAR_TOKEN" \
  /d:sonar.cs.opencover.reportsPaths="TestResults/**/coverage.opencover.xml"
dotnet build && dotnet test --no-build
dotnet-sonarscanner end /d:sonar.token="$SONAR_TOKEN"
```

Rate limiting вимкнений у середовищі `Testing`, тому інтеграційні тести не
залежать від лімітів. У Development ліміти активні.

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

Деталі: `READING_PROGRESS.md`, Swagger, `libraryAPI.http`.

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
