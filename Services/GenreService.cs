using LibraryAPI.Data;
using LibraryAPI.DTOs;
using LibraryAPI.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace LibraryAPI.Services;

public class GenreService : IGenreService
{
    private readonly AppDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly CachingOptions _options;
    private readonly RequestCacheStatus _cacheStatus;

    public GenreService(
        AppDbContext context,
        IMemoryCache cache,
        IOptions<CachingOptions> options,
        RequestCacheStatus cacheStatus)
    {
        _context = context;
        _cache = cache;
        _options = options.Value;
        _cacheStatus = cacheStatus;
    }

    public async Task<IReadOnlyList<GenreDto>> GetGenresAsync()
    {
        if (!_options.Enabled)
        {
            _cacheStatus.CacheHit = false;
            return await LoadGenresAsync();
        }

        if (_cache.TryGetValue(CacheKeys.Genres, out IReadOnlyList<GenreDto>? cached) && cached is not null)
        {
            _cacheStatus.CacheHit = true;
            return cached;
        }

        _cacheStatus.CacheHit = false;
        var genres = await LoadGenresAsync();

        _cache.Set(
            CacheKeys.Genres,
            genres,
            new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(_options.GenresSeconds),
                Size = 1
            });

        return genres;
    }

    private async Task<IReadOnlyList<GenreDto>> LoadGenresAsync()
    {
        return await _context.Genres
            .AsNoTracking()
            .OrderBy(g => g.Name)
            .Select(g => new GenreDto
            {
                Id = g.Id,
                Name = g.Name
            })
            .ToListAsync();
    }
}
