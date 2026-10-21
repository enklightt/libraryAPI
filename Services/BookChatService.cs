using System.Text;
using System.Text.Json;
using LibraryAPI.Data;
using LibraryAPI.Infrastructure;
using LibraryAPI.Models;
using Microsoft.EntityFrameworkCore;
using Polly.CircuitBreaker;

namespace LibraryAPI.Services
{
    public class BookChatService : IBookChatService
    {
        private const string GroqEndpoint = "openai/v1/chat/completions";
        private const string DefaultModel = "llama-3.3-70b-versatile";

        private readonly AppDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<BookChatService> _logger;

        public BookChatService(
            AppDbContext context,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<BookChatService> logger)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<string?> AskAboutBookAsync(string bookId, string question, int? currentPage = null)
        {
            var book = await GetBookAsync(bookId);
            if (book == null)
                return null;

            var apiKey = _configuration["GroqSettings:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
                return "API ключ не налаштовано";

            var request = BuildRequest(apiKey, BuildPrompt(book, question, currentPage), stream: false);
            var client = _httpClientFactory.CreateClient(HttpClientExtensions.GroqClientName);

            try
            {
                using var response = await client.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "Groq API returned {StatusCode} for book {BookId}",
                        (int)response.StatusCode,
                        bookId);
                    return "AI-сервіс тимчасово недоступний";
                }

                var responseBody = await response.Content.ReadAsStringAsync();
                using var document = JsonDocument.Parse(responseBody);
                var content = document.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();

                return content?.Trim();
            }
            catch (BrokenCircuitException ex)
            {
                _logger.LogWarning(ex, "Groq circuit breaker is open");
                return "AI-сервіс тимчасово недоступний. Спробуйте пізніше";
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
            {
                _logger.LogWarning(ex, "Failed to call Groq API for book {BookId}", bookId);
                return "Не вдалося отримати відповідь від AI-сервісу";
            }
        }

        public async Task AskAboutBookStreamAsync(string bookId, string question, Func<string, Task> onChunk, int? currentPage = null)
        {
            var book = await GetBookAsync(bookId);
            if (book == null)
                return;

            var apiKey = _configuration["GroqSettings:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
                return;

            var request = BuildRequest(apiKey, BuildPrompt(book, question, currentPage), stream: true);
            var client = _httpClientFactory.CreateClient(HttpClientExtensions.GroqClientName);

            try
            {
                using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "Groq streaming API returned {StatusCode} for book {BookId}",
                        (int)response.StatusCode,
                        bookId);
                    return;
                }

                await using var stream = await response.Content.ReadAsStreamAsync();
                using var reader = new StreamReader(stream);

                while (await reader.ReadLineAsync() is { } line)
                {
                    var chunk = ParseStreamingChunk(line);
                    if (chunk is null)
                        continue;

                    await onChunk(chunk);
                }
            }
            catch (BrokenCircuitException ex)
            {
                _logger.LogWarning(ex, "Groq circuit breaker is open while streaming");
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
            {
                _logger.LogWarning(ex, "Failed to stream Groq response for book {BookId}", bookId);
            }
        }

        private async Task<Book?> GetBookAsync(string bookId)
        {
            return await _context.Books
                .AsNoTracking()
                .Include(b => b.Genre)
                .FirstOrDefaultAsync(b => b.Id == bookId);
        }

        private static string? ParseStreamingChunk(string line)
        {
            if (string.IsNullOrEmpty(line) || !line.StartsWith("data: ", StringComparison.Ordinal))
                return null;

            var data = line[6..];
            if (data == "[DONE]")
                return null;

            using var document = JsonDocument.Parse(data);
            if (!document.RootElement.TryGetProperty("choices", out var choices) ||
                choices.GetArrayLength() == 0)
            {
                return null;
            }

            var delta = choices[0].GetProperty("delta");
            if (!delta.TryGetProperty("content", out var content))
                return null;

            var text = content.GetString();
            return string.IsNullOrEmpty(text) ? null : text;
        }

        private static string BuildPrompt(Book book, string question, int? currentPage)
        {
            var contextText = new StringBuilder()
                .AppendLine($"Назва: {book.Title}")
                .AppendLine($"Автор: {book.Author}");

            if (book.Genre != null)
                contextText.AppendLine($"Жанр: {book.Genre.Name}");
            if (book.Pages.HasValue)
                contextText.AppendLine($"Сторінок: {book.Pages}");
            if (!string.IsNullOrEmpty(book.Description))
                contextText.AppendLine($"Опис: {book.Description}");
            if (!string.IsNullOrEmpty(book.Quote))
                contextText.AppendLine($"Цитата: \"{book.Quote}\"");
            if (book.Rating.HasValue)
                contextText.AppendLine($"Рейтинг: {book.Rating}/5");

            var pageContext = currentPage.HasValue
                ? $"\nКористувач зараз читає сторінку {currentPage}."
                : string.Empty;

            return $"Ти — бібліотечний помічник, який знає все про книги. " +
                   $"Відповідай коротко і по суті українською мовою. " +
                   $"Не використовуй виділення зірочками або markdown.{pageContext}\n\n" +
                   $"Інформація про книгу:\n{contextText}\n" +
                   $"Питання користувача: {question}\n\nВідповідь:";
        }

        private HttpRequestMessage BuildRequest(string apiKey, string prompt, bool stream)
        {
            var model = _configuration["GroqSettings:Model"] ?? DefaultModel;
            var requestBody = new
            {
                model,
                messages = new[] { new { role = "user", content = prompt } },
                max_tokens = 500,
                temperature = 0.7,
                stream
            };

            var json = JsonSerializer.Serialize(requestBody);
            var request = new HttpRequestMessage(HttpMethod.Post, GroqEndpoint);
            request.Headers.Add("Authorization", $"Bearer {apiKey}");
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            return request;
        }
    }
}
