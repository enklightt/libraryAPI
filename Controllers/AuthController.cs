using LibraryAPI.DTOs;
using LibraryAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace LibraryAPI.Controllers
{
    /// <summary>Registers users and manages login and token refresh.</summary>
    [ApiController]
    [Route("api/v1/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>Creates a customer account.</summary>
        /// <param name="dto">The name, email address, and password for the new account.</param>
        /// <response code="200">The account was created and authentication data is returned.</response>
        /// <response code="400">The request failed model validation.</response>
        /// <response code="409">An account with the supplied email already exists.</response>
        /// <remarks>Example request: <code>{"name":"Ada Reader","email":"ada@example.com","password":"reader123"}</code>. Example response: <code>{"accessToken":"&lt;access-token&gt;","refreshToken":"&lt;refresh-token&gt;","user":{"id":"...","name":"Ada Reader","email":"ada@example.com","role":"customer"}}</code></remarks>
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            var result = await _authService.RegisterAsync(dto);

            if (!result.Success)
                return Conflict(new { message = result.Error });

            return Ok(result.Data);
        }

        /// <summary>Authenticates a user and issues access and refresh tokens.</summary>
        /// <param name="dto">The user's email address and password.</param>
        /// <response code="200">The credentials are valid; token data is returned.</response>
        /// <response code="400">The request failed model validation.</response>
        /// <response code="401">The credentials are invalid.</response>
        /// <remarks>Example request: <code>{"email":"ada@example.com","password":"reader123"}</code>. Example response: <code>{"accessToken":"&lt;access-token&gt;","refreshToken":"&lt;refresh-token&gt;","user":{"id":"...","name":"Ada Reader","email":"ada@example.com","role":"customer"}}</code></remarks>
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            var result = await _authService.LoginAsync(dto);

            if (!result.Success)
                return Unauthorized(new { message = result.Error });

            return Ok(result.Data);
        }

        /// <summary>Rotates a refresh token and returns a new token pair.</summary>
        /// <param name="dto">The refresh token to exchange.</param>
        /// <response code="200">A new token pair is returned.</response>
        /// <response code="400">The request failed model validation.</response>
        /// <response code="401">The refresh token is invalid or expired.</response>
        /// <remarks>Example request: <code>{"refreshToken":"&lt;refresh-token&gt;"}</code>. Example response: <code>{"accessToken":"&lt;new-access-token&gt;","refreshToken":"&lt;new-refresh-token&gt;","user":{"id":"...","name":"Ada Reader","email":"ada@example.com","role":"customer"}}</code></remarks>
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh(RefreshTokenRequest dto)
        {
            var result = await _authService.RefreshTokenAsync(dto);

            if (!result.Success)
                return Unauthorized(new { message = result.Error });

            return Ok(result.Data);
        }
    }
}
