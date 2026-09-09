namespace LibraryAPI.DTOs;

public class AchievementDto
{
    public string Code { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string Badge { get; set; } = null!;
    public bool IsUnlocked { get; set; }
    public int Progress { get; set; }
    public int Target { get; set; }
    public DateTime? UnlockedAt { get; set; }
}
