using System.ComponentModel.DataAnnotations;
using LibraryAPI.DTOs;
using LibraryAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryAPI.Controllers
{
    /// <summary>Searches and manages books, reading content, PDFs, and book chat.</summary>
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

        /// <summary>Returns a filtered and paginated book catalog.</summary>
        /// <param name="page">One-based result page.</param>
        /// <param name="pageSize">Maximum number of books to return.</param>
        /// <param name="search">Optional title or author search.</param>
        /// <param name="genreId">Optional genre identifier.</param>
        /// <param name="sortBy">Optional sort field: title, author, rating, or created.</param>
        /// <param name="sortOrder">Sort direction, asc or desc.</param>
        /// <response code="200">The page of matching books.</response>
        /// <remarks>Example request: <code>GET /api/v1/books?page=1&amp;pageSize=20&amp;search=history</code>. Example response: <code>{"items":[],"page":1,"pageSize":20,"totalCount":0}</code></remarks>
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

        /// <summary>Returns a book by identifier.</summary>
        /// <param name="id">The book identifier.</param>
        /// <response code="200">The book was found.</response>
        /// <response code="404">No book has this identifier.</response>
        /// <remarks>Example request: <code>GET /api/v1/books/{id}</code>. Example response: <code>{"id":"book-id","title":"The Hobbit","author":"J. R. R. Tolkien"}</code></remarks>
        [HttpGet("{id}")]
        public async Task<ActionResult<BookResponseDto>> GetBookById(string id)
        {
            var book = await _bookService.GetByIdAsync(id);

            if (book == null)
                return NotFound(new { message = "Книгу не знайдено" });

            return Ok(book);
        }

        /// <summary>Adds a book to the catalog.</summary>
        /// <param name="dto">The new book fields.</param>
        /// <response code="201">The book was created and its resource location is returned.</response>
        /// <response code="400">The request failed validation.</response>
        /// <response code="401">Authentication is required.</response>
        /// <response code="403">The caller is not an admin or manager.</response>
        /// <response code="409">A book with the supplied ISBN already exists.</response>
        /// <remarks>Example request: <code>{"title":"The Hobbit","author":"J. R. R. Tolkien","isbn":"9780547928227","totalCopies":5}</code>. Example response: <code>{"id":"book-id","title":"The Hobbit","author":"J. R. R. Tolkien","isbn":"9780547928227"}</code></remarks>
        [Authorize(Roles = "admin,manager")]
        [HttpPost]
        public async Task<ActionResult<BookResponseDto>> CreateBook(CreateBookDto dto)
        {
            var result = await _bookService.CreateAsync(dto);

            if (!result.Success)
                return Conflict(new { message = result.Error });

            return CreatedAtAction(nameof(GetBookById), new { id = result.Book!.Id }, result.Book);
        }

        /// <summary>Replaces the editable fields of a catalog book.</summary>
        /// <param name="id">The book identifier.</param>
        /// <param name="dto">The updated book fields.</param>
        /// <response code="200">The book was updated.</response>
        /// <response code="400">The request failed validation.</response>
        /// <response code="401">Authentication is required.</response>
        /// <response code="403">The caller is not an admin or manager.</response>
        /// <response code="404">No book has this identifier.</response>
        /// <response code="409">Another book uses the supplied ISBN.</response>
        /// <remarks>Example request: <code>{"title":"The Hobbit","author":"J. R. R. Tolkien","isbn":"9780547928227","totalCopies":5,"availableCopies":5}</code>. Example response: <code>{"message":"Книгу оновлено"}</code></remarks>
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

        /// <summary>Deletes a book and its associated data.</summary>
        /// <param name="id">The book identifier.</param>
        /// <response code="204">The book was deleted.</response>
        /// <response code="401">Authentication is required.</response>
        /// <response code="403">The caller is not an admin or manager.</response>
        /// <response code="404">No book has this identifier.</response>
        /// <remarks>Example request: <code>DELETE /api/v1/books/{id}</code>. Example response: <code>204 No Content</code></remarks>
        [Authorize(Roles = "admin,manager")]
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteBook(string id)
        {
            var result = await _bookService.DeleteAsync(id);

            if (!result.Success)
                return NotFound(new { message = result.Error });

            return NoContent();
        }

        /// <summary>Uploads a PDF file for a book.</summary>
        /// <param name="id">The book identifier.</param>
        /// <param name="pdf">The PDF file sent as multipart form data.</param>
        /// <response code="200">The PDF was uploaded.</response>
        /// <response code="400">The upload was invalid or failed.</response>
        /// <response code="401">Authentication is required.</response>
        /// <response code="403">The caller is not an admin or manager.</response>
        /// <remarks>Example request: <code>POST /api/v1/books/{id}/pdf with multipart/form-data field pdf=@book.pdf</code>. Example response: <code>{"message":"PDF завантажено"}</code></remarks>
        [Authorize(Roles = "admin,manager")]
        [HttpPost("{id}/pdf")]
        public async Task<ActionResult> UploadPdf(string id, IFormFile pdf)
        {
            var result = await _bookService.UploadPdfAsync(id, pdf);

            if (!result.Success)
                return BadRequest(new { message = result.Error });

            return Ok(new { message = "PDF завантажено" });
        }

        /// <summary>Returns readable text for a book when a text source is available.</summary>
        /// <param name="id">The book identifier.</param>
        /// <response code="200">Text and book metadata were returned.</response>
        /// <response code="404">The book or its text is unavailable.</response>
        /// <remarks>Example request: <code>GET /api/v1/books/{id}/text</code>. Example response: <code>{"text":"...","title":"...","author":"..."}</code></remarks>
        [AllowAnonymous]
        [HttpGet("{id}/text")]
        public async Task<ActionResult> GetBookText(string id)
        {
            var (found, text, title, author) = await _bookTextService.GetBookTextAsync(id);
            if (!found || text == null)
                return NotFound(new { message = "Текст недоступний для цієї книги" });

            return Ok(new { text, title, author });
        }

        /// <summary>Streams a book PDF from its configured local or remote source.</summary>
        /// <param name="id">The book identifier.</param>
        /// <response code="200">The PDF file is returned as application/pdf.</response>
        /// <response code="404">The book or PDF is unavailable.</response>
        /// <remarks>Example request: <code>GET /api/v1/books/{id}/pdf</code>. Example response: PDF byte stream with content type <code>application/pdf</code>.</remarks>
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

        /// <summary>Asks a question about a book and returns one answer.</summary>
        /// <param name="id">The book identifier.</param>
        /// <param name="request">The question and optional current reading page.</param>
        /// <response code="200">The answer was generated.</response>
        /// <response code="400">The question failed validation.</response>
        /// <response code="401">Authentication is required.</response>
        /// <response code="404">The book was not found.</response>
        /// <remarks>Example request: <code>{"question":"Who is the main character?","currentPage":12}</code>. Example response: <code>{"answer":"..."}</code></remarks>
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

        /// <summary>Streams an answer to a book question using server-sent events.</summary>
        /// <param name="id">The book identifier.</param>
        /// <param name="request">The question and optional current reading page.</param>
        /// <response code="200">Text chunks are sent as server-sent events, followed by a completion event.</response>
        /// <response code="401">Authentication is required.</response>
        /// <remarks>Example request: <code>{"question":"Summarize this chapter","currentPage":12}</code>. Example response: SSE events <code>data: {"text":"..."}</code>, then <code>data: [DONE]</code>.</remarks>
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
