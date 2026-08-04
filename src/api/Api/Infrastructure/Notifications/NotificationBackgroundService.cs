namespace ProjectThor.Api.Infrastructure.Notifications;

/// <summary>Periodically notifies players when a game transitions into its sign-up window.</summary>
public class NotificationBackgroundService(IServiceScopeFactory scopeFactory, ILogger<NotificationBackgroundService> logger)
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
                var signupOpenNotificationService = scope.ServiceProvider.GetRequiredService<SignupOpenNotificationService>();
                await signupOpenNotificationService.NotifyNewlyOpenGamesAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Failed to notify newly-open games");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
