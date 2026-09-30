using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryAPI.Models
{
    [Table("books")]
    public class Book
    {
        [Key]
        [Column("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required(ErrorMessage = "Title is required")]
        [StringLength(255, MinimumLength = 2, ErrorMessage = "Title must be between 2 and 255 characters")]
        [Column("title")]
        public string Title { get; set; } = null!;

        [Required(ErrorMessage = "Author is required")]
        [StringLength(150, MinimumLength = 2, ErrorMessage = "Author must be between 2 and 150 characters")]
        [Column("author")]
        public string Author { get; set; } = null!;

        [Required(ErrorMessage = "ISBN is required")]
        [StringLength(20, MinimumLength = 10, ErrorMessage = "ISBN must be between 10 and 20 characters")]
        [Column("isbn")]
        public string Isbn { get; set; } = null!;

        [Column("genre_id")]
        public string? GenreId { get; set; }

        [Range(1, 1000, ErrorMessage = "Total copies must be between 1 and 1000")]
        [Column("total_copies")]
        public int TotalCopies { get; set; } = 1;

        [Range(0, 1000, ErrorMessage = "Available copies must be between 0 and 1000")]
        [Column("available_copies")]
        public int AvailableCopies { get; set; } = 1;

        [StringLength(500, ErrorMessage = "PDF URL is too long")]
        [Column("pdf_url")]
        public string? PdfUrl { get; set; }

        [Column("quote")]
        public string? Quote { get; set; }

        [Range(1, 100000, ErrorMessage = "Pages must be between 1 and 100000")]
        [Column("pages")]
        public int? Pages { get; set; }

        [Range(0, 5, ErrorMessage = "Rating must be between 0 and 5")]
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

        [StringLength(500, ErrorMessage = "Source URL is too long")]
        [Column("source_url")]
        public string? SourceUrl { get; set; }

        [StringLength(500, ErrorMessage = "Image URL is too long")]
        [Column("image_url")]
        public string? ImageUrl { get; set; }

        [Column("description")]
        public string? Description { get; set; }

        public Genre? Genre { get; set; }
        public ICollection<Review> Reviews { get; set; } = [];
        public ICollection<Favorite> Favorites { get; set; } = [];
        public ICollection<WantToRead> WantToReads { get; set; } = [];
    }
}