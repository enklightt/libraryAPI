namespace LibraryAPI.Infrastructure;

public sealed class CachingOptions
{
    public const string SectionName = "Caching";

    public bool Enabled { get; set; } = true;
    public int BookListSeconds { get; set; } = 30;
    public int BookSeconds { get; set; } = 60;
    public int GenresSeconds { get; set; } = 300;
}

public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    public bool Enabled { get; set; } = true;
    public int AuthPermitLimit { get; set; } = 10;
    public int WritePermitLimit { get; set; } = 60;
    public int WindowSeconds { get; set; } = 60;
}

public sealed class ResilienceOptions
{
    public const string SectionName = "Resilience";

    public int TimeoutSeconds { get; set; } = 10;
    public int AttemptTimeoutSeconds { get; set; } = 5;
    public int RetryCount { get; set; } = 3;
    public double CircuitBreakerFailureRatio { get; set; } = 0.5;
    public int CircuitBreakerMinimumThroughput { get; set; } = 5;
    public int CircuitBreakerSamplingSeconds { get; set; } = 30;
    public int CircuitBreakerBreakSeconds { get; set; } = 15;
}
