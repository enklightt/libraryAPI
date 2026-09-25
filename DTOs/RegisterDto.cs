using System.ComponentModel.DataAnnotations;

namespace LibraryAPI.DTOs
{
    public class RegisterDto
    {
        [Required(ErrorMessage = "Ім'я обов'язкове")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Ім'я має бути від 2 до 100 символів")]
        public string Name { get; set; } = null!;

        [Required(ErrorMessage = "Email обов'язковий")]
        [EmailAddress(ErrorMessage = "Некоректний формат email")]
        [StringLength(254, ErrorMessage = "Email не може перевищувати 254 символи")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Пароль обов'язковий")]
        [MinLength(6, ErrorMessage = "Пароль має містити мінімум 6 символів")]
        public string Password { get; set; } = null!;
    }
}
