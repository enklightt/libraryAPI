using System.Security.Claims;
using LibraryAPI.Controllers;
using LibraryAPI.DTOs;
using LibraryAPI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace LibraryAPI.Tests.Controllers;

/// <summary>Тести контролера відгуків із використанням Moq.</summary>
public class ReviewsControllerTests
{
    private readonly Mock<IReviewService> _service = new();

    private ReviewsController CreateController(string userId = "user-1")
    {
        var identity = new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, userId) }, "Test");
        var controller = new ReviewsController(_service.Object);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity)
            }
        };

        return controller;
    }

    [Fact]
    public async Task GetReviews_ReturnsOkWithServiceData()
    {
        var reviews = new List<ReviewResponseDto>
        {
            new() { Id = "review-1", BookId = "book-1", UserId = "user-1", UserName = "Test", Rating = 5 }
        };
        _service.Setup(s => s.GetBookReviewsAsync("book-1")).ReturnsAsync(reviews);
        var controller = CreateController();

        var result = await controller.GetReviews("book-1");

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(reviews, ok.Value);
        _service.Verify(s => s.GetBookReviewsAsync("book-1"), Times.Once);
    }

    [Fact]
    public async Task CreateOrUpdate_WhenServiceFails_ReturnsBadRequest()
    {
        _service
            .Setup(s => s.CreateOrUpdateAsync("book-1", "user-1", It.IsAny<CreateReviewDto>()))
            .ReturnsAsync((false, "Книгу не знайдено", (ReviewResponseDto?)null));
        var controller = CreateController();

        var result = await controller.CreateOrUpdate(
            "book-1", new CreateReviewDto { Rating = 5 });

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateOrUpdate_WhenServiceSucceeds_ReturnsOkWithReview()
    {
        var review = new ReviewResponseDto
        {
            Id = "review-1",
            BookId = "book-1",
            UserId = "user-1",
            UserName = "Test",
            Rating = 5
        };
        _service
            .Setup(s => s.CreateOrUpdateAsync("book-1", "user-1", It.IsAny<CreateReviewDto>()))
            .ReturnsAsync((true, null, review));
        var controller = CreateController();

        var result = await controller.CreateOrUpdate(
            "book-1", new CreateReviewDto { Rating = 5 });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(review, ok.Value);
        _service.Verify(s => s.CreateOrUpdateAsync("book-1", "user-1", It.IsAny<CreateReviewDto>()), Times.Once);
    }

    [Fact]
    public async Task Delete_WhenReviewIsMissing_ReturnsNotFound()
    {
        _service
            .Setup(s => s.DeleteAsync("book-1", "user-1"))
            .ReturnsAsync((false, "Відгук не знайдено"));
        var controller = CreateController();

        var result = await controller.Delete("book-1");

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Delete_WhenReviewExists_ReturnsNoContent()
    {
        _service
            .Setup(s => s.DeleteAsync("book-1", "user-1"))
            .ReturnsAsync((true, null));
        var controller = CreateController();

        var result = await controller.Delete("book-1");

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task DeleteById_WhenReviewExists_ReturnsNoContent()
    {
        _service
            .Setup(s => s.DeleteByIdAsync("book-1", "review-1"))
            .ReturnsAsync((true, null));
        var controller = CreateController();

        var result = await controller.DeleteById("book-1", "review-1");

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task DeleteById_WhenReviewIsMissing_ReturnsNotFound()
    {
        _service
            .Setup(s => s.DeleteByIdAsync("book-1", "missing"))
            .ReturnsAsync((false, "Відгук не знайдено"));
        var controller = CreateController();

        var result = await controller.DeleteById("book-1", "missing");

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetMyReviews_UsesUserIdFromClaims()
    {
        _service
            .Setup(s => s.GetUserReviewsAsync("user-42"))
            .ReturnsAsync(new List<ReviewResponseDto>());
        var controller = CreateController("user-42");

        var result = await controller.GetMyReviews();

        Assert.IsType<OkObjectResult>(result.Result);
        _service.Verify(s => s.GetUserReviewsAsync("user-42"), Times.Once);
    }
}
