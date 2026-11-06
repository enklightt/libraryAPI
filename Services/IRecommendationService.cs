using LibraryAPI.DTOs;

namespace LibraryAPI.Services;

public interface IRecommendationService
{
    Task<PagedResponse<RecommendationDto>> GetRecommendationsAsync(string? userId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<PagedResponse<RecommendationDto>?> GetSimilarBooksAsync(string bookId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<RecommendationDto?> ExplainAsync(string userId, string bookId, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error)> SaveFeedbackAsync(string userId, string bookId, bool isPositive, CancellationToken cancellationToken = default);
    Task WarmCacheAsync(string userId, CancellationToken cancellationToken = default);
    void InvalidateUserCache(string userId);
}
