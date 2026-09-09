using System.ComponentModel.DataAnnotations;

namespace LibraryAPI.DTOs;

public class UpdateReadingProgressDto
{
    [Required(ErrorMessage = "BookId є обов'язковим")]
    public string BookId { get; set; } = null!;

    [Range(0, int.MaxValue, ErrorMessage = "CurrentPage має бути >= 0")]
    public int CurrentPage { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "TotalPages має бути >= 1")]
    public int TotalPages { get; set; }

    public string? Status { get; set; }
}
