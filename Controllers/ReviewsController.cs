using System.Security.Claims;
using LibraryAPI.DTOs;
using LibraryAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryAPI.Controllers;

/// <summary>Reads and manages book reviews.</summary>
[ApiController]
[Route("api/v1/books/{bookId}/reviews")]
public class ReviewsController : ControllerBase
{
    private readonly IReviewService _reviewService;

    public ReviewsController(IReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    /// <summary>Lists reviews for a book.</summary>
    /// <param name="bookId">The book identifier.</param>
    /// <response code="200">The book's reviews.</response>
    /// <remarks>Example request: <code>GET /api/v1/books/{bookId}/reviews</code>. Example response: <code>[{"rating":5,"text":"Excellent read"}]</code></remarks>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ReviewResponseDto>>> GetReviews(string bookId)
    {
        var reviews = await _reviewService.GetBookReviewsAsync(bookId);
        return Ok(reviews);
    }

    /// <summary>Creates or updates the authenticated user's review for a book.</summary>
    /// <param name="bookId">The book identifier.</param>
    /// <param name="dto">The review rating and optional text.</param>
    /// <response code="200">The saved review.</response>
    /// <response code="400">The request failed validation or could not be saved.</response>
    /// <response code="401">Authentication is required.</response>
    /// <remarks>Example request: <code>{"rating":5,"text":"Excellent read"}</code>. Example response: <code>{"bookId":"book-id","rating":5,"text":"Excellent read"}</code></remarks>
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

    /// <summary>Deletes the authenticated user's review for a book.</summary>
    /// <param name="bookId">The book identifier.</param>
    /// <response code="204">The review was deleted.</response>
    /// <response code="401">Authentication is required.</response>
    /// <response code="404">The user's review was not found.</response>
    /// <remarks>Example request: <code>DELETE /api/v1/books/{bookId}/reviews</code>. Example response: <code>204 No Content</code></remarks>
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

    /// <summary>Lists reviews written by the authenticated user.</summary>
    /// <response code="200">The user's reviews.</response>
    /// <response code="401">Authentication is required.</response>
    /// <remarks>Example request: <code>GET /api/v1/reviews/my</code>. Example response: <code>[{"bookId":"book-id","rating":5,"text":"Excellent read"}]</code></remarks>
    [Authorize]
    [HttpGet("~/api/v1/reviews/my")]
    public async Task<ActionResult<IEnumerable<ReviewResponseDto>>> GetMyReviews()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var reviews = await _reviewService.GetUserReviewsAsync(userId);
        return Ok(reviews);
    }

    /// <summary>Deletes a review by identifier as an admin or manager.</summary>
    /// <param name="bookId">The parent book identifier.</param>
    /// <param name="reviewId">The review identifier.</param>
    /// <response code="204">The review was deleted.</response>
    /// <response code="401">Authentication is required.</response>
    /// <response code="403">The caller is not an admin or manager.</response>
    /// <response code="404">The review was not found.</response>
    /// <remarks>Example request: <code>DELETE /api/v1/books/{bookId}/reviews/{reviewId}</code>. Example response: <code>204 No Content</code></remarks>
    [Authorize(Roles = "admin,manager")]
    [HttpDelete("{reviewId}")]
    public async Task<ActionResult> DeleteById(string bookId, string reviewId)
    {
        var result = await _reviewService.DeleteByIdAsync(bookId, reviewId);

        if (!result.Success)
            return NotFound(new { message = result.Error });

        return NoContent();
    }
}
