namespace LibraryAPI.Services;

public interface IUserAdminService
{
    Task<(bool Success, string? Error, object? Data)> GetAllAsync();
    Task<(bool Success, string? Error)> SetRoleAsync(string userId, string roleName);
    Task<(bool Success, string? Error)> ToggleActiveAsync(string userId);
}
