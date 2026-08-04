namespace ProjectThor.Api.Infrastructure.Scheduling;

/// <summary>Pure date math for "the next time this weekly day/time occurs strictly after a reference instant," in UTC.</summary>
public static class RecurringScheduleCalculator
{
    public static DateTimeOffset GetNextOccurrence(DayOfWeek dayOfWeek, TimeOnly timeOfDay, DateTimeOffset after)
    {
        var afterUtc = after.ToUniversalTime();
        var candidateDate = DateOnly.FromDateTime(afterUtc.UtcDateTime);
        var candidate = ToOccurrence(candidateDate, dayOfWeek, timeOfDay);

        while (candidate <= afterUtc)
        {
            candidateDate = candidateDate.AddDays(1);
            candidate = ToOccurrence(candidateDate, dayOfWeek, timeOfDay);
        }

        return candidate;
    }

    private static DateTimeOffset ToOccurrence(DateOnly fromDate, DayOfWeek dayOfWeek, TimeOnly timeOfDay)
    {
        var daysUntil = ((int)dayOfWeek - (int)fromDate.DayOfWeek + 7) % 7;
        var occurrenceDate = fromDate.AddDays(daysUntil);
        return new DateTimeOffset(occurrenceDate.ToDateTime(timeOfDay), TimeSpan.Zero);
    }
}
