using LibraryAPI.DTOs;

namespace LibraryAPI.Interfaces;

public interface IGamificationService
{
    Task<IEnumerable<AchievementDto>> GetUserAchievementsAsync(string userId);
}
