using System.Security.Claims;
using LibraryAPI.DTOs;
using LibraryAPI.Interfaces;
using LibraryAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryAPI.Controllers;

[ApiController]
[Route("api/v1/books/{bookId}/reviews")]
public class ReviewsController : ControllerBase
{
    private readonly IReviewService _reviewService;

    public ReviewsController(IReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ReviewResponseDto>>> GetReviews(string bookId)
    {
        var reviews = await _reviewService.GetBookReviewsAsync(bookId);
        return Ok(reviews);
    }

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<ReviewResponseDto>> CreateOrUpdate(string bookId, CreateReviewDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _reviewService.CreateOrUpdateAsync(bookId, userId, dto);

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Review);
    }

    [Authorize]
    [HttpDelete]
    public async Task<ActionResult> Delete(string bookId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _reviewService.DeleteAsync(bookId, userId);

        if (!result.Success)
            return NotFound(new { message = result.Error });

        return NoContent();
    }

    [Authorize]
    [HttpGet("~/api/v1/reviews/my")]
    public async Task<ActionResult<IEnumerable<ReviewResponseDto>>> GetMyReviews()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var reviews = await _reviewService.GetUserReviewsAsync(userId);
        return Ok(reviews);
    }

    [Authorize(Roles = "admin,manager")]
    [HttpDelete("{reviewId}")]
    public async Task<ActionResult> DeleteById(string reviewId)
    {
        var result = await _reviewService.DeleteByIdAsync(reviewId);

        if (!result.Success)
            return NotFound(new { message = result.Error });

        return NoContent();
    }
}
