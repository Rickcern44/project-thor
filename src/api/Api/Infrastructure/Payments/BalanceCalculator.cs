using Microsoft.EntityFrameworkCore;
using ProjectThor.Data;
using ProjectThor.Data.Entities;

namespace ProjectThor.Api.Infrastructure.Payments;

/// <summary>
/// Balance is computed, never stored: imported legacy debt (RosterRecord.LegacyBalance) plus
/// every currently-Owed charge. Nothing here can drift out of sync with the charge ledger.
/// </summary>
public static class BalanceCalculator
{
    public static async Task<decimal> GetBalanceAsync(AppDbContext dbContext, Guid playerUserId, CancellationToken cancellationToken)
    {
        var legacyBalance = await dbContext.Users
            .Where(u => u.Id == playerUserId)
            .Select(u => u.RosterRecord != null ? u.RosterRecord.LegacyBalance : 0m)
            .FirstOrDefaultAsync(cancellationToken);

        var owedTotal = await dbContext.Charges
            .Where(c => c.PlayerUserId == playerUserId && c.Status == ChargeStatus.Owed)
            .SumAsync(c => (decimal?)c.Amount, cancellationToken) ?? 0m;

        return legacyBalance + owedTotal;
    }
}
