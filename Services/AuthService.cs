using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BCrypt.Net;
using LibraryAPI.Data;
using LibraryAPI.DTOs;
using LibraryAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace LibraryAPI.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;

        public AuthService(AppDbContext context, IConfiguration configuration, ILogger<AuthService> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<(bool Success, string? Error, object? Data)> RegisterAsync(RegisterDto dto)
        {
            _logger.LogInformation("Спроба реєстрації користувача з email {Email}", dto.Email);

            var userExists = await _context.Users.AnyAsync(u => u.Email == dto.Email);
            if (userExists)
            {
                _logger.LogWarning("Спроба повторної реєстрації для email {Email}", dto.Email);
                return (false, "Користувач з таким email вже існує", null);
            }

            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            var user = new User
            {
                Name = dto.Name,
                Email = dto.Email,
                Password = hashedPassword,
                RoleId = 2,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Завантажуємо роль для генерації токена
            await _context.Entry(user).Reference(u => u.Role).LoadAsync();

            var accessToken = GenerateJwtToken(user);
            var refreshToken = await GenerateAndStoreRefreshToken(user);

            _logger.LogInformation("Користувач з email {Email} успішно зареєстрований", dto.Email);

            var result = new
            {
                accessToken,
                refreshToken,
                user = new
                {
                    user.Id,
                    user.Name,
                    user.Email,
                    Role = user.Role.Name
                }
            };

            return (true, null, result);
        }

        public async Task<(bool Success, string? Error, object? Data)> LoginAsync(LoginDto dto)
        {
            _logger.LogInformation("Спроба входу для email {Email}", dto.Email);

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == dto.Email);

            if (user == null)
            {
                _logger.LogWarning("Невдала спроба входу. Email {Email} не знайдено", dto.Email);
                return (false, "Невірний email або пароль", null);
            }

            if (!user.IsActive)
            {
                _logger.LogWarning("Блокований користувач {Email} намагається увійти", dto.Email);
                return (false, "Користувача заблоковано", null);
            }

            var isPasswordValid = BCrypt.Net.BCrypt.Verify(dto.Password, user.Password);
            if (!isPasswordValid)
            {
                _logger.LogWarning("Невдала спроба входу. Невірний пароль для {Email}", dto.Email);
                return (false, "Невірний email або пароль", null);
            }

            var accessToken = GenerateJwtToken(user);
            var refreshToken = await GenerateAndStoreRefreshToken(user);

            _logger.LogInformation("Користувач {Email} успішно увійшов у систему", dto.Email);

            var result = new
            {
                accessToken,
                refreshToken,
                user = new
                {
                    user.Id,
                    user.Name,
                    user.Email,
                    Role = user.Role?.Name
                }
            };

            return (true, null, result);
        }

        public async Task<(bool Success, string? Error, object? Data)> RefreshTokenAsync(RefreshTokenRequest dto)
        {
            var storedToken = await _context.RefreshTokens
                .Include(rt => rt.User)
                .ThenInclude(u => u.Role)
                .FirstOrDefaultAsync(rt => rt.Token == dto.RefreshToken);

            if (storedToken == null)
            {
                return (false, "Невірний refresh token", null);
            }

            if (storedToken.ExpiresAt < DateTime.UtcNow)
            {
                _context.RefreshTokens.Remove(storedToken);
                await _context.SaveChangesAsync();
                return (false, "Refresh token прострочений", null);
            }

            var newAccessToken = GenerateJwtToken(storedToken.User);

            _context.RefreshTokens.Remove(storedToken);
            var newRefreshToken = await GenerateAndStoreRefreshToken(storedToken.User);

            var result = new
            {
                accessToken = newAccessToken,
                refreshToken = newRefreshToken,
                user = new
                {
                    storedToken.User.Id,
                    storedToken.User.Name,
                    storedToken.User.Email,
                    Role = storedToken.User.Role?.Name
                }
            };

            return (true, null, result);
        }

        private async Task<string> GenerateAndStoreRefreshToken(User user)
        {
            var randomBytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            var token = Convert.ToBase64String(randomBytes);

            var refreshTokenExpiryDays = _configuration.GetValue<int>("JwtSettings:RefreshTokenExpiryDays", 7);

            var refreshToken = new RefreshToken
            {
                UserId = user.Id,
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddDays(refreshTokenExpiryDays)
            };

            _context.RefreshTokens.Add(refreshToken);

            var expiredTokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == user.Id && rt.ExpiresAt < DateTime.UtcNow)
                .ToListAsync();
            _context.RefreshTokens.RemoveRange(expiredTokens);

            await _context.SaveChangesAsync();

            return token;
        }

        private string GenerateJwtToken(User user)
        {
            var jwtSecret = _configuration["JwtSettings:Secret"]!;
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expiryMinutes = _configuration.GetValue<int>("JwtSettings:AccessTokenExpiryMinutes", 60);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role?.Name ?? "customer")
            };

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
