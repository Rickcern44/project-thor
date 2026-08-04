using ProjectThor.Data.Entities;

namespace ProjectThor.Api.Features.Charges;

public sealed record ChargeResponse(
    Guid Id,
    Guid GameId,
    Guid PlayerUserId,
    decimal Amount,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ResolvedAt)
{
    public static ChargeResponse From(Charge charge) => new(
        charge.Id,
        charge.GameId,
        charge.PlayerUserId,
        charge.Amount,
        charge.Status.ToString(),
        charge.CreatedAt,
        charge.ResolvedAt);
}
