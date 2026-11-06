using LibraryAPI.Data;
using LibraryAPI.DTOs;
using LibraryAPI.Models;
using LibraryAPI.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LibraryAPI.Tests;

/// <summary>Unit-тести бізнес-логіки відгуків (EF Core InMemory).</summary>
public class ReviewServiceTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static async Task SeedBookAndUserAsync(AppDbContext context)
    {
        var role = new Role { Id = 2, Name = "customer" };
        var user = new User
        {
            Id = "user-1",
            Name = "Test User",
            Email = "test@example.com",
            Password = "hash",
            Role = role
        };
        var book = new Book
        {
            Id = "book-1",
            Title = "Test Book",
            Author = "Test Author",
            Isbn = "isbn-1"
        };

        context.AddRange(role, user, book);
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task CreateOrUpdateAsync_WhenReviewIsNew_CreatesReviewAndRecalculatesRating()
    {
        await using var context = CreateContext();
        await SeedBookAndUserAsync(context);
        var service = new ReviewService(context);

        var result = await service.CreateOrUpdateAsync(
            "book-1", "user-1", new CreateReviewDto { Rating = 5, Text = "Great" });

        Assert.True(result.Success);
        Assert.Null(result.Error);
        Assert.NotNull(result.Review);
        Assert.Equal("Test User", result.Review!.UserName);
        Assert.Equal(5, result.Review.Rating);
        Assert.Equal(5m, (await context.Books.FindAsync("book-1"))!.Rating);
    }

    [Fact]
    public async Task CreateOrUpdateAsync_WhenReviewExists_UpdatesInsteadOfDuplicating()
    {
        await using var context = CreateContext();
        await SeedBookAndUserAsync(context);
        context.Reviews.Add(new Review
        {
            Id = "review-1",
            BookId = "book-1",
            UserId = "user-1",
            Rating = 3,
            Text = "Old"
        });
        await context.SaveChangesAsync();
        var service = new ReviewService(context);

        var result = await service.CreateOrUpdateAsync(
            "book-1", "user-1", new CreateReviewDto { Rating = 4, Text = "Updated" });

        Assert.True(result.Success);
        Assert.Equal("review-1", result.Review!.Id);
        Assert.Equal(4, result.Review.Rating);
        Assert.Equal(1, await context.Reviews.CountAsync());
        Assert.Equal(4m, (await context.Books.FindAsync("book-1"))!.Rating);
    }

    [Fact]
    public async Task CreateOrUpdateAsync_WhenBookDoesNotExist_ReturnsError()
    {
        await using var context = CreateContext();
        var service = new ReviewService(context);

        var result = await service.CreateOrUpdateAsync(
            "missing-book", "user-1", new CreateReviewDto { Rating = 5 });

        Assert.False(result.Success);
        Assert.Equal("Книгу не знайдено", result.Error);
        Assert.Null(result.Review);
    }

    [Fact]
    public async Task DeleteAsync_WhenReviewExists_RemovesItAndClearsRating()
    {
        await using var context = CreateContext();
        await SeedBookAndUserAsync(context);
        context.Reviews.Add(new Review
        {
            Id = "review-1",
            BookId = "book-1",
            UserId = "user-1",
            Rating = 4
        });
        await context.SaveChangesAsync();
        var service = new ReviewService(context);

        var result = await service.DeleteAsync("book-1", "user-1");

        Assert.True(result.Success);
        Assert.False(await context.Reviews.AnyAsync());
        Assert.Null((await context.Books.FindAsync("book-1"))!.Rating);
    }

    [Fact]
    public async Task DeleteAsync_WhenReviewIsMissing_ReturnsError()
    {
        await using var context = CreateContext();
        var service = new ReviewService(context);

        var result = await service.DeleteAsync("book-1", "user-1");

        Assert.False(result.Success);
        Assert.Equal("Відгук не знайдено", result.Error);
    }

    [Fact]
    public async Task DeleteByIdAsync_WhenReviewExists_RemovesItAndClearsRating()
    {
        await using var context = CreateContext();
        await SeedBookAndUserAsync(context);
        context.Reviews.Add(new Review
        {
            Id = "review-1",
            BookId = "book-1",
            UserId = "user-1",
            Rating = 5
        });
        await context.SaveChangesAsync();
        var service = new ReviewService(context);

        var result = await service.DeleteByIdAsync("book-1", "review-1");

        Assert.True(result.Success);
        Assert.False(await context.Reviews.AnyAsync());
        Assert.Null((await context.Books.FindAsync("book-1"))!.Rating);
    }

    [Fact]
    public async Task DeleteByIdAsync_WhenReviewIsMissing_ReturnsError()
    {
        await using var context = CreateContext();
        var service = new ReviewService(context);

        var result = await service.DeleteByIdAsync("book-1", "missing-review");

        Assert.False(result.Success);
        Assert.Equal("Відгук не знайдено", result.Error);
    }

    [Fact]
    public async Task GetUserReviewsAsync_ReturnsReviewsWithBookData()
    {
        await using var context = CreateContext();
        await SeedBookAndUserAsync(context);
        context.Reviews.Add(new Review
        {
            Id = "review-1",
            BookId = "book-1",
            UserId = "user-1",
            Rating = 5
        });
        await context.SaveChangesAsync();
        var service = new ReviewService(context);

        var result = (await service.GetUserReviewsAsync("user-1")).Single();

        Assert.Equal("review-1", result.Id);
        Assert.Equal("Test Book", result.BookTitle);
        Assert.Equal(5, result.Rating);
    }

    [Fact]
    public async Task GetBookReviewsAsync_ReturnsOnlyBookReviewsOrderedByDate()
    {
        await using var context = CreateContext();
        await SeedBookAndUserAsync(context);
        var secondUser = new User
        {
            Id = "user-2",
            Name = "Second User",
            Email = "second@example.com",
            Password = "hash",
            RoleId = 2
        };
        var secondBook = new Book
        {
            Id = "book-2",
            Title = "Other Book",
            Author = "Author",
            Isbn = "isbn-2"
        };
        context.AddRange(secondUser, secondBook);
        context.Reviews.AddRange(
            new Review { BookId = "book-1", UserId = "user-1", Rating = 3, CreatedAt = DateTime.UtcNow.AddDays(-2) },
            new Review { BookId = "book-1", UserId = "user-2", Rating = 5, CreatedAt = DateTime.UtcNow },
            new Review { BookId = "book-2", UserId = "user-1", Rating = 4 });
        await context.SaveChangesAsync();
        var service = new ReviewService(context);

        var result = (await service.GetBookReviewsAsync("book-1")).ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal("Second User", result[0].UserName);
        Assert.Equal(5, result[0].Rating);
    }
}
