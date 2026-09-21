using LibraryAPI.Data;
using LibraryAPI.DTOs;
using LibraryAPI.Interfaces;
using LibraryAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace LibraryAPI.Services;

public class FavoriteService : IFavoriteService
{
    private readonly AppDbContext _context;
    private readonly ILogger<FavoriteService> _logger;

    public FavoriteService(AppDbContext context, ILogger<FavoriteService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<FavoriteBookDto>> GetUserFavoritesAsync(string userId)
    {
        return await _context.Favorites
            .Where(f => f.UserId == userId)
            .Include(f => f.Book)
            .Select(f => new FavoriteBookDto
            {
                Id = f.Book.Id,
                Title = f.Book.Title,
                Author = f.Book.Author,
                Isbn = f.Book.Isbn,
                GenreId = f.Book.GenreId,
                TotalCopies = f.Book.TotalCopies,
                AvailableCopies = f.Book.AvailableCopies,
                PdfUrl = f.Book.PdfUrl,
                Rating = f.Book.Rating,
                IsActive = f.Book.IsActive,
                AddedAt = f.AddedAt
            })
            .ToListAsync();
    }

    public async Task<(bool Success, string? Error)> AddToFavoritesAsync(string userId, string bookId)
    {
        _logger.LogInformation("Спроба додати книгу {BookId} до улюблених користувача {UserId}", bookId, userId);

        var bookExists = await _context.Books.AnyAsync(b => b.Id == bookId && b.IsActive);
        if (!bookExists)
        {
            _logger.LogWarning("Книгу {BookId} не знайдено або вона неактивна", bookId);
            return (false, "Книгу не знайдено");
        }

        var alreadyFavorite = await _context.Favorites
            .AnyAsync(f => f.UserId == userId && f.BookId == bookId);

        if (alreadyFavorite)
        {
            _logger.LogWarning("Книга {BookId} вже у улюблених користувача {UserId}", bookId, userId);
            return (false, "Книга вже у улюблених");
        }

        var favorite = new Favorite
        {
            UserId = userId,
            BookId = bookId,
            AddedAt = DateTime.UtcNow
        };

        _context.Favorites.Add(favorite);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Книгу {BookId} додано до улюблених користувача {UserId}", bookId, userId);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> RemoveFromFavoritesAsync(string userId, string bookId)
    {
        _logger.LogInformation("Спроба видалити книгу {BookId} з улюблених користувача {UserId}", bookId, userId);

        var favorite = await _context.Favorites
            .FirstOrDefaultAsync(f => f.UserId == userId && f.BookId == bookId);

        if (favorite == null)
        {
            _logger.LogWarning("Книга {BookId} не знайдена у улюблених користувача {UserId}", bookId, userId);
            return (false, "Книга не знайдена у улюблених");
        }

        _context.Favorites.Remove(favorite);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Книгу {BookId} видалено з улюблених користувача {UserId}", bookId, userId);
        return (true, null);
    }

    public async Task<bool> IsFavoriteAsync(string userId, string bookId)
    {
        return await _context.Favorites
            .AnyAsync(f => f.UserId == userId && f.BookId == bookId);
    }
}
