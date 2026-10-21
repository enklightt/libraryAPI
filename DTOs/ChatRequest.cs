using System.ComponentModel.DataAnnotations;

namespace LibraryAPI.DTOs;

/// <summary>
/// Запит для чату з книгою.
/// </summary>
public sealed class ChatRequest
{
    /// <summary>
    /// Текст питання до книги.
    /// </summary>
    /// <example>Про що ця книга?</example>
    [Required(ErrorMessage = "Введіть питання")]
    [StringLength(1000, MinimumLength = 1, ErrorMessage = "Питання має бути від 1 до 1000 символів")]
    public string Question { get; set; } = string.Empty;

    /// <summary>
    /// Номер поточної сторінки (опціонально), щоб відповідь була більш контекстною.
    /// </summary>
    /// <example>42</example>
    [Range(0, int.MaxValue, ErrorMessage = "Номер сторінки має бути невід'ємним")]
    public int? CurrentPage { get; set; }
}
