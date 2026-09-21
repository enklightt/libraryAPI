using System.Security.Claims;
using LibraryAPI.Interfaces;
using LibraryAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryAPI.Controllers;

[ApiController]
[Route("api/v1/favorites")]
[Authorize]
public class FavoritesController : ControllerBase
{
    private readonly IFavoriteService _favoriteService;

    public FavoritesController(IFavoriteService favoriteService)
    {
        _favoriteService = favoriteService;
    }

    [HttpGet]
    public async Task<IActionResult> GetMyFavorites()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
            return Unauthorized(new { message = "Користувача не авторизовано" });

        var favorites = await _favoriteService.GetUserFavoritesAsync(userId);
        return Ok(favorites);
    }

    [HttpPost("{bookId}")]
    public async Task<IActionResult> AddToFavorites(string bookId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
            return Unauthorized(new { message = "Користувача не авторизовано" });

        var result = await _favoriteService.AddToFavoritesAsync(userId, bookId);

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(new { message = "Книгу додано до улюблених" });
    }

    [HttpDelete("{bookId}")]
    public async Task<IActionResult> RemoveFromFavorites(string bookId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
            return Unauthorized(new { message = "Користувача не авторизовано" });

        var result = await _favoriteService.RemoveFromFavoritesAsync(userId, bookId);

        if (!result.Success)
            return NotFound(new { message = result.Error });

        return Ok(new { message = "Книгу видалено з улюблених" });
    }

    [HttpGet("check/{bookId}")]
    public async Task<IActionResult> CheckIsFavorite(string bookId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
            return Unauthorized(new { message = "Користувача не авторизовано" });

        var isFavorite = await _favoriteService.IsFavoriteAsync(userId, bookId);
        return Ok(new { isFavorite });
    }
}
