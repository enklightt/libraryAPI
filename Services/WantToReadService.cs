using LibraryAPI.Data;
using LibraryAPI.DTOs;
using LibraryAPI.Interfaces;
using LibraryAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace LibraryAPI.Services;

public class WantToReadService : IWantToReadService
{
    private readonly AppDbContext _context;

    public WantToReadService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<BookResponseDto>> GetUserWantsAsync(string userId)
    {
        var books = await _context.Set<WantToRead>()
            .Where(w => w.UserId == userId)
            .Include(w => w.Book).ThenInclude(b => b.Genre)
            .OrderByDescending(w => w.CreatedAt)
            .Select(w => new BookResponseDto
            {
                Id = w.Book.Id,
                Title = w.Book.Title,
                Author = w.Book.Author,
                Isbn = w.Book.Isbn,
                ImageUrl = w.Book.ImageUrl,
                Description = w.Book.Description,
                Rating = w.Book.Rating,
                Pages = w.Book.Pages,
                Quote = w.Book.Quote,
                PdfUrl = w.Book.PdfUrl,
                GenreId = w.Book.GenreId,
                GenreName = w.Book.Genre!.Name,
                CreatedAt = w.Book.CreatedAt
            })
            .ToListAsync();

        return books;
    }

    public async Task<bool> IsWantedAsync(string userId, string bookId)
    {
        return await _context.Set<WantToRead>()
            .AnyAsync(w => w.UserId == userId && w.BookId == bookId);
    }

    public async Task<bool> ToggleAsync(string userId, string bookId)
    {
        var existing = await _context.Set<WantToRead>()
            .FirstOrDefaultAsync(w => w.UserId == userId && w.BookId == bookId);

        if (existing != null)
        {
            _context.Set<WantToRead>().Remove(existing);
            await _context.SaveChangesAsync();
            return false;
        }
        else
        {
            _context.Set<WantToRead>().Add(new WantToRead
            {
                UserId = userId,
                BookId = bookId
            });
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
