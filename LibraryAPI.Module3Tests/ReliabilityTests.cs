using System.Net;
using LibraryAPI.Data;
using LibraryAPI.Infrastructure;
using LibraryAPI.Models;
using LibraryAPI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Polly;
using Polly.CircuitBreaker;
using Polly.Timeout;
using Xunit;

namespace LibraryAPI.Module3Tests;

/// <summary>Перевірка health check, retry, timeout, circuit breaker і fallback.</summary>
public class ReliabilityTests : IClassFixture<LibraryApiFactory>
{
    private readonly HttpClient _client;

    public ReliabilityTests(LibraryApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Health_ReturnsHealthy()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task HttpClientPipeline_RetriesTransientFailures()
    {
        var pipeline = CreatePipeline(retryCount: 2);
        var attempts = 0;

        var response = await pipeline.ExecuteAsync(_ =>
        {
            attempts++;
            var status = attempts < 3 ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.OK;
            return ValueTask.FromResult(new HttpResponseMessage(status));
        });

        Assert.Equal(3, attempts);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task HttpClientPipeline_OpensCircuitAfterRepeatedFailures()
    {
        var pipeline = CreatePipeline(retryCount: 1, minimumThroughput: 2);
        static ValueTask<HttpResponseMessage> Fail(CancellationToken _) =>
            ValueTask.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));

        await pipeline.ExecuteAsync(Fail);

        await Assert.ThrowsAsync<BrokenCircuitException>(async () => await pipeline.ExecuteAsync(Fail));
    }

    [Fact]
    public async Task HttpClientPipeline_TimesOutSlowDependency()
    {
        var pipeline = CreatePipeline(retryCount: 1, attemptTimeoutSeconds: 1);

        await Assert.ThrowsAsync<TimeoutRejectedException>(async () =>
            await pipeline.ExecuteAsync(async token =>
            {
                await Task.Delay(TimeSpan.FromSeconds(5), token);
                return new HttpResponseMessage(HttpStatusCode.OK);
            }));
    }

    [Fact]
    public async Task BookTextService_WhenCircuitIsOpen_ReturnsFallback()
    {
        await using var context = await CreateContextAsync();
        context.Books.Add(NewBook());
        await context.SaveChangesAsync();
        var service = new BookTextService(
            context,
            new ThrowingClientFactory(new BrokenCircuitException("open")),
            NullLogger<BookTextService>.Instance);

        var result = await service.GetBookTextAsync("book-1");

        Assert.False(result.Found);
        Assert.Null(result.Text);
    }

    [Fact]
    public async Task BookTextService_WhenHttpRequestFails_ReturnsFallback()
    {
        await using var context = await CreateContextAsync();
        context.Books.Add(NewBook());
        await context.SaveChangesAsync();
        var service = new BookTextService(
            context,
            new ThrowingClientFactory(new HttpRequestException("network error")),
            NullLogger<BookTextService>.Instance);

        var result = await service.GetBookTextAsync("book-1");

        Assert.False(result.Found);
        Assert.Null(result.Text);
    }

    private static Book NewBook() => new()
    {
        Id = "book-1",
        Title = "Test Book",
        Author = "Test Author",
        Isbn = "isbn-1",
        GutenbergId = 123
    };

    private static async Task<AppDbContext> CreateContextAsync()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        var context = new AppDbContext(options);
        await context.Database.OpenConnectionAsync();
        await context.Database.EnsureCreatedAsync();
        return context;
    }

    private static ResiliencePipeline<HttpResponseMessage> CreatePipeline(
        int retryCount,
        int minimumThroughput = 20,
        int attemptTimeoutSeconds = 2)
    {
        var options = new ResilienceOptions
        {
            TimeoutSeconds = 5,
            AttemptTimeoutSeconds = attemptTimeoutSeconds,
            RetryCount = retryCount,
            CircuitBreakerFailureRatio = 0.5,
            CircuitBreakerMinimumThroughput = minimumThroughput,
            CircuitBreakerSamplingSeconds = 30,
            CircuitBreakerBreakSeconds = 15
        };

        var builder = new ResiliencePipelineBuilder<HttpResponseMessage>();
        HttpClientExtensions.ConfigurePipeline(builder, options);
        return builder.Build();
    }

    private sealed class ThrowingClientFactory : IHttpClientFactory
    {
        private readonly HttpClient _client;

        public ThrowingClientFactory(Exception exception) =>
            _client = new HttpClient(new ThrowingHandler(exception));

        public HttpClient CreateClient(string name) => _client;
    }

    private sealed class ThrowingHandler : HttpMessageHandler
    {
        private readonly Exception _exception;

        public ThrowingHandler(Exception exception) => _exception = exception;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromException<HttpResponseMessage>(_exception);
    }
}
