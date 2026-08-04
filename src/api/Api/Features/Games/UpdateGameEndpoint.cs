using Microsoft.EntityFrameworkCore;
using ProjectThor.Api.Infrastructure.Scheduling;
using ProjectThor.Data;

namespace ProjectThor.Api.Features.Games;

/// <summary>Edits a single game instance only - no series effects (design.md D7).</summary>
public static class UpdateGameEndpoint
{
    public static void MapUpdateGame(this IEndpointRouteBuilder app)
    {
        app.MapPatch("/admin/games/{id:guid}", Handle).RequireAuthorization("AdminOnly");
    }

    private static async Task<IResult> Handle(
        Guid id,
        UpdateGameRequest request,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var game = await dbContext.Games.FirstOrDefaultAsync(g => g.Id == id, cancellationToken);
        if (game is null)
        {
            return Results.NotFound();
        }

        if (game.IsCancelled)
        {
            return Results.Conflict("Cannot edit a cancelled game.");
        }

        var newStartsAt = request.StartsAt ?? game.StartsAt;
        var newSignupOpensAt = request.SignupOpensAt ?? game.SignupOpensAt;

        if (newSignupOpensAt >= newStartsAt)
        {
            return Results.BadRequest("Sign-up must open before the game starts.");
        }

        var otherGames = await dbContext.Games
            .Where(g => g.Id != id && !g.IsCancelled)
            .Select(g => new { g.SignupOpensAt, g.StartsAt })
            .ToListAsync(cancellationToken);

        var overlaps = GameOverlapValidator.OverlapsAnOpenWindow(
            otherGames.Select(g => (g.SignupOpensAt, g.StartsAt)),
            newSignupOpensAt,
            newStartsAt);

        if (overlaps)
        {
            return Results.Conflict("This change would overlap with another game's sign-up window.");
        }

        game.StartsAt = newStartsAt;
        game.SignupOpensAt = newSignupOpensAt;
        game.Capacity = request.Capacity ?? game.Capacity;
        game.Fee = request.Fee ?? game.Fee;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(GameResponse.From(game, DateTimeOffset.UtcNow));
    }

    public sealed record UpdateGameRequest(
        DateTimeOffset? StartsAt, DateTimeOffset? SignupOpensAt, int? Capacity, decimal? Fee);
}
