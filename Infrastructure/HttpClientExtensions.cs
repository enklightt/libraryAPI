using Microsoft.Extensions.Http.Resilience;
using Polly;

namespace LibraryAPI.Infrastructure;

public static class HttpClientExtensions
{
    public const string GroqClientName = "Groq";
    public const string ExternalContentClientName = "ExternalContent";
    public const string PdfDownloadClientName = "PdfDownload";

    public static IServiceCollection AddResilientHttpClients(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = configuration
            .GetSection(ResilienceOptions.SectionName)
            .Get<ResilienceOptions>() ?? new ResilienceOptions();

        services.AddHttpClient(GroqClientName, client =>
            {
                client.BaseAddress = new Uri("https://api.groq.com/");
                client.Timeout = Timeout.InfiniteTimeSpan;
            })
            .AddResilienceHandler("groq-resilience", builder =>
                ConfigurePipeline(builder, options));

        services.AddHttpClient(ExternalContentClientName, client =>
            {
                client.Timeout = Timeout.InfiniteTimeSpan;
            })
            .AddResilienceHandler("external-content-resilience", builder =>
                ConfigurePipeline(builder, options));

        services.AddHttpClient(PdfDownloadClientName, client =>
            {
                client.Timeout = Timeout.InfiniteTimeSpan;
            })
            .AddResilienceHandler("pdf-download-resilience", builder =>
                ConfigurePipeline(builder, options));

        return services;
    }

    /// <summary>
    /// Configures the timeout / retry / circuit-breaker pipeline used by all
    /// outbound HTTP dependencies. The same method is reused by tests so the
    /// behaviour under temporary failures can be verified without a real network.
    /// </summary>
    public static void ConfigurePipeline(
        ResiliencePipelineBuilder<HttpResponseMessage> builder,
        ResilienceOptions options)
    {
        builder.AddTimeout(TimeSpan.FromSeconds(options.TimeoutSeconds));

        builder.AddRetry(new HttpRetryStrategyOptions
        {
            MaxRetryAttempts = options.RetryCount,
            Delay = TimeSpan.FromMilliseconds(200),
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true,
            ShouldHandle = args => ValueTask.FromResult(
                args.Outcome.Result is null ||
                args.Outcome.Result.StatusCode is System.Net.HttpStatusCode.RequestTimeout
                    or System.Net.HttpStatusCode.TooManyRequests
                    or >= System.Net.HttpStatusCode.InternalServerError)
        });

        builder.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
        {
            FailureRatio = options.CircuitBreakerFailureRatio,
            MinimumThroughput = options.CircuitBreakerMinimumThroughput,
            SamplingDuration = TimeSpan.FromSeconds(options.CircuitBreakerSamplingSeconds),
            BreakDuration = TimeSpan.FromSeconds(options.CircuitBreakerBreakSeconds),
            ShouldHandle = args => ValueTask.FromResult(
                args.Outcome.Result is null ||
                args.Outcome.Result.StatusCode is System.Net.HttpStatusCode.RequestTimeout
                    or System.Net.HttpStatusCode.TooManyRequests
                    or >= System.Net.HttpStatusCode.InternalServerError)
        });

        builder.AddTimeout(TimeSpan.FromSeconds(options.AttemptTimeoutSeconds));
    }

    public static void ConfigurePipeline(
        ResiliencePipelineBuilder<HttpResponseMessage> builder,
        IConfiguration configuration)
    {
        var options = configuration
            .GetSection(ResilienceOptions.SectionName)
            .Get<ResilienceOptions>() ?? new ResilienceOptions();
        ConfigurePipeline(builder, options);
    }
}
