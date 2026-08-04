using Microsoft.EntityFrameworkCore;
using ProjectThor.Data;
using ProjectThor.Data.Entities;

namespace ProjectThor.Api.Infrastructure.Notifications;

/// <summary>Notifies all active players once a game transitions into its sign-up window (spec: game-scheduling).</summary>
public class SignupOpenNotificationService(AppDbContext dbContext, NotificationService notificationService)
{
    public async Task NotifyNewlyOpenGamesAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        var newlyOpenGames = await dbContext.Games
            .Where(g => !g.IsCancelled && g.SignupOpensAt <= now && g.StartsAt > now && g.SignupOpenNotifiedAt == null)
            .ToListAsync(cancellationToken);

        if (newlyOpenGames.Count == 0)
        {
            return;
        }

        var activePlayerIds = await dbContext.Users
            .Where(u => u.Status == UserStatus.Active)
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        foreach (var game in newlyOpenGames)
        {
            foreach (var playerId in activePlayerIds)
            {
                await notificationService.NotifyAsync(
                    playerId, NotificationType.NewGameOpen, "Sign-ups are open for the next game!", cancellationToken);
            }

            game.SignupOpenNotifiedAt = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
