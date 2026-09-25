using System.Security.Claims;
using LibraryAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryAPI.Controllers;

/// <summary>Manages the authenticated user's favorite books.</summary>
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

    /// <summary>Lists the authenticated user's favorite books.</summary>
    /// <response code="200">The favorites list.</response>
    /// <response code="401">Authentication is required.</response>
    /// <remarks>Example request: <code>GET /api/v1/favorites</code>. Example response: <code>[{"id":"book-id","title":"The Hobbit"}]</code></remarks>
    [HttpGet]
    public async Task<IActionResult> GetMyFavorites()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
            return Unauthorized(new { message = "Користувача не авторизовано" });

        var favorites = await _favoriteService.GetUserFavoritesAsync(userId);
        return Ok(favorites);
    }

    /// <summary>Adds a book to the authenticated user's favorites.</summary>
    /// <param name="bookId">The book identifier.</param>
    /// <response code="200">The book was added.</response>
    /// <response code="400">The operation failed.</response>
    /// <response code="401">Authentication is required.</response>
    /// <remarks>Example: <code>POST /api/v1/favorites/{bookId}</code>. Example response: <code>{"message":"Книгу додано до улюблених"}</code></remarks>
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

    /// <summary>Removes a book from the authenticated user's favorites.</summary>
    /// <param name="bookId">The book identifier.</param>
    /// <response code="200">The book was removed.</response>
    /// <response code="401">Authentication is required.</response>
    /// <response code="404">The favorite entry was not found.</response>
    /// <remarks>Example: <code>DELETE /api/v1/favorites/{bookId}</code>. Example response: <code>{"message":"Книгу видалено з улюблених"}</code></remarks>
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

    /// <summary>Checks whether a book is in the authenticated user's favorites.</summary>
    /// <param name="bookId">The book identifier.</param>
    /// <response code="200">Returns the favorite flag.</response>
    /// <response code="401">Authentication is required.</response>
    /// <remarks>Example request: <code>GET /api/v1/favorites/check/{bookId}</code>. Example response: <code>{"isFavorite":true}</code></remarks>
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
