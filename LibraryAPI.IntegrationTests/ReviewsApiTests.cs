using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LibraryAPI.Data;
using LibraryAPI.DTOs;
using LibraryAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LibraryAPI.IntegrationTests;

/// <summary>REST API та інтеграційні тести модуля відгуків.</summary>
public class ReviewsApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ReviewsApiTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetReviews_Returns200AndJsonArray()
    {
        var response = await _client.GetAsync("/api/v1/books/no-reviews-book/reviews");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");

        var reviews = await response.Content.ReadFromJsonAsync<List<ReviewResponseDto>>();
        reviews.Should().NotBeNull();
        reviews.Should().BeEmpty();
    }

    [Fact]
    public async Task GetReviews_WithSeededReview_ReturnsReviewFromDatabase()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            if (!await db.Roles.AnyAsync(r => r.Id == 2))
                db.Roles.Add(new Role { Id = 2, Name = "customer" });

            db.Users.Add(new User
            {
                Id = "it-user",
                Name = "IT User",
                Email = "it@example.com",
                Password = "hash",
                RoleId = 2
            });
            db.Books.Add(new Book
            {
                Id = "it-book",
                Title = "IT Book",
                Author = "IT Author",
                Isbn = "it-isbn"
            });
            db.Reviews.Add(new Review
            {
                Id = "it-review",
                BookId = "it-book",
                UserId = "it-user",
                Rating = 4,
                Text = "Nice"
            });
            await db.SaveChangesAsync();
        }

        var response = await _client.GetAsync("/api/v1/books/it-book/reviews");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var reviews = await response.Content.ReadFromJsonAsync<List<ReviewResponseDto>>();
        reviews.Should().ContainSingle(r => r.Id == "it-review" && r.Rating == 4);
    }

    [Fact]
    public async Task CreateReview_WithoutToken_Returns401()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/books/it-book/reviews", new { rating = 5, text = "test" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteReview_WithoutToken_Returns401()
    {
        var response = await _client.DeleteAsync("/api/v1/books/it-book/reviews");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMyReviews_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync("/api/v1/reviews/my");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
