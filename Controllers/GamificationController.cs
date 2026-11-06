using Asp.Versioning;
using System.Security.Claims;
using LibraryAPI.DTOs;
using LibraryAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryAPI.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/gamification")]
[Authorize]
public class GamificationController : ControllerBase
{
    private readonly IGamificationService _gamificationService;

    public GamificationController(IGamificationService gamificationService)
    {
        _gamificationService = gamificationService;
    }

    [HttpGet("achievements")]
    public async Task<ActionResult<IEnumerable<AchievementDto>>> GetMyAchievements()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
            return Unauthorized(new { message = "Користувача не авторизовано" });

        var achievements = await _gamificationService.GetUserAchievementsAsync(userId);
        return Ok(achievements);
    }

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
