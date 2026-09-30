using LibraryAPI.DTOs;

namespace LibraryAPI.Services
{
    public interface IAuthService
    {
        Task<(bool Success, string? Error, object? Data)> RegisterAsync(RegisterDto dto);
        Task<(bool Success, string? Error, object? Data)> LoginAsync(LoginDto dto);
        Task<(bool Success, string? Error, object? Data)> RefreshTokenAsync(RefreshTokenRequest dto);
    }
}
