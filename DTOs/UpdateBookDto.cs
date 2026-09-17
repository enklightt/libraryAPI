using System.ComponentModel.DataAnnotations;
using LibraryAPI.Validation;

namespace LibraryAPI.DTOs
{
    public class UpdateBookDto : IValidatableObject
    {
        [Required(ErrorMessage = "Назва книги обов'язкова")]
        [StringLength(255, MinimumLength = 2, ErrorMessage = "Назва має бути від 2 до 255 символів")]
        public string Title { get; set; } = null!;

        [Required(ErrorMessage = "Автор обов'язковий")]
        [StringLength(150, MinimumLength = 2, ErrorMessage = "Ім'я автора має бути від 2 до 150 символів")]
        public string Author { get; set; } = null!;

        [Required(ErrorMessage = "ISBN обов'язковий")]
        [StringLength(20, MinimumLength = 10, ErrorMessage = "ISBN має бути від 10 до 20 символів")]
        [Isbn]
        public string Isbn { get; set; } = null!;

        [RegularExpression(@"^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$", ErrorMessage = "GenreId має бути коректним ідентифікатором")]
        public string? GenreId { get; set; }

        [Range(1, 1000, ErrorMessage = "Загальна кількість примірників має бути від 1 до 1000")]
        public int TotalCopies { get; set; }

        [Range(0, 1000, ErrorMessage = "Доступна кількість примірників має бути від 0 до 1000")]
        public int AvailableCopies { get; set; }

        [StringLength(500, ErrorMessage = "Посилання на файл занадто довге")]
        [RegularExpression(@"^(https?://|/).+", ErrorMessage = "PdfUrl має бути URL або локальним шляхом")]
        public string? PdfUrl { get; set; }

        [Range(0, 5, ErrorMessage = "Рейтинг має бути від 0 до 5")]
        public decimal? Rating { get; set; }

        public bool IsActive { get; set; } = true;

        [StringLength(2000, ErrorMessage = "Цитата занадто довга")]
        public string? Quote { get; set; }

        [Range(1, 100000, ErrorMessage = "Кількість сторінок має бути від 1 до 100000")]
        public int? Pages { get; set; }

        [StringLength(1000, ErrorMessage = "Посилання на зображення занадто довге")]
        [RegularExpression(@"^(https?://|/).+", ErrorMessage = "ImageUrl має бути URL або локальним шляхом")]
        public string? ImageUrl { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "GutenbergId має бути більшим за 0")]
        public int? GutenbergId { get; set; }

        [StringLength(5000, ErrorMessage = "Опис книги занадто довгий")]
        public string? Description { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (AvailableCopies > TotalCopies)
            {
                yield return new ValidationResult(
                    "Доступна кількість не може перевищувати загальну кількість примірників",
                    [nameof(AvailableCopies), nameof(TotalCopies)]);
            }
        }
    }
}
