using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LibraryAPI.Validation;
using LibraryAPI.Models;

[Table("books")]
public class Book
{
    [Column("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Column("title")]
    [Required]
    [StringLength(255, MinimumLength = 2)]
    public string Title { get; set; } = null!;

    [Column("author")]
    [Required]
    [StringLength(150, MinimumLength = 2)]
    public string Author { get; set; } = null!;

    [Column("isbn")]
    [Required]
    [StringLength(20, MinimumLength = 10)]
    [Isbn]
    public string Isbn { get; set; } = null!;

    [Column("genre_id")]
    public string? GenreId { get; set; }

    [Column("total_copies")]
    [Range(1, 1000)]
    public int TotalCopies { get; set; } = 1;

    [Column("available_copies")]
    [Range(0, 1000)]
    public int AvailableCopies { get; set; } = 1;

    [Column("pdf_url")]
    public string? PdfUrl { get; set; }

    [Column("quote")]
    [StringLength(2000)]
    public string? Quote { get; set; }

    [Column("pages")]
    [Range(1, 100000)]
    public int? Pages { get; set; }

    [Column("rating")]
    [Range(0, 5)]
    public decimal? Rating { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Column("gutenberg_id")]
    [Range(1, int.MaxValue)]
    public int? GutenbergId { get; set; }

    [Column("source_url")]
    [StringLength(1000)]
    public string? SourceUrl { get; set; }

    [Column("image_url")]
    [StringLength(1000)]
    public string? ImageUrl { get; set; }

    [Column("description")]
    [StringLength(5000)]
    public string? Description { get; set; }

    public Genre? Genre { get; set; }
    public ICollection<Review> Reviews { get; set; } = [];
    public ICollection<Favorite> Favorites { get; set; } = [];
    public ICollection<WantToRead> WantToReads { get; set; } = [];
}
