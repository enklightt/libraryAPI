using LibraryAPI.DTOs;

namespace LibraryAPI.Services;

public interface IGamificationService
{
    Task<IEnumerable<AchievementDto>> GetUserAchievementsAsync(string userId);
}
