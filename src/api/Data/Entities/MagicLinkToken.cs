namespace ProjectThor.Data.Entities;

public enum MagicLinkPurpose
{
    Invite,
    Login
}

/// <summary>
/// An invite is the first magic link a User ever receives (Purpose = Invite);
/// every subsequent login issues a new token with Purpose = Login. No passwords exist anywhere.
/// </summary>
public class MagicLinkToken
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required Guid UserId { get; set; }
    public User? User { get; set; }
    public required string TokenHash { get; set; }
    public MagicLinkPurpose Purpose { get; set; }
    public required DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? ConsumedAt { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}
