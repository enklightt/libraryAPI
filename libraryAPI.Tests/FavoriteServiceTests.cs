using LibraryAPI.Data;
using LibraryAPI.Models;
using LibraryAPI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LibraryAPI.Tests;

public class FavoriteServiceTests
{
    private static AppDbContext CreateContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        var context = new AppDbContext(options);
        return context;
    }

    [Fact]
    public async Task GetUserFavoritesAsync_ReturnsOnlyUserFavorites()
    {
        await using var context = CreateContext(nameof(GetUserFavoritesAsync_ReturnsOnlyUserFavorites));
        var book1 = new Book { Id = "book-1", Title = "Alpha", Author = "A", Isbn = "123", IsActive = true };
        var book2 = new Book { Id = "book-2", Title = "Beta", Author = "B", Isbn = "456", IsActive = true };
        context.Books.AddRange(book1, book2);
        context.Favorites.AddRange(
            new Favorite { UserId = "user-1", BookId = "book-1", Book = book1 },
            new Favorite { UserId = "user-2", BookId = "book-2", Book = book2 }
        );
        await context.SaveChangesAsync();

        var service = new FavoriteService(context, NullLogger<FavoriteService>.Instance);

        var result = (await service.GetUserFavoritesAsync("user-1")).ToList();

        Assert.Single(result);
        Assert.Equal("book-1", result[0].Id);
        Assert.Equal("Alpha", result[0].Title);
    }

    [Fact]
    public async Task GetUserFavoritesAsync_WhenNoFavorites_ReturnsEmpty()
    {
        await using var context = CreateContext(nameof(GetUserFavoritesAsync_WhenNoFavorites_ReturnsEmpty));
        var service = new FavoriteService(context, NullLogger<FavoriteService>.Instance);

        var result = await service.GetUserFavoritesAsync("missing-user");

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetUserFavoritesAsync_MapsBookPropertiesCorrectly()
    {
        await using var context = CreateContext(nameof(GetUserFavoritesAsync_MapsBookPropertiesCorrectly));
        var book = new Book
        {
            Id = "book-map",
            Title = "Mapped Title",
            Author = "Map Author",
            Isbn = "map-123",
            GenreId = "genre-1",
            TotalCopies = 7,
            AvailableCopies = 3,
            PdfUrl = "https://example.com/pdf",
            Rating = 4.7m,
            IsActive = true
        };
        context.Books.Add(book);
        context.Favorites.Add(new Favorite { UserId = "user-map", BookId = book.Id, Book = book, AddedAt = DateTime.UtcNow.AddDays(-2) });
        await context.SaveChangesAsync();

        var service = new FavoriteService(context, NullLogger<FavoriteService>.Instance);

        var result = (await service.GetUserFavoritesAsync("user-map")).Single();

        Assert.Equal(book.Id, result.Id);
        Assert.Equal(book.Title, result.Title);
        Assert.Equal(book.Author, result.Author);
        Assert.Equal(book.Isbn, result.Isbn);
        Assert.Equal(book.GenreId, result.GenreId);
        Assert.Equal(book.TotalCopies, result.TotalCopies);
        Assert.Equal(book.AvailableCopies, result.AvailableCopies);
        Assert.Equal(book.PdfUrl, result.PdfUrl);
        Assert.Equal(book.Rating, result.Rating);
    }

    [Fact]
    public async Task AddToFavoritesAsync_WhenBookExistsAndIsNotFavorite_ReturnsSuccess()
    {
        await using var context = CreateContext(nameof(AddToFavoritesAsync_WhenBookExistsAndIsNotFavorite_ReturnsSuccess));
        var book = new Book { Id = "book-add", Title = "Add Title", Author = "A", Isbn = "abc", IsActive = true };
        context.Books.Add(book);
        await context.SaveChangesAsync();

        var service = new FavoriteService(context, NullLogger<FavoriteService>.Instance);

        var result = await service.AddToFavoritesAsync("user-1", "book-add");

        Assert.True(result.Success);
        Assert.Null(result.Error);
        Assert.True(await service.IsFavoriteAsync("user-1", "book-add"));
    }

    [Fact]
    public async Task AddToFavoritesAsync_WhenBookDoesNotExist_ReturnsFailure()
    {
        await using var context = CreateContext(nameof(AddToFavoritesAsync_WhenBookDoesNotExist_ReturnsFailure));
        var service = new FavoriteService(context, NullLogger<FavoriteService>.Instance);

        var result = await service.AddToFavoritesAsync("user-1", "missing-book");

        Assert.False(result.Success);
        Assert.Equal("Книгу не знайдено", result.Error);
    }

    [Fact]
    public async Task AddToFavoritesAsync_WhenBookInactive_ReturnsFailure()
    {
        await using var context = CreateContext(nameof(AddToFavoritesAsync_WhenBookInactive_ReturnsFailure));
        var book = new Book { Id = "book-inactive", Title = "Hidden", Author = "A", Isbn = "inactive", IsActive = false };
        context.Books.Add(book);
        await context.SaveChangesAsync();

        var service = new FavoriteService(context, NullLogger<FavoriteService>.Instance);

        var result = await service.AddToFavoritesAsync("user-1", "book-inactive");

        Assert.False(result.Success);
        Assert.Equal("Книгу не знайдено", result.Error);
    }

    [Fact]
    public async Task AddToFavoritesAsync_WhenAlreadyFavorite_ReturnsFailure()
    {
        await using var context = CreateContext(nameof(AddToFavoritesAsync_WhenAlreadyFavorite_ReturnsFailure));
        var book = new Book { Id = "book-already", Title = "Already", Author = "A", Isbn = "dup", IsActive = true };
        context.Books.Add(book);
        context.Favorites.Add(new Favorite { UserId = "user-1", BookId = "book-already", Book = book });
        await context.SaveChangesAsync();

        var service = new FavoriteService(context, NullLogger<FavoriteService>.Instance);

        var result = await service.AddToFavoritesAsync("user-1", "book-already");

        Assert.False(result.Success);
        Assert.Equal("Книга вже у улюблених", result.Error);
    }

    [Fact]
    public async Task RemoveFromFavoritesAsync_WhenFavoriteExists_RemovesFavorite()
    {
        await using var context = CreateContext(nameof(RemoveFromFavoritesAsync_WhenFavoriteExists_RemovesFavorite));
        var book = new Book { Id = "book-remove", Title = "Delete Me", Author = "A", Isbn = "rem", IsActive = true };
        context.Books.Add(book);
        context.Favorites.Add(new Favorite { UserId = "user-1", BookId = "book-remove", Book = book });
        await context.SaveChangesAsync();

        var service = new FavoriteService(context, NullLogger<FavoriteService>.Instance);

        var result = await service.RemoveFromFavoritesAsync("user-1", "book-remove");

        Assert.True(result.Success);
        Assert.Null(result.Error);
        Assert.False(await service.IsFavoriteAsync("user-1", "book-remove"));
    }

    [Fact]
    public async Task RemoveFromFavoritesAsync_WhenFavoriteMissing_ReturnsFailure()
    {
        await using var context = CreateContext(nameof(RemoveFromFavoritesAsync_WhenFavoriteMissing_ReturnsFailure));
        var service = new FavoriteService(context, NullLogger<FavoriteService>.Instance);

        var result = await service.RemoveFromFavoritesAsync("user-1", "missing-book");

        Assert.False(result.Success);
        Assert.Equal("Книга не знайдена у улюблених", result.Error);
    }

    [Fact]
    public async Task IsFavoriteAsync_WhenFavoriteExists_ReturnsTrue()
    {
        await using var context = CreateContext(nameof(IsFavoriteAsync_WhenFavoriteExists_ReturnsTrue));
        var book = new Book { Id = "book-check", Title = "Check", Author = "A", Isbn = "check", IsActive = true };
        context.Books.Add(book);
        context.Favorites.Add(new Favorite { UserId = "user-1", BookId = "book-check", Book = book });
        await context.SaveChangesAsync();

        var service = new FavoriteService(context, NullLogger<FavoriteService>.Instance);

        var result = await service.IsFavoriteAsync("user-1", "book-check");

        Assert.True(result);
    }

    [Fact]
    public async Task IsFavoriteAsync_WhenFavoriteMissing_ReturnsFalse()
    {
        await using var context = CreateContext(nameof(IsFavoriteAsync_WhenFavoriteMissing_ReturnsFalse));
        var service = new FavoriteService(context, NullLogger<FavoriteService>.Instance);

        var result = await service.IsFavoriteAsync("user-1", "book-missing");

        Assert.False(result);
    }

    [Fact]
    public async Task GetUserFavoritesAsync_WhenUserIdIsEmpty_ThrowsArgumentException()
    {
        await using var context = CreateContext(nameof(GetUserFavoritesAsync_WhenUserIdIsEmpty_ThrowsArgumentException));
        var service = new FavoriteService(context, NullLogger<FavoriteService>.Instance);

        await Assert.ThrowsAsync<ArgumentException>(() => service.GetUserFavoritesAsync("   "));
    }

    [Fact]
    public async Task AddToFavoritesAsync_WhenUserIdIsEmpty_ThrowsArgumentException()
    {
        await using var context = CreateContext(nameof(AddToFavoritesAsync_WhenUserIdIsEmpty_ThrowsArgumentException));
        var service = new FavoriteService(context, NullLogger<FavoriteService>.Instance);

        await Assert.ThrowsAsync<ArgumentException>(() => service.AddToFavoritesAsync("   ", "book-1"));
    }

    [Fact]
    public async Task RemoveFromFavoritesAsync_WhenBookIdIsEmpty_ThrowsArgumentException()
    {
        await using var context = CreateContext(nameof(RemoveFromFavoritesAsync_WhenBookIdIsEmpty_ThrowsArgumentException));
        var service = new FavoriteService(context, NullLogger<FavoriteService>.Instance);

        await Assert.ThrowsAsync<ArgumentException>(() => service.RemoveFromFavoritesAsync("user-1", "   "));
    }

    [Fact]
    public async Task GetUserFavoritesAsync_WhenUserIdIsNull_ThrowsArgumentNullException()
    {
        await using var context = CreateContext(nameof(GetUserFavoritesAsync_WhenUserIdIsNull_ThrowsArgumentNullException));
        var service = new FavoriteService(context, NullLogger<FavoriteService>.Instance);

        await Assert.ThrowsAsync<ArgumentNullException>(() => service.GetUserFavoritesAsync(null!));
    }

    [Fact]
    public async Task AddToFavoritesAsync_WhenBookIdIsEmpty_ThrowsArgumentException()
    {
        await using var context = CreateContext(nameof(AddToFavoritesAsync_WhenBookIdIsEmpty_ThrowsArgumentException));
        var service = new FavoriteService(context, NullLogger<FavoriteService>.Instance);

        await Assert.ThrowsAsync<ArgumentException>(() => service.AddToFavoritesAsync("user-1", " "));
    }

    [Fact]
    public async Task AddToFavoritesAsync_WhenBookIdIsNull_ThrowsArgumentNullException()
    {
        await using var context = CreateContext(nameof(AddToFavoritesAsync_WhenBookIdIsNull_ThrowsArgumentNullException));
        var service = new FavoriteService(context, NullLogger<FavoriteService>.Instance);

        await Assert.ThrowsAsync<ArgumentNullException>(() => service.AddToFavoritesAsync("user-1", null!));
    }

    [Fact]
    public async Task RemoveFromFavoritesAsync_WhenUserIdIsEmpty_ThrowsArgumentException()
    {
        await using var context = CreateContext(nameof(RemoveFromFavoritesAsync_WhenUserIdIsEmpty_ThrowsArgumentException));
        var service = new FavoriteService(context, NullLogger<FavoriteService>.Instance);

        await Assert.ThrowsAsync<ArgumentException>(() => service.RemoveFromFavoritesAsync(" ", "book-1"));
    }

    [Fact]
    public async Task RemoveFromFavoritesAsync_WhenUserIdIsNull_ThrowsArgumentNullException()
    {
        await using var context = CreateContext(nameof(RemoveFromFavoritesAsync_WhenUserIdIsNull_ThrowsArgumentNullException));
        var service = new FavoriteService(context, NullLogger<FavoriteService>.Instance);

        await Assert.ThrowsAsync<ArgumentNullException>(() => service.RemoveFromFavoritesAsync(null!, "book-1"));
    }

    [Fact]
    public async Task IsFavoriteAsync_WhenUserIdIsEmpty_ThrowsArgumentException()
    {
        await using var context = CreateContext(nameof(IsFavoriteAsync_WhenUserIdIsEmpty_ThrowsArgumentException));
        var service = new FavoriteService(context, NullLogger<FavoriteService>.Instance);

        await Assert.ThrowsAsync<ArgumentException>(() => service.IsFavoriteAsync(" ", "book-1"));
    }

    [Fact]
    public async Task IsFavoriteAsync_WhenBookIdIsNull_ThrowsArgumentNullException()
    {
        await using var context = CreateContext(nameof(IsFavoriteAsync_WhenBookIdIsNull_ThrowsArgumentNullException));
        var service = new FavoriteService(context, NullLogger<FavoriteService>.Instance);

        await Assert.ThrowsAsync<ArgumentNullException>(() => service.IsFavoriteAsync("user-1", null!));
    }
}
