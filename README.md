# LibraryAPI

## Призначення

LibraryAPI — це backend-застосунок для електронної бібліотеки, який надає REST API для управління книгами, жанрами, користувачами та пов'язаною діяльністю (рецензії, обране, список "хочу прочитати", прогрес читання). Застосунок також підтримує роботу з PDF-файлами книг та інтеграцію з AI-сервісом для чату щодо змісту книги.

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

Підключіться до MySQL під root-користувачем (за потреби — вказавши повний шлях до `mysql.exe`, якщо він не доданий у PATH):

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

### 3. Налаштування конфігурації (User Secrets)

Секрети (рядок підключення до бази, ключі JWT та Groq) не зберігаються в `appsettings.json`, а передаються через User Secrets:

```bash
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=127.0.0.1;Port=3306;Database=library_db;User=lib_user;Password=ваш_пароль;"
dotnet user-secrets set "JwtSettings:Secret" "<мінімум 32 символи>"
dotnet user-secrets set "GroqSettings:ApiKey" "<gsk_...>"
```

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
- **`Unable to create a 'DbContext'... Format of the initialization string does not conform to specification`** — помилка у форматі рядка підключення в User Secrets (частіше через спецсимволи в паролі); перевірити командою `dotnet user-secrets list` та за потреби задати простіший пароль.