using System.Text;
using Microsoft.Extensions.Http;
using LibraryAPI.Interfaces;

namespace LibraryAPI.Services
{
    public class BookPdfService : IBookPdfService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<BookPdfService> _logger;

        public BookPdfService(IHttpClientFactory httpClientFactory, ILogger<BookPdfService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<(bool Success, string? Error, Stream? Stream)> GetPdfStreamAsync(string? PdfUrl)
        {
            if (string.IsNullOrEmpty(PdfUrl))
                return (false, "PDF недоступний для цієї кнниги", null);
            if (File.Exists(PdfUrl))
            {
                var fileStream = File.OpenRead(PdfUrl);
                if (!await IsPdfAsync(fileStream))
                {
                    await fileStream.DisposeAsync();
                    return (false, "Файл книги пошкоджений або не є PDF", null);
                }
                return (true, null, fileStream);
            }
            if (PdfUrl.StartsWith("/"))
            {
                var wwwrootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var filePath = Path.Combine(wwwrootPath, PdfUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(filePath))
                {
                    var fileStream = File.OpenRead(filePath);
                    if (!await IsPdfAsync(fileStream))
                    {
                        await fileStream.DisposeAsync();
                        return (false, "Файл книги пошкоджений або не є PDF", null);
                    }
                    return (true, null, fileStream);
                }

                _logger.LogWarning("PDF файл не знайдено за шляхом: {PdfUrl}", PdfUrl);
                return (false, $"PDF файл не знайдено за шляхом: {PdfUrl}", null);
            }
            if (Uri.TryCreate(PdfUrl, UriKind.Absolute, out var uri))
            {
                var httpClient = _httpClientFactory.CreateClient();
                try
                {
                    var pdfBytes = await httpClient.GetByteArrayAsync(uri);
                    if (pdfBytes.Length < 5 || Encoding.ASCII.GetString(pdfBytes, 0, 5) != "%PDF-")
                        return (false, "Файл книги пошкоджений або не є PDF", null);

                    return (true, null, new MemoryStream(pdfBytes));
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogWarning(ex, "Не вдалося завантажити PDF файл за посиланням {Url}", PdfUrl);
                    return (false, "Не вдалося завантажити PDF файл", null);
                }
            }

            return (false, "Невірний шлях до PDF файлу", null);
        }

        private static async Task<bool> IsPdfAsync(Stream stream)
        {
            var header = new byte[5];
            var bytesRead = await stream.ReadAsync(header.AsMemory(0, header.Length));
            stream.Position = 0;
            return bytesRead == header.Length && Encoding.ASCII.GetString(header) == "%PDF-";
        }
    }
}
