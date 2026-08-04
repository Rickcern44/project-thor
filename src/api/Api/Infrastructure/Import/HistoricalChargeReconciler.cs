namespace ProjectThor.Api.Infrastructure.Import;

public sealed record HistoricalChargeReconciliation(int PaidGameCount, decimal LegacyBalanceAdjustment);

/// <summary>
/// Reconciles a season of imported attendance against an aggregate (not per-game) payment total.
/// Oldest games are treated as paid first (FIFO); any dollar remainder that doesn't divide evenly
/// into whole per-game charges is absorbed into a small RosterRecord.LegacyBalance adjustment, so
/// the CURRENT balance always comes out exact even though individual historical charges are only
/// ever whole-game amounts.
/// </summary>
public static class HistoricalChargeReconciler
{
    public static HistoricalChargeReconciliation Reconcile(int attendedGameCount, decimal perGameRate, decimal amountPaid, decimal totalDue)
    {
        if (attendedGameCount == 0)
        {
            return new HistoricalChargeReconciliation(0, totalDue - amountPaid);
        }

        var gamesCoveredByPayment = perGameRate == 0m ? attendedGameCount : (int)Math.Floor(amountPaid / perGameRate);
        var paidGameCount = Math.Clamp(gamesCoveredByPayment, 0, attendedGameCount);

        var owedGameCount = attendedGameCount - paidGameCount;
        var owedFromCharges = owedGameCount * perGameRate;
        var trueOwed = totalDue - amountPaid;
        var adjustment = trueOwed - owedFromCharges;

        return new HistoricalChargeReconciliation(paidGameCount, adjustment);
    }
}
