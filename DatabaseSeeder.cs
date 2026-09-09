using Microsoft.EntityFrameworkCore;
using LibraryAPI.Data;
using LibraryAPI.Models;

namespace LibraryAPI;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        Console.WriteLine("📝 Перевірка початкових даних...");

        // Ролі
        if (!await context.Roles.AnyAsync())
        {
            var roles = new[]
            {
                new Role { Id = 1, Name = "admin" },
                new Role { Id = 2, Name = "customer" },
                new Role { Id = 3, Name = "librarian" },
                new Role { Id = 4, Name = "manager" }
            };

            context.Roles.AddRange(roles);
            await context.SaveChangesAsync();
            Console.WriteLine("✓ Додано 4 ролі");
        }

        // Жанри
        if (!await context.Genres.AnyAsync())
        {
            var genres = new[]
            {
                new Genre { Id = Guid.NewGuid().ToString(), Name = "Фантастика" },
                new Genre { Id = Guid.NewGuid().ToString(), Name = "Детектив" },
                new Genre { Id = Guid.NewGuid().ToString(), Name = "Роман" },
                new Genre { Id = Guid.NewGuid().ToString(), Name = "Наукова література" },
                new Genre { Id = Guid.NewGuid().ToString(), Name = "Історія" },
                new Genre { Id = Guid.NewGuid().ToString(), Name = "Поезія" }
            };

            context.Genres.AddRange(genres);
            await context.SaveChangesAsync();
            Console.WriteLine("✓ Додано 6 жанрів");
        }

        Console.WriteLine("✅ БД готова до роботи!\n");
    }
}
