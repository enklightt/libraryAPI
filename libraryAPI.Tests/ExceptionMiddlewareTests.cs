using System.Text.Json;
using LibraryAPI.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LibraryAPI.Tests.Middleware;

/// <summary>Тести обробки винятків ExceptionMiddleware.</summary>
public class ExceptionMiddlewareTests
{
    private static async Task<(HttpContext Context, string Body)> InvokeAsync(Exception exception)
    {
        var context = new DefaultHttpContext { TraceIdentifier = "test-trace" };
        context.Response.Body = new MemoryStream();

        var middleware = new ExceptionMiddleware(
            _ => throw exception,
            NullLogger<ExceptionMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        return (context, await reader.ReadToEndAsync());
    }

    private static string ReadMessage(string body) =>
        JsonDocument.Parse(body).RootElement.GetProperty("message").GetString()!;

    [Fact]
    public async Task InvokeAsync_WhenArgumentException_Returns400()
    {
        var (context, body) = await InvokeAsync(new ArgumentException("secret detail"));

        Assert.Equal(400, context.Response.StatusCode);
        Assert.Equal("Некоректний запит", ReadMessage(body));
        Assert.DoesNotContain("secret detail", body);
    }

    [Fact]
    public async Task InvokeAsync_WhenKeyNotFound_Returns404WithMessage()
    {
        var (context, body) = await InvokeAsync(new KeyNotFoundException("Книгу не знайдено"));

        Assert.Equal(404, context.Response.StatusCode);
        Assert.Equal("Книгу не знайдено", ReadMessage(body));
    }

    [Fact]
    public async Task InvokeAsync_WhenUnauthorizedAccess_Returns403()
    {
        var (context, body) = await InvokeAsync(new UnauthorizedAccessException());

        Assert.Equal(403, context.Response.StatusCode);
        Assert.Equal("Доступ заборонено", ReadMessage(body));
    }

    [Fact]
    public async Task InvokeAsync_WhenUnexpectedException_Returns500WithoutDetails()
    {
        var (context, body) = await InvokeAsync(new InvalidOperationException("database password"));

        Assert.Equal(500, context.Response.StatusCode);
        Assert.Equal("Внутрішня помилка сервера", ReadMessage(body));
        Assert.DoesNotContain("database password", body);
        Assert.Equal("test-trace", JsonDocument.Parse(body).RootElement.GetProperty("traceId").GetString());
    }

    [Fact]
    public async Task InvokeAsync_WhenOperationCanceled_Rethrows()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var middleware = new ExceptionMiddleware(
            _ => throw new OperationCanceledException(),
            NullLogger<ExceptionMiddleware>.Instance);

        await Assert.ThrowsAsync<OperationCanceledException>(() => middleware.InvokeAsync(context));
    }
}
