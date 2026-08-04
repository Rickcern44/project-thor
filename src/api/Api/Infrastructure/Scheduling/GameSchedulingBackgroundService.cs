using ProjectThor.Data;

namespace ProjectThor.Api.Infrastructure.Scheduling;

/// <summary>Periodically ensures the next game is materialized, so generation doesn't depend on someone polling the live-game endpoint.</summary>
public class GameSchedulingBackgroundService(IServiceScopeFactory scopeFactory, ILogger<GameSchedulingBackgroundService> logger)
    : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        do
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var schedulingService = new GameSchedulingService(dbContext);
                await schedulingService.EnsureNextGameMaterializedAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Failed to ensure next game is materialized");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
