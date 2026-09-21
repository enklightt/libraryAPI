using LibraryAPI.DTOs;

namespace LibraryAPI.Interfaces
{
    public interface IBookService
    {
        Task<PagedResponse<BookResponseDto>> GetAllAsync(int page, int pageSize, string? search = null, string? genreId = null, string? sortBy = null, string sortOrder = "asc");
        Task<BookResponseDto?> GetByIdAsync(string id);
        Task<(bool Success, string? Error, BookResponseDto? Book)> CreateAsync(CreateBookDto dto);
        Task<(bool Success, string? Error)> UpdateAsync(string id, UpdateBookDto dto);
        Task<(bool Success, string? Error)> DeleteAsync(string id);
        Task<(bool Success, string? Error)> UploadPdfAsync(string id, IFormFile file);
    }
}
