using LibraryAPI.Data;
using LibraryAPI.DTOs;
using LibraryAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace LibraryAPI.Services;

public class ReviewService : IReviewService
{
    private readonly AppDbContext _context;

    public ReviewService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ReviewResponseDto>> GetBookReviewsAsync(string bookId)
    {
        return await _context.Reviews
            .Include(r => r.User)
            .Where(r => r.BookId == bookId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new ReviewResponseDto
            {
                Id = r.Id,
                BookId = r.BookId,
                UserId = r.UserId,
                UserName = r.User.Name,
                Rating = r.Rating,
                Text = r.Text,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt
            })
            .ToListAsync();
    }

    public async Task<(bool Success, string? Error, ReviewResponseDto? Review)> CreateOrUpdateAsync(string bookId, string userId, CreateReviewDto dto)
    {
        var book = await _context.Books.FindAsync(bookId);
        if (book == null)
            return (false, "Книгу не знайдено", null);

        var existing = await _context.Reviews
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.BookId == bookId && r.UserId == userId);

        if (existing != null)
        {
            existing.Rating = dto.Rating;
            existing.Text = dto.Text;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            existing = new Review
            {
                BookId = bookId,
                UserId = userId,
                Rating = dto.Rating,
                Text = dto.Text
            };
            _context.Reviews.Add(existing);
        }

        await _context.SaveChangesAsync();
        await RecalculateBookRating(bookId);

        existing = await _context.Reviews.Include(r => r.User).FirstAsync(r => r.Id == existing.Id);

        return (true, null, new ReviewResponseDto
        {
            Id = existing.Id,
            BookId = existing.BookId,
            UserId = existing.UserId,
            UserName = existing.User.Name,
            Rating = existing.Rating,
            Text = existing.Text,
            CreatedAt = existing.CreatedAt,
            UpdatedAt = existing.UpdatedAt
        });
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(string bookId, string userId)
    {
        var review = await _context.Reviews
            .FirstOrDefaultAsync(r => r.BookId == bookId && r.UserId == userId);

        if (review == null)
            return (false, "Відгук не знайдено");

        _context.Reviews.Remove(review);
        await _context.SaveChangesAsync();
        await RecalculateBookRating(bookId);

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteByIdAsync(string bookId, string reviewId)
    {
        var review = await _context.Reviews
            .FirstOrDefaultAsync(r => r.BookId == bookId && r.Id == reviewId);
        if (review == null)
            return (false, "Відгук не знайдено");

        _context.Reviews.Remove(review);
        await _context.SaveChangesAsync();
        await RecalculateBookRating(bookId);

        return (true, null);
    }

    public async Task<IEnumerable<ReviewResponseDto>> GetUserReviewsAsync(string userId)
    {
        return await _context.Reviews
            .Include(r => r.Book)
            .Include(r => r.User)
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new ReviewResponseDto
            {
                Id = r.Id,
                BookId = r.BookId,
                BookTitle = r.Book.Title,
                BookImageUrl = r.Book.ImageUrl,
                UserId = r.UserId,
                UserName = r.User.Name,
                Rating = r.Rating,
                Text = r.Text,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt
            })
            .ToListAsync();
    }

    private async Task RecalculateBookRating(string bookId)
    {
        var avg = await _context.Reviews
            .Where(r => r.BookId == bookId)
            .AverageAsync(r => (decimal?)r.Rating);

        var book = await _context.Books.FindAsync(bookId);
        if (book != null)
        {
            book.Rating = avg.HasValue ? Math.Round(avg.Value, 1) : null;
            await _context.SaveChangesAsync();
        }
    }
}
