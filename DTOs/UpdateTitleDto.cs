using System.ComponentModel.DataAnnotations;

namespace LibraryAPI.DTOs;

public sealed class UpdateTitleDto
{
    [StringLength(100, ErrorMessage = "Титул має бути не довшим за 100 символів")]
    public string? Title { get; set; }
}
