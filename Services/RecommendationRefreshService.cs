using LibraryAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace LibraryAPI.Services;

public sealed class RecommendationRefreshService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RecommendationRefreshService> _logger;

    public RecommendationRefreshService(IServiceScopeFactory scopeFactory, ILogger<RecommendationRefreshService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var recommendationService = scope.ServiceProvider.GetRequiredService<IRecommendationService>();
                var userIds = await context.Users.AsNoTracking()
                    .Where(user => user.IsActive)
                    .Select(user => user.Id)
                    .ToListAsync(stoppingToken);

                foreach (var userId in userIds)
                    await recommendationService.WarmCacheAsync(userId, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Не вдалося оновити кеш рекомендацій");
            }
        }
    }
}
