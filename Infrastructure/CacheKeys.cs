namespace LibraryAPI.Infrastructure;

public static class CacheKeys
{
    public const string Genres = "genres:all";
    public const string BookListVersion = "books:list:version";

    public static string BookById(string id) => $"books:item:{id}";

    public static string BookList(
        int page,
        int pageSize,
        string? search,
        string? genreId,
        string? sortBy,
        string sortOrder)
    {
        var normalizedSearch = string.IsNullOrWhiteSpace(search) ? "-" : search.Trim().ToLowerInvariant();
        var normalizedGenre = string.IsNullOrWhiteSpace(genreId) ? "-" : genreId;
        var normalizedSort = string.IsNullOrWhiteSpace(sortBy) ? "title" : sortBy.ToLowerInvariant();
        var normalizedOrder = sortOrder.Equals("desc", StringComparison.OrdinalIgnoreCase) ? "desc" : "asc";

        return $"books:list:{page}:{pageSize}:{normalizedSearch}:{normalizedGenre}:{normalizedSort}:{normalizedOrder}";
    }
}
