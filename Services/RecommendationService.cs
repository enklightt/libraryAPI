using LibraryAPI.Data;
using LibraryAPI.DTOs;
using LibraryAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace LibraryAPI.Services;

public class RecommendationService : IRecommendationService
{
    private const int CandidateLimit = 500;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);
    private readonly AppDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly ILogger<RecommendationService> _logger;

    public RecommendationService(AppDbContext context, IMemoryCache cache, ILogger<RecommendationService> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    public async Task<PagedResponse<RecommendationDto>> GetRecommendationsAsync(
        string? userId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 50);
        var cacheKey = $"recommendations:{userId ?? "anonymous"}";

        var cached = await _cache.GetOrCreateAsync(cacheKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            return BuildRecommendationsAsync(userId, cancellationToken);
        }) ?? new RecommendationCacheEntry([], 0);

        return ToPagedResponse(cached.Items, cached.TotalCount, page, pageSize);
    }

    public async Task<PagedResponse<RecommendationDto>?> GetSimilarBooksAsync(
        string bookId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 50);
        var source = await _context.Books.AsNoTracking()
            .Where(book => book.Id == bookId && book.IsActive)
            .Select(book => new { book.Id, book.GenreId, book.Genre!.Name, book.Author })
            .FirstOrDefaultAsync(cancellationToken);

        if (source == null)
            return null;

        var similarQuery = _context.Books.AsNoTracking()
                .Where(book => book.IsActive && book.Id != source.Id &&
                (book.GenreId == source.GenreId || book.Author == source.Author));
        var totalCount = await similarQuery.CountAsync(cancellationToken);
        var candidates = await ProjectBooks(similarQuery)
            .OrderByDescending(book => book.GenreId == source.GenreId)
            .ThenByDescending(book => book.Rating ?? 0)
            .ThenByDescending(book => book.ReviewCount)
            .Take(CandidateLimit)
            .ToListAsync(cancellationToken);

        var result = candidates.Select(book => new RecommendationDto
        {
            Book = ToBookDto(book),
            Score = Math.Round((book.GenreId == source.GenreId ? 0.75m : 0.35m) +
                Math.Min((book.Rating ?? 0) / 5m * 0.25m, 0.25m), 2),
            Reason = book.GenreId == source.GenreId
                ? $"Такий самий жанр: {source.Name ?? "цей жанр"}"
                : $"Схожий автор: {source.Author}",
            MatchedGenres = book.GenreId == source.GenreId && source.Name != null ? [source.Name] : []
        }).ToList();
        return ToPagedResponse(result, Math.Min(totalCount, CandidateLimit), page, pageSize);
    }

    public async Task<RecommendationDto?> ExplainAsync(string userId, string bookId, CancellationToken cancellationToken = default)
    {
        var recommendations = await GetRecommendationsAsync(userId, 1, CandidateLimit, cancellationToken);
        return recommendations.Items.FirstOrDefault(item => item.Book.Id == bookId);
    }

    public async Task<(bool Success, string? Error)> SaveFeedbackAsync(
        string userId, string bookId, bool isPositive, CancellationToken cancellationToken = default)
    {
        var bookExists = await _context.Books.AnyAsync(book => book.Id == bookId && book.IsActive, cancellationToken);
        if (!bookExists)
            return (false, "Книгу не знайдено");

        var feedback = await _context.RecommendationFeedback
            .FirstOrDefaultAsync(item => item.UserId == userId && item.BookId == bookId, cancellationToken);

        if (feedback == null)
        {
            _context.RecommendationFeedback.Add(new RecommendationFeedback
            {
                UserId = userId,
                BookId = bookId,
                IsPositive = isPositive
            });
        }
        else
        {
            feedback.IsPositive = isPositive;
            feedback.CreatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
        _cache.Remove($"recommendations:{userId}");
        return (true, null);
    }

    public async Task WarmCacheAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            await GetRecommendationsAsync(userId, 1, CandidateLimit, cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Не вдалося оновити рекомендації для користувача {UserId}", userId);
        }
    }

    public void InvalidateUserCache(string userId)
    {
        _cache.Remove($"recommendations:{userId}");
    }

    private async Task<RecommendationCacheEntry> BuildRecommendationsAsync(string? userId, CancellationToken cancellationToken)
    {
        var excludedBookIds = new List<string>();
        var genreWeights = new Dictionary<string, decimal>();

        if (!string.IsNullOrWhiteSpace(userId))
        {
            excludedBookIds = await _context.ReadingProgress.AsNoTracking()
                .Where(progress => progress.UserId == userId &&
                    (progress.Status == "finished" || progress.ProgressPercent >= 100))
                .Select(progress => progress.BookId)
                .Union(_context.WantToRead.AsNoTracking()
                    .Where(item => item.UserId == userId)
                    .Select(item => item.BookId))
                .ToListAsync(cancellationToken);

            excludedBookIds.AddRange(await _context.RecommendationFeedback.AsNoTracking()
                .Where(feedback => feedback.UserId == userId && !feedback.IsPositive)
                .Select(feedback => feedback.BookId)
                .ToListAsync(cancellationToken));

            await AddGenreWeightsAsync(
                _context.Reviews.AsNoTracking()
                    .Where(review => review.UserId == userId && review.Rating >= 4)
                    .Select(review => new GenreWeightRow { GenreId = review.Book.GenreId, Weight = review.Rating }),
                genreWeights, cancellationToken);
            await AddGenreWeightsAsync(
                _context.Favorites.AsNoTracking()
                    .Where(favorite => favorite.UserId == userId)
                    .Select(favorite => new GenreWeightRow { GenreId = favorite.Book.GenreId, Weight = 3 }),
                genreWeights, cancellationToken);
            await AddGenreWeightsAsync(
                _context.ReadingProgress.AsNoTracking()
                    .Where(progress => progress.UserId == userId)
                    .Select(progress => new GenreWeightRow { GenreId = progress.Book.GenreId, Weight = 2 }),
                genreWeights, cancellationToken);
            await AddGenreWeightsAsync(
                _context.RecommendationFeedback.AsNoTracking()
                    .Where(feedback => feedback.UserId == userId && feedback.IsPositive)
                    .Select(feedback => new GenreWeightRow { GenreId = feedback.Book.GenreId, Weight = 4 }),
                genreWeights, cancellationToken);

            var friendIds = await _context.Friends.AsNoTracking()
                .Where(friend => friend.UserId == userId)
                .Select(friend => friend.FriendUserId)
                .ToListAsync(cancellationToken);
            if (friendIds.Count > 0)
            {
                await AddGenreWeightsAsync(
                    _context.Favorites.AsNoTracking()
                        .Where(favorite => friendIds.Contains(favorite.UserId))
                        .Select(favorite => new GenreWeightRow { GenreId = favorite.Book.GenreId, Weight = 1 }),
                    genreWeights, cancellationToken);
                await AddGenreWeightsAsync(
                    _context.ReadingProgress.AsNoTracking()
                        .Where(progress => friendIds.Contains(progress.UserId) &&
                            (progress.Status == "finished" || progress.ProgressPercent >= 100))
                        .Select(progress => new GenreWeightRow { GenreId = progress.Book.GenreId, Weight = 2 }),
                    genreWeights, cancellationToken);
            }
        }

        var query = _context.Books.AsNoTracking()
            .Where(book => book.IsActive && !excludedBookIds.Contains(book.Id));
        var totalCount = await query.CountAsync(cancellationToken);
        var candidates = await ProjectBooks(query)
            .OrderByDescending(book => book.ReviewCount)
            .ThenByDescending(book => book.Rating ?? 0)
            .ThenByDescending(book => book.CreatedAt)
            .Take(CandidateLimit)
            .ToListAsync(cancellationToken);

        var recommendations = candidates.Select(book =>
        {
            var preferenceWeight = book.GenreId != null && genreWeights.TryGetValue(book.GenreId, out var weight)
                ? weight : 0m;
            var popularity = Math.Min(book.ReviewCount / 20m, 1m) * 0.25m +
                (book.Rating ?? 0) / 5m * 0.2m +
                Math.Min(book.FavoriteCount / 25m, 1m) * 0.15m;
            var score = Math.Round(Math.Min(popularity + Math.Min(preferenceWeight / 10m, 1m) * 0.4m, 1m), 2);
            var matchedGenres = book.GenreId != null && preferenceWeight > 0 && book.GenreName != null
                ? [book.GenreName] : new List<string>();

            return new RecommendationDto
            {
                Book = ToBookDto(book),
                Score = score,
                Reason = matchedGenres.Count > 0
                    ? "Схоже на книги, які ви оцінили або додали до активності"
                    : "Популярна книга серед читачів",
                MatchedGenres = matchedGenres
            };
        })
        .OrderByDescending(item => item.Score)
        .ThenByDescending(item => item.Book.Rating ?? 0)
        .ToList();

        return new RecommendationCacheEntry(recommendations, Math.Min(totalCount, CandidateLimit));
    }

    private async Task AddGenreWeightsAsync(IQueryable<GenreWeightRow> query, Dictionary<string, decimal> weights, CancellationToken cancellationToken)
    {
        var rows = await query.Where(row => row.GenreId != null)
            .GroupBy(row => row.GenreId!)
            .Select(group => new { GenreId = group.Key, Weight = group.Sum(row => row.Weight) })
            .ToListAsync(cancellationToken);

        foreach (var row in rows)
            weights[row.GenreId] = weights.GetValueOrDefault(row.GenreId) + row.Weight;
    }

    private static IQueryable<BookProjection> ProjectBooks(IQueryable<Book> query)
    {
        return query.Select(book => new BookProjection
        {
            Id = book.Id,
            Title = book.Title,
            Author = book.Author,
            Isbn = book.Isbn,
            GenreId = book.GenreId,
            GenreName = book.Genre != null ? book.Genre.Name : null,
            ImageUrl = book.ImageUrl,
            Description = book.Description,
            GutenbergId = book.GutenbergId,
            TotalCopies = book.TotalCopies,
            AvailableCopies = book.AvailableCopies,
            PdfUrl = book.PdfUrl,
            Quote = book.Quote,
            Pages = book.Pages,
            Rating = book.Rating,
            ReviewCount = book.Reviews.Count,
            IsActive = book.IsActive,
            CreatedAt = book.CreatedAt,
            FavoriteCount = book.Favorites.Count
        });
    }

    private static BookResponseDto ToBookDto(BookProjection book) => new()
    {
        Id = book.Id, Title = book.Title, Author = book.Author, Isbn = book.Isbn,
        GenreId = book.GenreId, GenreName = book.GenreName, ImageUrl = book.ImageUrl,
        Description = book.Description, GutenbergId = book.GutenbergId, TotalCopies = book.TotalCopies,
        AvailableCopies = book.AvailableCopies, PdfUrl = book.PdfUrl, Quote = book.Quote,
        Pages = book.Pages, Rating = book.Rating, ReviewCount = book.ReviewCount,
        IsActive = book.IsActive, CreatedAt = book.CreatedAt
    };

    private static PagedResponse<RecommendationDto> ToPagedResponse(List<RecommendationDto> items, int totalCount, int page, int pageSize) => new()
    {
        Items = items.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
        Page = page,
        PageSize = pageSize,
        TotalCount = totalCount
    };

    private sealed record RecommendationCacheEntry(List<RecommendationDto> Items, int TotalCount);

    private sealed class GenreWeightRow
    {
        public string? GenreId { get; set; }
        public decimal Weight { get; set; }
    }

    private sealed class BookProjection
    {
        public string Id { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string Author { get; set; } = null!;
        public string Isbn { get; set; } = null!;
        public string? GenreId { get; set; }
        public string? GenreName { get; set; }
        public string? ImageUrl { get; set; }
        public string? Description { get; set; }
        public int? GutenbergId { get; set; }
        public int TotalCopies { get; set; }
        public int AvailableCopies { get; set; }
        public string? PdfUrl { get; set; }
        public string? Quote { get; set; }
        public int? Pages { get; set; }
        public decimal? Rating { get; set; }
        public int ReviewCount { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public int FavoriteCount { get; set; }
    }
}
