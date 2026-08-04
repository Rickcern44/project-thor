using ProjectThor.Data.Entities;

namespace ProjectThor.Api.Features.SignUps;

public sealed record SignUpResponse(
    Guid Id,
    Guid GameId,
    Guid PlayerUserId,
    string Status,
    int? WaitlistPosition,
    DateTimeOffset SignedUpAt)
{
    public static SignUpResponse From(SignUp signUp) => new(
        signUp.Id,
        signUp.GameId,
        signUp.PlayerUserId,
        signUp.Status.ToString(),
        signUp.WaitlistPosition,
        signUp.SignedUpAt);
}
