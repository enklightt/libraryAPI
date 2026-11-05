using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using LibraryAPI.Data;
using LibraryAPI.DTOs;
using LibraryAPI.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace LibraryAPI.Tests;

public class FavoritesApiIntegrationTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;

    public FavoritesApiIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync() => await ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetFavorites_WhenUnauthorized_Returns401()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/api/v1/favorites");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AddFavorite_WhenAuthenticated_ReturnsOkAndPersistsFavorite()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");

        var book = new Book
        {
            Id = "api-book-1",
            Title = "API Book",
            Author = "Author",
            Isbn = "api-1",
            IsActive = true
        };

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Books.Add(book);
            await db.SaveChangesAsync();
        }

        var response = await client.PostAsync($"/api/v1/favorites/{book.Id}", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var favorite = await db.Favorites.SingleOrDefaultAsync(f => f.UserId == "test-user" && f.BookId == book.Id);
            Assert.NotNull(favorite);
        }
    }

    [Fact]
    public async Task RemoveFavorite_WhenNotFound_Returns404()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");

        var response = await client.DeleteAsync("/api/v1/favorites/missing-book");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetFavorites_WhenAuthenticatedAndEmpty_ReturnsEmptyList()
    {
        var client = CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/v1/favorites");
        var favorites = await response.Content.ReadFromJsonAsync<List<FavoriteBookDto>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(favorites);
        Assert.Empty(favorites);
    }

    [Fact]
    public async Task GetFavorites_WhenFavoritesBelongToDifferentUsers_ReturnsOnlyCurrentUsersBooks()
    {
        var currentBook = CreateBook("integration-list-current");
        var otherBook = CreateBook("integration-list-other");
        await SeedFavoriteAsync("test-user", currentBook);
        await SeedFavoriteAsync("other-user", otherBook);
        var client = CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/v1/favorites");
        var favorites = await response.Content.ReadFromJsonAsync<List<FavoriteBookDto>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(favorites);
        Assert.Contains(favorites, favorite => favorite.Id == currentBook.Id);
        Assert.DoesNotContain(favorites, favorite => favorite.Id == otherBook.Id);
    }

    [Fact]
    public async Task CheckFavorite_WhenFavoriteExists_ReturnsTrue()
    {
        var book = CreateBook("integration-check-true");
        await SeedFavoriteAsync("test-user", book);
        var client = CreateAuthenticatedClient();

        var response = await client.GetAsync($"/api/v1/favorites/check/{book.Id}");
        var body = await response.Content.ReadFromJsonAsync<FavoriteCheckResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(body?.IsFavorite);
    }

    [Fact]
    public async Task CheckFavorite_WhenFavoriteDoesNotExist_ReturnsFalse()
    {
        var client = CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/v1/favorites/check/integration-check-false");
        var body = await response.Content.ReadFromJsonAsync<FavoriteCheckResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(body?.IsFavorite);
    }

    [Fact]
    public async Task AddFavorite_WhenBookDoesNotExist_ReturnsBadRequest()
    {
        var client = CreateAuthenticatedClient();

        var response = await client.PostAsync("/api/v1/favorites/integration-missing-book", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AddFavorite_WhenBookIsInactive_ReturnsBadRequest()
    {
        var book = CreateBook("integration-inactive", isActive: false);
        await SeedBookAsync(book);
        var client = CreateAuthenticatedClient();

        var response = await client.PostAsync($"/api/v1/favorites/{book.Id}", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AddFavorite_WhenAlreadyFavorited_ReturnsBadRequestAndKeepsSingleRow()
    {
        var book = CreateBook("integration-duplicate");
        await SeedFavoriteAsync("test-user", book);
        var client = CreateAuthenticatedClient();

        var response = await client.PostAsync($"/api/v1/favorites/{book.Id}", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(1, await db.Favorites.CountAsync(f => f.UserId == "test-user" && f.BookId == book.Id));
    }

    [Fact]
    public async Task RemoveFavorite_WhenFavoriteExists_ReturnsOkAndDeletesRow()
    {
        var book = CreateBook("integration-remove-existing");
        await SeedFavoriteAsync("test-user", book);
        var client = CreateAuthenticatedClient();

        var response = await client.DeleteAsync($"/api/v1/favorites/{book.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.False(await db.Favorites.AnyAsync(f => f.UserId == "test-user" && f.BookId == book.Id));
    }

    [Fact]
    public async Task AddFavorite_WhenUnauthorized_Returns401()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.PostAsync("/api/v1/favorites/integration-unauthorized", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private HttpClient CreateAuthenticatedClient(string userId = "test-user")
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test", userId);
        return client;
    }

    private async Task ResetDatabaseAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Favorites.RemoveRange(db.Favorites);
        db.Books.RemoveRange(db.Books);
        await db.SaveChangesAsync();
    }

    private async Task SeedFavoriteAsync(string userId, Book book)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Books.Add(book);
        db.Favorites.Add(new Favorite { UserId = userId, BookId = book.Id, Book = book });
        await db.SaveChangesAsync();
    }

    private async Task SeedBookAsync(Book book)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Books.Add(book);
        await db.SaveChangesAsync();
    }

    private static Book CreateBook(string id, bool isActive = true) => new()
    {
        Id = id,
        Title = id,
        Author = "Integration Author",
        Isbn = id,
        IsActive = isActive
    };
}

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"FavoritesIntegrationTests-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<AppDbContext>();

            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));

            services.AddAuthentication("Test")
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });
        });
    }
}

public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authorization = Request.Headers.Authorization.ToString().Split(' ', 2);
        if (!string.Equals(authorization[0], "Test", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var userId = authorization.ElementAtOrDefault(1) ?? "test-user";
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Name, userId)
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

public sealed class FavoriteCheckResponse
{
    public bool IsFavorite { get; set; }
}
