using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProjectThor.Api.Features.Charges;
using ProjectThor.Api.Features.SignUps;
using ProjectThor.Api.Infrastructure.Payments;
using ProjectThor.Data;
using ProjectThor.Data.Entities;

namespace ProjectThor.Api.IntegrationTests;

public class PaymentFlowTests : IAsyncLifetime
{
    private readonly ApiFixture _fixture = new();

    public ValueTask InitializeAsync() => _fixture.InitializeAsync();

    public ValueTask DisposeAsync() => _fixture.DisposeAsync();

    [Fact]
    public async Task Sign_up_creates_a_charge_for_the_game_fee()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var game = await SeedOpenGameAsync(capacity: 10, fee: 7.50m, cancellationToken);
        var player = await TestUsers.SeedActiveUserAsync(_fixture.Factory, UserRole.Player, cancellationToken);
        using var client = await TestUsers.SignedInClientAsync(_fixture.Factory, player, cancellationToken);

        await client.PostAsync($"/games/{game.Id}/signup", null, cancellationToken);

        using var scope = _fixture.Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var charge = await dbContext.Charges.SingleAsync(c => c.GameId == game.Id && c.PlayerUserId == player.Id, cancellationToken);
        Assert.Equal(7.50m, charge.Amount);
        Assert.Equal(ChargeStatus.Owed, charge.Status);
    }

    [Fact]
    public async Task Cancelling_before_game_erases_the_charge()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var game = await SeedOpenGameAsync(capacity: 10, fee: 5.00m, cancellationToken);
        var player = await TestUsers.SeedActiveUserAsync(_fixture.Factory, UserRole.Player, cancellationToken);
        using var client = await TestUsers.SignedInClientAsync(_fixture.Factory, player, cancellationToken);
        await client.PostAsync($"/games/{game.Id}/signup", null, cancellationToken);

        await client.PostAsync($"/games/{game.Id}/cancel", null, cancellationToken);

        using var scope = _fixture.Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var charge = await dbContext.Charges.SingleAsync(c => c.GameId == game.Id && c.PlayerUserId == player.Id, cancellationToken);
        Assert.Equal(ChargeStatus.Erased, charge.Status);
        Assert.NotNull(charge.ResolvedAt);
    }

    [Fact]
    public async Task Reconciliation_erases_charge_for_a_player_still_waitlisted_at_game_time()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = _fixture.Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Game already started, capacity 1, one rostered + one still-waitlisted signup.
        var game = new Game
        {
            StartsAt = DateTimeOffset.UtcNow.AddMinutes(-5),
            SignupOpensAt = DateTimeOffset.UtcNow.AddHours(-2),
            Capacity = 1,
            Fee = 5.00m,
            IsAdHoc = true
        };
        dbContext.Games.Add(game);

        var rosteredPlayer = await TestUsers.SeedActiveUserAsync(_fixture.Factory, UserRole.Player, cancellationToken);
        var waitlistedPlayer = await TestUsers.SeedActiveUserAsync(_fixture.Factory, UserRole.Player, cancellationToken);
        var rosteredPlayerId = rosteredPlayer.Id;
        var waitlistedPlayerId = waitlistedPlayer.Id;
        dbContext.SignUps.Add(new SignUp { GameId = game.Id, PlayerUserId = rosteredPlayerId, Status = SignUpStatus.Rostered });
        dbContext.SignUps.Add(new SignUp
        {
            GameId = game.Id, PlayerUserId = waitlistedPlayerId, Status = SignUpStatus.Waitlisted, WaitlistPosition = 1
        });
        dbContext.Charges.Add(new Charge { GameId = game.Id, PlayerUserId = rosteredPlayerId, Amount = 5.00m, Status = ChargeStatus.Owed });
        dbContext.Charges.Add(new Charge { GameId = game.Id, PlayerUserId = waitlistedPlayerId, Amount = 5.00m, Status = ChargeStatus.Owed });
        await dbContext.SaveChangesAsync(cancellationToken);

        var reconciliationService = scope.ServiceProvider.GetRequiredService<PaymentReconciliationService>();
        await reconciliationService.ReconcilePastGameChargesAsync(cancellationToken);

        var rosteredCharge = await dbContext.Charges.SingleAsync(c => c.PlayerUserId == rosteredPlayerId, cancellationToken);
        var waitlistedCharge = await dbContext.Charges.SingleAsync(c => c.PlayerUserId == waitlistedPlayerId, cancellationToken);
        Assert.Equal(ChargeStatus.Owed, rosteredCharge.Status);
        Assert.Equal(ChargeStatus.Erased, waitlistedCharge.Status);
    }

    [Fact]
    public async Task Waiving_a_charge_before_the_game_starts_is_rejected()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var charge = await SeedOwedChargeForFutureGameAsync(cancellationToken);
        using var adminClient = await TestUsers.ActiveClientAsync(_fixture.Factory, UserRole.Admin, cancellationToken);

        var response = await adminClient.PostAsync($"/admin/charges/{charge.Id}/waive", null, cancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Admin_waives_a_no_show_charge_after_the_game()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var charge = await SeedOwedChargeForPastGameAsync(cancellationToken);
        using var adminClient = await TestUsers.ActiveClientAsync(_fixture.Factory, UserRole.Admin, cancellationToken);

        var response = await adminClient.PostAsync($"/admin/charges/{charge.Id}/waive", null, cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var waived = await response.Content.ReadFromJsonAsync<ChargeResponse>(cancellationToken);
        Assert.Equal("Waived", waived!.Status);
    }

    [Fact]
    public async Task Admin_marks_a_charge_paid()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var charge = await SeedOwedChargeForPastGameAsync(cancellationToken);
        using var adminClient = await TestUsers.ActiveClientAsync(_fixture.Factory, UserRole.Admin, cancellationToken);

        var response = await adminClient.PostAsync($"/admin/charges/{charge.Id}/pay", null, cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var paid = await response.Content.ReadFromJsonAsync<ChargeResponse>(cancellationToken);
        Assert.Equal("Paid", paid!.Status);
    }

    [Fact]
    public async Task Player_can_view_their_own_balance_but_not_another_players()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var game = await SeedOpenGameAsync(capacity: 10, fee: 10.00m, cancellationToken);
        var player = await TestUsers.SeedActiveUserAsync(_fixture.Factory, UserRole.Player, cancellationToken);
        using var client = await TestUsers.SignedInClientAsync(_fixture.Factory, player, cancellationToken);
        await client.PostAsync($"/games/{game.Id}/signup", null, cancellationToken);

        var ownBalanceResponse = await client.GetAsync($"/players/{player.Id}/balance", cancellationToken);
        Assert.Equal(HttpStatusCode.OK, ownBalanceResponse.StatusCode);
        var ownBalance = await ownBalanceResponse.Content.ReadFromJsonAsync<BalanceResponse>(cancellationToken);
        Assert.Equal(10.00m, ownBalance!.Balance);

        var otherPlayer = await TestUsers.SeedActiveUserAsync(_fixture.Factory, UserRole.Player, cancellationToken);
        var otherBalanceResponse = await client.GetAsync($"/players/{otherPlayer.Id}/balance", cancellationToken);
        Assert.Equal(HttpStatusCode.Forbidden, otherBalanceResponse.StatusCode);
    }

    [Fact]
    public async Task Balance_never_blocks_signup_for_a_second_game()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var firstGame = await SeedOpenGameAsync(capacity: 10, fee: 10.00m, cancellationToken);
        var player = await TestUsers.SeedActiveUserAsync(_fixture.Factory, UserRole.Player, cancellationToken);
        using var client = await TestUsers.SignedInClientAsync(_fixture.Factory, player, cancellationToken);
        await client.PostAsync($"/games/{firstGame.Id}/signup", null, cancellationToken);

        var secondGame = await SeedOpenGameAsync(capacity: 10, fee: 10.00m, cancellationToken);
        var response = await client.PostAsync($"/games/{secondGame.Id}/signup", null, cancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private async Task<Charge> SeedOwedChargeForFutureGameAsync(CancellationToken cancellationToken)
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var game = new Game
        {
            StartsAt = DateTimeOffset.UtcNow.AddDays(1),
            SignupOpensAt = DateTimeOffset.UtcNow.AddHours(-1),
            Capacity = 10,
            Fee = 5.00m,
            IsAdHoc = true
        };
        dbContext.Games.Add(game);
        var player = await TestUsers.SeedActiveUserAsync(_fixture.Factory, UserRole.Player, cancellationToken);
        var charge = new Charge { GameId = game.Id, PlayerUserId = player.Id, Amount = 5.00m, Status = ChargeStatus.Owed };
        dbContext.Charges.Add(charge);
        await dbContext.SaveChangesAsync(cancellationToken);
        return charge;
    }

    private async Task<Charge> SeedOwedChargeForPastGameAsync(CancellationToken cancellationToken)
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var game = new Game
        {
            StartsAt = DateTimeOffset.UtcNow.AddHours(-1),
            SignupOpensAt = DateTimeOffset.UtcNow.AddHours(-2),
            Capacity = 10,
            Fee = 5.00m,
            IsAdHoc = true
        };
        dbContext.Games.Add(game);
        var player = await TestUsers.SeedActiveUserAsync(_fixture.Factory, UserRole.Player, cancellationToken);
        var charge = new Charge { GameId = game.Id, PlayerUserId = player.Id, Amount = 5.00m, Status = ChargeStatus.Owed };
        dbContext.Charges.Add(charge);
        await dbContext.SaveChangesAsync(cancellationToken);
        return charge;
    }

    private async Task<Game> SeedOpenGameAsync(int capacity, decimal fee, CancellationToken cancellationToken)
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var game = new Game
        {
            StartsAt = DateTimeOffset.UtcNow.AddHours(2),
            SignupOpensAt = DateTimeOffset.UtcNow.AddHours(-1),
            Capacity = capacity,
            Fee = fee,
            IsAdHoc = true
        };
        dbContext.Games.Add(game);
        await dbContext.SaveChangesAsync(cancellationToken);
        return game;
    }
}
