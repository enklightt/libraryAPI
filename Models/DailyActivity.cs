using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryAPI.Models;

[Table("daily_activity")]
public class DailyActivity
{
    [Column("id")] public string Id { get; set; } = Guid.NewGuid().ToString();
    [Column("user_id")] public string UserId { get; set; } = null!;
    [Column("date")] public DateTime Date { get; set; }
    [Column("pages_read")] public int PagesRead { get; set; }
    public User User { get; set; } = null!;
}
