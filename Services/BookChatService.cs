using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using LibraryAPI.Data;
using LibraryAPI.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LibraryAPI.Services
{
    public class BookChatService : IBookChatService
    {
        private readonly AppDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public BookChatService(AppDbContext context, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        public async Task<string?> AskAboutBookAsync(string bookId, string question, int? currentPage = null)
        {
            var book = await _context.Books.Include(b => b.Genre).FirstOrDefaultAsync(b => b.Id == bookId);
            if (book == null) return null;

            var apiKey = _configuration["GroqSettings:ApiKey"];
            var model = _configuration["GroqSettings:Model"] ?? "llama-3.3-70b-versatile";
            if (string.IsNullOrEmpty(apiKey)) return "API ключ не налаштовано";

            var contextText = $"Назва: {book.Title}\nАвтор: {book.Author}";
            if (book.Genre != null) contextText += $"\nЖанр: {book.Genre.Name}";
            if (book.Pages.HasValue) contextText += $"\nСторінок: {book.Pages}";
            if (!string.IsNullOrEmpty(book.Description)) contextText += $"\nОпис: {book.Description}";
            if (!string.IsNullOrEmpty(book.Quote)) contextText += $"\nЦитата: \"{book.Quote}\"";
            if (book.Rating.HasValue) contextText += $"\nРейтинг: {book.Rating}/5";

            var pageContext = currentPage.HasValue ? $"\nКористувач зараз читає сторінку {currentPage}." : "";
            var prompt = $"Ти — бібліотечний помічник, який знає все про книги. Відповідай коротко і по суті українською мовою. Не використовуй виділення зірочками або markdown.{pageContext}\n\nІнформація про книгу:\n{contextText}\n\nПитання користувача: {question}\n\nВідповідь:";

            var client = _httpClientFactory.CreateClient();
            var requestBody = new
            {
                model,
                messages = new[]
                {
                    new { role = "user", content = prompt }
                },
                max_tokens = 500,
                temperature = 0.7
            };

            var json = JsonSerializer.Serialize(requestBody);
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.groq.com/openai/v1/chat/completions");
            httpRequest.Headers.Add("Authorization", $"Bearer {apiKey}");
            httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await client.SendAsync(httpRequest);
                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    return $"Помилка API: {response.StatusCode}";
                }

                var responseBody = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseBody);
                var choice = doc.RootElement.GetProperty("choices")[0];
                var message = choice.GetProperty("message").GetProperty("content").GetString();
                return message?.Trim();
            }
            catch (HttpRequestException)
            {
                return "Не вдалося з'єднатися з API";
            }
        }

        public async Task AskAboutBookStreamAsync(string bookId, string question, Func<string, Task> onChunk, int? currentPage = null)
        {
            var book = await _context.Books.Include(b => b.Genre).FirstOrDefaultAsync(b => b.Id == bookId);
            if (book == null) return;

            var apiKey = _configuration["GroqSettings:ApiKey"];
            var model = _configuration["GroqSettings:Model"] ?? "llama-3.3-70b-versatile";
            if (string.IsNullOrEmpty(apiKey)) return;

            var contextText = $"Назва: {book.Title}\nАвтор: {book.Author}";
            if (book.Genre != null) contextText += $"\nЖанр: {book.Genre.Name}";
            if (book.Pages.HasValue) contextText += $"\nСторінок: {book.Pages}";
            if (!string.IsNullOrEmpty(book.Description)) contextText += $"\nОпис: {book.Description}";
            if (!string.IsNullOrEmpty(book.Quote)) contextText += $"\nЦитата: \"{book.Quote}\"";
            if (book.Rating.HasValue) contextText += $"\nРейтинг: {book.Rating}/5";

            var pageContext = currentPage.HasValue ? $"\nКористувач зараз читає сторінку {currentPage}." : "";
            var prompt = $"Ти — бібліотечний помічник, який знає все про книги. Відповідай коротко і по суті українською мовою. Не використовуй виділення зірочками або markdown.{pageContext}\n\nІнформація про книгу:\n{contextText}\n\nПитання користувача: {question}\n\nВідповідь:";

            var client = _httpClientFactory.CreateClient();
            var requestBody = new
            {
                model,
                messages = new[] { new { role = "user", content = prompt } },
                max_tokens = 500,
                temperature = 0.7,
                stream = true
            };

            var json = JsonSerializer.Serialize(requestBody);
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.groq.com/openai/v1/chat/completions");
            httpRequest.Headers.Add("Authorization", $"Bearer {apiKey}");
            httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await client.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead);
                if (!response.IsSuccessStatusCode) return;

                using var stream = await response.Content.ReadAsStreamAsync();
                using var reader = new StreamReader(stream);

                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync();
                    if (string.IsNullOrEmpty(line)) continue;
                    if (!line.StartsWith("data: ")) continue;

                    var data = line[6..];
                    if (data == "[DONE]") break;

                    using var doc = JsonDocument.Parse(data);
                    if (doc.RootElement.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                    {
                        var delta = choices[0].GetProperty("delta");
                        if (delta.TryGetProperty("content", out var content))
                        {
                            var text = content.GetString();
                            if (!string.IsNullOrEmpty(text))
                                await onChunk(text);
                        }
                    }
                }
            }
            catch { }
        }
    }
}