using LibraryAPI.DTOs;

namespace LibraryAPI.Services;

public interface IWantToReadService
{
    Task<IEnumerable<BookResponseDto>> GetUserWantsAsync(string userId);
    Task<bool> IsWantedAsync(string userId, string bookId);
    Task<bool> ToggleAsync(string userId, string bookId);
}
