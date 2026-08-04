using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProjectThor.Api.Features.SignUps;
using ProjectThor.Data;
using ProjectThor.Data.Entities;

namespace ProjectThor.Api.IntegrationTests;

public class SignUpFlowTests : IAsyncLifetime
{
    private readonly ApiFixture _fixture = new();

    public ValueTask InitializeAsync() => _fixture.InitializeAsync();

    public ValueTask DisposeAsync() => _fixture.DisposeAsync();

    [Fact]
    public async Task Player_signs_up_and_is_rostered_when_under_capacity()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var game = await SeedOpenGameAsync(capacity: 10, cancellationToken);
        using var client = await TestUsers.ActiveClientAsync(_fixture.Factory, UserRole.Player, cancellationToken);

        var response = await client.PostAsync($"/games/{game.Id}/signup", null, cancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var signUp = await response.Content.ReadFromJsonAsync<SignUpResponse>(cancellationToken);
        Assert.Equal("Rostered", signUp!.Status);
    }

    [Fact]
    public async Task Player_is_waitlisted_in_arrival_order_when_at_capacity()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var game = await SeedOpenGameAsync(capacity: 1, cancellationToken);

        using var firstClient = await TestUsers.ActiveClientAsync(_fixture.Factory, UserRole.Player, cancellationToken);
        await firstClient.PostAsync($"/games/{game.Id}/signup", null, cancellationToken);

        using var secondClient = await TestUsers.ActiveClientAsync(_fixture.Factory, UserRole.Player, cancellationToken);
        var secondResponse = await secondClient.PostAsync($"/games/{game.Id}/signup", null, cancellationToken);
        var second = await secondResponse.Content.ReadFromJsonAsync<SignUpResponse>(cancellationToken);
        Assert.Equal("Waitlisted", second!.Status);
        Assert.Equal(1, second.WaitlistPosition);

        using var thirdClient = await TestUsers.ActiveClientAsync(_fixture.Factory, UserRole.Player, cancellationToken);
        var thirdResponse = await thirdClient.PostAsync($"/games/{game.Id}/signup", null, cancellationToken);
        var third = await thirdResponse.Content.ReadFromJsonAsync<SignUpResponse>(cancellationToken);
        Assert.Equal("Waitlisted", third!.Status);
        Assert.Equal(2, third.WaitlistPosition);
    }

    [Fact]
    public async Task Sign_up_is_rejected_when_game_is_not_open()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = _fixture.Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var closedGame = new Game
        {
            StartsAt = DateTimeOffset.UtcNow.AddDays(10),
            SignupOpensAt = DateTimeOffset.UtcNow.AddDays(9),
            Capacity = 10,
            Fee = 5.00m,
            IsAdHoc = true
        };
        dbContext.Games.Add(closedGame);
        await dbContext.SaveChangesAsync(cancellationToken);

        using var client = await TestUsers.ActiveClientAsync(_fixture.Factory, UserRole.Player, cancellationToken);
        var response = await client.PostAsync($"/games/{closedGame.Id}/signup", null, cancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Duplicate_sign_up_is_rejected()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var game = await SeedOpenGameAsync(capacity: 10, cancellationToken);
        using var client = await TestUsers.ActiveClientAsync(_fixture.Factory, UserRole.Player, cancellationToken);

        await client.PostAsync($"/games/{game.Id}/signup", null, cancellationToken);
        var secondAttempt = await client.PostAsync($"/games/{game.Id}/signup", null, cancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, secondAttempt.StatusCode);
    }

    [Fact]
    public async Task Unauthenticated_sign_up_is_rejected()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var game = await SeedOpenGameAsync(capacity: 10, cancellationToken);
        using var client = _fixture.Factory.CreateClient();

        var response = await client.PostAsync($"/games/{game.Id}/signup", null, cancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Player_cancels_their_own_roster_spot()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var game = await SeedOpenGameAsync(capacity: 10, cancellationToken);
        using var client = await TestUsers.ActiveClientAsync(_fixture.Factory, UserRole.Player, cancellationToken);
        await client.PostAsync($"/games/{game.Id}/signup", null, cancellationToken);

        var cancelResponse = await client.PostAsync($"/games/{game.Id}/cancel", null, cancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, cancelResponse.StatusCode);

        var rosterResponse = await client.GetAsync($"/games/{game.Id}/roster", cancellationToken);
        var roster = await rosterResponse.Content.ReadFromJsonAsync<List<SignUpResponse>>(cancellationToken);
        Assert.Empty(roster!);
    }

    [Fact]
    public async Task Admin_adds_and_removes_a_player_directly()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var game = await SeedOpenGameAsync(capacity: 10, cancellationToken);
        var player = await TestUsers.SeedActiveUserAsync(_fixture.Factory, UserRole.Player, cancellationToken);
        using var adminClient = await TestUsers.ActiveClientAsync(_fixture.Factory, UserRole.Admin, cancellationToken);

        var addResponse = await adminClient.PostAsJsonAsync(
            $"/admin/games/{game.Id}/roster", new { PlayerUserId = player.Id }, cancellationToken);
        Assert.Equal(HttpStatusCode.Created, addResponse.StatusCode);

        var removeResponse = await adminClient.DeleteAsync($"/admin/games/{game.Id}/roster/{player.Id}", cancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, removeResponse.StatusCode);
    }

    [Fact]
    public async Task Admin_promotes_a_waitlisted_player_into_an_open_spot()
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

        // No open spot yet - promotion should be rejected.
        var rejectedPromotion = await adminClient.PostAsJsonAsync(
            $"/admin/games/{game.Id}/promote", new { PlayerUserId = waitlistedPlayer.Id }, cancellationToken);
        Assert.Equal(HttpStatusCode.Conflict, rejectedPromotion.StatusCode);

        // Free the only roster spot.
        await adminClient.DeleteAsync($"/admin/games/{game.Id}/roster/{rosteredPlayer.Id}", cancellationToken);

        var promoteResponse = await adminClient.PostAsJsonAsync(
            $"/admin/games/{game.Id}/promote", new { PlayerUserId = waitlistedPlayer.Id }, cancellationToken);

        Assert.Equal(HttpStatusCode.OK, promoteResponse.StatusCode);
        var promoted = await promoteResponse.Content.ReadFromJsonAsync<SignUpResponse>(cancellationToken);
        Assert.Equal("Rostered", promoted!.Status);
        Assert.Null(promoted.WaitlistPosition);
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
