using Microsoft.EntityFrameworkCore;
using ProjectThor.Api.Infrastructure.Scheduling;
using ProjectThor.Data;

namespace ProjectThor.Api.Features.Games;

/// <summary>The next non-cancelled game, whether currently Open or still Closed (opens in the future).</summary>
public static class GetLiveGameEndpoint
{
    public static void MapGetLiveGame(this IEndpointRouteBuilder app)
    {
        app.MapGet("/games/live", Handle).RequireAuthorization();
    }

    private static async Task<IResult> Handle(
        AppDbContext dbContext,
        GameSchedulingService schedulingService,
        CancellationToken cancellationToken)
    {
        await schedulingService.EnsureNextGameMaterializedAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var game = await dbContext.Games
            .Where(g => !g.IsCancelled && g.StartsAt > now)
            .OrderBy(g => g.StartsAt)
            .FirstOrDefaultAsync(cancellationToken);

        return game is null ? Results.NoContent() : Results.Ok(GameResponse.From(game, now));
    }
}
