using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using ProjectThor.Api.Infrastructure.SignUps;
using ProjectThor.Data;
using ProjectThor.Data.Entities;

namespace ProjectThor.Api.Features.SignUps;

/// <summary>Player self-service sign-up for the live game (own account only).</summary>
public static class SignUpForGameEndpoint
{
    public static void MapSignUpForGame(this IEndpointRouteBuilder app)
    {
        app.MapPost("/games/{gameId:guid}/signup", Handle).RequireAuthorization();
    }

    private static async Task<IResult> Handle(
        Guid gameId,
        ClaimsPrincipal user,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var playerUserId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var game = await dbContext.Games.FirstOrDefaultAsync(g => g.Id == gameId, cancellationToken);
        if (game is null || game.IsCancelled)
        {
            return Results.NotFound();
        }

        if (game.GetState(DateTimeOffset.UtcNow) != GameState.Open)
        {
            return Results.BadRequest("This game is not currently open for sign-up.");
        }

        var alreadySignedUp = await dbContext.SignUps
            .AnyAsync(s => s.GameId == gameId && s.PlayerUserId == playerUserId && s.CancelledAt == null, cancellationToken);
        if (alreadySignedUp)
        {
            return Results.Conflict("You are already signed up for this game.");
        }

        var rosterCount = await dbContext.SignUps
            .CountAsync(s => s.GameId == gameId && s.Status == SignUpStatus.Rostered && s.CancelledAt == null, cancellationToken);
        var waitlistCount = await dbContext.SignUps
            .CountAsync(s => s.GameId == gameId && s.Status == SignUpStatus.Waitlisted && s.CancelledAt == null, cancellationToken);

        var (status, waitlistPosition) = SignUpAssignment.Assign(rosterCount, game.Capacity, waitlistCount);

        var signUp = new SignUp
        {
            GameId = gameId,
            PlayerUserId = playerUserId,
            Status = status,
            WaitlistPosition = waitlistPosition
        };
        dbContext.SignUps.Add(signUp);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Created($"/games/{gameId}/signup/{signUp.Id}", SignUpResponse.From(signUp));
    }
}
