namespace ProjectThor.Data.Entities;

public enum ChargeStatus
{
    Owed,
    Paid,
    Waived,
    Erased
}

/// <summary>
/// The charge lifecycle IS the attendance record (design.md D4): created on sign-up,
/// Erased on a pre-game cancel, stands as Owed if still rostered at game time, and can be
/// Waived by an admin after the game (no-show reconciliation) or marked Paid once settled.
/// </summary>
public class Charge
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required Guid GameId { get; set; }
    public Game? Game { get; set; }
    public required Guid PlayerUserId { get; set; }
    public User? PlayerUser { get; set; }
    public decimal Amount { get; set; }
    public ChargeStatus Status { get; set; } = ChargeStatus.Owed;
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ResolvedAt { get; set; }
}
