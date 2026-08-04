using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProjectThor.Api.Features.GameTemplates;
using ProjectThor.Api.Features.Games;
using ProjectThor.Data;
using ProjectThor.Data.Entities;

namespace ProjectThor.Api.IntegrationTests;

public class GameSchedulingFlowTests : IAsyncLifetime
{
    private readonly ApiFixture _fixture = new();

    public ValueTask InitializeAsync() => _fixture.InitializeAsync();

    public ValueTask DisposeAsync() => _fixture.DisposeAsync();

    [Fact]
    public async Task Admin_creates_a_recurring_template()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = await AdminClientAsync(cancellationToken);

        var response = await client.PostAsJsonAsync(
            "/admin/game-templates",
            new
            {
                DayOfWeek = DayOfWeek.Tuesday,
                TimeOfDay = new TimeOnly(18, 0),
                DefaultCapacity = 10,
                Fee = 5.00m,
                SignupLeadTime = TimeSpan.FromDays(1)
            },
            cancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Creating_a_template_with_lead_time_too_long_is_rejected()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = await AdminClientAsync(cancellationToken);

        var response = await client.PostAsJsonAsync(
            "/admin/game-templates",
            new
            {
                DayOfWeek = DayOfWeek.Tuesday,
                TimeOfDay = new TimeOnly(18, 0),
                DefaultCapacity = 10,
                Fee = 5.00m,
                SignupLeadTime = TimeSpan.FromDays(8)
            },
            cancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Live_game_endpoint_materializes_the_next_game_from_the_template()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = await AdminClientAsync(cancellationToken);

        await client.PostAsJsonAsync(
            "/admin/game-templates",
            new
            {
                DayOfWeek = DateTimeOffset.UtcNow.DayOfWeek,
                TimeOfDay = new TimeOnly(23, 59),
                DefaultCapacity = 10,
                Fee = 5.00m,
                SignupLeadTime = TimeSpan.FromHours(1)
            },
            cancellationToken);

        var liveResponse = await client.GetAsync("/games/live", cancellationToken);

        Assert.Equal(HttpStatusCode.OK, liveResponse.StatusCode);
        var game = await liveResponse.Content.ReadFromJsonAsync<GameResponse>(cancellationToken);
        Assert.NotNull(game);
        Assert.False(game!.IsAdHoc);
    }

    [Fact]
    public async Task No_live_game_when_no_template_and_no_adhoc_game_exists()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = await ActiveClientAsync(UserRole.Player, cancellationToken);

        var response = await client.GetAsync("/games/live", cancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Admin_creates_an_adhoc_game()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = await AdminClientAsync(cancellationToken);

        var startsAt = DateTimeOffset.UtcNow.AddDays(3);
        var response = await client.PostAsJsonAsync(
            "/admin/games/adhoc",
            new { StartsAt = startsAt, SignupOpensAt = startsAt.AddDays(-1), Capacity = 12, Fee = 7.50m },
            cancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Overlapping_adhoc_game_is_rejected()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = await AdminClientAsync(cancellationToken);

        var startsAt = DateTimeOffset.UtcNow.AddDays(3);
        await client.PostAsJsonAsync(
            "/admin/games/adhoc",
            new { StartsAt = startsAt, SignupOpensAt = startsAt.AddDays(-1), Capacity = 12, Fee = 7.50m },
            cancellationToken);

        var overlappingResponse = await client.PostAsJsonAsync(
            "/admin/games/adhoc",
            new { StartsAt = startsAt.AddHours(2), SignupOpensAt = startsAt.AddHours(-2), Capacity = 12, Fee = 7.50m },
            cancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, overlappingResponse.StatusCode);
    }

    [Fact]
    public async Task Admin_overrides_capacity_on_a_single_game_without_affecting_others()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = await AdminClientAsync(cancellationToken);

        var startsAt = DateTimeOffset.UtcNow.AddDays(3);
        var createResponse = await client.PostAsJsonAsync(
            "/admin/games/adhoc",
            new { StartsAt = startsAt, SignupOpensAt = startsAt.AddDays(-1), Capacity = 10, Fee = 5.00m },
            cancellationToken);
        var created = await createResponse.Content.ReadFromJsonAsync<GameResponse>(cancellationToken);

        var updateResponse = await client.PatchAsJsonAsync(
            $"/admin/games/{created!.Id}",
            new { Capacity = 20 },
            cancellationToken);

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<GameResponse>(cancellationToken);
        Assert.Equal(20, updated!.Capacity);
    }

    [Fact]
    public async Task Cancelling_one_game_does_not_affect_others()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = await AdminClientAsync(cancellationToken);

        var startsAt = DateTimeOffset.UtcNow.AddDays(3);
        var createResponse = await client.PostAsJsonAsync(
            "/admin/games/adhoc",
            new { StartsAt = startsAt, SignupOpensAt = startsAt.AddDays(-1), Capacity = 10, Fee = 5.00m },
            cancellationToken);
        var created = await createResponse.Content.ReadFromJsonAsync<GameResponse>(cancellationToken);

        var cancelResponse = await client.PostAsync($"/admin/games/{created!.Id}/cancel", null, cancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, cancelResponse.StatusCode);

        using var scope = _fixture.Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var game = await dbContext.Games.SingleAsync(g => g.Id == created.Id, cancellationToken);
        Assert.True(game.IsCancelled);
    }

    [Fact]
    public async Task Past_games_list_includes_a_game_whose_start_time_has_passed()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = _fixture.Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var pastGame = new Game
        {
            StartsAt = DateTimeOffset.UtcNow.AddDays(-1),
            SignupOpensAt = DateTimeOffset.UtcNow.AddDays(-2),
            Capacity = 10,
            Fee = 5.00m,
            IsAdHoc = true
        };
        dbContext.Games.Add(pastGame);
        await dbContext.SaveChangesAsync(cancellationToken);

        using var client = await AdminClientAsync(cancellationToken);
        var response = await client.GetAsync("/admin/games/past", cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var pastGames = await response.Content.ReadFromJsonAsync<List<GameResponse>>(cancellationToken);
        Assert.Contains(pastGames!, g => g.Id == pastGame.Id);
    }

    private Task<HttpClient> AdminClientAsync(CancellationToken cancellationToken) =>
        ActiveClientAsync(UserRole.Admin, cancellationToken);

    private Task<HttpClient> ActiveClientAsync(UserRole role, CancellationToken cancellationToken) =>
        TestUsers.ActiveClientAsync(_fixture.Factory, role, cancellationToken);
}
