using System.Net;
using System.Text.Json;
using Xunit;

namespace LibraryAPI.Module3Tests;

public class GenresApiTests : IClassFixture<LibraryApiFactory>
{
    private readonly HttpClient _client;

    public GenresApiTests(LibraryApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetGenres_ReturnsSeededGenres_AndUsesCache()
    {
        var response = await _client.GetAsync("/api/v1/genres");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(json.RootElement.GetArrayLength() > 0);

        response = await _client.GetAsync("/api/v1/genres");
        Assert.Equal("HIT", response.Headers.GetValues("X-Cache").Single());
    }
}
