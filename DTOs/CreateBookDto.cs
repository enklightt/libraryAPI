using System.ComponentModel.DataAnnotations;

namespace LibraryAPI.DTOs
{
    public class CreateBookDto
    {
        [Required(ErrorMessage = "Назва книги є обов'язковою.")]
        [StringLength(200, MinimumLength = 1, ErrorMessage = "Назва має містити від 1 до 200 символів.")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ім'я автора є обов'язковим.")]
        [StringLength(100, ErrorMessage = "Ім'я автора не повинно перевищувати 100 символів.")]
        public string Author { get; set; } = string.Empty;

        [Required(ErrorMessage = "Поле ISBN є обов'язковим.")]
        [StringLength(20, MinimumLength = 10, ErrorMessage = "ISBN має бути від 10 до 20 символів.")]
        public string Isbn { get; set; } = string.Empty;

        [Range(1000, 2026, ErrorMessage = "Введіть дійсний рік видання.")]
        public int Year { get; set; }

        [Range(0.01, 10000.00, ErrorMessage = "Ціна має бути більшою за нуль.")]
        public decimal Price { get; set; }

        public string? GenreId { get; set; }

        [Range(1, 10000, ErrorMessage = "Кількість копій має бути від 1 до 10000.")]
        public int TotalCopies { get; set; } = 1;

        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public string? PdfUrl { get; set; }
        public string? Quote { get; set; }
        public int? Pages { get; set; }
        public int? GutenbergId { get; set; }
    }
}