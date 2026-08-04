using ProjectThor.Api.Infrastructure.SignUps;
using ProjectThor.Data.Entities;

namespace ProjectThor.Api.UnitTests;

public class SignUpAssignmentTests
{
    [Fact]
    public void Rosters_when_under_capacity()
    {
        var (status, position) = SignUpAssignment.Assign(currentRosterCount: 5, capacity: 10, currentWaitlistCount: 0);

        Assert.Equal(SignUpStatus.Rostered, status);
        Assert.Null(position);
    }

    [Fact]
    public void Waitlists_at_first_position_when_at_capacity_with_no_existing_waitlist()
    {
        var (status, position) = SignUpAssignment.Assign(currentRosterCount: 10, capacity: 10, currentWaitlistCount: 0);

        Assert.Equal(SignUpStatus.Waitlisted, status);
        Assert.Equal(1, position);
    }

    [Fact]
    public void Waitlists_behind_existing_waitlisted_players_in_arrival_order()
    {
        var (status, position) = SignUpAssignment.Assign(currentRosterCount: 10, capacity: 10, currentWaitlistCount: 3);

        Assert.Equal(SignUpStatus.Waitlisted, status);
        Assert.Equal(4, position);
    }
}
