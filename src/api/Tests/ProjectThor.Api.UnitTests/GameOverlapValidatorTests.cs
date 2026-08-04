using ProjectThor.Api.Infrastructure.Scheduling;

namespace ProjectThor.Api.UnitTests;

public class GameOverlapValidatorTests
{
    private static readonly DateTimeOffset Day1 = new(2024, 1, 1, 18, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Day8 = new(2024, 1, 8, 18, 0, 0, TimeSpan.Zero);

    [Fact]
    public void No_overlap_when_windows_are_in_a_quiet_gap_between_games()
    {
        // Existing game: opens Day7 18:00, starts Day8 18:00. Candidate: opens Day1, starts Day2 -> no overlap.
        var existing = new[] { (SignupOpensAt: Day8.AddDays(-1), StartsAt: Day8) };

        var overlaps = GameOverlapValidator.OverlapsAnOpenWindow(existing, Day1, Day1.AddDays(1));

        Assert.False(overlaps);
    }

    [Fact]
    public void Overlap_when_candidate_window_starts_before_existing_game_ends()
    {
        // Existing: opens Day1, starts Day8. Candidate opens Day1+1, starts Day1+2 -> falls inside existing's open window.
        var existing = new[] { (SignupOpensAt: Day1, StartsAt: Day8) };

        var overlaps = GameOverlapValidator.OverlapsAnOpenWindow(existing, Day1.AddDays(1), Day1.AddDays(2));

        Assert.True(overlaps);
    }

    [Fact]
    public void No_overlap_when_candidate_starts_exactly_when_existing_opens()
    {
        // Boundary: candidate's window ends exactly as existing's begins - touching, not overlapping.
        var existing = new[] { (SignupOpensAt: Day8, StartsAt: Day8.AddDays(7)) };

        var overlaps = GameOverlapValidator.OverlapsAnOpenWindow(existing, Day1, Day8);

        Assert.False(overlaps);
    }
}
