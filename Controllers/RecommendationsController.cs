using System.Security.Claims;
using LibraryAPI.DTOs;
using LibraryAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryAPI.Controllers;

[ApiController]
[Route("api/v1/recommendations")]
public class RecommendationsController : ControllerBase
{
    private readonly IRecommendationService _recommendationService;

    public RecommendationsController(IRecommendationService recommendationService)
    {
        _recommendationService = recommendationService;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<PagedResponse<RecommendationDto>>> GetRecommendations(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var result = await _recommendationService.GetRecommendationsAsync(userId, page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("similar/{bookId}")]
    [AllowAnonymous]
    public async Task<ActionResult<PagedResponse<RecommendationDto>>> GetSimilarBooks(
        string bookId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await _recommendationService.GetSimilarBooksAsync(bookId, page, pageSize, cancellationToken);
        return result == null
            ? NotFound(new { message = "Книгу не знайдено" })
            : Ok(result);
    }

    [HttpGet("explanation/{bookId}")]
    [Authorize]
    public async Task<ActionResult<RecommendationDto>> GetExplanation(string bookId, CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(new { message = "Користувача не авторизовано" });

        var recommendation = await _recommendationService.ExplainAsync(userId, bookId, cancellationToken);
        return recommendation == null
            ? NotFound(new { message = "Рекомендацію не знайдено" })
            : Ok(recommendation);
    }

    [HttpPost("{bookId}/feedback")]
    [Authorize]
    public async Task<IActionResult> SaveFeedback(
        string bookId, [FromBody] RecommendationFeedbackDto dto, CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(new { message = "Користувача не авторизовано" });

        var result = await _recommendationService.SaveFeedbackAsync(userId, bookId, dto.IsPositive, cancellationToken);
        return result.Success ? Ok(new { message = "Вподобання збережено" }) : NotFound(new { message = result.Error });
    }
}
