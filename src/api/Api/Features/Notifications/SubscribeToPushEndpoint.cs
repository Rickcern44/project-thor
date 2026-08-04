using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using ProjectThor.Data;
using ProjectThor.Data.Entities;

namespace ProjectThor.Api.Features.Notifications;

public static class SubscribeToPushEndpoint
{
    public static void MapSubscribeToPush(this IEndpointRouteBuilder app)
    {
        app.MapPost("/push/subscribe", Handle).RequireAuthorization();
    }

    private static async Task<IResult> Handle(
        SubscribeRequest request, ClaimsPrincipal user, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var existing = await dbContext.PushSubscriptions
            .FirstOrDefaultAsync(p => p.Endpoint == request.Endpoint, cancellationToken);

        if (existing is not null)
        {
            existing.UserId = userId;
            existing.P256dhKey = request.P256dhKey;
            existing.AuthKey = request.AuthKey;
        }
        else
        {
            dbContext.PushSubscriptions.Add(new PushSubscription
            {
                UserId = userId,
                Endpoint = request.Endpoint,
                P256dhKey = request.P256dhKey,
                AuthKey = request.AuthKey
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.NoContent();
    }

    public sealed record SubscribeRequest(string Endpoint, string P256dhKey, string AuthKey);
}
