using LibraryAPI.DTOs;

namespace LibraryAPI.Services;

public interface IReviewService
{
    Task<IEnumerable<ReviewResponseDto>> GetBookReviewsAsync(string bookId);
    Task<(bool Success, string? Error, ReviewResponseDto? Review)> CreateOrUpdateAsync(string bookId, string userId, CreateReviewDto dto);
    Task<(bool Success, string? Error)> DeleteAsync(string bookId, string userId);
    Task<(bool Success, string? Error)> DeleteByIdAsync(string bookId, string reviewId);
    Task<IEnumerable<ReviewResponseDto>> GetUserReviewsAsync(string userId);
}
