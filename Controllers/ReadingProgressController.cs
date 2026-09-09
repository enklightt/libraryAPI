using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LibraryAPI.DTOs;
using LibraryAPI.Services;
using System.Security.Claims;

namespace LibraryAPI.Controllers;

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

    [HttpPost]
    public async Task<ActionResult<ReadingProgressDto>> UpdateProgress([FromBody] UpdateReadingProgressDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var progress = await _progressService.UpdateProgressAsync(userId, dto);
        return Ok(progress);
    }

    [HttpGet("my")]
    public async Task<ActionResult<IEnumerable<ReadingProgressDto>>> GetMyProgress()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var progress = await _progressService.GetUserProgressAsync(userId);
        return Ok(progress);
    }

    [HttpGet("book/{bookId}")]
    public async Task<ActionResult<ReadingProgressDto>> GetBookProgress(string bookId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var progress = await _progressService.GetBookProgressAsync(userId, bookId);

        if (progress == null)
            return NotFound(new { message = "Прогрес читання не знайдено" });

        return Ok(progress);
    }

    [HttpGet("recent")]
    public async Task<ActionResult<IEnumerable<ReadingProgressDto>>> GetRecentReads([FromQuery] int count = 5)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var progress = await _progressService.GetRecentReadsAsync(userId, count);
        return Ok(progress);
    }

    [HttpGet("activity")]
    public async Task<ActionResult> GetActivity([FromQuery] int weeks = 12)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var data = await _progressService.GetActivityDataAsync(userId, weeks);
        return Ok(data);
    }
}
