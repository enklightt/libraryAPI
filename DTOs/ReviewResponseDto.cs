namespace LibraryAPI.DTOs;

public class ReviewResponseDto
{
    public string Id { get; set; } = null!;
    public string BookId { get; set; } = null!;
    public string BookTitle { get; set; } = null!;
    public string? BookImageUrl { get; set; }
    public string UserId { get; set; } = null!;
    public string UserName { get; set; } = null!;
    public int Rating { get; set; }
    public string? Text { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
