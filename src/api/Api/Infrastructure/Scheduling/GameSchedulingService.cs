using Microsoft.EntityFrameworkCore;
using ProjectThor.Data;
using ProjectThor.Data.Entities;

namespace ProjectThor.Api.Infrastructure.Scheduling;

/// <summary>
/// Materializes the next game from the active template once the current one's start time
/// passes, with no admin action required (design.md D6/D7 - the template is a generator,
/// not a live link, so each materialized instance is independent).
/// </summary>
public class GameSchedulingService(AppDbContext dbContext)
{
    public async Task EnsureNextGameMaterializedAsync(CancellationToken cancellationToken)
    {
        var template = await dbContext.GameTemplates
            .Where(t => t.IsActive)
            .FirstOrDefaultAsync(cancellationToken);

        if (template is null)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;

        var latestGeneratedGame = await dbContext.Games
            .Where(g => g.TemplateId == template.Id && !g.IsCancelled)
            .OrderByDescending(g => g.StartsAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (latestGeneratedGame is not null && latestGeneratedGame.StartsAt > now)
        {
            // The current game hasn't started yet - nothing to generate.
            return;
        }

        var referenceInstant = latestGeneratedGame?.StartsAt ?? now;
        var nextStartsAt = RecurringScheduleCalculator.GetNextOccurrence(template.DayOfWeek, template.TimeOfDay, referenceInstant);

        var alreadyExists = await dbContext.Games
            .AnyAsync(g => g.TemplateId == template.Id && g.StartsAt == nextStartsAt, cancellationToken);
        if (alreadyExists)
        {
            return;
        }

        dbContext.Games.Add(new Game
        {
            TemplateId = template.Id,
            StartsAt = nextStartsAt,
            SignupOpensAt = nextStartsAt - template.SignupLeadTime,
            Capacity = template.DefaultCapacity,
            Fee = template.Fee,
            IsAdHoc = false
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
