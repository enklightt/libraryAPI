using System.ComponentModel.DataAnnotations.Schema;
using LibraryAPI.Models;

[Table("books")]
public class Book
{
    [Column("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Column("title")]
    public string Title { get; set; } = null!;

    [Column("author")]
    public string Author { get; set; } = null!;

    [Column("isbn")]
    public string Isbn { get; set; } = null!;

    [Column("genre_id")]
    public string? GenreId { get; set; }

    [Column("total_copies")]
    public int TotalCopies { get; set; } = 1;

    [Column("available_copies")]
    public int AvailableCopies { get; set; } = 1;

    [Column("pdf_url")]
    public string? PdfUrl { get; set; }

    [Column("quote")]
    public string? Quote { get; set; }

    [Column("pages")]
    public int? Pages { get; set; }

    [Column("rating")]
    public decimal? Rating { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Column("gutenberg_id")]
    public int? GutenbergId { get; set; }

    [Column("source_url")]
    public string? SourceUrl { get; set; }

    [Column("image_url")]
    public string? ImageUrl { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    public Genre? Genre { get; set; }
    public ICollection<Review> Reviews { get; set; } = [];
    public ICollection<Favorite> Favorites { get; set; } = [];
    public ICollection<WantToRead> WantToReads { get; set; } = [];
}
