using ProjectThor.Data.Entities;

namespace ProjectThor.Api.Infrastructure.SignUps;

/// <summary>Decides roster vs. waitlist placement (spec: waitlist overflow in arrival order when at capacity).</summary>
public static class SignUpAssignment
{
    public static (SignUpStatus Status, int? WaitlistPosition) Assign(int currentRosterCount, int capacity, int currentWaitlistCount) =>
        currentRosterCount < capacity
            ? (SignUpStatus.Rostered, null)
            : (SignUpStatus.Waitlisted, currentWaitlistCount + 1);
}
