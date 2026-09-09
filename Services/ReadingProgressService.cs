using Microsoft.EntityFrameworkCore;
using LibraryAPI.Data;
using LibraryAPI.DTOs;
using LibraryAPI.Models;

namespace LibraryAPI.Services;

public class ReadingProgressService : IReadingProgressService
{
    private readonly AppDbContext _context;

    public ReadingProgressService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ReadingProgressDto> UpdateProgressAsync(string userId, UpdateReadingProgressDto dto)
    {
        var book = await _context.Books.FindAsync(dto.BookId);
        if (book == null)
            throw new KeyNotFoundException("Книга не знайдена");

        var progress = await _context.ReadingProgress
            .FirstOrDefaultAsync(rp => rp.UserId == userId && rp.BookId == dto.BookId);

        var oldPage = progress?.CurrentPage ?? 0;
        var calculatedProgress = CalculateProgress(dto.CurrentPage, dto.TotalPages);

        if (progress == null)
        {
            progress = new ReadingProgress
            {
                UserId = userId,
                BookId = dto.BookId,
                CurrentPage = dto.CurrentPage,
                TotalPages = dto.TotalPages,
                ProgressPercent = calculatedProgress,
                LastReadAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Status = dto.Status ?? "reading"
            };
            _context.ReadingProgress.Add(progress);
        }
        else
        {
            progress.CurrentPage = dto.CurrentPage;
            progress.TotalPages = dto.TotalPages;
            progress.ProgressPercent = calculatedProgress;
            progress.LastReadAt = DateTime.UtcNow;
            progress.UpdatedAt = DateTime.UtcNow;
            if (calculatedProgress >= 100)
                progress.Status = "finished";
            else if (dto.Status != null)
                progress.Status = dto.Status;
        }

        var pagesRead = dto.CurrentPage - oldPage;
        if (pagesRead > 0)
        {
            var today = DateTime.UtcNow.Date;
            var activity = await _context.DailyActivities
                .FirstOrDefaultAsync(a => a.UserId == userId && a.Date == today);
            if (activity == null)
            {
                _context.DailyActivities.Add(new DailyActivity
                {
                    UserId = userId,
                    Date = today,
                    PagesRead = pagesRead
                });
            }
            else
            {
                activity.PagesRead += pagesRead;
            }
        }

        await _context.SaveChangesAsync();

        return await GetProgressDto(progress.Id);
    }

    public async Task<IEnumerable<ReadingProgressDto>> GetUserProgressAsync(string userId)
    {
        var progressList = await _context.ReadingProgress
            .Include(rp => rp.Book)
            .Where(rp => rp.UserId == userId)
            .OrderByDescending(rp => rp.LastReadAt)
            .ToListAsync();

        return progressList.Select(MapToDto);
    }

    public async Task<ReadingProgressDto?> GetBookProgressAsync(string userId, string bookId)
    {
        var progress = await _context.ReadingProgress
            .Include(rp => rp.Book)
            .FirstOrDefaultAsync(rp => rp.UserId == userId && rp.BookId == bookId);

        return progress == null ? null : MapToDto(progress);
    }

    public async Task<IEnumerable<ReadingProgressDto>> GetRecentReadsAsync(string userId, int count = 5)
    {
        var progressList = await _context.ReadingProgress
            .Include(rp => rp.Book)
            .Where(rp => rp.UserId == userId && (rp.Status == "finished" || rp.ProgressPercent >= 100))
            .OrderByDescending(rp => rp.LastReadAt)
            .Take(count)
            .ToListAsync();

        return progressList.Select(MapToDto);
    }

    public async Task<IEnumerable<object>> GetActivityDataAsync(string userId, int weeks = 12)
    {
        var since = DateTime.UtcNow.Date.AddDays(-weeks * 7);
        var activity = await _context.DailyActivities
            .Where(a => a.UserId == userId && a.Date >= since)
            .OrderBy(a => a.Date)
            .ToListAsync();

        var allDays = new List<object>();
        for (var d = since; d <= DateTime.UtcNow.Date; d = d.AddDays(1))
        {
            var match = activity.FirstOrDefault(a => a.Date == d);
            allDays.Add(new { date = d, pages = match?.PagesRead ?? 0 });
        }

        return allDays;
    }

    private async Task<ReadingProgressDto> GetProgressDto(string progressId)
    {
        var progress = await _context.ReadingProgress
            .Include(rp => rp.Book)
            .FirstAsync(rp => rp.Id == progressId);

        return MapToDto(progress);
    }

    private static ReadingProgressDto MapToDto(ReadingProgress progress)
    {
        var status = progress.ProgressPercent >= 100 ? "finished" : progress.Status;
        if (string.IsNullOrEmpty(status))
            status = "reading";

        return new ReadingProgressDto
        {
            Id = progress.Id,
            UserId = progress.UserId,
            BookId = progress.BookId,
            BookTitle = progress.Book.Title,
            BookAuthor = progress.Book.Author,
            BookImageUrl = progress.Book.ImageUrl,
            BookIsbn = progress.Book.Isbn,
            BookRating = progress.Book.Rating,
            CurrentPage = progress.CurrentPage,
            TotalPages = progress.TotalPages,
            ProgressPercent = progress.ProgressPercent,
            LastReadAt = progress.LastReadAt,
            Status = status
        };
    }

    private static decimal CalculateProgress(int currentPage, int totalPages)
    {
        if (totalPages <= 0) return 0;
        var percent = (decimal)currentPage / totalPages * 100;
        return Math.Round(Math.Min(percent, 100), 2);
    }
}
