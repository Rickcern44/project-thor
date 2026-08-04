using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ProjectThor.Data;
using ProjectThor.Data.Entities;
using WebPush;
using PushSubscriptionEntity = ProjectThor.Data.Entities.PushSubscription;

namespace ProjectThor.Api.Infrastructure.Notifications;

/// <summary>
/// The in-app list is the reliable baseline (design.md D9); every call records a Notification
/// row regardless of push outcome, then best-effort attempts push to each of the user's
/// subscriptions. A subscription that's gone stale (410/404) is removed; other push failures
/// are swallowed so a delivery failure can never stop the in-app record from existing.
/// </summary>
public class NotificationService(AppDbContext dbContext, WebPushClient webPushClient, IOptions<VapidOptions> vapidOptions, ILogger<NotificationService> logger)
{
    public async Task NotifyAsync(Guid userId, NotificationType type, string message, CancellationToken cancellationToken)
    {
        dbContext.Notifications.Add(new Notification { UserId = userId, Type = type, Message = message });
        await dbContext.SaveChangesAsync(cancellationToken);

        var subscriptions = await dbContext.PushSubscriptions
            .Where(p => p.UserId == userId)
            .ToListAsync(cancellationToken);

        foreach (var subscription in subscriptions)
        {
            await SendPushAsync(subscription, message, cancellationToken);
        }
    }

    private async Task SendPushAsync(PushSubscriptionEntity subscription, string message, CancellationToken cancellationToken)
    {
        var vapid = vapidOptions.Value;
        var pushSubscription = new WebPush.PushSubscription(subscription.Endpoint, subscription.P256dhKey, subscription.AuthKey);
        var vapidDetails = new VapidDetails(vapid.Subject, vapid.PublicKey, vapid.PrivateKey);

        try
        {
            await webPushClient.SendNotificationAsync(pushSubscription, message, vapidDetails, cancellationToken);
        }
        catch (WebPushException ex) when (ex.StatusCode is System.Net.HttpStatusCode.Gone or System.Net.HttpStatusCode.NotFound)
        {
            dbContext.PushSubscriptions.Remove(subscription);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (WebPushException ex)
        {
            logger.LogWarning(ex, "Push delivery failed for subscription {SubscriptionId}", subscription.Id);
        }
    }
}
