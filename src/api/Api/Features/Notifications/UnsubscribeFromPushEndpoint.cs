using Microsoft.EntityFrameworkCore;
using ProjectThor.Data;

namespace ProjectThor.Api.Features.Notifications;

public static class UnsubscribeFromPushEndpoint
{
    public static void MapUnsubscribeFromPush(this IEndpointRouteBuilder app)
    {
        app.MapPost("/push/unsubscribe", Handle).RequireAuthorization();
    }

    private static async Task<IResult> Handle(UnsubscribeRequest request, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        var subscription = await dbContext.PushSubscriptions
            .FirstOrDefaultAsync(p => p.Endpoint == request.Endpoint, cancellationToken);

        if (subscription is not null)
        {
            dbContext.PushSubscriptions.Remove(subscription);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return Results.NoContent();
    }

    public sealed record UnsubscribeRequest(string Endpoint);
}
