namespace ProjectThor.Api.Features.Charges;

public sealed record BalanceResponse(Guid PlayerUserId, decimal Balance);
