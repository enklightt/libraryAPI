using System.Text.Json;
using System.Text.Json.Serialization;
using System.Security.Claims;
using LibraryAPI.Data;
using LibraryAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryAPI.Controllers;

/// <summary>Returns and administers user accounts.</summary>
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

    /// <summary>Returns the authenticated user's profile, lists, achievements, and statistics.</summary>
    /// <response code="200">The current user's profile and summary.</response>
    /// <response code="401">Authentication is required.</response>
    /// <response code="404">The authenticated user was not found.</response>
    /// <remarks>Example request: <code>GET /api/v1/users/me</code>. Example response: <code>{"id":"user-guid","name":"Ada Reader","stats":{"finishedBooks":1,"totalPages":200}}</code></remarks>
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

    /// <summary>Sets or clears the authenticated user's equipped title.</summary>
    /// <param name="body">A JSON object with a title string, or null to clear it.</param>
    /// <response code="200">The equipped title value.</response>
    /// <response code="400">The request body is invalid.</response>
    /// <response code="401">Authentication is required.</response>
    /// <response code="404">The authenticated user was not found.</response>
    /// <remarks>Example request: <code>{"title":"Bookworm"}</code>. Example response: <code>{"equippedTitle":"Bookworm"}</code></remarks>
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

    /// <summary>Lists users for administrative management.</summary>
    /// <response code="200">The user list.</response>
    /// <response code="401">Authentication is required.</response>
    /// <response code="403">The caller is not an admin or manager.</response>
    /// <remarks>Example request: <code>GET /api/v1/users</code>. Example response: <code>[{"id":"user-guid","name":"Ada Reader","email":"ada@example.com"}]</code></remarks>
    [Authorize(Roles = "admin,manager")]
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _userAdminService.GetAllAsync();
        return Ok(result.Data);
    }

    /// <summary>Changes a user's role.</summary>
    /// <param name="id">The user identifier.</param>
    /// <param name="roleName">The role name supplied as a JSON string.</param>
    /// <response code="200">The role was updated.</response>
    /// <response code="400">The request body is invalid.</response>
    /// <response code="401">Authentication is required.</response>
    /// <response code="403">The caller is not an admin.</response>
    /// <response code="404">The user or role was not found.</response>
    /// <remarks>Example request: <code>PUT /api/v1/users/{id}/role</code> with JSON body <code>"librarian"</code>. Example response: <code>{"message":"Роль оновлено"}</code></remarks>
    [Authorize(Roles = "admin")]
    [HttpPut("{id}/role")]
    public async Task<IActionResult> SetRole(string id, [FromBody] string roleName)
    {
        var result = await _userAdminService.SetRoleAsync(id, roleName);
        if (!result.Success)
            return NotFound(new { message = result.Error });
        return Ok(new { message = "Роль оновлено" });
    }

    /// <summary>Enables or disables a user account.</summary>
    /// <param name="id">The user identifier.</param>
    /// <response code="200">The active status was toggled.</response>
    /// <response code="401">Authentication is required.</response>
    /// <response code="403">The caller is not an admin or manager.</response>
    /// <response code="404">The user was not found.</response>
    /// <remarks>Example request: <code>PUT /api/v1/users/{id}/toggle-active</code>. Example response: <code>{"message":"Статус оновлено"}</code></remarks>
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
