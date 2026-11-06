using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryAPI.Models;

[Table("recommendation_feedback")]
public class RecommendationFeedback
{
    [Column("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Column("user_id")]
    public string UserId { get; set; } = null!;

    [Column("book_id")]
    public string BookId { get; set; } = null!;

    [Column("is_positive")]
    public bool IsPositive { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public Book Book { get; set; } = null!;
}
