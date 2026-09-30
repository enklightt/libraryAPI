using LibraryAPI.Data;
using LibraryAPI.DTOs;
using LibraryAPI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace LibraryAPI.Services
{
    public class BookService : IBookService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<BookService> _logger;

        public BookService(AppDbContext context, ILogger<BookService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<PagedResponse<BookResponseDto>> GetAllAsync(int page, int pageSize, string? search = null, string? genreId = null, string? sortBy = null, string sortOrder = "asc")
        {
            var query = _context.Books.Include(b => b.Genre).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(b => b.Title.ToLower().Contains(term) || b.Author.ToLower().Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(genreId))
                query = query.Where(b => b.GenreId == genreId);

            query = sortBy?.ToLower() switch
            {
                "title" => sortOrder == "desc" ? query.OrderByDescending(b => b.Title) : query.OrderBy(b => b.Title),
                "author" => sortOrder == "desc" ? query.OrderByDescending(b => b.Author) : query.OrderBy(b => b.Author),
                "rating" => sortOrder == "desc" ? query.OrderByDescending(b => b.Rating) : query.OrderBy(b => b.Rating),
                "created" => sortOrder == "desc" ? query.OrderByDescending(b => b.CreatedAt) : query.OrderBy(b => b.CreatedAt),
                _ => query.OrderBy(b => b.Title)
            };

            var totalCount = await query.CountAsync();

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(b => new BookResponseDto
                {
                    Id = b.Id,
                    Title = b.Title,
                    Author = b.Author,
                    Isbn = b.Isbn,
                    GenreId = b.GenreId,
                    GenreName = b.Genre != null ? b.Genre.Name : null,
                    ImageUrl = b.ImageUrl,
                    Description = b.Description,
                    GutenbergId = b.GutenbergId,

                    TotalCopies = b.TotalCopies,
                    AvailableCopies = b.AvailableCopies,
                    PdfUrl = b.PdfUrl,
                    Quote = b.Quote,
                    Pages = b.Pages,
                    Rating = b.Rating,
                    ReviewCount = b.Reviews.Count,
                    IsActive = b.IsActive,
                    CreatedAt = b.CreatedAt
                })
                .ToListAsync();

            return new PagedResponse<BookResponseDto>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }

        public async Task<BookResponseDto?> GetByIdAsync(string id)
        {
            return await _context.Books
                .Include(b => b.Genre)
                .Where(b => b.Id == id)
                .Select(b => new BookResponseDto
                {
                    Id = b.Id,
                    Title = b.Title,
                    Author = b.Author,
                    Isbn = b.Isbn,
                    GenreId = b.GenreId,
                    GenreName = b.Genre != null ? b.Genre.Name : null,
                    ImageUrl = b.ImageUrl,
                    Description = b.Description,
                    GutenbergId = b.GutenbergId,

                    TotalCopies = b.TotalCopies,
                    AvailableCopies = b.AvailableCopies,
                    PdfUrl = b.PdfUrl,
                    Quote = b.Quote,
                    Pages = b.Pages,
                    Rating = b.Rating,
                    ReviewCount = b.Reviews.Count,
                    IsActive = b.IsActive,
                    CreatedAt = b.CreatedAt
                })
                .FirstOrDefaultAsync();
        }

        public async Task<(bool Success, string? Error, BookResponseDto? Book)> CreateAsync(CreateBookDto dto)
        {
            _logger.LogInformation("Спроба створити книгу з ISBN {Isbn}", dto.Isbn);

            var isbnExists = await _context.Books.AnyAsync(b => b.Isbn == dto.Isbn);
            if (isbnExists)
            {
                _logger.LogWarning("Спроба створити книгу з існуючим ISBN {Isbn}", dto.Isbn);
                return (false, "Книга з таким ISBN уже існує", null);
            }

            var book = new Book
            {
                Title = dto.Title,
                Author = dto.Author,
                Isbn = dto.Isbn,
                GenreId = dto.GenreId,
                TotalCopies = dto.TotalCopies,
                AvailableCopies = dto.TotalCopies,
                PdfUrl = dto.PdfUrl,
                Quote = dto.Quote,
                Pages = dto.Pages,
                ImageUrl = dto.ImageUrl,
                Description = dto.Description,
                GutenbergId = dto.GutenbergId,

                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Books.Add(book);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Книгу {Title} успішно створено", book.Title);

            var genre = book.GenreId != null ? await _context.Genres.FindAsync(book.GenreId) : null;

            return (true, null, new BookResponseDto
            {
                Id = book.Id,
                Title = book.Title,
                Author = book.Author,
                Isbn = book.Isbn,
                GenreId = book.GenreId,
                GenreName = genre?.Name,
                ImageUrl = book.ImageUrl,
                Description = book.Description,
                GutenbergId = book.GutenbergId,

                TotalCopies = book.TotalCopies,
                AvailableCopies = book.AvailableCopies,
                PdfUrl = book.PdfUrl,
                Quote = book.Quote,
                Pages = book.Pages,
                Rating = book.Rating,
                ReviewCount = 0,
                IsActive = book.IsActive,
                CreatedAt = book.CreatedAt
            });
        }

        public async Task<(bool Success, string? Error)> UpdateAsync(string id, UpdateBookDto dto)
        {
            _logger.LogInformation("Спроба оновити книгу з id {Id}", id);

            var book = await _context.Books.FirstOrDefaultAsync(b => b.Id == id);
            if (book == null)
            {
                _logger.LogWarning("Книгу з id {Id} не знайдено для оновлення", id);
                return (false, "Книгу не знайдено");
            }

            var isbnExists = await _context.Books.AnyAsync(b => b.Isbn == dto.Isbn && b.Id != id);
            if (isbnExists)
            {
                _logger.LogWarning("Спроба оновити книгу {Id} з ISBN, який вже зайнятий: {Isbn}", id, dto.Isbn);
                return (false, "Інша книга з таким ISBN уже існує");
            }

            book.Title = dto.Title;
            book.Author = dto.Author;
            book.Isbn = dto.Isbn;
            book.GenreId = dto.GenreId;
            book.TotalCopies = dto.TotalCopies;
            book.AvailableCopies = dto.AvailableCopies;
            book.PdfUrl = dto.PdfUrl;
            book.Quote = dto.Quote;
            book.Pages = dto.Pages;
            book.ImageUrl = dto.ImageUrl;
            book.Description = dto.Description;
            book.GutenbergId = dto.GutenbergId;

            book.Rating = dto.Rating;
            book.IsActive = dto.IsActive;
            book.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            _logger.LogInformation("Книгу з id {Id} успішно оновлено", id);

            return (true, null);
        }

        public async Task<(bool Success, string? Error)> DeleteAsync(string id)
        {
            _logger.LogInformation("Спроба видалити книгу з id {Id}", id);

            var book = await _context.Books.FirstOrDefaultAsync(b => b.Id == id);
            if (book == null)
            {
                _logger.LogWarning("Книгу з id {Id} не знайдено для видалення", id);
                return (false, "Книгу не знайдено");
            }

            var reviews = await _context.Reviews.Where(r => r.BookId == id).ToListAsync();
            _context.Reviews.RemoveRange(reviews);
            var favorites = await _context.Favorites.Where(f => f.BookId == id).ToListAsync();
            _context.Favorites.RemoveRange(favorites);
            var readingProgress = await _context.ReadingProgress.Where(rp => rp.BookId == id).ToListAsync();
            _context.ReadingProgress.RemoveRange(readingProgress);
            var wantToRead = await _context.WantToRead.Where(w => w.BookId == id).ToListAsync();
            _context.WantToRead.RemoveRange(wantToRead);

            _context.Books.Remove(book);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Книгу з id {Id} успішно видалено", id);

            return (true, null);
        }

        public async Task<(bool Success, string? Error)> UploadPdfAsync(string id, IFormFile file)
        {
            var book = await _context.Books.FirstOrDefaultAsync(b => b.Id == id);
            if (book == null)
                return (false, "Книгу не знайдено");

            if (file == null || file.Length == 0)
                return (false, "Файл не вибрано або він порожній");

            if (!file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                return (false, "Дозволені тільки PDF файли");

            var wwwrootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "books");
            Directory.CreateDirectory(wwwrootPath);

            var fileName = $"{id}_{Guid.NewGuid():N}.pdf";
            var filePath = Path.Combine(wwwrootPath, fileName);

            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            book.PdfUrl = $"/books/{fileName}";
            book.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("PDF завантажено для книги {Id}: {FileName}", id, fileName);
            return (true, null);
        }
    }
}
