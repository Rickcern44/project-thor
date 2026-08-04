using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using ProjectThor.Data;

namespace ProjectThor.Api.Features.Notifications;

/// <summary>The caller's own in-app notification list - the reliable baseline (design.md D9).</summary>
public static class GetNotificationsEndpoint
{
    public static void MapGetNotifications(this IEndpointRouteBuilder app)
    {
        app.MapGet("/notifications", Handle).RequireAuthorization();
    }

    private static async Task<IResult> Handle(ClaimsPrincipal user, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var notifications = await dbContext.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync(cancellationToken);

        return Results.Ok(notifications.Select(NotificationResponse.From));
    }
}
