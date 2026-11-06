using LibraryAPI.Data;
using LibraryAPI.DTOs;
using LibraryAPI.Infrastructure;
using LibraryAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace LibraryAPI.Services
{
    public class BookService : IBookService
    {
        private const int MaxPageSize = 100;

        private readonly AppDbContext _context;
        private readonly ILogger<BookService> _logger;
        private readonly IMemoryCache _cache;
        private readonly CachingOptions _options;
        private readonly RequestCacheStatus _cacheStatus;

        private static int _listVersion = 1;

        public BookService(
            AppDbContext context,
            ILogger<BookService> logger,
            IMemoryCache cache,
            IOptions<CachingOptions> options,
            RequestCacheStatus cacheStatus)
        {
            _context = context;
            _logger = logger;
            _cache = cache;
            _options = options.Value;
            _cacheStatus = cacheStatus;
        }

        public async Task<PagedResponse<BookResponseDto>> GetAllAsync(int page, int pageSize, string? search = null, string? genreId = null, string? sortBy = null, string sortOrder = "asc")
        {
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

            var cacheKey = $"{CacheKeys.BookList(page, pageSize, search, genreId, sortBy, sortOrder)}:{Volatile.Read(ref _listVersion)}";

            if (_options.Enabled &&
                _cache.TryGetValue(cacheKey, out PagedResponse<BookResponseDto>? cached) &&
                cached is not null)
            {
                _cacheStatus.CacheHit = true;
                return cached;
            }

            _cacheStatus.CacheHit = false;
            var result = await LoadBooksAsync(page, pageSize, search, genreId, sortBy, sortOrder);

            if (_options.Enabled)
            {
                _cache.Set(
                    cacheKey,
                    result,
                    new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(_options.BookListSeconds),
                        Size = 1
                    });
            }

            return result;
        }

        private async Task<PagedResponse<BookResponseDto>> LoadBooksAsync(int page, int pageSize, string? search, string? genreId, string? sortBy, string sortOrder)
        {
            var query = _context.Books
                .AsNoTracking()
                .Include(b => b.Genre)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var pattern = $"%{EscapeLikePattern(search.Trim())}%";
                query = query.Where(b =>
                    EF.Functions.Like(b.Title, pattern) ||
                    EF.Functions.Like(b.Author, pattern));
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

        private static string EscapeLikePattern(string value)
        {
            return value
                .Replace("\\", "\\\\", StringComparison.Ordinal)
                .Replace("%", "\\%", StringComparison.Ordinal)
                .Replace("_", "\\_", StringComparison.Ordinal);
        }

        public async Task<BookResponseDto?> GetByIdAsync(string id)
        {
            var cacheKey = CacheKeys.BookById(id);

            if (_options.Enabled &&
                _cache.TryGetValue(cacheKey, out BookResponseDto? cached) &&
                cached is not null)
            {
                _cacheStatus.CacheHit = true;
                return cached;
            }

            _cacheStatus.CacheHit = false;

            var book = await _context.Books
                .AsNoTracking()
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

            if (book is not null && _options.Enabled)
            {
                _cache.Set(
                    cacheKey,
                    book,
                    new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(_options.BookSeconds),
                        Size = 1
                    });
            }

            return book;
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
            InvalidateBookCaches();

            _logger.LogInformation("Книгу {Title} успішно створено", book.Title);

            var genre = book.GenreId != null
                ? await _context.Genres.AsNoTracking().FirstOrDefaultAsync(g => g.Id == book.GenreId)
                : null;

            return (true, null, MapToDto(book, genre?.Name, reviewCount: 0));
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
            InvalidateBookCaches(id);
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

            await _context.Reviews.Where(r => r.BookId == id).ExecuteDeleteAsync();
            await _context.Favorites.Where(f => f.BookId == id).ExecuteDeleteAsync();
            await _context.ReadingProgress.Where(rp => rp.BookId == id).ExecuteDeleteAsync();
            await _context.WantToRead.Where(w => w.BookId == id).ExecuteDeleteAsync();

            _context.Books.Remove(book);
            await _context.SaveChangesAsync();
            InvalidateBookCaches(id);
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
            InvalidateBookCaches(id);

            _logger.LogInformation("PDF завантажено для книги {Id}: {FileName}", id, fileName);
            return (true, null);
        }

        private void InvalidateBookCaches(string? bookId = null)
        {
            Interlocked.Increment(ref _listVersion);

            if (!string.IsNullOrWhiteSpace(bookId))
                _cache.Remove(CacheKeys.BookById(bookId));
        }

        private static BookResponseDto MapToDto(Book book, string? genreName, int reviewCount)
        {
            return new BookResponseDto
            {
                Id = book.Id,
                Title = book.Title,
                Author = book.Author,
                Isbn = book.Isbn,
                GenreId = book.GenreId,
                GenreName = genreName,
                ImageUrl = book.ImageUrl,
                Description = book.Description,
                GutenbergId = book.GutenbergId,
                TotalCopies = book.TotalCopies,
                AvailableCopies = book.AvailableCopies,
                PdfUrl = book.PdfUrl,
                Quote = book.Quote,
                Pages = book.Pages,
                Rating = book.Rating,
                ReviewCount = reviewCount,
                IsActive = book.IsActive,
                CreatedAt = book.CreatedAt
            };
        }
    }
}
