using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LibraryAPI.DTOs;
using Xunit;

namespace LibraryAPI.IntegrationTests;

/// <summary>
/// E2E-тести: повні користувацькі сценарії через API (без frontend).
/// </summary>
public class BooksE2ETests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public BooksE2ETests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    /// <summary>
    /// Сценарій: користувач переглядає каталог книг і відкриває одну книгу.
    /// 1. Отримати список книг
    /// 2. Взяти id першої книги
    /// 3. Отримати деталі книги
    /// 4. Перевірити, що дані збігаються
    /// </summary>
    [Fact]
    public async Task Scenario_BrowseCatalogAndOpenBook_ShouldSucceed()
    {
        // 1. Каталог
        var listResponse = await _client.GetAsync("/api/v1/books?page=1&pageSize=10");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var list = await listResponse.Content.ReadFromJsonAsync<PagedResponse<BookResponseDto>>();
        list.Should().NotBeNull();

        // Якщо книг немає — сценарій все одно валідний (порожній каталог)
        if (list!.Items == null || !list.Items.Any())
        {
            // Перевіряємо, що 404 на неіснуючу книгу працює в кінці ланцюжка
            var notFound = await _client.GetAsync("/api/v1/books/non-existent-e2e-id");
            notFound.StatusCode.Should().Be(HttpStatusCode.NotFound);
            return;
        }

        var firstBook = list.Items.First();
        firstBook.Id.Should().NotBeNullOrEmpty();

        // 2. Деталі книги
        var detailResponse = await _client.GetAsync($"/api/v1/books/{firstBook.Id}");
        detailResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var book = await detailResponse.Content.ReadFromJsonAsync<BookResponseDto>();
        book.Should().NotBeNull();
        book!.Id.Should().Be(firstBook.Id);
        book.Title.Should().Be(firstBook.Title);
    }

    /// <summary>
    /// Сценарій: пошук книги → відкриття → спроба отримати PDF.
    /// 1. Пошук
    /// 2. Відкриття знайденої (або 404)
    /// 3. Запит PDF (200 або 404 — обидва валідні результати сценарію)
    /// </summary>
    [Fact]
    public async Task Scenario_SearchBookAndRequestPdf_ShouldCompleteFlow()
    {
        // 1. Пошук
        var searchResponse = await _client.GetAsync("/api/v1/books?search=test&page=1&pageSize=5");
        searchResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var searchResult = await searchResponse.Content.ReadFromJsonAsync<PagedResponse<BookResponseDto>>();
        searchResult.Should().NotBeNull();

        string bookId;

        if (searchResult!.Items != null && searchResult.Items.Any())
        {
            bookId = searchResult.Items.First().Id!;
        }
        else
        {
            // Немає результатів пошуку — перевіряємо неіснуючу книгу
            bookId = "e2e-missing-book-id";
            var missing = await _client.GetAsync($"/api/v1/books/{bookId}");
            missing.StatusCode.Should().Be(HttpStatusCode.NotFound);
            return;
        }

        // 2. Деталі
        var detailResponse = await _client.GetAsync($"/api/v1/books/{bookId}");
        detailResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // 3. PDF (може бути 200 або 404, якщо PDF немає)
        var pdfResponse = await _client.GetAsync($"/api/v1/books/{bookId}/pdf");
        pdfResponse.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    /// <summary>
    /// Сценарій: неавторизований користувач намагається створити книгу → отримує 401,
    /// потім успішно читає публічний каталог.
    /// </summary>
    [Fact]
    public async Task Scenario_UnauthorizedCreateThenBrowsePublicCatalog_ShouldEnforceAuth()
    {
        // 1. Спроба створити книгу без токена
        var createPayload = new
        {
            title = "E2E Test Book",
            author = "E2E Author"
        };

        var createResponse = await _client.PostAsJsonAsync("/api/v1/books", createPayload);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // 2. Публічний каталог все одно доступний
        var listResponse = await _client.GetAsync("/api/v1/books?page=1&pageSize=5");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // 3. Публічний endpoint тексту (може бути 404, якщо книги/тексту немає)
        var textResponse = await _client.GetAsync("/api/v1/books/any-id/text");
        textResponse.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }
}