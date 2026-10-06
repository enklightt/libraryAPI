using System.Security.Claims;
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

    /// <summary>Повертає список улюблених книг поточного користувача.</summary>
    /// <response code="200">Список улюблених книг повернуто.</response>
    /// <response code="401">Потрібна автентифікація.</response>
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [HttpGet]
    public async Task<IActionResult> GetMyFavorites()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
            return Unauthorized(new { message = "Користувача не авторизовано" });

        var favorites = await _favoriteService.GetUserFavoritesAsync(userId);
        return Ok(favorites);
    }

    /// <summary>Додає книгу до улюблених поточного користувача.</summary>
    /// <param name="bookId">Ідентифікатор книги.</param>
    /// <response code="200">Книгу додано до улюблених.</response>
    /// <response code="400">Книгу не вдалося додати.</response>
    /// <response code="401">Потрібна автентифікація.</response>
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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

    /// <summary>Видаляє книгу з улюблених поточного користувача.</summary>
    /// <param name="bookId">Ідентифікатор книги.</param>
    /// <response code="200">Книгу видалено з улюблених.</response>
    /// <response code="404">Книгу в улюблених не знайдено.</response>
    /// <response code="401">Потрібна автентифікація.</response>
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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

    /// <summary>Перевіряє, чи є книга в улюблених поточного користувача.</summary>
    /// <param name="bookId">Ідентифікатор книги.</param>
    /// <response code="200">Повернуто результат перевірки.</response>
    /// <response code="401">Потрібна автентифікація.</response>
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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
