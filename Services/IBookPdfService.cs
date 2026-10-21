namespace LibraryAPI.Services
{
    public interface IBookPdfService
    {
        Task<(bool Success, string? Error, Stream? Stream)> GetPdfStreamAsync(string? pdfUrl);
    }
}
