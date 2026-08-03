namespace ProjectThor.Data.Entities;

public enum NotificationType
{
    WaitlistPromotion,
    NewGameOpen
}

/// <summary>
/// The in-app list is the reliable baseline (design.md D9); every notification lands
/// here regardless of whether push delivery also succeeds.
/// </summary>
public class Notification
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required Guid UserId { get; set; }
    public User? User { get; set; }
    public NotificationType Type { get; set; }
    public required string Message { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ReadAt { get; set; }
}
