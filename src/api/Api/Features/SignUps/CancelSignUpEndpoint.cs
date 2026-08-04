using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using ProjectThor.Api.Infrastructure.Payments;
using ProjectThor.Data;
using ProjectThor.Data.Entities;

namespace ProjectThor.Api.Features.SignUps;

/// <summary>Player self-service cancellation of their own sign-up (roster or waitlist).</summary>
public static class CancelSignUpEndpoint
{
    public static void MapCancelSignUp(this IEndpointRouteBuilder app)
    {
        app.MapPost("/games/{gameId:guid}/cancel", Handle).RequireAuthorization();
    }

    private static async Task<IResult> Handle(
        Guid gameId,
        ClaimsPrincipal user,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var playerUserId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var signUp = await dbContext.SignUps.FirstOrDefaultAsync(
            s => s.GameId == gameId && s.PlayerUserId == playerUserId && s.CancelledAt == null,
            cancellationToken);

        if (signUp is null)
        {
            return Results.NotFound();
        }

        var now = DateTimeOffset.UtcNow;
        signUp.CancelledAt = now;
        await ChargeErasure.EraseIfOwedAsync(dbContext, gameId, playerUserId, now, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.NoContent();
    }
}
