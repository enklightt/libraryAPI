using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryAPI.Models;

[Table("friend_requests")]
public class FriendRequest
{
    [Column("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Column("from_user_id")]
    public string FromUserId { get; set; } = null!;

    [Column("to_user_id")]
    public string ToUserId { get; set; } = null!;

    [Column("status")]
    public string Status { get; set; } = "pending";

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User FromUser { get; set; } = null!;
    public User ToUser { get; set; } = null!;
}
