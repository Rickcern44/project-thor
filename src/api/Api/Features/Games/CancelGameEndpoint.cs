using Microsoft.EntityFrameworkCore;
using ProjectThor.Data;

namespace ProjectThor.Api.Features.Games;

/// <summary>Cancels a single game instance only - no series effects (design.md D7).</summary>
public static class CancelGameEndpoint
{
    public static void MapCancelGame(this IEndpointRouteBuilder app)
    {
        app.MapPost("/admin/games/{id:guid}/cancel", Handle).RequireAuthorization("AdminOnly");
    }

    private static async Task<IResult> Handle(Guid id, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        var game = await dbContext.Games.FirstOrDefaultAsync(g => g.Id == id, cancellationToken);
        if (game is null)
        {
            return Results.NotFound();
        }

        game.IsCancelled = true;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.NoContent();
    }
}
