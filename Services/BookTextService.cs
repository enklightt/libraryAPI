using System.Text.Json;
using System.Text.Json.Serialization;
using LibraryAPI.Data;
using LibraryAPI.Infrastructure;
using LibraryAPI.Models;
using Microsoft.EntityFrameworkCore;
using Polly.CircuitBreaker;

namespace LibraryAPI.Services;

public class BookTextService : IBookTextService
{
    private readonly AppDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<BookTextService> _logger;

    public BookTextService(
        AppDbContext context,
        IHttpClientFactory httpClientFactory,
        ILogger<BookTextService> logger)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<(bool Found, string? Text, string? Title, string? Author)> GetBookTextAsync(string bookId)
    {
        var book = await _context.Books
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == bookId);

        if (book == null)
            return (false, null, null, null);

        try
        {
            var client = _httpClientFactory.CreateClient(HttpClientExtensions.ExternalContentClientName);

            if (book.GutenbergId.HasValue)
                return await FetchFromGutenberg(client, book.GutenbergId.Value, book.Title, book.Author);

            return await SearchGutendex(client, book);
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogWarning(ex, "External content circuit breaker is open for book {BookId}", bookId);
            return (false, null, null, null);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            _logger.LogWarning(ex, "Failed to fetch text for book {BookId}", bookId);
            return (false, null, null, null);
        }
    }

    private static async Task<(bool Found, string? Text, string? Title, string? Author)> FetchFromGutenberg(
        HttpClient httpClient,
        int gutenbergId,
        string title,
        string author)
    {
        var urls = new[]
        {
            $"https://www.gutenberg.org/files/{gutenbergId}/{gutenbergId}-0.txt",
            $"https://www.gutenberg.org/ebooks/{gutenbergId}.txt.utf-8",
            $"https://www.gutenberg.org/cache/epub/{gutenbergId}/pg{gutenbergId}.txt"
        };

        foreach (var textUrl in urls)
        {
            using var textResponse = await httpClient.GetAsync(textUrl);
            if (!textResponse.IsSuccessStatusCode)
                continue;

            var text = await textResponse.Content.ReadAsStringAsync();
            return (true, text, title, author);
        }

        return (false, null, null, null);
    }

    private static async Task<(bool Found, string? Text, string? Title, string? Author)> SearchGutendex(
        HttpClient httpClient,
        Book book)
    {
        var gutendexUrl = $"https://gutendex.com/books?search={Uri.EscapeDataString(book.Title)}";
        using var gutendexResponse = await httpClient.GetAsync(gutendexUrl);
        if (!gutendexResponse.IsSuccessStatusCode)
            return (false, null, null, null);

        var gutendexJson = await gutendexResponse.Content.ReadAsStringAsync();
        var gutendexResult = JsonSerializer.Deserialize<GutendexResponse>(gutendexJson);
        var gutenbergBook = gutendexResult?.Results?.FirstOrDefault(r =>
            r.Authors != null && r.Authors.Exists(a =>
                !string.IsNullOrWhiteSpace(book.Author) &&
                a.Name != null &&
                a.Name.Contains(book.Author.Split(',')[0].Trim(), StringComparison.OrdinalIgnoreCase)));

        if (gutenbergBook == null)
            return (false, null, null, null);

        return await FetchFromGutenberg(httpClient, gutenbergBook.Id, book.Title, book.Author);
    }

    private sealed class GutendexResponse
    {
        [JsonPropertyName("results")]
        public List<GutendexBook>? Results { get; set; }
    }

    private sealed class GutendexBook
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("authors")]
        public List<GutendexAuthor>? Authors { get; set; }
    }

    private sealed class GutendexAuthor
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }
}
