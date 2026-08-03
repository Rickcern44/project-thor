namespace ProjectThor.Data.Entities;

/// <summary>A browser's Web Push subscription (PushSubscriptionJSON) for one user's one device.</summary>
public class PushSubscription
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required Guid UserId { get; set; }
    public User? User { get; set; }
    public required string Endpoint { get; set; }
    public required string P256dhKey { get; set; }
    public required string AuthKey { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}
