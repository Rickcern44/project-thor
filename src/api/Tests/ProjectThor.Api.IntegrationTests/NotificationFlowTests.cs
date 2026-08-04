using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProjectThor.Api.Features.Notifications;
using ProjectThor.Api.Infrastructure.Notifications;
using ProjectThor.Data;
using ProjectThor.Data.Entities;

namespace ProjectThor.Api.IntegrationTests;

public class NotificationFlowTests : IAsyncLifetime
{
    private readonly ApiFixture _fixture = new();

    public ValueTask InitializeAsync() => _fixture.InitializeAsync();

    public ValueTask DisposeAsync() => _fixture.DisposeAsync();

    [Fact]
    public async Task Notification_list_is_scoped_to_the_caller_only()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var player = await TestUsers.SeedActiveUserAsync(_fixture.Factory, UserRole.Player, cancellationToken);
        var otherPlayer = await TestUsers.SeedActiveUserAsync(_fixture.Factory, UserRole.Player, cancellationToken);

        using var scope = _fixture.Factory.Services.CreateScope();
        var notificationService = scope.ServiceProvider.GetRequiredService<NotificationService>();
        await notificationService.NotifyAsync(player.Id, NotificationType.NewGameOpen, "for player", cancellationToken);
        await notificationService.NotifyAsync(otherPlayer.Id, NotificationType.NewGameOpen, "for other player", cancellationToken);

        using var client = await TestUsers.SignedInClientAsync(_fixture.Factory, player, cancellationToken);
        var response = await client.GetAsync("/notifications", cancellationToken);
        var notifications = await response.Content.ReadFromJsonAsync<List<NotificationResponse>>(cancellationToken);

        Assert.Single(notifications!);
        Assert.Equal("for player", notifications![0].Message);
    }

    [Fact]
    public async Task Marking_a_notification_read_sets_ReadAt()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var player = await TestUsers.SeedActiveUserAsync(_fixture.Factory, UserRole.Player, cancellationToken);

        using var scope = _fixture.Factory.Services.CreateScope();
        var notificationService = scope.ServiceProvider.GetRequiredService<NotificationService>();
        await notificationService.NotifyAsync(player.Id, NotificationType.NewGameOpen, "hello", cancellationToken);

        using var client = await TestUsers.SignedInClientAsync(_fixture.Factory, player, cancellationToken);
        var listResponse = await client.GetAsync("/notifications", cancellationToken);
        var notifications = await listResponse.Content.ReadFromJsonAsync<List<NotificationResponse>>(cancellationToken);
        var notificationId = notifications![0].Id;

        var readResponse = await client.PostAsync($"/notifications/{notificationId}/read", null, cancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, readResponse.StatusCode);

        // Fresh scope: the `scope` above already has this entity tracked (stale) from NotifyAsync.
        using var verifyScope = _fixture.Factory.Services.CreateScope();
        var dbContext = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var notification = await dbContext.Notifications.SingleAsync(n => n.Id == notificationId, cancellationToken);
        Assert.NotNull(notification.ReadAt);
    }

    [Fact]
    public async Task Waitlist_promotion_emits_a_notification_to_the_promoted_player()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var game = await SeedOpenGameAsync(capacity: 1, cancellationToken);

        var rosteredPlayer = await TestUsers.SeedActiveUserAsync(_fixture.Factory, UserRole.Player, cancellationToken);
        using var rosteredClient = await TestUsers.SignedInClientAsync(_fixture.Factory, rosteredPlayer, cancellationToken);
        await rosteredClient.PostAsync($"/games/{game.Id}/signup", null, cancellationToken);

        var waitlistedPlayer = await TestUsers.SeedActiveUserAsync(_fixture.Factory, UserRole.Player, cancellationToken);
        using var waitlistedClient = await TestUsers.SignedInClientAsync(_fixture.Factory, waitlistedPlayer, cancellationToken);
        await waitlistedClient.PostAsync($"/games/{game.Id}/signup", null, cancellationToken);

        using var adminClient = await TestUsers.ActiveClientAsync(_fixture.Factory, UserRole.Admin, cancellationToken);
        await adminClient.DeleteAsync($"/admin/games/{game.Id}/roster/{rosteredPlayer.Id}", cancellationToken);
        await adminClient.PostAsJsonAsync($"/admin/games/{game.Id}/promote", new { PlayerUserId = waitlistedPlayer.Id }, cancellationToken);

        var notificationsResponse = await waitlistedClient.GetAsync("/notifications", cancellationToken);
        var notifications = await notificationsResponse.Content.ReadFromJsonAsync<List<NotificationResponse>>(cancellationToken);

        Assert.Contains(notifications!, n => n.Type == "WaitlistPromotion");
    }

    [Fact]
    public async Task Signup_open_notification_fires_once_for_a_newly_open_game()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = _fixture.Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var player = await TestUsers.SeedActiveUserAsync(_fixture.Factory, UserRole.Player, cancellationToken);
        var game = new Game
        {
            StartsAt = DateTimeOffset.UtcNow.AddHours(2),
            SignupOpensAt = DateTimeOffset.UtcNow.AddMinutes(-1),
            Capacity = 10,
            Fee = 5.00m,
            IsAdHoc = true
        };
        dbContext.Games.Add(game);
        await dbContext.SaveChangesAsync(cancellationToken);

        var signupOpenNotificationService = scope.ServiceProvider.GetRequiredService<SignupOpenNotificationService>();
        await signupOpenNotificationService.NotifyNewlyOpenGamesAsync(cancellationToken);
        await signupOpenNotificationService.NotifyNewlyOpenGamesAsync(cancellationToken); // second call must not double-notify

        var notificationCount = await dbContext.Notifications
            .CountAsync(n => n.UserId == player.Id && n.Type == NotificationType.NewGameOpen, cancellationToken);
        Assert.Equal(1, notificationCount);
    }

    [Fact]
    public async Task Subscribing_and_unsubscribing_to_push_manages_the_subscription_record()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = await TestUsers.ActiveClientAsync(_fixture.Factory, UserRole.Player, cancellationToken);
        var endpoint = $"https://push.example.com/{Guid.NewGuid()}";

        var subscribeResponse = await client.PostAsJsonAsync(
            "/push/subscribe", new { Endpoint = endpoint, P256dhKey = "key", AuthKey = "auth" }, cancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, subscribeResponse.StatusCode);

        using var scope = _fixture.Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.True(await dbContext.PushSubscriptions.AnyAsync(p => p.Endpoint == endpoint, cancellationToken));

        var unsubscribeResponse = await client.PostAsJsonAsync("/push/unsubscribe", new { Endpoint = endpoint }, cancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, unsubscribeResponse.StatusCode);
        Assert.False(await dbContext.PushSubscriptions.AnyAsync(p => p.Endpoint == endpoint, cancellationToken));
    }

    private async Task<Game> SeedOpenGameAsync(int capacity, CancellationToken cancellationToken)
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var game = new Game
        {
            StartsAt = DateTimeOffset.UtcNow.AddHours(2),
            SignupOpensAt = DateTimeOffset.UtcNow.AddHours(-1),
            Capacity = capacity,
            Fee = 5.00m,
            IsAdHoc = true
        };
        dbContext.Games.Add(game);
        await dbContext.SaveChangesAsync(cancellationToken);
        return game;
    }
}
