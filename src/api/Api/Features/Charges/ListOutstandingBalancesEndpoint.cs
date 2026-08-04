using Microsoft.EntityFrameworkCore;
using ProjectThor.Data;
using ProjectThor.Data.Entities;

namespace ProjectThor.Api.Features.Charges;

/// <summary>Every player with an outstanding balance, for an Admin to chase - informational only, never blocks sign-up.</summary>
public static class ListOutstandingBalancesEndpoint
{
    public static void MapListOutstandingBalances(this IEndpointRouteBuilder app)
    {
        app.MapGet("/admin/balances", Handle).RequireAuthorization("AdminOnly");
    }

    private static async Task<IResult> Handle(AppDbContext dbContext, CancellationToken cancellationToken)
    {
        var owedByPlayer = await dbContext.Charges
            .Where(c => c.Status == ChargeStatus.Owed)
            .GroupBy(c => c.PlayerUserId)
            .Select(g => new { PlayerUserId = g.Key, Owed = g.Sum(c => c.Amount) })
            .ToDictionaryAsync(x => x.PlayerUserId, x => x.Owed, cancellationToken);

        var legacyByPlayer = await dbContext.Users
            .Where(u => u.RosterRecord != null)
            .Select(u => new { PlayerUserId = u.Id, Legacy = u.RosterRecord!.LegacyBalance })
            .ToDictionaryAsync(x => x.PlayerUserId, x => x.Legacy, cancellationToken);

        var playerIds = owedByPlayer.Keys.Union(legacyByPlayer.Keys.Where(id => legacyByPlayer[id] != 0m));

        var balances = playerIds
            .Select(id => new BalanceResponse(
                id,
                owedByPlayer.GetValueOrDefault(id) + legacyByPlayer.GetValueOrDefault(id)))
            .Where(b => b.Balance != 0m)
            .OrderByDescending(b => b.Balance);

        return Results.Ok(balances);
    }
}
