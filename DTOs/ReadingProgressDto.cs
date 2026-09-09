namespace LibraryAPI.DTOs;

public class ReadingProgressDto
{
    public string Id { get; set; } = null!;
    public string UserId { get; set; } = null!;
    public string BookId { get; set; } = null!;
    public string BookTitle { get; set; } = null!;
    public string BookAuthor { get; set; } = null!;
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public decimal ProgressPercent { get; set; }
    public string? BookImageUrl { get; set; }
    public string? BookIsbn { get; set; }
    public decimal? BookRating { get; set; }
    public DateTime LastReadAt { get; set; }
    public string Status { get; set; } = "reading";
}
