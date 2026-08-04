using ProjectThor.Api.Infrastructure.Scheduling;

namespace ProjectThor.Api.UnitTests;

public class RecurringScheduleCalculatorTests
{
    // 2024-01-01 is a known Monday, used as a stable anchor independent of "today".
    private static readonly DateTimeOffset MondayMidnight = new(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Returns_this_weeks_occurrence_when_it_has_not_happened_yet()
    {
        var next = RecurringScheduleCalculator.GetNextOccurrence(DayOfWeek.Wednesday, new TimeOnly(18, 0), MondayMidnight);

        Assert.Equal(new DateTimeOffset(2024, 1, 3, 18, 0, 0, TimeSpan.Zero), next);
    }

    [Fact]
    public void Returns_same_day_occurrence_when_time_has_not_yet_passed()
    {
        var wednesdayMorning = new DateTimeOffset(2024, 1, 3, 10, 0, 0, TimeSpan.Zero);

        var next = RecurringScheduleCalculator.GetNextOccurrence(DayOfWeek.Wednesday, new TimeOnly(18, 0), wednesdayMorning);

        Assert.Equal(new DateTimeOffset(2024, 1, 3, 18, 0, 0, TimeSpan.Zero), next);
    }

    [Fact]
    public void Rolls_to_next_week_when_exactly_at_the_occurrence_instant()
    {
        var occurrence = new DateTimeOffset(2024, 1, 3, 18, 0, 0, TimeSpan.Zero);

        var next = RecurringScheduleCalculator.GetNextOccurrence(DayOfWeek.Wednesday, new TimeOnly(18, 0), occurrence);

        Assert.Equal(new DateTimeOffset(2024, 1, 10, 18, 0, 0, TimeSpan.Zero), next);
    }

    [Fact]
    public void Rolls_to_next_week_when_this_weeks_time_has_already_passed()
    {
        var thursday = new DateTimeOffset(2024, 1, 4, 0, 0, 0, TimeSpan.Zero);

        var next = RecurringScheduleCalculator.GetNextOccurrence(DayOfWeek.Wednesday, new TimeOnly(18, 0), thursday);

        Assert.Equal(new DateTimeOffset(2024, 1, 10, 18, 0, 0, TimeSpan.Zero), next);
    }
}
