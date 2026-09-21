using System.Text.Json;
using System.Text.Json.Serialization;
using System.Security.Claims;
using LibraryAPI.Data;
using LibraryAPI.Interfaces;
using LibraryAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryAPI.Controllers;

[ApiController]
[Route("api/v1/users")]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IFavoriteService _favoriteService;
    private readonly IReadingProgressService _readingProgressService;
    private readonly IUserAdminService _userAdminService;
    private readonly IGamificationService _gamificationService;

    public UsersController(
        AppDbContext context,
        IFavoriteService favoriteService,
        IReadingProgressService readingProgressService,
        IUserAdminService userAdminService,
        IGamificationService gamificationService)
    {
        _context = context;
        _favoriteService = favoriteService;
        _readingProgressService = readingProgressService;
        _userAdminService = userAdminService;
        _gamificationService = gamificationService;
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
            return Unauthorized(new { message = "Користувача не авторизовано" });

        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            return NotFound(new { message = "Користувача не знайдено" });

        var favorites = await _favoriteService.GetUserFavoritesAsync(userId);
        var readingProgress = await _readingProgressService.GetUserProgressAsync(userId);
        var achievements = await _gamificationService.GetUserAchievementsAsync(userId);

        var finishedCount = readingProgress.Count(p => p.ProgressPercent >= 100);
        var totalPages = readingProgress.Sum(p => p.CurrentPage);
        var reviewCount = await _context.Reviews.CountAsync(r => r.UserId == userId);
        var readingCount = readingProgress.Count(p => p.Status == "reading" || p.ProgressPercent < 100);

        return Ok(new
        {
            user.Id,
            user.Name,
            user.Email,
            user.AvatarUrl,
            user.EquippedTitle,
            Role = user.Role.Name,
            user.CreatedAt,
            Favorites = favorites,
            ReadingProgress = readingProgress,
            Achievements = achievements,
            Stats = new
            {
                FinishedBooks = finishedCount,
                TotalPages = totalPages,
                Reviews = reviewCount,
                Reading = readingCount,
                FavoritesCount = favorites.Count(),
                AchievementsUnlocked = achievements.Count(a => a.IsUnlocked),
                AchievementsTotal = achievements.Count()
            }
        });
    }

    [Authorize]
    [HttpPut("me/title")]
    public async Task<IActionResult> SetTitle([FromBody] JsonElement body)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(new { message = "Користувача не авторизовано" });

        var title = body.TryGetProperty("title", out var t) ? t.GetString() : null;

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
            return NotFound(new { message = "Користувача не знайдено" });

        user.EquippedTitle = string.IsNullOrWhiteSpace(title) ? null : title;
        await _context.SaveChangesAsync();

        return Ok(new { equippedTitle = user.EquippedTitle });
    }

    [Authorize(Roles = "admin,manager")]
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _userAdminService.GetAllAsync();
        return Ok(result.Data);
    }

    [Authorize(Roles = "admin")]
    [HttpPut("{id}/role")]
    public async Task<IActionResult> SetRole(string id, [FromBody] string roleName)
    {
        var result = await _userAdminService.SetRoleAsync(id, roleName);
        if (!result.Success)
            return NotFound(new { message = result.Error });
        return Ok(new { message = "Роль оновлено" });
    }

    [Authorize(Roles = "admin,manager")]
    [HttpPut("{id}/toggle-active")]
    public async Task<IActionResult> ToggleActive(string id)
    {
        var result = await _userAdminService.ToggleActiveAsync(id);
        if (!result.Success)
            return NotFound(new { message = result.Error });
        return Ok(new { message = "Статус оновлено" });
    }
}
