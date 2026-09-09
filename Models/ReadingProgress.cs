using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryAPI.Models;

[Table("reading_progress")]
public class ReadingProgress
{
    [Column("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Column("user_id")]
    public string UserId { get; set; } = null!;

    [Column("book_id")]
    public string BookId { get; set; } = null!;

    [Column("current_page")]
    public int CurrentPage { get; set; } = 0;

    [Column("total_pages")]
    public int TotalPages { get; set; } = 0;

    [Column("progress_percent")]
    public decimal ProgressPercent { get; set; } = 0;

    [Column("last_read_at")]
    public DateTime LastReadAt { get; set; } = DateTime.UtcNow;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Column("status")]
    public string Status { get; set; } = "reading";

    public User User { get; set; } = null!;
    public Book Book { get; set; } = null!;
}
