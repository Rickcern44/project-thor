using System.Security.Claims;
using ProjectThor.Api.Infrastructure.Auth;
using ProjectThor.Api.Infrastructure.Payments;
using ProjectThor.Data;

namespace ProjectThor.Api.Features.Charges;

/// <summary>A player's own balance, or any player's balance for an Admin - surfaced prominently, never blocking sign-up.</summary>
public static class GetPlayerBalanceEndpoint
{
    public static void MapGetPlayerBalance(this IEndpointRouteBuilder app)
    {
        app.MapGet("/players/{playerUserId:guid}/balance", Handle).RequireAuthorization();
    }

    private static async Task<IResult> Handle(
        Guid playerUserId,
        ClaimsPrincipal user,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (!PlayerScopeAuthorization.CanActOnPlayer(user, playerUserId))
        {
            return Results.Forbid();
        }

        var balance = await BalanceCalculator.GetBalanceAsync(dbContext, playerUserId, cancellationToken);
        return Results.Ok(new BalanceResponse(playerUserId, balance));
    }
}
