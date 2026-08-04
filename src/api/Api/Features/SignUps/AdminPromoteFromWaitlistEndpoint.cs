using Microsoft.EntityFrameworkCore;
using ProjectThor.Data;
using ProjectThor.Data.Entities;

namespace ProjectThor.Api.Features.SignUps;

/// <summary>Admin-decided waitlist promotion into an open roster spot - never automatic.</summary>
public static class AdminPromoteFromWaitlistEndpoint
{
    public static void MapAdminPromoteFromWaitlist(this IEndpointRouteBuilder app)
    {
        app.MapPost("/admin/games/{gameId:guid}/promote", Handle).RequireAuthorization("AdminOnly");
    }

    private static async Task<IResult> Handle(
        Guid gameId,
        AdminPromoteRequest request,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var game = await dbContext.Games.FirstOrDefaultAsync(g => g.Id == gameId, cancellationToken);
        if (game is null || game.IsCancelled)
        {
            return Results.NotFound();
        }

        var signUp = await dbContext.SignUps.FirstOrDefaultAsync(
            s => s.GameId == gameId
                && s.PlayerUserId == request.PlayerUserId
                && s.Status == SignUpStatus.Waitlisted
                && s.CancelledAt == null,
            cancellationToken);

        if (signUp is null)
        {
            return Results.NotFound("This player is not on the waitlist for this game.");
        }

        var rosterCount = await dbContext.SignUps
            .CountAsync(s => s.GameId == gameId && s.Status == SignUpStatus.Rostered && s.CancelledAt == null, cancellationToken);
        if (rosterCount >= game.Capacity)
        {
            return Results.Conflict("No open roster spot to promote into.");
        }

        signUp.Status = SignUpStatus.Rostered;
        signUp.WaitlistPosition = null;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(SignUpResponse.From(signUp));
    }

    public sealed record AdminPromoteRequest(Guid PlayerUserId);
}
