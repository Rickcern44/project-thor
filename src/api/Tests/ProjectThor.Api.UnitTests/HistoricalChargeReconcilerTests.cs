using ProjectThor.Api.Infrastructure.Import;

namespace ProjectThor.Api.UnitTests;

public class HistoricalChargeReconcilerTests
{
    [Fact]
    public void Exact_payment_needs_no_adjustment()
    {
        // Dylan: 18 games, $7/game, paid $84 -> 12 paid, 6 owed ($42), true owed $42.
        var result = HistoricalChargeReconciler.Reconcile(attendedGameCount: 18, perGameRate: 7.00m, amountPaid: 84.00m, totalDue: 126.00m);

        Assert.Equal(12, result.PaidGameCount);
        Assert.Equal(0m, result.LegacyBalanceAdjustment);
    }

    [Fact]
    public void Non_divisible_payment_absorbs_the_remainder_into_the_adjustment()
    {
        // Jason: 19 games, $7/game, paid $123 -> floor(123/7)=17 paid, 2 owed ($14), true owed $10 -> adjustment -4.
        var result = HistoricalChargeReconciler.Reconcile(attendedGameCount: 19, perGameRate: 7.00m, amountPaid: 123.00m, totalDue: 133.00m);

        Assert.Equal(17, result.PaidGameCount);
        Assert.Equal(-4.00m, result.LegacyBalanceAdjustment);
    }

    [Fact]
    public void Overpayment_marks_every_game_paid_and_credits_the_excess()
    {
        // John Cooke: 23 games, $7/game, paid $196 (due $161) -> all 23 paid, credit -35.
        var result = HistoricalChargeReconciler.Reconcile(attendedGameCount: 23, perGameRate: 7.00m, amountPaid: 196.00m, totalDue: 161.00m);

        Assert.Equal(23, result.PaidGameCount);
        Assert.Equal(-35.00m, result.LegacyBalanceAdjustment);
    }

    [Fact]
    public void Zero_attendance_has_no_games_and_pure_legacy_balance()
    {
        var result = HistoricalChargeReconciler.Reconcile(attendedGameCount: 0, perGameRate: 7.00m, amountPaid: 0m, totalDue: 0m);

        Assert.Equal(0, result.PaidGameCount);
        Assert.Equal(0m, result.LegacyBalanceAdjustment);
    }
}
