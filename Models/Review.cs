namespace LibraryAPI.Models;

public class Review
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string BookId { get; set; } = null!;
    public string UserId { get; set; } = null!;
    public int Rating { get; set; }                 // 1–5
    public string? Text { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Book Book { get; set; } = null!;
    public User User { get; set; } = null!;
}