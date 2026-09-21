using LibraryAPI.DTOs;

namespace LibraryAPI.Interfaces;

public interface IFriendService
{
    Task<IEnumerable<FriendDto>> GetFriendsAsync(string userId);
    Task<(bool Success, string? Error)> SendFriendRequestAsync(string fromUserId, string toUserId);
    Task<IEnumerable<FriendRequestDto>> GetIncomingRequestsAsync(string userId);
    Task<IEnumerable<FriendRequestDto>> GetSentRequestsAsync(string userId);
    Task<(bool Success, string? Error)> AcceptFriendRequestAsync(string requestId, string userId);
    Task<bool> RejectFriendRequestAsync(string requestId, string userId);
    Task<bool> CancelFriendRequestAsync(string requestId, string userId);
    Task<bool> RemoveFriendAsync(string userId, string friendUserId);
    Task<FriendProfileDto?> GetFriendProfileAsync(string friendUserId);
    Task<IEnumerable<FavoriteBookDto>> GetFriendFavoritesAsync(string friendUserId);
    Task<IEnumerable<ReadingProgressDto>> GetFriendReadingAsync(string friendUserId);
    Task<IEnumerable<UserSearchResultDto>> SearchUsersAsync(string query, string currentUserId);
}
