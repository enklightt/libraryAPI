using System.ComponentModel.DataAnnotations;

namespace LibraryAPI.DTOs;

public class CreateReviewDto
{
    [Range(1, 5, ErrorMessage = "Рейтинг має бути від 1 до 5")]
    public int Rating { get; set; }

    [StringLength(2000, ErrorMessage = "Текст відгуку занадто довгий")]
    public string? Text { get; set; }
}
