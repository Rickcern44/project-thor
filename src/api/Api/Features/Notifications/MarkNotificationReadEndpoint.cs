using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using ProjectThor.Data;

namespace ProjectThor.Api.Features.Notifications;

public static class MarkNotificationReadEndpoint
{
    public static void MapMarkNotificationRead(this IEndpointRouteBuilder app)
    {
        app.MapPost("/notifications/{id:guid}/read", Handle).RequireAuthorization();
    }

    private static async Task<IResult> Handle(
        Guid id, ClaimsPrincipal user, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var notification = await dbContext.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId, cancellationToken);

        if (notification is null)
        {
            return Results.NotFound();
        }

        notification.ReadAt ??= DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.NoContent();
    }
}
