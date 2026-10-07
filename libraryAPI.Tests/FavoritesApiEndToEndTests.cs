using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using LibraryAPI.Data;
using LibraryAPI.DTOs;
using LibraryAPI.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace LibraryAPI.Tests;

public class FavoritesApiEndToEndTests : IClassFixture<EndToEndWebApplicationFactory>
{
    private readonly EndToEndWebApplicationFactory _factory;

    public FavoritesApiEndToEndTests(EndToEndWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task UserCanRegisterLoginAddFavoriteAndSeeItInFavoritesList()
    {
        await ResetDatabaseAsync();
        using var client = _factory.CreateClient();
        var email = $"{Guid.NewGuid():N}@example.test";
        var accessToken = await RegisterAndLoginAsync(client, email);
        var book = await CreateBookAsync("e2e-list-book");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var addResponse = await client.PostAsync($"/api/v1/favorites/{book.Id}", null);
        var favoritesResponse = await client.GetAsync("/api/v1/favorites");
        var favorites = await favoritesResponse.Content.ReadFromJsonAsync<List<FavoriteBookDto>>();

        Assert.Equal(HttpStatusCode.OK, addResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, favoritesResponse.StatusCode);
        Assert.Contains(favorites!, favorite => favorite.Id == book.Id);
    }

    [Fact]
    public async Task UserCanAddCheckAndRemoveFavorite()
    {
        await ResetDatabaseAsync();
        using var client = _factory.CreateClient();
        var accessToken = await RegisterAndLoginAsync(client, $"{Guid.NewGuid():N}@example.test");
        var book = await CreateBookAsync("e2e-toggle-book");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var addResponse = await client.PostAsync($"/api/v1/favorites/{book.Id}", null);
        var addedCheck = await GetFavoriteCheckAsync(client, book.Id);
        var removeResponse = await client.DeleteAsync($"/api/v1/favorites/{book.Id}");
        var removedCheck = await GetFavoriteCheckAsync(client, book.Id);

        Assert.Equal(HttpStatusCode.OK, addResponse.StatusCode);
        Assert.True(addedCheck);
        Assert.Equal(HttpStatusCode.OK, removeResponse.StatusCode);
        Assert.False(removedCheck);
    }

    [Fact]
    public async Task UserCannotAddMissingBookAndFavoritesRemainEmpty()
    {
        await ResetDatabaseAsync();
        using var client = _factory.CreateClient();
        var accessToken = await RegisterAndLoginAsync(client, $"{Guid.NewGuid():N}@example.test");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var addResponse = await client.PostAsync("/api/v1/favorites/e2e-missing-book", null);
        var favoritesResponse = await client.GetAsync("/api/v1/favorites");
        var favorites = await favoritesResponse.Content.ReadFromJsonAsync<List<FavoriteBookDto>>();

        Assert.Equal(HttpStatusCode.BadRequest, addResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, favoritesResponse.StatusCode);
        Assert.Empty(favorites!);
    }

    private async Task<string> RegisterAndLoginAsync(HttpClient client, string email)
    {
        var registerResponse = await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterDto
        {
            Name = "E2E Reader",
            Email = email,
            Password = "Passw0rd!"
        });
        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginDto
        {
            Email = email,
            Password = "Passw0rd!"
        });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        using var json = JsonDocument.Parse(await loginResponse.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("accessToken").GetString()!;
    }

    private async Task<bool> GetFavoriteCheckAsync(HttpClient client, string bookId)
    {
        var response = await client.GetAsync($"/api/v1/favorites/check/{bookId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("isFavorite").GetBoolean();
    }

    private async Task<Book> CreateBookAsync(string id)
    {
        var book = new Book
        {
            Id = $"{id}-{Guid.NewGuid():N}",
            Title = "E2E Book",
            Author = "E2E Author",
            Isbn = Guid.NewGuid().ToString("N"),
            IsActive = true
        };

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Books.Add(book);
        await db.SaveChangesAsync();
        return book;
    }

    private async Task ResetDatabaseAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Favorites.RemoveRange(db.Favorites);
        db.RefreshTokens.RemoveRange(db.RefreshTokens);
        db.Users.RemoveRange(db.Users);
        db.Books.RemoveRange(db.Books);
        db.Roles.RemoveRange(db.Roles);
        db.Roles.Add(new Role { Id = 2, Name = "customer" });
        await db.SaveChangesAsync();
    }
}

public class EndToEndWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"FavoritesEndToEnd-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<AppDbContext>();
            services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(_databaseName));
        });
    }
}