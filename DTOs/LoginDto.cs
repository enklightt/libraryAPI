using System.ComponentModel.DataAnnotations;

namespace LibraryAPI.DTOs
{
    public class LoginDto
    {
        [Required(ErrorMessage = "Email обов'язковий")]
        [EmailAddress(ErrorMessage = "Некоректний формат email")]
        [StringLength(254, ErrorMessage = "Email не може перевищувати 254 символи")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Пароль обов'язковий")]
        public string Password { get; set; } = null!;
    }
}
