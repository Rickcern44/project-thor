using Microsoft.EntityFrameworkCore;
using ProjectThor.Data;

namespace ProjectThor.Api.Features.SignUps;

/// <summary>Admin removes any player from a game's roster or waitlist; the spot becomes open.</summary>
public static class AdminRemovePlayerEndpoint
{
    public static void MapAdminRemovePlayer(this IEndpointRouteBuilder app)
    {
        app.MapDelete("/admin/games/{gameId:guid}/roster/{playerUserId:guid}", Handle).RequireAuthorization("AdminOnly");
    }

    private static async Task<IResult> Handle(
        Guid gameId,
        Guid playerUserId,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var signUp = await dbContext.SignUps.FirstOrDefaultAsync(
            s => s.GameId == gameId && s.PlayerUserId == playerUserId && s.CancelledAt == null,
            cancellationToken);

        if (signUp is null)
        {
            return Results.NotFound();
        }

        signUp.CancelledAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.NoContent();
    }
}
