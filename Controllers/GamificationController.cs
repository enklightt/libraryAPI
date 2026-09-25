using System.Security.Claims;
using LibraryAPI.DTOs;
using LibraryAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryAPI.Controllers;

/// <summary>Returns achievements and titles for the authenticated user.</summary>
[ApiController]
[Route("api/v1/gamification")]
[Authorize]
public class GamificationController : ControllerBase
{
    private readonly IGamificationService _gamificationService;

    public GamificationController(IGamificationService gamificationService)
    {
        _gamificationService = gamificationService;
    }

    /// <summary>Lists the authenticated user's achievements and unlock states.</summary>
    /// <response code="200">The user's achievements.</response>
    /// <response code="401">Authentication is required.</response>
    /// <remarks>Example request: <code>GET /api/v1/gamification/achievements</code>. Example response: <code>[{"code":"first_book","isUnlocked":true}]</code></remarks>
    [HttpGet("achievements")]
    public async Task<ActionResult<IEnumerable<AchievementDto>>> GetMyAchievements()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
            return Unauthorized(new { message = "Користувача не авторизовано" });

        var achievements = await _gamificationService.GetUserAchievementsAsync(userId);
        return Ok(achievements);
    }

    /// <summary>Lists titles unlocked by the authenticated user.</summary>
    /// <response code="200">The unlocked titles.</response>
    /// <response code="401">Authentication is required.</response>
    /// <remarks>Example request: <code>GET /api/v1/gamification/titles</code>. Example response: <code>[{"code":"first_book","title":"Bookworm"}]</code></remarks>
    [HttpGet("titles")]
    public async Task<ActionResult<IEnumerable<object>>> GetUnlockedTitles()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(new { message = "Користувача не авторизовано" });

        var achievements = await _gamificationService.GetUserAchievementsAsync(userId);
        var titles = achievements
            .Where(a => a.IsUnlocked)
            .Select(a => new { code = a.Code, title = a.Title });
        return Ok(titles);
    }
}
