using System.Text.Json;
using System.Text.Json.Serialization;
using LibraryAPI.Data;
using LibraryAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace LibraryAPI.Services;

public class BookTextService : IBookTextService
{
    private readonly AppDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<BookTextService> _logger;

    public BookTextService(AppDbContext context, IHttpClientFactory httpClientFactory, ILogger<BookTextService> logger)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<(bool Found, string? Text, string? Title, string? Author)> GetBookTextAsync(string bookId)
    {
        var book = await _context.Books.FirstOrDefaultAsync(b => b.Id == bookId);
        if (book == null)
            return (false, null, null, null);

        try
        {
            if (book.GutenbergId.HasValue)
            {
                return await FetchFromGutenberg(book.GutenbergId.Value, book.Title, book.Author);
            }

            return await SearchGutendex(book);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch text for book {BookId}", bookId);
            return (false, null, null, null);
        }
    }

    private async Task<(bool Found, string? Text, string? Title, string? Author)> FetchFromGutenberg(int gutenbergId, string title, string author)
    {
        var httpClient = _httpClientFactory.CreateClient();
        httpClient.Timeout = TimeSpan.FromSeconds(15);

        var urls = new[]
        {
            $"https://www.gutenberg.org/files/{gutenbergId}/{gutenbergId}-0.txt",
            $"https://www.gutenberg.org/ebooks/{gutenbergId}.txt.utf-8",
            $"https://www.gutenberg.org/cache/epub/{gutenbergId}/pg{gutenbergId}.txt"
        };

        foreach (var textUrl in urls)
        {
            var textResponse = await httpClient.GetAsync(textUrl);
            if (textResponse.IsSuccessStatusCode)
            {
                var text = await textResponse.Content.ReadAsStringAsync();
                return (true, text, title, author);
            }
        }

        return (false, null, null, null);
    }

    private async Task<(bool Found, string? Text, string? Title, string? Author)> SearchGutendex(Book book)
    {
        var httpClient = _httpClientFactory.CreateClient();
        httpClient.Timeout = TimeSpan.FromSeconds(15);

        var gutendexUrl = $"https://gutendex.com/books?search={Uri.EscapeDataString(book.Title)}";
        var gutendexResponse = await httpClient.GetAsync(gutendexUrl);
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

        return await FetchFromGutenberg(gutenbergBook.Id, book.Title, book.Author);
    }

    private class GutendexResponse
    {
        [JsonPropertyName("results")]
        public List<GutendexBook>? Results { get; set; }
    }

    private class GutendexBook
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        [JsonPropertyName("authors")]
        public List<GutendexAuthor>? Authors { get; set; }
    }

    private class GutendexAuthor
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }
}
