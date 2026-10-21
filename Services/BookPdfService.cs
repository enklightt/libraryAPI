using LibraryAPI.Infrastructure;
using Polly.CircuitBreaker;

namespace LibraryAPI.Services
{
    public class BookPdfService : IBookPdfService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<BookPdfService> _logger;

        public BookPdfService(
            IWebHostEnvironment environment,
            IHttpClientFactory httpClientFactory,
            ILogger<BookPdfService> logger)
        {
            _environment = environment;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<(bool Success, string? Error, Stream? Stream)> GetPdfStreamAsync(string? pdfUrl)
        {
            if (string.IsNullOrWhiteSpace(pdfUrl))
                return (false, "PDF не знайдено", null);

            if (Uri.TryCreate(pdfUrl, UriKind.Absolute, out var uri))
            {
                if (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
                    return await DownloadRemotePdfAsync(uri);

                if (uri.Scheme == Uri.UriSchemeFile && File.Exists(uri.LocalPath))
                    return OpenLocalFile(uri.LocalPath);
            }

            var webRootPath = Path.GetFullPath(
                _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"));
            var fullPath = Path.GetFullPath(Path.Combine(webRootPath, pdfUrl.TrimStart('/')));

            if (!fullPath.StartsWith(webRootPath + Path.DirectorySeparatorChar, StringComparison.Ordinal))
                return (false, "Некоректний шлях до PDF", null);

            if (!File.Exists(fullPath))
                return (false, "PDF не знайдено", null);

            return OpenLocalFile(fullPath);
        }

        private async Task<(bool Success, string? Error, Stream? Stream)> DownloadRemotePdfAsync(Uri uri)
        {
            try
            {
                var client = _httpClientFactory.CreateClient(HttpClientExtensions.PdfDownloadClientName);
                using var response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);

                if (!response.IsSuccessStatusCode)
                    return (false, "Не вдалося завантажити PDF з зовнішнього URL", null);

                var buffer = new MemoryStream();
                await response.Content.CopyToAsync(buffer);
                buffer.Position = 0;
                return (true, null, buffer);
            }
            catch (BrokenCircuitException ex)
            {
                _logger.LogWarning(ex, "PDF download circuit breaker is open for {Url}", uri);
                return (false, "Сервіс завантаження PDF тимчасово недоступний", null);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                _logger.LogWarning(ex, "Failed to download PDF from {Url}", uri);
                return (false, "Не вдалося завантажити PDF з зовнішнього URL", null);
            }
        }

        private static (bool Success, string? Error, Stream? Stream) OpenLocalFile(string path)
        {
            var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            return (true, null, stream);
        }
    }
}
