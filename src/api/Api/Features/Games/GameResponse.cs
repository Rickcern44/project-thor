using ProjectThor.Data.Entities;

namespace ProjectThor.Api.Features.Games;

public sealed record GameResponse(
    Guid Id,
    DateTimeOffset StartsAt,
    DateTimeOffset SignupOpensAt,
    int Capacity,
    decimal Fee,
    bool IsAdHoc,
    bool IsCancelled,
    string State)
{
    public static GameResponse From(Game game, DateTimeOffset now) => new(
        game.Id,
        game.StartsAt,
        game.SignupOpensAt,
        game.Capacity,
        game.Fee,
        game.IsAdHoc,
        game.IsCancelled,
        game.GetState(now).ToString());
}
