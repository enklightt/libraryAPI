using LibraryAPI.DTOs;

namespace LibraryAPI.Interfaces;

public interface IReadingProgressService
{
    Task<ReadingProgressDto> UpdateProgressAsync(string userId, UpdateReadingProgressDto dto);
    Task<IEnumerable<ReadingProgressDto>> GetUserProgressAsync(string userId);
    Task<ReadingProgressDto?> GetBookProgressAsync(string userId, string bookId);
    Task<IEnumerable<ReadingProgressDto>> GetRecentReadsAsync(string userId, int count = 5);
    Task<IEnumerable<object>> GetActivityDataAsync(string userId, int weeks = 12);
}
