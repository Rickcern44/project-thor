namespace ProjectThor.Data.Entities;

public enum UserRole
{
    Admin,
    Player
}

public enum UserStatus
{
    Pending,
    Active
}

public class User
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Email { get; set; }
    public required string Phone { get; set; }
    public required string Name { get; set; }
    public UserRole Role { get; set; } = UserRole.Player;
    public UserStatus Status { get; set; } = UserStatus.Pending;
    public Guid? RosterRecordId { get; set; }
    public RosterRecord? RosterRecord { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ActivatedAt { get; set; }
}
