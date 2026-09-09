namespace LibraryAPI.DTOs
{
    public class BookResponseDto
    {
        public string Id { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string Author { get; set; } = null!;
        public string Isbn { get; set; } = null!;
        public string? GenreId { get; set; }
        public string? GenreName { get; set; }
        public string? ImageUrl { get; set; }
        public string? Description { get; set; }
        public int? GutenbergId { get; set; }
        public int TotalCopies { get; set; }
        public int AvailableCopies { get; set; }
        public string? PdfUrl { get; set; }
        public string? Quote { get; set; }
        public int? Pages { get; set; }
        public decimal? Rating { get; set; }
        public int ReviewCount { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
