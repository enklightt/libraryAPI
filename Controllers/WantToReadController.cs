using System.Security.Claims;
using LibraryAPI.DTOs;
using LibraryAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryAPI.Controllers;

/// <summary>Manages the authenticated user's want-to-read list.</summary>
[ApiController]
[Route("api/v1/want-to-read")]
[Authorize]
public class WantToReadController : ControllerBase
{
    private readonly IWantToReadService _service;

    public WantToReadController(IWantToReadService service)
    {
        _service = service;
    }

    /// <summary>Lists books in the authenticated user's want-to-read list.</summary>
    /// <response code="200">The reading list.</response>
    /// <response code="401">Authentication is required.</response>
    /// <remarks>Example request: <code>GET /api/v1/want-to-read</code>. Example response: <code>[{"id":"book-id","title":"The Hobbit"}]</code></remarks>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<BookResponseDto>>> GetWants()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var books = await _service.GetUserWantsAsync(userId);
        return Ok(books);
    }

    /// <summary>Checks whether a book is in the authenticated user's want-to-read list.</summary>
    /// <param name="bookId">The book identifier.</param>
    /// <response code="200">A boolean membership result.</response>
    /// <response code="401">Authentication is required.</response>
    /// <remarks>Example request: <code>GET /api/v1/want-to-read/{bookId}</code>. Example response: <code>true</code></remarks>
    [HttpGet("{bookId}")]
    public async Task<ActionResult<bool>> IsWanted(string bookId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isWanted = await _service.IsWantedAsync(userId, bookId);
        return Ok(isWanted);
    }

    /// <summary>Toggles a book in the authenticated user's want-to-read list.</summary>
    /// <param name="bookId">The book identifier.</param>
    /// <response code="200">Returns the updated membership state.</response>
    /// <response code="401">Authentication is required.</response>
    /// <remarks>Example request: <code>POST /api/v1/want-to-read/{bookId}</code>. Example response: <code>{"isWanted":true}</code></remarks>
    [HttpPost("{bookId}")]
    public async Task<ActionResult> Toggle(string bookId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isWanted = await _service.ToggleAsync(userId, bookId);
        return Ok(new { isWanted });
    }
}
