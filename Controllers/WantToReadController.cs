using System.Security.Claims;
using LibraryAPI.DTOs;
using LibraryAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryAPI.Controllers;

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

    [HttpGet]
    public async Task<ActionResult<IEnumerable<BookResponseDto>>> GetWants()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var books = await _service.GetUserWantsAsync(userId);
        return Ok(books);
    }

    [HttpGet("{bookId}")]
    public async Task<ActionResult<bool>> IsWanted(string bookId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isWanted = await _service.IsWantedAsync(userId, bookId);
        return Ok(isWanted);
    }

    [HttpPost("{bookId}")]
    public async Task<ActionResult> Toggle(string bookId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isWanted = await _service.ToggleAsync(userId, bookId);
        return Ok(new { isWanted });
    }
}
