using System.Security.Claims;

namespace LibraryAPI.Infrastructure;

public static class RateLimitKeyProvider
{
    public static string GetClientKey(HttpContext context)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrWhiteSpace(userId))
            return $"user:{userId}";

        var ip = context.Connection.RemoteIpAddress?.ToString();
        return $"ip:{ip ?? "unknown"}";
    }
}
