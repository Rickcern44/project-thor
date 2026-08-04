using ProjectThor.Data.Entities;

namespace ProjectThor.Api.Infrastructure.Payments;

/// <summary>Charge attaches on sign-up regardless of roster/waitlist status (spec: payment-tracking).</summary>
public static class ChargeFactory
{
    public static Charge CreateForSignUp(Guid gameId, Guid playerUserId, decimal fee) => new()
    {
        GameId = gameId,
        PlayerUserId = playerUserId,
        Amount = fee,
        Status = ChargeStatus.Owed
    };
}
