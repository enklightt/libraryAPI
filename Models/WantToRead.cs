namespace LibraryAPI.Models;

public class WantToRead
{
    public string UserId { get; set; } = null!;
    public string BookId { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public Book Book { get; set; } = null!;
}
