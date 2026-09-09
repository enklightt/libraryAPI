namespace LibraryAPI.DTOs;

public class FavoriteBookDto
{
    public string Id { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string Author { get; set; } = null!;
    public string Isbn { get; set; } = null!;
    public string? GenreId { get; set; }
    public int TotalCopies { get; set; }
    public int AvailableCopies { get; set; }
    public string? PdfUrl { get; set; }
    public decimal? Rating { get; set; }
    public bool IsActive { get; set; }
    public DateTime AddedAt { get; set; }
}
