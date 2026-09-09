using System.ComponentModel.DataAnnotations;

namespace LibraryAPI.DTOs
{
    public class LoginDto
    {
        [Required(ErrorMessage = "Email обов'язковий")]
        [EmailAddress(ErrorMessage = "Некоректний формат email")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Пароль обов'язковий")]
        public string Password { get; set; } = null!;
    }
}
