using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace LibraryAPI.Module3Tests;

public class BooksApiTests : IClassFixture<LibraryApiFactory>
{
    private readonly LibraryApiFactory _factory;
    private readonly HttpClient _client;

    public BooksApiTests(LibraryApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetBooks_ReturnsPagedResult_AndCacheHeader()
    {
        var response = await _client.GetAsync("/api/v1/books?page=1&pageSize=2");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains("X-Cache"));

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(1, json.RootElement.GetProperty("page").GetInt32());
        Assert.Equal(2, json.RootElement.GetProperty("pageSize").GetInt32());
        Assert.True(json.RootElement.GetProperty("totalCount").GetInt32() > 0);

        response = await _client.GetAsync("/api/v1/books?page=1&pageSize=2");
        Assert.Equal("HIT", response.Headers.GetValues("X-Cache").Single());
    }

    [Fact]
    public async Task GetBooks_InvalidPageSize_ReturnsBadRequest()
    {
        var response = await _client.GetAsync("/api/v1/books?page=0&pageSize=1000");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetBooksV2_ReturnsEnvelope()
    {
        var response = await _client.GetAsync("/api/v2/books?page=1&pageSize=1");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("2.0", response.Headers.GetValues("api-supported-versions").Single());

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("2.0", json.RootElement.GetProperty("meta").GetProperty("apiVersion").GetString());
        Assert.Equal(1, json.RootElement.GetProperty("meta").GetProperty("pageSize").GetInt32());
    }

    [Fact]
    public async Task CreateBook_InvalidatesListCache()
    {
        var adminToken = await TestHelpers.LoginAsync(_client, "admin@library.com", "admin123");
        using var adminClient = _factory.CreateClient().CreateAuthorizedClient(adminToken);

        var before = await _client.GetFromJsonAsync<JsonElement>("/api/v1/books?page=1&pageSize=100");
        var beforeTotal = before.GetProperty("totalCount").GetInt32();

        var createResponse = await adminClient.PostAsJsonAsync("/api/v1/books", new
        {
            title = "Тестова книга",
            author = "Тестовий автор",
            isbn = Guid.NewGuid().ToString("N")[..20],
            totalCopies = 1,
            pdfUrl = "../../appsettings.json"
        });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var after = await _client.GetFromJsonAsync<JsonElement>("/api/v1/books?page=1&pageSize=100");
        Assert.Equal(beforeTotal + 1, after.GetProperty("totalCount").GetInt32());
    }

    [Fact]
    public async Task GetPdf_WithPathTraversal_ReturnsNotFound()
    {
        var adminToken = await TestHelpers.LoginAsync(_client, "admin@library.com", "admin123");
        using var adminClient = _factory.CreateClient().CreateAuthorizedClient(adminToken);

        var createResponse = await adminClient.PostAsJsonAsync("/api/v1/books", new
        {
            title = "Path traversal",
            author = "Test",
            isbn = Guid.NewGuid().ToString("N")[..20],
            totalCopies = 1,
            pdfUrl = "../../appsettings.json"
        });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        using var createdJson = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
        var bookId = createdJson.RootElement.GetProperty("id").GetString();

        var pdfResponse = await _client.GetAsync($"/api/v1/books/{bookId}/pdf");
        Assert.Equal(HttpStatusCode.NotFound, pdfResponse.StatusCode);
    }

    [Fact]
    public async Task GetMissingBook_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/v1/books/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
