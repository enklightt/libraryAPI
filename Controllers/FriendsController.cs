using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LibraryAPI.DTOs;
using LibraryAPI.Services;
using System.Security.Claims;

namespace LibraryAPI.Controllers;

/// <summary>Manages friend requests and authenticated users' social reading views.</summary>
[ApiController]
[Route("api/v1/friends")]
[Authorize]
public class FriendsController : ControllerBase
{
    private readonly IFriendService _friendService;

    public FriendsController(IFriendService friendService)
    {
        _friendService = friendService;
    }

    /// <summary>Lists the authenticated user's friends.</summary>
    /// <response code="200">The friends list.</response>
    /// <response code="401">Authentication is required.</response>
    /// <remarks>Example request: <code>GET /api/v1/friends</code>. Example response: <code>[{"userId":"...","name":"..."}]</code></remarks>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<FriendDto>>> GetFriends()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var friends = await _friendService.GetFriendsAsync(userId);
        return Ok(friends);
    }

    /// <summary>Sends a friend request.</summary>
    /// <param name="dto">The target user's identifier.</param>
    /// <response code="200">The request was sent.</response>
    /// <response code="400">The request could not be sent.</response>
    /// <response code="401">Authentication is required.</response>
    /// <remarks>Example request: <code>{"userId":"user-guid"}</code>. Example response: <code>{"message":"Заявку відправлено"}</code></remarks>
    [HttpPost("request")]
    public async Task<ActionResult> SendRequest([FromBody] AddFriendDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var (success, error) = await _friendService.SendFriendRequestAsync(userId, dto.UserId);

        if (!success)
            return BadRequest(new { message = error });

        return Ok(new { message = "Заявку відправлено" });
    }

    /// <summary>Lists incoming friend requests.</summary>
    /// <response code="200">Incoming requests.</response>
    /// <response code="401">Authentication is required.</response>
    /// <remarks>Example request: <code>GET /api/v1/friends/requests</code>. Example response: <code>[{"id":"request-id","status":"pending"}]</code></remarks>
    [HttpGet("requests")]
    public async Task<ActionResult<IEnumerable<FriendRequestDto>>> GetRequests()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var requests = await _friendService.GetIncomingRequestsAsync(userId);
        return Ok(requests);
    }

    /// <summary>Lists friend requests sent by the authenticated user.</summary>
    /// <response code="200">Outgoing requests.</response>
    /// <response code="401">Authentication is required.</response>
    /// <remarks>Example request: <code>GET /api/v1/friends/requests/sent</code>. Example response: <code>[{"id":"request-id","status":"pending"}]</code></remarks>
    [HttpGet("requests/sent")]
    public async Task<ActionResult<IEnumerable<FriendRequestDto>>> GetSentRequests()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var requests = await _friendService.GetSentRequestsAsync(userId);
        return Ok(requests);
    }

    /// <summary>Accepts an incoming friend request.</summary>
    /// <param name="requestId">The friend request identifier.</param>
    /// <response code="200">The request was accepted.</response>
    /// <response code="400">The request cannot be accepted.</response>
    /// <response code="401">Authentication is required.</response>
    /// <remarks>Example: <code>POST /api/v1/friends/requests/{requestId}/accept</code>. Example response: <code>{"message":"Заявку прийнято"}</code></remarks>
    [HttpPost("requests/{requestId}/accept")]
    public async Task<ActionResult> AcceptRequest(string requestId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var (success, error) = await _friendService.AcceptFriendRequestAsync(requestId, userId);

        if (!success)
            return BadRequest(new { message = error ?? "Заявку не знайдено" });

        return Ok(new { message = "Заявку прийнято" });
    }

    /// <summary>Rejects an incoming friend request.</summary>
    /// <param name="requestId">The friend request identifier.</param>
    /// <response code="200">The request was rejected.</response>
    /// <response code="401">Authentication is required.</response>
    /// <response code="404">The request was not found.</response>
    /// <remarks>Example: <code>POST /api/v1/friends/requests/{requestId}/reject</code>. Example response: <code>{"message":"Заявку відхилено"}</code></remarks>
    [HttpPost("requests/{requestId}/reject")]
    public async Task<ActionResult> RejectRequest(string requestId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var success = await _friendService.RejectFriendRequestAsync(requestId, userId);

        if (!success)
            return NotFound(new { message = "Заявку не знайдено" });

        return Ok(new { message = "Заявку відхилено" });
    }

    /// <summary>Cancels an outgoing friend request.</summary>
    /// <param name="requestId">The friend request identifier.</param>
    /// <response code="200">The request was cancelled.</response>
    /// <response code="401">Authentication is required.</response>
    /// <response code="404">The request was not found.</response>
    /// <remarks>Example: <code>DELETE /api/v1/friends/requests/{requestId}</code>. Example response: <code>{"message":"Заявку скасовано"}</code></remarks>
    [HttpDelete("requests/{requestId}")]
    public async Task<ActionResult> CancelRequest(string requestId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var success = await _friendService.CancelFriendRequestAsync(requestId, userId);

        if (!success)
            return NotFound(new { message = "Заявку не знайдено" });

        return Ok(new { message = "Заявку скасовано" });
    }

    /// <summary>Removes a friend link.</summary>
    /// <param name="friendUserId">The friend's user identifier.</param>
    /// <response code="200">The friend was removed.</response>
    /// <response code="401">Authentication is required.</response>
    /// <response code="404">The friend link was not found.</response>
    /// <remarks>Example: <code>DELETE /api/v1/friends/{friendUserId}</code>. Example response: <code>{"message":"Друга видалено"}</code></remarks>
    [HttpDelete("{friendUserId}")]
    public async Task<ActionResult> RemoveFriend(string friendUserId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var success = await _friendService.RemoveFriendAsync(userId, friendUserId);

        if (!success)
            return NotFound(new { message = "Друга не знайдено" });

        return Ok(new { message = "Друга видалено" });
    }

    /// <summary>Returns a friend's public profile.</summary>
    /// <param name="friendUserId">The friend's user identifier.</param>
    /// <response code="200">The profile was found.</response>
    /// <response code="401">Authentication is required.</response>
    /// <response code="404">The user was not found.</response>
    /// <remarks>Example request: <code>GET /api/v1/friends/{friendUserId}/profile</code>. Example response: <code>{"userId":"user-guid","name":"Ada Reader"}</code></remarks>
    [HttpGet("{friendUserId}/profile")]
    public async Task<ActionResult<FriendProfileDto>> GetFriendProfile(string friendUserId)
    {
        var profile = await _friendService.GetFriendProfileAsync(friendUserId);
        if (profile == null)
            return NotFound(new { message = "Користувача не знайдено" });

        return Ok(profile);
    }

    /// <summary>Lists a friend's favorite books.</summary>
    /// <param name="friendUserId">The friend's user identifier.</param>
    /// <response code="200">The favorites list.</response>
    /// <response code="401">Authentication is required.</response>
    /// <remarks>Example request: <code>GET /api/v1/friends/{friendUserId}/favorites</code>. Example response: <code>[{"bookId":"...","title":"..."}]</code></remarks>
    [HttpGet("{friendUserId}/favorites")]
    public async Task<ActionResult<IEnumerable<FavoriteBookDto>>> GetFriendFavorites(string friendUserId)
    {
        var favorites = await _friendService.GetFriendFavoritesAsync(friendUserId);
        return Ok(favorites);
    }

    /// <summary>Lists a friend's reading progress.</summary>
    /// <param name="friendUserId">The friend's user identifier.</param>
    /// <response code="200">The reading progress list.</response>
    /// <response code="401">Authentication is required.</response>
    /// <remarks>Example request: <code>GET /api/v1/friends/{friendUserId}/reading</code>. Example response: <code>[{"bookId":"...","progressPercent":25}]</code></remarks>
    [HttpGet("{friendUserId}/reading")]
    public async Task<ActionResult<IEnumerable<ReadingProgressDto>>> GetFriendReading(string friendUserId)
    {
        var reading = await _friendService.GetFriendReadingAsync(friendUserId);
        return Ok(reading);
    }

    /// <summary>Searches for users who can be added as friends.</summary>
    /// <param name="q">The name or email search query.</param>
    /// <response code="200">Matching users.</response>
    /// <response code="401">Authentication is required.</response>
    /// <remarks>Example request: <code>GET /api/v1/friends/search?q=ada</code>. Example response: <code>[{"userId":"...","name":"Ada Reader"}]</code></remarks>
    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<UserSearchResultDto>>> SearchUsers([FromQuery] string q)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var results = await _friendService.SearchUsersAsync(q ?? "", userId);
        return Ok(results);
    }
}
