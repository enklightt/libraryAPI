using System.Security.Claims;
using LibraryAPI.Controllers;
using LibraryAPI.DTOs;
using LibraryAPI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace LibraryAPI.Tests;

public class FavoritesControllerTests
{
    private readonly Mock<IFavoriteService> _favoriteService = new();

    [Fact]
    public async Task GetMyFavorites_WhenUnauthorized_Returns401WithoutCallingService()
    {
        var controller = CreateController(null);

        var result = await controller.GetMyFavorites();

        Assert.IsType<UnauthorizedObjectResult>(result);
        _favoriteService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetMyFavorites_WhenAuthenticated_ReturnsFavoritesForCurrentUser()
    {
        var favorites = new[] { new FavoriteBookDto { Id = "book-1", Title = "Title" } };
        _favoriteService.Setup(service => service.GetUserFavoritesAsync("user-1"))
            .ReturnsAsync(favorites);
        var controller = CreateController("user-1");

        var result = await controller.GetMyFavorites();

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Same(favorites, okResult.Value);
        _favoriteService.Verify(service => service.GetUserFavoritesAsync("user-1"), Times.Once);
    }

    [Fact]
    public async Task AddToFavorites_WhenServiceFails_ReturnsBadRequest()
    {
        _favoriteService.Setup(service => service.AddToFavoritesAsync("user-1", "missing-book"))
            .ReturnsAsync((false, "Книгу не знайдено"));
        var controller = CreateController("user-1");

        var result = await controller.AddToFavorites("missing-book");

        Assert.IsType<BadRequestObjectResult>(result);
        _favoriteService.Verify(service => service.AddToFavoritesAsync("user-1", "missing-book"), Times.Once);
    }

    [Fact]
    public async Task AddToFavorites_WhenUnauthorized_Returns401WithoutCallingService()
    {
        var controller = CreateController(null);

        var result = await controller.AddToFavorites("book-1");

        Assert.IsType<UnauthorizedObjectResult>(result);
        _favoriteService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AddToFavorites_WhenServiceSucceeds_ReturnsOk()
    {
        _favoriteService.Setup(service => service.AddToFavoritesAsync("user-1", "book-1"))
            .ReturnsAsync((true, null));
        var controller = CreateController("user-1");

        var result = await controller.AddToFavorites("book-1");

        Assert.IsType<OkObjectResult>(result);
        _favoriteService.Verify(service => service.AddToFavoritesAsync("user-1", "book-1"), Times.Once);
    }

    [Fact]
    public async Task RemoveFromFavorites_WhenServiceFails_ReturnsNotFound()
    {
        _favoriteService.Setup(service => service.RemoveFromFavoritesAsync("user-1", "missing-book"))
            .ReturnsAsync((false, "Книга не знайдена у улюблених"));
        var controller = CreateController("user-1");

        var result = await controller.RemoveFromFavorites("missing-book");

        Assert.IsType<NotFoundObjectResult>(result);
        _favoriteService.Verify(service => service.RemoveFromFavoritesAsync("user-1", "missing-book"), Times.Once);
    }

    [Fact]
    public async Task RemoveFromFavorites_WhenUnauthorized_Returns401WithoutCallingService()
    {
        var controller = CreateController(null);

        var result = await controller.RemoveFromFavorites("book-1");

        Assert.IsType<UnauthorizedObjectResult>(result);
        _favoriteService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RemoveFromFavorites_WhenServiceSucceeds_ReturnsOk()
    {
        _favoriteService.Setup(service => service.RemoveFromFavoritesAsync("user-1", "book-1"))
            .ReturnsAsync((true, null));
        var controller = CreateController("user-1");

        var result = await controller.RemoveFromFavorites("book-1");

        Assert.IsType<OkObjectResult>(result);
        _favoriteService.Verify(service => service.RemoveFromFavoritesAsync("user-1", "book-1"), Times.Once);
    }

    [Fact]
    public async Task CheckIsFavorite_ReturnsServiceResultForCurrentUser()
    {
        _favoriteService.Setup(service => service.IsFavoriteAsync("user-1", "book-1"))
            .ReturnsAsync(true);
        var controller = CreateController("user-1");

        var result = await controller.CheckIsFavorite("book-1");

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(true, okResult.Value!.GetType().GetProperty("isFavorite")!.GetValue(okResult.Value));
        _favoriteService.Verify(service => service.IsFavoriteAsync("user-1", "book-1"), Times.Once);
    }

    [Fact]
    public async Task CheckIsFavorite_WhenUnauthorized_Returns401WithoutCallingService()
    {
        var controller = CreateController(null);

        var result = await controller.CheckIsFavorite("book-1");

        Assert.IsType<UnauthorizedObjectResult>(result);
        _favoriteService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CheckIsFavorite_WhenNotFavorite_ReturnsFalse()
    {
        _favoriteService.Setup(service => service.IsFavoriteAsync("user-1", "book-1"))
            .ReturnsAsync(false);
        var controller = CreateController("user-1");

        var result = await controller.CheckIsFavorite("book-1");

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(false, okResult.Value!.GetType().GetProperty("isFavorite")!.GetValue(okResult.Value));
        _favoriteService.Verify(service => service.IsFavoriteAsync("user-1", "book-1"), Times.Once);
    }

    [Fact]
    public async Task GetMyFavorites_WhenServiceThrows_PropagatesException()
    {
        _favoriteService.Setup(service => service.GetUserFavoritesAsync("user-1"))
            .ThrowsAsync(new InvalidOperationException("service failure"));
        var controller = CreateController("user-1");

        await Assert.ThrowsAsync<InvalidOperationException>(() => controller.GetMyFavorites());
        _favoriteService.Verify(service => service.GetUserFavoritesAsync("user-1"), Times.Once);
    }

    private FavoritesController CreateController(string? userId)
    {
        var claims = userId is null
            ? Array.Empty<Claim>()
            : new[] { new Claim(ClaimTypes.NameIdentifier, userId) };
        var identity = new ClaimsIdentity(claims, userId is null ? null : "Test");
        var controller = new FavoritesController(_favoriteService.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(identity)
                }
            }
        };

        return controller;
    }
}
