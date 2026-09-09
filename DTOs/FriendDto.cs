namespace LibraryAPI.DTOs;

public class FriendDto
{
    public string Id { get; set; } = null!;
    public string FriendUserId { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? EquippedTitle { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class FriendProfileDto
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? EquippedTitle { get; set; }
    public int FinishedBooks { get; set; }
    public int TotalPages { get; set; }
    public int ReadingNow { get; set; }
}

public class AddFriendDto
{
    public string UserId { get; set; } = null!;
}

public class UserSearchResultDto
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? EquippedTitle { get; set; }
}

public class FriendRequestDto
{
    public string Id { get; set; } = null!;
    public string FromUserId { get; set; } = null!;
    public string FromName { get; set; } = null!;
    public string? FromTitle { get; set; }
    public string Status { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}
