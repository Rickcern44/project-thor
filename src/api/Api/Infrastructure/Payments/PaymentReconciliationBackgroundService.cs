namespace ProjectThor.Api.Infrastructure.Payments;

/// <summary>Periodically closes out charges for signups that stayed Waitlisted past game time.</summary>
public class PaymentReconciliationBackgroundService(
    IServiceScopeFactory scopeFactory, ILogger<PaymentReconciliationBackgroundService> logger)
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
                var reconciliationService = scope.ServiceProvider.GetRequiredService<PaymentReconciliationService>();
                await reconciliationService.ReconcilePastGameChargesAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Failed to reconcile past-game charges");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
