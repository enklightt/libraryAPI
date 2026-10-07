using LibraryAPI.Controllers;
using LibraryAPI.DTOs;
using LibraryAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;

namespace LibraryAPI.Tests.Controllers
{
    [TestFixture]
    public class BooksControllerTests
    {
        private Mock<IBookService> _bookServiceMock = null!;
        private Mock<IBookTextService> _bookTextServiceMock = null!;
        private Mock<IBookChatService> _bookChatServiceMock = null!;
        private Mock<IBookPdfService> _bookPdfServiceMock = null!;
        private BooksController _controller = null!;

        [SetUp]
        public void SetUp()
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
        [Test]
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
            Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
            var okResult = result.Result as OkObjectResult;
            Assert.That(okResult!.Value, Is.EqualTo(expectedBook));

            _bookServiceMock.Verify(s => s.GetByIdAsync(bookId), Times.Once);
        }

        /// <summary>
        /// Перевіряє, що GetBookById повертає 404 NotFound, коли книга не існує.
        /// </summary>
        [Test]
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
            Assert.That(result.Result, Is.InstanceOf<NotFoundObjectResult>());

            var notFoundResult = result.Result as NotFoundObjectResult;
            Assert.That(notFoundResult, Is.Not.Null);

            _bookServiceMock.Verify(s => s.GetByIdAsync(bookId), Times.Once);
        }

        /// <summary>
        /// Перевіряє, що GetBooks повертає 200 OK і передає сервісу всі параметри пагінації, пошуку, жанру та сортування.
        /// </summary>
        [Test]
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
            Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());

            var okResult = result.Result as OkObjectResult;
            Assert.That(okResult!.Value, Is.EqualTo(expectedResponse));

            // Перевіряємо, що сервіс був викликаний саме з цими параметрами
            _bookServiceMock.Verify(
                s => s.GetAllAsync(page, pageSize, search, genreId, sortBy, sortOrder),
                Times.Once);
        }
    }
}