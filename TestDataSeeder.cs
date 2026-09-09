using Microsoft.EntityFrameworkCore;
using LibraryAPI.Data;
using LibraryAPI.Models;

namespace LibraryAPI;

public static class TestDataSeeder
{
    public static async Task SeedTestDataAsync(AppDbContext context)
    {
        Console.WriteLine("\n📚 Додавання тестових даних...\n");

        // Отримати жанри
        var genres = await context.Genres.ToListAsync();
        var fantasyGenre = genres.FirstOrDefault(g => g.Name == "Фантастика");
        var novelGenre = genres.FirstOrDefault(g => g.Name == "Роман");
        var poetryGenre = genres.FirstOrDefault(g => g.Name == "Поезія");
        var scienceGenre = genres.FirstOrDefault(g => g.Name == "Наукова література");
        var detectiveGenre = genres.FirstOrDefault(g => g.Name == "Детектив");

        // Тестові користувачі
        if (!await context.Users.AnyAsync())
        {
            var users = new[]
            {
                new User
                {
                    Name = "Admin User",
                    Email = "admin@library.com",
                    Password = BCrypt.Net.BCrypt.HashPassword("admin123"),
                    RoleId = 1, // admin
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new User
                {
                    Name = "Іван Петренко",
                    Email = "ivan@example.com",
                    Password = BCrypt.Net.BCrypt.HashPassword("password123"),
                    RoleId = 2, // customer
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new User
                {
                    Name = "Марія Коваленко",
                    Email = "maria@example.com",
                    Password = BCrypt.Net.BCrypt.HashPassword("password123"),
                    RoleId = 2, // customer
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new User
                {
                    Name = "Бібліотекар",
                    Email = "librarian@library.com",
                    Password = BCrypt.Net.BCrypt.HashPassword("librarian123"),
                    RoleId = 3, // librarian
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            context.Users.AddRange(users);
            await context.SaveChangesAsync();
            Console.WriteLine("✓ Додано 4 тестових користувачів");
        }

        // Книги
        if (!await context.Books.AnyAsync())
        {
            var books = new[]
            {
                new Book
                {
                    Title = "1984",
                    Author = "George Orwell",
                    Isbn = "978-0-452-28423-4",
                    GenreId = fantasyGenre?.Id,
                    TotalCopies = 5,
                    AvailableCopies = 5,
                    PdfUrl = "/books/1984.pdf",
                    Quote = "War is peace. Freedom is slavery. Ignorance is strength.",
                    Pages = 328,
                    Rating = 4.7m,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Book
                {
                    Title = "Animal Farm",
                    Author = "George Orwell",
                    Isbn = "978-0-452-28424-1",
                    GenreId = fantasyGenre?.Id,
                    TotalCopies = 3,
                    AvailableCopies = 3,
                    PdfUrl = "/books/animal-farm.pdf",
                    Quote = "All animals are equal, but some animals are more equal than others.",
                    Pages = 112,
                    Rating = 4.5m,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Book
                {
                    Title = "Кобзар",
                    Author = "Тарас Шевченко",
                    Isbn = "978-966-03-4567-8",
                    GenreId = poetryGenre?.Id,
                    TotalCopies = 10,
                    AvailableCopies = 10,
                    PdfUrl = "/books/kobzar.pdf",
                    Quote = "Борітеся — поборете, вам Бог помагає! За вас правда, за вас слава і воля святая!",
                    Pages = 256,
                    Rating = 5.0m,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Book
                {
                    Title = "Pride and Prejudice",
                    Author = "Jane Austen",
                    Isbn = "978-0-141-43951-8",
                    GenreId = novelGenre?.Id,
                    TotalCopies = 4,
                    AvailableCopies = 4,
                    PdfUrl = "/books/pride-and-prejudice.pdf",
                    Quote = "It is a truth universally acknowledged, that a single man in possession of a good fortune, must be in want of a wife.",
                    Pages = 384,
                    Rating = 4.8m,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Book
                {
                    Title = "The Prince",
                    Author = "Niccolò Machiavelli",
                    Isbn = "978-0-140-44915-0",
                    GenreId = scienceGenre?.Id,
                    TotalCopies = 2,
                    AvailableCopies = 2,
                    PdfUrl = "/books/prince.pdf",
                    Quote = "It is better to be feared than loved, if you cannot be both.",
                    Pages = 140,
                    Rating = 4.3m,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Book
                {
                    Title = "Вбивство у Східному експресі",
                    Author = "Агата Крісті",
                    Isbn = "978-0-00-711931-8",
                    GenreId = detectiveGenre?.Id,
                    TotalCopies = 4,
                    AvailableCopies = 4,
                    ImageUrl = "https://covers.openlibrary.org/b/isbn/9780007119318-L.jpg",
                    Quote = "Потяг — це ідеальне місце для злочину: замкнений простір, безліч підозрюваних, жодного шляху для втечі.",
                    Pages = 256,
                    Rating = 4.7m,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Book
                {
                    Title = "Дівчина з татуюванням дракона",
                    Author = "Стіг Ларссон",
                    Isbn = "978-0-307-45454-6",
                    GenreId = detectiveGenre?.Id,
                    TotalCopies = 4,
                    AvailableCopies = 4,
                    ImageUrl = "https://covers.openlibrary.org/b/isbn/9780307454546-L.jpg",
                    Quote = "Правда — це рідкісний товар, і її ніколи не буває забагато.",
                    Pages = 672,
                    Rating = 4.5m,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Book
                {
                    Title = "Маруся Чурай",
                    Author = "Ліна Костенко",
                    Isbn = "978-966-03-4183-0",
                    GenreId = poetryGenre?.Id,
                    TotalCopies = 5,
                    AvailableCopies = 5,
                    ImageUrl = "https://covers.openlibrary.org/b/id/12648987-L.jpg",
                    Quote = "Життя людське — як той ручай, тече собі, біжить у вічність.",
                    Pages = 176,
                    Rating = 4.9m,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Book
                {
                    Title = "Палімпсести",
                    Author = "Василь Стус",
                    Isbn = "978-966-03-3816-8",
                    GenreId = poetryGenre?.Id,
                    TotalCopies = 5,
                    AvailableCopies = 5,
                    ImageUrl = "https://upload.wikimedia.org/wikipedia/uk/thumb/c/c8/%D0%9E%D0%B1%D0%BA%D0%BB%D0%B0%D0%B4%D0%B8%D0%BD%D0%BA%D0%B0_%D0%B5%D0%BB%D0%B5%D0%BA%D1%82%D1%80%D0%BE%D0%BD%D0%BD%D0%BE%D1%97_%D0%BA%D0%BD%D0%B8%D0%B3%D0%B8_%22%D0%9F%D0%B0%D0%BB%D1%96%D0%BC%D0%BF%D1%81%D0%B5%D1%81%D1%82%D0%B8%22.jpg/330px-%D0%9E%D0%B1%D0%BA%D0%BB%D0%B0%D0%B4%D0%B8%D0%BD%D0%BA%D0%B0_%D0%B5%D0%BB%D0%B5%D0%BA%D1%82%D1%80%D0%BE%D0%BD%D0%BD%D0%BE%D1%97_%D0%BA%D0%BD%D0%B8%D0%B3%D0%B8_%22%D0%9F%D0%B0%D0%BB%D1%96%D0%BC%D0%BF%D1%81%D0%B5%D1%81%D1%82%D0%B8%22.jpg",
                    Quote = "Нація, що не має поетів, приречена на забуття.",
                    Pages = 240,
                    Rating = 4.8m,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            context.Books.AddRange(books);
            await context.SaveChangesAsync();
            Console.WriteLine("✓ Додано 5 книг з PDF файлами");
        }

        // Тестовий прогрес читання
        if (!await context.ReadingProgress.AnyAsync())
        {
            var users = await context.Users.Where(u => u.RoleId == 2).ToListAsync();
            var books = await context.Books.ToListAsync();

            if (users.Any() && books.Any())
            {
                var progressList = new List<ReadingProgress>();

                // Іван читає "1984" (45%)
                if (books.Any(b => b.Title == "1984") && users.Any(u => u.Email == "ivan@example.com"))
                {
                    var book = books.First(b => b.Title == "1984");
                    var user = users.First(u => u.Email == "ivan@example.com");
                    progressList.Add(new ReadingProgress
                    {
                        UserId = user.Id,
                        BookId = book.Id,
                        CurrentPage = 147,
                        TotalPages = 328,
                        ProgressPercent = 44.82m,
                        LastReadAt = DateTime.UtcNow.AddHours(-2),
                        CreatedAt = DateTime.UtcNow.AddDays(-3),
                        UpdatedAt = DateTime.UtcNow.AddHours(-2)
                    });
                }

                // Іван дочитав "Animal Farm" (100%)
                if (books.Any(b => b.Title == "Animal Farm") && users.Any(u => u.Email == "ivan@example.com"))
                {
                    var book = books.First(b => b.Title == "Animal Farm");
                    var user = users.First(u => u.Email == "ivan@example.com");
                    progressList.Add(new ReadingProgress
                    {
                        UserId = user.Id,
                        BookId = book.Id,
                        CurrentPage = 112,
                        TotalPages = 112,
                        ProgressPercent = 100.00m,
                        Status = "finished",
                        LastReadAt = DateTime.UtcNow.AddDays(-1),
                        CreatedAt = DateTime.UtcNow.AddDays(-7),
                        UpdatedAt = DateTime.UtcNow.AddDays(-1)
                    });
                }

                // Марія читає "Pride and Prejudice" (23%)
                if (books.Any(b => b.Title == "Pride and Prejudice") && users.Any(u => u.Email == "maria@example.com"))
                {
                    var book = books.First(b => b.Title == "Pride and Prejudice");
                    var user = users.First(u => u.Email == "maria@example.com");
                    progressList.Add(new ReadingProgress
                    {
                        UserId = user.Id,
                        BookId = book.Id,
                        CurrentPage = 89,
                        TotalPages = 384,
                        ProgressPercent = 23.18m,
                        LastReadAt = DateTime.UtcNow.AddMinutes(-30),
                        CreatedAt = DateTime.UtcNow.AddDays(-2),
                        UpdatedAt = DateTime.UtcNow.AddMinutes(-30)
                    });
                }

                context.ReadingProgress.AddRange(progressList);
                await context.SaveChangesAsync();
                Console.WriteLine($"✓ Додано {progressList.Count} записів прогресу читання");
            }
        }

        // Денна активність читання (останні 12 тижнів)
        if (!await context.DailyActivities.AnyAsync())
        {
            var users = await context.Users.Where(u => u.RoleId == 2).ToListAsync();
            var rng = new Random(42);
            var today = DateTime.UtcNow.Date;
            var allActivity = new List<DailyActivity>();

            foreach (var user in users)
            {
                for (var d = today.AddDays(-83); d <= today; d = d.AddDays(1))
                {
                    // More realistic: some days 0, most days 5-40 pages, occasional spikes
                    var pages = rng.Next(0, 8) switch
                    {
                        0 => 0,
                        1 => 0,
                        2 => rng.Next(5, 15),
                        3 => rng.Next(10, 25),
                        4 => rng.Next(15, 35),
                        5 => rng.Next(20, 50),
                        6 => rng.Next(30, 70),
                        7 => rng.Next(50, 100),
                        _ => 0
                    };
                    if (pages > 0)
                    {
                        allActivity.Add(new DailyActivity
                        {
                            UserId = user.Id,
                            Date = d,
                            PagesRead = pages
                        });
                    }
                }
            }

            context.DailyActivities.AddRange(allActivity);
            await context.SaveChangesAsync();
            Console.WriteLine($"✓ Додано {allActivity.Count} записів денної активності");
        }

        Console.WriteLine("\n✅ Тестові дані успішно додані!\n");
        Console.WriteLine("📊 Фінальна статистика:");
        Console.WriteLine($"  Ролей: {await context.Roles.CountAsync()}");
        Console.WriteLine($"  Жанрів: {await context.Genres.CountAsync()}");
        Console.WriteLine($"  Користувачів: {await context.Users.CountAsync()}");
        Console.WriteLine($"  Книг: {await context.Books.CountAsync()}");
        Console.WriteLine($"  Прогресу читання: {await context.ReadingProgress.CountAsync()}");
        Console.WriteLine("\n🔐 Тестові облікові записи:");
        Console.WriteLine("  Admin: admin@library.com / admin123");
        Console.WriteLine("  Користувач 1: ivan@example.com / password123 (читає 2 книги)");
        Console.WriteLine("  Користувач 2: maria@example.com / password123 (читає 1 книгу)");
        Console.WriteLine("  Бібліотекар: librarian@library.com / librarian123");
    }
}
