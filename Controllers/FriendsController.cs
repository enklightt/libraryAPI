using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LibraryAPI.DTOs;
using LibraryAPI.Interfaces;
using LibraryAPI.Services;
using System.Security.Claims;

namespace LibraryAPI.Controllers;

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

    [HttpGet]
    public async Task<ActionResult<IEnumerable<FriendDto>>> GetFriends()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var friends = await _friendService.GetFriendsAsync(userId);
        return Ok(friends);
    }

    [HttpPost("request")]
    public async Task<ActionResult> SendRequest([FromBody] AddFriendDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var (success, error) = await _friendService.SendFriendRequestAsync(userId, dto.UserId);

        if (!success)
            return BadRequest(new { message = error });

        return Ok(new { message = "Заявку відправлено" });
    }

    [HttpGet("requests")]
    public async Task<ActionResult<IEnumerable<FriendRequestDto>>> GetRequests()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var requests = await _friendService.GetIncomingRequestsAsync(userId);
        return Ok(requests);
    }

    [HttpGet("requests/sent")]
    public async Task<ActionResult<IEnumerable<FriendRequestDto>>> GetSentRequests()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var requests = await _friendService.GetSentRequestsAsync(userId);
        return Ok(requests);
    }

    [HttpPost("requests/{requestId}/accept")]
    public async Task<ActionResult> AcceptRequest(string requestId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var (success, error) = await _friendService.AcceptFriendRequestAsync(requestId, userId);

        if (!success)
            return BadRequest(new { message = error ?? "Заявку не знайдено" });

        return Ok(new { message = "Заявку прийнято" });
    }

    [HttpPost("requests/{requestId}/reject")]
    public async Task<ActionResult> RejectRequest(string requestId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var success = await _friendService.RejectFriendRequestAsync(requestId, userId);

        if (!success)
            return NotFound(new { message = "Заявку не знайдено" });

        return Ok(new { message = "Заявку відхилено" });
    }

    [HttpDelete("requests/{requestId}")]
    public async Task<ActionResult> CancelRequest(string requestId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var success = await _friendService.CancelFriendRequestAsync(requestId, userId);

        if (!success)
            return NotFound(new { message = "Заявку не знайдено" });

        return Ok(new { message = "Заявку скасовано" });
    }

    [HttpDelete("{friendUserId}")]
    public async Task<ActionResult> RemoveFriend(string friendUserId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var success = await _friendService.RemoveFriendAsync(userId, friendUserId);

        if (!success)
            return NotFound(new { message = "Друга не знайдено" });

        return Ok(new { message = "Друга видалено" });
    }

    [HttpGet("{friendUserId}/profile")]
    public async Task<ActionResult<FriendProfileDto>> GetFriendProfile(string friendUserId)
    {
        var profile = await _friendService.GetFriendProfileAsync(friendUserId);
        if (profile == null)
            return NotFound(new { message = "Користувача не знайдено" });

        return Ok(profile);
    }

    [HttpGet("{friendUserId}/favorites")]
    public async Task<ActionResult<IEnumerable<FavoriteBookDto>>> GetFriendFavorites(string friendUserId)
    {
        var favorites = await _friendService.GetFriendFavoritesAsync(friendUserId);
        return Ok(favorites);
    }

    [HttpGet("{friendUserId}/reading")]
    public async Task<ActionResult<IEnumerable<ReadingProgressDto>>> GetFriendReading(string friendUserId)
    {
        var reading = await _friendService.GetFriendReadingAsync(friendUserId);
        return Ok(reading);
    }

    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<UserSearchResultDto>>> SearchUsers([FromQuery] string q)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var results = await _friendService.SearchUsersAsync(q ?? "", userId);
        return Ok(results);
    }
}
