using Microsoft.AspNetCore.Hosting;

namespace LibraryAPI.Services
{
    public class BookPdfService : IBookPdfService
    {
        private readonly IWebHostEnvironment _environment;

        public BookPdfService(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public async Task<(bool Success, string? Error, Stream? Stream)> GetPdfStreamAsync(string? pdfUrl)
        {
            if (string.IsNullOrWhiteSpace(pdfUrl))
                return (false, "PDF не знайдено", null);

            if (Uri.TryCreate(pdfUrl, UriKind.Absolute, out var uri) &&
                (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                using var client = new HttpClient();
                try
                {
                    var bytes = await client.GetByteArrayAsync(uri);
                    return (true, null, new MemoryStream(bytes));
                }
                catch
                {
                    return (false, "Не вдалося завантажити PDF з зовнішнього URL", null);
                }
            }

            var webRootPath = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var relativePath = pdfUrl.TrimStart('/');
            var fullPath = Path.Combine(webRootPath, relativePath);

            if (!File.Exists(fullPath))
                return (false, "PDF не знайдено", null);

            var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            return (true, null, stream);
        }
    }
}
