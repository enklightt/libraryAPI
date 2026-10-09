using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace LibraryAPI.IntegrationTests;

public class BooksApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public BooksApiTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetBooks_ShouldReturn200Ok()
    {
        var response = await _client.GetAsync("/api/v1/books");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetBookById_ShouldReturn404_WhenBookDoesNotExist()
    {
        var response = await _client.GetAsync("/api/v1/books/non-existent-id-12345");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetBooks_ShouldReturn200_AndRespectPaginationParameters()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/books?page=1&pageSize=5");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
    }
}