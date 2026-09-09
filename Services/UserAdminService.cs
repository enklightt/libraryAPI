using LibraryAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace LibraryAPI.Services;

public class UserAdminService : IUserAdminService
{
    private readonly AppDbContext _context;

    public UserAdminService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<(bool Success, string? Error, object? Data)> GetAllAsync()
    {
        var users = await _context.Users
            .Include(u => u.Role)
            .OrderBy(u => u.Name)
            .Select(u => new
            {
                u.Id,
                u.Name,
                u.Email,
                Role = u.Role.Name,
                u.IsActive,
                u.CreatedAt
            })
            .ToListAsync();

        return (true, null, users);
    }

    public async Task<(bool Success, string? Error)> SetRoleAsync(string userId, string roleName)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return (false, "Користувача не знайдено");

        var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
        if (role == null)
            return (false, "Роль не знайдено");

        user.RoleId = role.Id;
        await _context.SaveChangesAsync();

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> ToggleActiveAsync(string userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return (false, "Користувача не знайдено");

        user.IsActive = !user.IsActive;
        await _context.SaveChangesAsync();

        return (true, null);
    }
}
