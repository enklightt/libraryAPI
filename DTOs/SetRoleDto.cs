using System.ComponentModel.DataAnnotations;

namespace LibraryAPI.DTOs;

public sealed class SetRoleDto
{
    [Required(ErrorMessage = "Роль є обов'язковою")]
    [StringLength(50, MinimumLength = 1, ErrorMessage = "Некоректна назва ролі")]
    public string RoleName { get; set; } = null!;
}
