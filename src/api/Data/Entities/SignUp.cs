namespace ProjectThor.Data.Entities;

public enum SignUpStatus
{
    Rostered,
    Waitlisted
}

public class SignUp
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required Guid GameId { get; set; }
    public Game? Game { get; set; }
    public required Guid PlayerUserId { get; set; }
    public User? PlayerUser { get; set; }
    public SignUpStatus Status { get; set; }

    /// <summary>Arrival order on the waitlist; null once Status is Rostered.</summary>
    public int? WaitlistPosition { get; set; }

    public DateTimeOffset SignedUpAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CancelledAt { get; set; }
}
