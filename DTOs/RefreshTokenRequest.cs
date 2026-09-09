using System.ComponentModel.DataAnnotations;

namespace LibraryAPI.DTOs;

public class RefreshTokenRequest
{
    [Required(ErrorMessage = "Refresh token обов'язковий")]
    public string RefreshToken { get; set; } = null!;
}
