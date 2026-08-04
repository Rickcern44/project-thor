using ProjectThor.Data.Entities;

namespace ProjectThor.Api.Features.Notifications;

public sealed record NotificationResponse(Guid Id, string Type, string Message, DateTimeOffset CreatedAt, DateTimeOffset? ReadAt)
{
    public static NotificationResponse From(Notification notification) => new(
        notification.Id, notification.Type.ToString(), notification.Message, notification.CreatedAt, notification.ReadAt);
}
