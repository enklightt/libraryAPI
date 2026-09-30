using LibraryAPI.DTOs;
using LibraryAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;

namespace LibraryAPI.Controllers
{
    /// <summary>
    /// Контролер для роботи з книгами: CRUD, завантаження PDF, отримання тексту та чат з книгою.
    /// </summary>
    [ApiController]
    [Route("api/v1/books")]
    [Produces(MediaTypeNames.Application.Json)]
    public class BooksController : ControllerBase
    {
        private readonly IBookService _bookService;
        private readonly IBookTextService _bookTextService;
        private readonly IBookChatService _bookChatService;
        private readonly IBookPdfService _bookPdfService;

        public BooksController(IBookService bookService, IBookTextService bookTextService, IBookChatService bookChatService, IBookPdfService bookPdfService)
        {
            _bookService = bookService;
            _bookTextService = bookTextService;
            _bookChatService = bookChatService;
            _bookPdfService = bookPdfService;
        }

        /// <summary>
        /// Отримує список книг з пагінацією, пошуком, фільтрацією та сортуванням.
        /// </summary>
        /// <param name="page">Номер сторінки (починається з 1). За замовчуванням: 1.</param>
        /// <param name="pageSize">Кількість елементів на сторінці. За замовчуванням: 20.</param>
        /// <param name="search">Пошуковий запит (назва, автор тощо).</param>
        /// <param name="genreId">Ідентифікатор жанру для фільтрації.</param>
        /// <param name="sortBy">Поле сортування (наприклад: title, author, year).</param>
        /// <param name="sortOrder">Порядок сортування: "asc" або "desc". За замовчуванням: "asc".</param>
        /// <returns>Сторінкова відповідь зі списком книг.</returns>
        /// <response code="200">Список книг успішно отримано.</response>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResponse<BookResponseDto>), StatusCodes.Status200OK)]
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

        /// <summary>
        /// Отримує книгу за її ідентифікатором.
        /// </summary>
        /// <param name="id">Ідентифікатор книги.</param>
        /// <returns>Дані книги.</returns>
        /// <response code="200">Книгу успішно знайдено.</response>
        /// <response code="404">Книгу не знайдено.</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(BookResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<BookResponseDto>> GetBookById(string id)
        {
            var book = await _bookService.GetByIdAsync(id);

            if (book == null)
                return NotFound(new { message = "Книгу не знайдено" });

            return Ok(book);
        }

