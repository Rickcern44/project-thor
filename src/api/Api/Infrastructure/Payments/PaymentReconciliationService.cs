using Microsoft.EntityFrameworkCore;
using ProjectThor.Data;
using ProjectThor.Data.Entities;

namespace ProjectThor.Api.Infrastructure.Payments;

/// <summary>
/// A charge "stands as owed" only for a player still on the roster at game time (spec:
/// payment-tracking). The spec is silent on a charge for a signup that stayed Waitlisted and
/// was never promoted - erasing it (no penalty, matching the cancel path) is the only outcome
/// consistent with design.md's "no charge without a roster spot" principle.
/// </summary>
public class PaymentReconciliationService(AppDbContext dbContext)
{
    public async Task ReconcilePastGameChargesAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        var owedChargesForPastGames = await dbContext.Charges
            .Where(c => c.Status == ChargeStatus.Owed && dbContext.Games.Any(g => g.Id == c.GameId && g.StartsAt <= now))
            .ToListAsync(cancellationToken);

        foreach (var charge in owedChargesForPastGames)
        {
            var isStillRostered = await dbContext.SignUps.AnyAsync(
                s => s.GameId == charge.GameId
                    && s.PlayerUserId == charge.PlayerUserId
                    && s.Status == SignUpStatus.Rostered
                    && s.CancelledAt == null,
                cancellationToken);

            if (!isStillRostered)
            {
                charge.Status = ChargeStatus.Erased;
                charge.ResolvedAt = now;
            }
        }

        if (owedChargesForPastGames.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
