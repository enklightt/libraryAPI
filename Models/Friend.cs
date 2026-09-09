using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryAPI.Models;

[Table("friends")]
public class Friend
{
    [Column("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Column("user_id")]
    public string UserId { get; set; } = null!;

    [Column("friend_user_id")]
    public string FriendUserId { get; set; } = null!;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public User FriendUser { get; set; } = null!;
}
