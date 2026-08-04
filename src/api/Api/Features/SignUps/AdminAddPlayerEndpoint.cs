using Microsoft.EntityFrameworkCore;
using ProjectThor.Api.Infrastructure.SignUps;
using ProjectThor.Data;
using ProjectThor.Data.Entities;

namespace ProjectThor.Api.Features.SignUps;

/// <summary>Admin adds any player to a game's roster or waitlist.</summary>
public static class AdminAddPlayerEndpoint
{
    public static void MapAdminAddPlayer(this IEndpointRouteBuilder app)
    {
        app.MapPost("/admin/games/{gameId:guid}/roster", Handle).RequireAuthorization("AdminOnly");
    }

    private static async Task<IResult> Handle(
        Guid gameId,
        AdminAddPlayerRequest request,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var game = await dbContext.Games.FirstOrDefaultAsync(g => g.Id == gameId, cancellationToken);
        if (game is null || game.IsCancelled)
        {
            return Results.NotFound();
        }

        var alreadySignedUp = await dbContext.SignUps.AnyAsync(
            s => s.GameId == gameId && s.PlayerUserId == request.PlayerUserId && s.CancelledAt == null,
            cancellationToken);
        if (alreadySignedUp)
        {
            return Results.Conflict("This player is already signed up for this game.");
        }

        var rosterCount = await dbContext.SignUps
            .CountAsync(s => s.GameId == gameId && s.Status == SignUpStatus.Rostered && s.CancelledAt == null, cancellationToken);
        var waitlistCount = await dbContext.SignUps
            .CountAsync(s => s.GameId == gameId && s.Status == SignUpStatus.Waitlisted && s.CancelledAt == null, cancellationToken);

        var (status, waitlistPosition) = SignUpAssignment.Assign(rosterCount, game.Capacity, waitlistCount);

        var signUp = new SignUp
        {
            GameId = gameId,
            PlayerUserId = request.PlayerUserId,
            Status = status,
            WaitlistPosition = waitlistPosition
        };
        dbContext.SignUps.Add(signUp);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Created($"/admin/games/{gameId}/roster/{signUp.Id}", SignUpResponse.From(signUp));
    }

    public sealed record AdminAddPlayerRequest(Guid PlayerUserId);
}
