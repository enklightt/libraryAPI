using System.ComponentModel.DataAnnotations;

namespace LibraryAPI.DTOs
{
    public class CreateBookDto
    {
        [Required(ErrorMessage = "Назва книги обов'язкова")]
        [StringLength(255, MinimumLength = 2, ErrorMessage = "Назва має бути від 2 до 255 символів")]
        public string Title { get; set; } = null!;

        [Required(ErrorMessage = "Автор обов'язковий")]
        [StringLength(150, MinimumLength = 2, ErrorMessage = "Ім'я автора має бути від 2 до 150 символів")]
        public string Author { get; set; } = null!;

        [Required(ErrorMessage = "ISBN обов'язковий")]
        [StringLength(20, MinimumLength = 10, ErrorMessage = "ISBN має бути від 10 до 20 символів")]
        public string Isbn { get; set; } = null!;

        public string? GenreId { get; set; }

        [Range(1, 1000, ErrorMessage = "Кількість примірників має бути від 1 до 1000")]
        public int TotalCopies { get; set; }

        [StringLength(500, ErrorMessage = "Посилання на файл занадто довге")]
        public string? PdfUrl { get; set; }

        [StringLength(1000, ErrorMessage = "Цитата занадто довга")]
        public string? Quote { get; set; }

        [Range(1, 100000, ErrorMessage = "Кількість сторінок має бути від 1 до 100000")]
        public int? Pages { get; set; }

        [StringLength(500, ErrorMessage = "Посилання на зображення занадто довге")]
        public string? ImageUrl { get; set; }

        [Range(1, 100000, ErrorMessage = "GutenbergId має бути від 1 до 100000")]
        public int? GutenbergId { get; set; }

        [StringLength(5000, ErrorMessage = "Опис занадто довгий")]
        public string? Description { get; set; }
    }
}
