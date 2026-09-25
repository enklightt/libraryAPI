using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LibraryAPI.DTOs;
using LibraryAPI.Services;
using System.Security.Claims;

namespace LibraryAPI.Controllers;

/// <summary>Creates and reads the authenticated user's reading progress and activity.</summary>
[ApiController]
[Route("api/v1/reading-progress")]
[Authorize]
public class ReadingProgressController : ControllerBase
{
    private readonly IReadingProgressService _progressService;

    public ReadingProgressController(IReadingProgressService progressService)
    {
        _progressService = progressService;
    }

    /// <summary>Creates or updates progress for a book.</summary>
    /// <param name="dto">The book identifier and page progress.</param>
    /// <response code="200">The updated reading progress.</response>
    /// <response code="400">The request failed validation.</response>
    /// <response code="401">Authentication is required.</response>
    /// <remarks>Example request: <code>{"bookId":"book-id","currentPage":25,"totalPages":200}</code>. Example response: <code>{"bookId":"book-id","currentPage":25,"totalPages":200,"progressPercent":12.5,"status":"reading"}</code></remarks>
    [HttpPost]
    public async Task<ActionResult<ReadingProgressDto>> UpdateProgress([FromBody] UpdateReadingProgressDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var progress = await _progressService.UpdateProgressAsync(userId, dto);
        return Ok(progress);
    }

    /// <summary>Lists the authenticated user's reading progress.</summary>
    /// <response code="200">The user's reading progress entries.</response>
    /// <response code="401">Authentication is required.</response>
    /// <remarks>Example request: <code>GET /api/v1/reading-progress/my</code>. Example response: <code>[{"bookId":"book-id","currentPage":25,"progressPercent":12.5}]</code></remarks>
    [HttpGet("my")]
    public async Task<ActionResult<IEnumerable<ReadingProgressDto>>> GetMyProgress()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var progress = await _progressService.GetUserProgressAsync(userId);
        return Ok(progress);
    }

    /// <summary>Returns the authenticated user's progress for one book.</summary>
    /// <param name="bookId">The book identifier.</param>
    /// <response code="200">The progress entry.</response>
    /// <response code="401">Authentication is required.</response>
    /// <response code="404">No progress entry exists.</response>
    /// <remarks>Example request: <code>GET /api/v1/reading-progress/book/{bookId}</code>. Example response: <code>{"bookId":"book-id","currentPage":25,"progressPercent":12.5}</code></remarks>
    [HttpGet("book/{bookId}")]
    public async Task<ActionResult<ReadingProgressDto>> GetBookProgress(string bookId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var progress = await _progressService.GetBookProgressAsync(userId, bookId);

        if (progress == null)
            return NotFound(new { message = "Прогрес читання не знайдено" });

        return Ok(progress);
    }

    /// <summary>Lists the authenticated user's most recently read books.</summary>
    /// <param name="count">Maximum number of entries to return.</param>
    /// <response code="200">The recent reading entries.</response>
    /// <response code="401">Authentication is required.</response>
    /// <remarks>Example request: <code>GET /api/v1/reading-progress/recent?count=5</code>. Example response: <code>[{"bookId":"book-id","currentPage":25}]</code></remarks>
    [HttpGet("recent")]
    public async Task<ActionResult<IEnumerable<ReadingProgressDto>>> GetRecentReads([FromQuery] int count = 5)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var progress = await _progressService.GetRecentReadsAsync(userId, count);
        return Ok(progress);
    }

    /// <summary>Returns daily reading activity for the requested number of weeks.</summary>
    /// <param name="weeks">Number of weeks to include.</param>
    /// <response code="200">Daily activity values for the requested period.</response>
    /// <response code="401">Authentication is required.</response>
    /// <remarks>Example request: <code>GET /api/v1/reading-progress/activity?weeks=12</code>. Example response: <code>[{"date":"2026-09-25","pagesRead":12}]</code></remarks>
    [HttpGet("activity")]
    public async Task<ActionResult> GetActivity([FromQuery] int weeks = 12)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var data = await _progressService.GetActivityDataAsync(userId, weeks);
        return Ok(data);
    }
}
