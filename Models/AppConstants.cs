namespace LibraryAPI.Models;

public static class RoleNames
{
    public const string Admin = "admin";
    public const string Manager = "manager";
    public const string AdminOrManager = Admin + "," + Manager;
}

public static class FriendRequestStatus
{
    public const string Pending = "pending";
    public const string Accepted = "accepted";
}

public static class ReadingStatus
{
    public const string Reading = "reading";
    public const string Finished = "finished";
}
