using Microsoft.EntityFrameworkCore;
using ProjectThor.Data;

namespace ProjectThor.Api.Features.Games;

/// <summary>Past games awaiting reconciliation, available to the Admin.</summary>
public static class ListPastGamesEndpoint
{
    public static void MapListPastGames(this IEndpointRouteBuilder app)
    {
        app.MapGet("/admin/games/past", Handle).RequireAuthorization("AdminOnly");
    }

    private static async Task<IResult> Handle(AppDbContext dbContext, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var pastGames = await dbContext.Games
            .Where(g => !g.IsCancelled && g.StartsAt <= now)
            .OrderByDescending(g => g.StartsAt)
            .ToListAsync(cancellationToken);

        return Results.Ok(pastGames.Select(g => GameResponse.From(g, now)));
    }
}
