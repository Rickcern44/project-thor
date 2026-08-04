namespace ProjectThor.Data.Entities;

public enum GameState
{
    Closed,
    Open,
    Past
}

/// <summary>
/// A single game instance. State is derived from time, not stored, so it can never drift
/// out of sync with the clock: Closed until SignupOpensAt, Open until StartsAt, Past after.
/// </summary>
public class Game
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid? TemplateId { get; set; }
    public GameTemplate? Template { get; set; }
    public required DateTimeOffset StartsAt { get; set; }
    public required DateTimeOffset SignupOpensAt { get; set; }
    public int Capacity { get; set; }
    public decimal Fee { get; set; }
    public bool IsAdHoc { get; set; }
    public bool IsCancelled { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>Set once the "sign-ups are open" notification has fired, so it's never sent twice.</summary>
    public DateTimeOffset? SignupOpenNotifiedAt { get; set; }

    public GameState GetState(DateTimeOffset now)
    {
        if (now >= StartsAt)
        {
            return GameState.Past;
        }

        return now >= SignupOpensAt ? GameState.Open : GameState.Closed;
    }
}
