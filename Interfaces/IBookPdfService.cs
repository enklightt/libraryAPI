namespace LibraryAPI.Interfaces
{
    public interface IBookPdfService
    {
        Task<(bool Success, string? Error, Stream? Stream)> GetPdfStreamAsync(string? PdfUrl);
    }
}
