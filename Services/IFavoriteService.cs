using LibraryAPI.DTOs;

namespace LibraryAPI.Services;

public interface IFavoriteService
{
    Task<IEnumerable<FavoriteBookDto>> GetUserFavoritesAsync(string userId);
    Task<(bool Success, string? Error)> AddToFavoritesAsync(string userId, string bookId);
    Task<(bool Success, string? Error)> RemoveFromFavoritesAsync(string userId, string bookId);
    Task<bool> IsFavoriteAsync(string userId, string bookId);
}
