using Microsoft.EntityFrameworkCore;
using ProjectThor.Api.Infrastructure.Scheduling;
using ProjectThor.Data;
using ProjectThor.Data.Entities;

namespace ProjectThor.Api.Features.Games;

public static class CreateAdHocGameEndpoint
{
    public static void MapCreateAdHocGame(this IEndpointRouteBuilder app)
    {
        app.MapPost("/admin/games/adhoc", Handle).RequireAuthorization("AdminOnly");
    }

    private static async Task<IResult> Handle(
        CreateAdHocGameRequest request,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (request.SignupOpensAt >= request.StartsAt)
        {
            return Results.BadRequest("Sign-up must open before the game starts.");
        }

        var otherGames = await dbContext.Games
            .Where(g => !g.IsCancelled)
            .Select(g => new { g.SignupOpensAt, g.StartsAt })
            .ToListAsync(cancellationToken);

        var overlaps = GameOverlapValidator.OverlapsAnOpenWindow(
            otherGames.Select(g => (g.SignupOpensAt, g.StartsAt)),
            request.SignupOpensAt,
            request.StartsAt);

        if (overlaps)
        {
            return Results.Conflict(
                "This game's sign-up window overlaps with another game's - only one game may be open for sign-up at a time.");
        }

        var game = new Game
        {
            TemplateId = null,
            StartsAt = request.StartsAt,
            SignupOpensAt = request.SignupOpensAt,
            Capacity = request.Capacity,
            Fee = request.Fee,
            IsAdHoc = true
        };
        dbContext.Games.Add(game);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Created($"/admin/games/{game.Id}", GameResponse.From(game, DateTimeOffset.UtcNow));
    }

    public sealed record CreateAdHocGameRequest(
        DateTimeOffset StartsAt, DateTimeOffset SignupOpensAt, int Capacity, decimal Fee);
}
