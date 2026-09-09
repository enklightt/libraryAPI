using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryAPI.Models;

public class Favorite
{
    [Column("user_id")]
    public string UserId { get; set; } = null!;

    [Column("book_id")]
    public string BookId { get; set; } = null!;

    [Column("added_at")]
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public Book Book { get; set; } = null!;
}