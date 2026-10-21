namespace LibraryAPI.Infrastructure;

/// <summary>
/// Scoped marker that lets controllers report whether the current request was
/// served from the in-memory cache. It is intentionally not thread-safe because
/// a single request is handled sequentially.
/// </summary>
public sealed class RequestCacheStatus
{
    public bool? CacheHit { get; set; }
}
