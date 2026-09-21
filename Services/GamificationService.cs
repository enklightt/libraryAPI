using LibraryAPI.Data;
using LibraryAPI.DTOs;
using LibraryAPI.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LibraryAPI.Services;

public class GamificationService : IGamificationService
{
    private readonly AppDbContext _context;

    public GamificationService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<AchievementDto>> GetUserAchievementsAsync(string userId)
    {
        var progress = await _context.ReadingProgress
            .Where(rp => rp.UserId == userId)
            .ToListAsync();

        var completedBooks = progress
            .Where(rp => rp.ProgressPercent >= 100)
            .ToList();

        var completedCount = completedBooks.Count;
        var favoriteCount = await _context.Favorites.CountAsync(f => f.UserId == userId);
        var hasNightReading = progress.Any(rp => rp.LastReadAt.Hour >= 22 || rp.LastReadAt.Hour < 6);
        var latestCompletedAt = completedBooks
            .OrderByDescending(rp => rp.UpdatedAt)
            .Select(rp => (DateTime?)rp.UpdatedAt)
            .FirstOrDefault();

        return new[]
        {
            CreateAchievement(
                code: "first_book",
                title: "Перший фініш",
                description: "Прочитати першу книгу до кінця",
                badge: "first-book",
                progress: completedCount,
                target: 1,
                unlockedAt: latestCompletedAt),

            CreateAchievement(
                code: "read_5_books",
                title: "Прочитав 5 книг",
                description: "Завершити читання 5 книг",
                badge: "five-books",
                progress: completedCount,
                target: 5,
                unlockedAt: latestCompletedAt),

            CreateAchievement(
                code: "bookworm",
                title: "Книгоман",
                description: "Завершити читання 10 книг",
                badge: "bookworm",
                progress: completedCount,
                target: 10,
                unlockedAt: latestCompletedAt),

            CreateAchievement(
                code: "night_reader",
                title: "Нічний читач",
                description: "Оновити прогрес читання з 22:00 до 06:00",
                badge: "night-reader",
                progress: hasNightReading ? 1 : 0,
                target: 1,
                unlockedAt: progress
                    .Where(rp => rp.LastReadAt.Hour >= 22 || rp.LastReadAt.Hour < 6)
                    .OrderByDescending(rp => rp.LastReadAt)
                    .Select(rp => (DateTime?)rp.LastReadAt)
                    .FirstOrDefault()),

            CreateAchievement(
                code: "favorite_collector",
                title: "Колекціонер",
                description: "Додати 5 книг в улюблені",
                badge: "favorite-collector",
                progress: favoriteCount,
                target: 5,
                unlockedAt: null),

        };
    }

    private static AchievementDto CreateAchievement(
        string code,
        string title,
        string description,
        string badge,
        int progress,
        int target,
        DateTime? unlockedAt)
    {
        var cappedProgress = Math.Min(progress, target);
        var isUnlocked = cappedProgress >= target;

        return new AchievementDto
        {
            Code = code,
            Title = title,
            Description = description,
            Badge = badge,
            IsUnlocked = isUnlocked,
            Progress = cappedProgress,
            Target = target,
            UnlockedAt = isUnlocked ? unlockedAt : null
        };
    }
}
