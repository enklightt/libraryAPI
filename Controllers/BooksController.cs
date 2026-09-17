using System.ComponentModel.DataAnnotations;
using LibraryAPI.DTOs;
using LibraryAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryAPI.Controllers
{
    [ApiController]
    [Route("api/v1/books")]
    public class BooksController : ControllerBase
    {
        private readonly IBookService _bookService;
        private readonly IBookTextService _bookTextService;
        private readonly IBookChatService _bookChatService;
        private readonly IHttpClientFactory _httpClientFactory;

        public BooksController(IBookService bookService, IBookTextService bookTextService, IBookChatService bookChatService, IHttpClientFactory httpClientFactory)
        {
            _bookService = bookService;
            _bookTextService = bookTextService;
            _bookChatService = bookChatService;
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResponse<BookResponseDto>>> GetBooks(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? search = null,
            [FromQuery] string? genreId = null,
            [FromQuery] string? sortBy = null,
            [FromQuery] string sortOrder = "asc")
        {
            var result = await _bookService.GetAllAsync(page, pageSize, search, genreId, sortBy, sortOrder);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<BookResponseDto>> GetBookById(string id)
        {
            var book = await _bookService.GetByIdAsync(id);

            if (book == null)
                return NotFound(new { message = "Книгу не знайдено" });

            return Ok(book);
        }

        [Authorize(Roles = "admin,manager")]
        [HttpPost]
        public async Task<ActionResult<BookResponseDto>> CreateBook(CreateBookDto dto)
        {
            var result = await _bookService.CreateAsync(dto);

            if (!result.Success)
                return Conflict(new { message = result.Error });

            return CreatedAtAction(nameof(GetBookById), new { id = result.Book!.Id }, result.Book);
        }

        [Authorize(Roles = "admin,manager")]
        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateBook(string id, UpdateBookDto dto)
        {
            var result = await _bookService.UpdateAsync(id, dto);

            if (!result.Success)
            {
                if (result.Error == "Книгу не знайдено")
                    return NotFound(new { message = result.Error });

                return Conflict(new { message = result.Error });
            }

            return Ok(new { message = "Книгу оновлено" });
        }

        [Authorize(Roles = "admin,manager")]
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteBook(string id)
        {
            var result = await _bookService.DeleteAsync(id);

            if (!result.Success)
                return NotFound(new { message = result.Error });

            return NoContent();
        }

        [Authorize(Roles = "admin,manager")]
        [HttpPost("{id}/pdf")]
        public async Task<ActionResult> UploadPdf(string id, IFormFile pdf)
        {
            var result = await _bookService.UploadPdfAsync(id, pdf);

            if (!result.Success)
                return BadRequest(new { message = result.Error });

            return Ok(new { message = "PDF завантажено" });
        }

        [AllowAnonymous]
        [HttpGet("{id}/text")]
        public async Task<ActionResult> GetBookText(string id)
        {
            var (found, text, title, author) = await _bookTextService.GetBookTextAsync(id);
            if (!found || text == null)
                return NotFound(new { message = "Текст недоступний для цієї книги" });

            return Ok(new { text, title, author });
        }

        [HttpGet("{id}/pdf")]
        public async Task<ActionResult> GetBookPdf(string id)
        {
            var book = await _bookService.GetByIdAsync(id);

            if (book == null)
                return NotFound(new { message = "Книгу не знайдено" });

            if (string.IsNullOrEmpty(book.PdfUrl))
                return NotFound(new { message = "PDF файл недоступний для цієї книги" });

            if (System.IO.File.Exists(book.PdfUrl))
            {
                var fileStream = System.IO.File.OpenRead(book.PdfUrl);
                Response.Headers["Content-Disposition"] = "inline";
                return File(fileStream, "application/pdf");
            }

            if (book.PdfUrl.StartsWith("/"))
            {
                var wwwrootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var filePath = Path.Combine(wwwrootPath, book.PdfUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

                if (System.IO.File.Exists(filePath))
                {
                    var fileStream = System.IO.File.OpenRead(filePath);
                    Response.Headers["Content-Disposition"] = "inline";
                    return File(fileStream, "application/pdf");
                }
                else
                {
                    return NotFound(new { message = $"PDF файл не знайдено за шляхом: {book.PdfUrl}" });
                }
            }

            if (Uri.TryCreate(book.PdfUrl, UriKind.Absolute, out var uri))
            {
                var httpClient = _httpClientFactory.CreateClient();
                try
                {
                    var pdfBytes = await httpClient.GetByteArrayAsync(uri);
                    Response.Headers["Content-Disposition"] = "inline";
                    return File(pdfBytes, "application/pdf");
                }
                catch (HttpRequestException)
                {
                    return NotFound(new { message = "Не вдалося завантажити PDF файл" });
                }
            }

            return NotFound(new { message = "Невірний шлях до PDF файлу" });
        }

        [Authorize]
        [HttpPost("{id}/chat")]
        public async Task<ActionResult> AskAboutBook(string id, [FromBody] ChatRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Question))
                return BadRequest(new { message = "Введіть питання" });

            var answer = await _bookChatService.AskAboutBookAsync(id, request.Question, request.CurrentPage);

            if (answer == null)
                return NotFound(new { message = "Книгу не знайдено" });

            return Ok(new { answer });
        }

        [Authorize]
        [HttpPost("{id}/chat/stream")]
        public async Task AskAboutBookStream(string id, [FromBody] ChatRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Question))
            {
                Response.ContentType = "text/event-stream";
                await Response.WriteAsync("data: {\"error\":\"Введіть питання\"}\n\n");
                await Response.Body.FlushAsync();
                return;
            }

            Response.ContentType = "text/event-stream";
            Response.Headers["Cache-Control"] = "no-cache";
            Response.Headers["Connection"] = "keep-alive";
            Response.Headers["X-Accel-Buffering"] = "no";

            var started = false;
            await _bookChatService.AskAboutBookStreamAsync(id, request.Question, async chunk =>
            {
                started = true;
                var escaped = chunk.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
                await Response.WriteAsync($"data: {{\"text\":\"{escaped}\"}}\n\n");
                await Response.Body.FlushAsync();
            }, request.CurrentPage);

            if (!started)
                await Response.WriteAsync("data: {\"error\":\"Книгу не знайдено\"}\n\n");
            else
                await Response.WriteAsync("data: [DONE]\n\n");

            await Response.Body.FlushAsync();
        }
    }
}

public class ChatRequest
{
    [Required(ErrorMessage = "Введіть питання")]
    [StringLength(2000, MinimumLength = 2, ErrorMessage = "Питання має бути від 2 до 2000 символів")]
    public string Question { get; set; } = "";

    [Range(1, int.MaxValue, ErrorMessage = "CurrentPage має бути більшим за 0")]
    public int? CurrentPage { get; set; }
}
