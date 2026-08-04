namespace ProjectThor.Api.Infrastructure.Scheduling;

/// <summary>
/// Enforces the single-live-game invariant (design.md D6): at most one game's sign-up
/// window may be open at a time. Applies to every game, template-generated or ad-hoc.
/// </summary>
public static class GameOverlapValidator
{
    public static bool OverlapsAnOpenWindow(
        IEnumerable<(DateTimeOffset SignupOpensAt, DateTimeOffset StartsAt)> otherGames,
        DateTimeOffset signupOpensAt,
        DateTimeOffset startsAt) =>
        otherGames.Any(g => signupOpensAt < g.StartsAt && startsAt > g.SignupOpensAt);
}
