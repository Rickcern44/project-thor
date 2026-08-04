using Microsoft.EntityFrameworkCore;
using ProjectThor.Data;
using ProjectThor.Data.Entities;

namespace ProjectThor.Api.Infrastructure.Payments;

/// <summary>Cancellation erases the charge with no penalty (design.md D4) - only if it's still Owed.</summary>
public static class ChargeErasure
{
    public static async Task EraseIfOwedAsync(
        AppDbContext dbContext, Guid gameId, Guid playerUserId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var charge = await dbContext.Charges.FirstOrDefaultAsync(
            c => c.GameId == gameId && c.PlayerUserId == playerUserId && c.Status == ChargeStatus.Owed,
            cancellationToken);

        if (charge is not null)
        {
            charge.Status = ChargeStatus.Erased;
            charge.ResolvedAt = now;
        }
    }
}