        /// <summary>
        /// Створює нову книгу.
        /// </summary>
        /// <remarks>
        /// Доступно лише для ролей **admin** та **manager**.
        /// </remarks>
        /// <param name="dto">Дані для створення книги.</param>
        /// <returns>Створена книга.</returns>
        /// <response code="201">Книгу успішно створено.</response>
        /// <response code="409">Конфлікт (наприклад, книга з такою назвою вже існує).</response>
        /// <response code="401">Користувач не авторизований.</response>
        /// <response code="403">Недостатньо прав доступу.</response>
        [Authorize(Roles = "admin,manager")]
        [HttpPost]
        [ProducesResponseType(typeof(BookResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<BookResponseDto>> CreateBook(CreateBookDto dto)
        {
            var result = await _bookService.CreateAsync(dto);

            if (!result.Success)
                return Conflict(new { message = result.Error });

            return CreatedAtAction(nameof(GetBookById), new { id = result.Book!.Id }, result.Book);
        }

        /// <summary>
        /// Оновлює існуючу книгу.
        /// </summary>
        /// <remarks>
        /// Доступно лише для ролей **admin** та **manager**.
        /// </remarks>
        /// <param name="id">Ідентифікатор книги.</param>
        /// <param name="dto">Дані для оновлення книги.</param>
        /// <returns>Повідомлення про успішне оновлення.</returns>
        /// <response code="200">Книгу успішно оновлено.</response>
        /// <response code="404">Книгу не знайдено.</response>
        /// <response code="409">Конфлікт під час оновлення.</response>
        /// <response code="401">Користувач не авторизований.</response>
        /// <response code="403">Недостатньо прав доступу.</response>
        [Authorize(Roles = "admin,manager")]
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
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

        /// <summary>
        /// Видаляє книгу за ідентифікатором.
        /// </summary>
        /// <remarks>
        /// Доступно лише для ролей **admin** та **manager**.
        /// </remarks>
        /// <param name="id">Ідентифікатор книги.</param>
        /// <response code="204">Книгу успішно видалено.</response>
        /// <response code="404">Книгу не знайдено.</response>
        /// <response code="401">Користувач не авторизований.</response>
        /// <response code="403">Недостатньо прав доступу.</response>
        [Authorize(Roles = "admin,manager")]
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> DeleteBook(string id)
        {
            var result = await _bookService.DeleteAsync(id);

            if (!result.Success)
                return NotFound(new { message = result.Error });

            return NoContent();
        }

        /// <summary>
        /// Завантажує PDF-файл для книги.
        /// </summary>
        /// <remarks>
        /// Доступно лише для ролей **admin** та **manager**.  
        /// Файл передається як multipart/form-data.
        /// </remarks>
        /// <param name="id">Ідентифікатор книги.</param>
        /// <param name="pdf">PDF-файл книги.</param>
        /// <returns>Повідомлення про успішне завантаження.</returns>
        /// <response code="200">PDF успішно завантажено.</response>
        /// <response code="400">Помилка валідації файлу або книги.</response>
        /// <response code="401">Користувач не авторизований.</response>
        /// <response code="403">Недостатньо прав доступу.</response>
        [Authorize(Roles = "admin,manager")]
        [HttpPost("{id}/pdf")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> UploadPdf(string id, IFormFile pdf)
        {
            var result = await _bookService.UploadPdfAsync(id, pdf);

            if (!result.Success)
                return BadRequest(new { message = result.Error });

            return Ok(new { message = "PDF завантажено" });
        }

        /// <summary>
        /// Отримує текст книги (якщо доступний).
        /// </summary>
        /// <remarks>
        /// Endpoint доступний без авторизації.
        /// </remarks>
        /// <param name="id">Ідентифікатор книги.</param>
        /// <returns>Текст книги, назва та автор.</returns>
        /// <response code="200">Текст успішно отримано.</response>
        /// <response code="404">Текст недоступний або книгу не знайдено.</response>
        [AllowAnonymous]
        [HttpGet("{id}/text")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetBookText(string id)
        {
            var (found, text, title, author) = await _bookTextService.GetBookTextAsync(id);
            if (!found || text == null)
                return NotFound(new { message = "Текст недоступний для цієї книги" });

            return Ok(new { text, title, author });
        }

        /// <summary>
        /// Отримує PDF-файл книги для перегляду або завантаження.
        /// </summary>
        /// <remarks>
        /// Повертає файл з Content-Disposition: inline (відкривається в браузері).  
        /// Підтримує локальні файли, файли з wwwroot та зовнішні URL.
        /// </remarks>
        /// <param name="id">Ідентифікатор книги.</param>
        /// <returns>Потік PDF-файлу.</returns>
        /// <response code="200">PDF-файл успішно повернуто.</response>
        /// <response code="404">Книгу або PDF-файл не знайдено.</response>
        [HttpGet("{id}/pdf")]
        [Produces("application/pdf")]
        [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetBookPdf(string id)
        {
            var book = await _bookService.GetByIdAsync(id);

            if (book == null)
                return NotFound(new { message = "Книгу не знайдено" });
            var result = await _bookPdfService.GetPdfStreamAsync(book.PdfUrl);
            if (!result.Success || result.Stream == null)
                return NotFound(new { message = result.Error });
            Response.Headers["Content-Disposition"] = "inline";
            return File(result.Stream, "application/pdf");
        }

        [Authorize]
        [HttpPost("{id}/chat")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult> AskAboutBook(string id, [FromBody] ChatRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Question))
                return BadRequest(new { message = "Введіть питання" });

            var answer = await _bookChatService.AskAboutBookAsync(id, request.Question, request.CurrentPage);

            if (answer == null)
                return NotFound(new { message = "Книгу не знайдено" });

            return Ok(new { answer });
        }

        /// <summary>
        /// Задає питання про книгу з потоковою відповіддю (Server-Sent Events).
        /// </summary>
        /// <remarks>
        /// Потрібна авторизація.  
        /// Відповідь надходить частинами у форматі text/event-stream.  
        /// Кожне повідомлення має вигляд:  
        /// `data: {"text":"..."}`  
        /// Наприкінці: `data: [DONE]`
        /// </remarks>
        /// <param name="id">Ідентифікатор книги.</param>
        /// <param name="request">Об'єкт з питанням та (опціонально) поточною сторінкою.</param>
        /// <response code="200">Потік відповіді розпочато.</response>
        /// <response code="400">Питання порожнє.</response>
        /// <response code="401">Користувач не авторизований.</response>
        [Authorize]
        [HttpPost("{id}/chat/stream")]
        [Produces("text/event-stream")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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

    /// <summary>
    /// Запит для чату з книгою.
    /// </summary>
    public class ChatRequest
    {
        /// <summary>
        /// Текст питання до книги.
        /// </summary>
        /// <example>Про що ця книга?</example>
        public string Question { get; set; } = "";

        /// <summary>
        /// Номер поточної сторінки (опціонально), щоб відповідь була більш контекстною.
        /// </summary>
        /// <example>42</example>
        public int? CurrentPage { get; set; }
    }
}