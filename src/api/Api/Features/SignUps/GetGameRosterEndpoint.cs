using Microsoft.EntityFrameworkCore;
using ProjectThor.Data;
using ProjectThor.Data.Entities;

namespace ProjectThor.Api.Features.SignUps;

/// <summary>The current roster and waitlist for a game, in arrival order.</summary>
public static class GetGameRosterEndpoint
{
    public static void MapGetGameRoster(this IEndpointRouteBuilder app)
    {
        app.MapGet("/games/{gameId:guid}/roster", Handle).RequireAuthorization();
    }

    private static async Task<IResult> Handle(Guid gameId, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        var signUps = await dbContext.SignUps
            .Where(s => s.GameId == gameId && s.CancelledAt == null)
            .OrderBy(s => s.Status == SignUpStatus.Waitlisted ? s.WaitlistPosition : 0)
            .ThenBy(s => s.SignedUpAt)
            .ToListAsync(cancellationToken);

        return Results.Ok(signUps.Select(SignUpResponse.From));
    }
}
