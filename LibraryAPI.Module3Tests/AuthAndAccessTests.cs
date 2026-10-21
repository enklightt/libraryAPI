using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LibraryAPI.Module3Tests;

public class AuthAndAccessTests : IClassFixture<LibraryApiFactory>
{
    private readonly LibraryApiFactory _factory;
    private readonly HttpClient _client;

    public AuthAndAccessTests(LibraryApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsTokens()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = "admin@library.com",
            password = "admin123"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.False(string.IsNullOrWhiteSpace(json.RootElement.GetProperty("accessToken").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(json.RootElement.GetProperty("refreshToken").GetString()));
        Assert.Equal("admin", json.RootElement.GetProperty("user").GetProperty("role").GetString());
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = "admin@library.com",
            password = "wrong-password"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithInvalidEmailFormat_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = "not-an-email",
            password = "whatever"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task FriendProfile_ForNonFriend_ReturnsForbidden()
    {
        var token = await TestHelpers.LoginAsync(_client, "admin@library.com", "admin123");
        using var client = _factory.CreateClient().CreateAuthorizedClient(token);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<LibraryAPI.Data.AppDbContext>();
        var mariaId = context.Users.Single(u => u.Email == "maria@example.com").Id;

        var response = await client.GetAsync($"/api/v1/friends/{mariaId}/profile");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task FriendProfile_ForSelf_ReturnsOk()
    {
        var token = await TestHelpers.LoginAsync(_client, "admin@library.com", "admin123");
        using var client = _factory.CreateClient().CreateAuthorizedClient(token);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<LibraryAPI.Data.AppDbContext>();
        var adminId = context.Users.Single(u => u.Email == "admin@library.com").Id;

        var response = await client.GetAsync($"/api/v1/friends/{adminId}/profile");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
