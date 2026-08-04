using Microsoft.Extensions.Options;
using ProjectThor.Api.Infrastructure.Notifications;

namespace ProjectThor.Api.Features.Notifications;

/// <summary>The VAPID public key the frontend needs to create a PushSubscription - not secret.</summary>
public static class GetVapidPublicKeyEndpoint
{
    public static void MapGetVapidPublicKey(this IEndpointRouteBuilder app)
    {
        app.MapGet("/push/vapid-public-key", Handle).RequireAuthorization();
    }

    private static IResult Handle(IOptions<VapidOptions> vapidOptions) =>
        Results.Ok(new { PublicKey = vapidOptions.Value.PublicKey });
}
