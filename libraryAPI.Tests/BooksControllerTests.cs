using LibraryAPI.Controllers;
using LibraryAPI.DTOs;
using LibraryAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace LibraryAPI.Tests.Controllers;

public class BooksControllerTests
{
    private readonly Mock<IBookService> _bookServiceMock;
    private readonly Mock<IBookTextService> _bookTextServiceMock;
    private readonly Mock<IBookChatService> _bookChatServiceMock;
    private readonly Mock<IBookPdfService> _bookPdfServiceMock;
    private readonly BooksController _controller;

    public BooksControllerTests()
    {
        _bookServiceMock = new Mock<IBookService>();
        _bookTextServiceMock = new Mock<IBookTextService>();
        _bookChatServiceMock = new Mock<IBookChatService>();
        _bookPdfServiceMock = new Mock<IBookPdfService>();

        _controller = new BooksController(
            _bookServiceMock.Object,
            _bookTextServiceMock.Object,
            _bookChatServiceMock.Object,
            _bookPdfServiceMock.Object);
    }

    /// <summary>
    /// Перевіряє, що GetBookById повертає 200 OK і книгу, коли книга існує.
    /// </summary>
    [Fact]
    public async Task GetBookById_ShouldReturnOkWithBook_WhenBookExists()
    {
        // Arrange
        var bookId = "book-123";
        var expectedBook = new BookResponseDto
        {
            Id = bookId,
            Title = "Кобзар",
            Author = "Тарас Шевченко"
        };

        _bookServiceMock
            .Setup(s => s.GetByIdAsync(bookId))
            .ReturnsAsync(expectedBook);

        // Act
        var result = await _controller.GetBookById(bookId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(expectedBook, okResult.Value);

        _bookServiceMock.Verify(s => s.GetByIdAsync(bookId), Times.Once);
    }

    /// <summary>
    /// Перевіряє, що GetBookById повертає 404 NotFound, коли книга не існує.
    /// </summary>
    [Fact]
    public async Task GetBookById_ShouldReturnNotFound_WhenBookDoesNotExist()
    {
        // Arrange
        var bookId = "non-existent-id";

        _bookServiceMock
            .Setup(s => s.GetByIdAsync(bookId))
            .ReturnsAsync((BookResponseDto?)null);

        // Act
        var result = await _controller.GetBookById(bookId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.NotNull(notFoundResult);

        _bookServiceMock.Verify(s => s.GetByIdAsync(bookId), Times.Once);
    }

    /// <summary>
    /// Перевіряє, що GetBooks повертає 200 OK і передає сервісу всі параметри пагінації, пошуку, жанру та сортування.
    /// </summary>
    [Fact]
    public async Task GetBooks_ShouldReturnOkAndPassParametersToService_WhenCalledWithFilters()
    {
        // Arrange
        var page = 2;
        var pageSize = 10;
        var search = "Кобзар";
        var genreId = "genre-poetry";
        var sortBy = "title";
        var sortOrder = "desc";

        var expectedResponse = new PagedResponse<BookResponseDto>
        {
            Items = new List<BookResponseDto>
            {
                new BookResponseDto { Id = "1", Title = "Кобзар", Author = "Тарас Шевченко" }
            },
            Page = page,
            PageSize = pageSize,
            TotalCount = 1
        };

        _bookServiceMock
            .Setup(s => s.GetAllAsync(page, pageSize, search, genreId, sortBy, sortOrder))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetBooks(page, pageSize, search, genreId, sortBy, sortOrder);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(expectedResponse, okResult.Value);

        // Перевіряємо, що сервіс був викликаний саме з цими параметрами
        _bookServiceMock.Verify(
            s => s.GetAllAsync(page, pageSize, search, genreId, sortBy, sortOrder),
            Times.Once);
    }

    /// <summary>
    /// Перевіряє, що CreateBook повертає 409 Conflict, коли сервіс повертає помилку.
    /// </summary>
    [Fact]
    public async Task CreateBook_ShouldReturnConflict_WhenServiceReturnsFailure()
    {
        // Arrange
        var dto = new CreateBookDto
        {
            Title = "Кобзар",
            Author = "Тарас Шевченко"
        };

        _bookServiceMock
            .Setup(s => s.CreateAsync(dto))
            .ReturnsAsync((false, "Книга з такою назвою вже існує", (BookResponseDto?)null));

        // Act
        var result = await _controller.CreateBook(dto);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.NotNull(conflictResult);

        _bookServiceMock.Verify(s => s.CreateAsync(dto), Times.Once);
    }

    /// <summary>
    /// Перевіряє, що UpdateBook повертає 404 NotFound, коли книгу не знайдено.
    /// </summary>
    [Fact]
    public async Task UpdateBook_ShouldReturnNotFound_WhenBookDoesNotExist()
    {
        // Arrange
        var bookId = "non-existent-id";
        var dto = new UpdateBookDto { Title = "Нова назва" };

        _bookServiceMock
            .Setup(s => s.UpdateAsync(bookId, dto))
            .ReturnsAsync((false, "Книгу не знайдено"));

        // Act
        var result = await _controller.UpdateBook(bookId, dto);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);

        _bookServiceMock.Verify(s => s.UpdateAsync(bookId, dto), Times.Once);
    }

    /// <summary>
    /// Перевіряє, що DeleteBook повертає 404 NotFound, коли книгу не знайдено.
    /// </summary>
    [Fact]
    public async Task DeleteBook_ShouldReturnNotFound_WhenBookDoesNotExist()
    {
        // Arrange
        var bookId = "non-existent-id";

        _bookServiceMock
            .Setup(s => s.DeleteAsync(bookId))
            .ReturnsAsync((false, "Книгу не знайдено"));

        // Act
        var result = await _controller.DeleteBook(bookId);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);

        _bookServiceMock.Verify(s => s.DeleteAsync(bookId), Times.Once);
    }

    /// <summary>
    /// Перевіряє, що AskAboutBook повертає 400 BadRequest, коли питання порожнє.
    /// </summary>
    [Fact]
    public async Task AskAboutBook_ShouldReturnBadRequest_WhenQuestionIsEmpty()
    {
        // Arrange
        var bookId = "book-123";
        var request = new ChatRequest { Question = "   " }; // порожнє / пробіли

        // Act
        var result = await _controller.AskAboutBook(bookId, request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);

        _bookChatServiceMock.Verify(
            s => s.AskAboutBookAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>()),
            Times.Never);
    }
}
