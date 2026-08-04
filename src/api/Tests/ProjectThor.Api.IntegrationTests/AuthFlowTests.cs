using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProjectThor.Api.Features.Auth;
using ProjectThor.Api.Features.Invites;
using ProjectThor.Data;
using ProjectThor.Data.Entities;

namespace ProjectThor.Api.IntegrationTests;

public class AuthFlowTests : IAsyncLifetime
{
    private readonly ApiFixture _fixture = new();

    public ValueTask InitializeAsync() => _fixture.InitializeAsync();

    public ValueTask DisposeAsync() => _fixture.DisposeAsync();

    [Fact]
    public async Task Unauthenticated_request_to_issue_invite_is_rejected()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = _fixture.Factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/admin/invites",
            new { RosterRecordId = Guid.NewGuid() },
            cancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Player_cannot_issue_invites()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var player = await SeedActiveUserAsync(UserRole.Player, cancellationToken);
        using var client = await SignedInClientAsync(player, cancellationToken);

        var response = await client.PostAsJsonAsync(
            "/admin/invites",
            new { RosterRecordId = Guid.NewGuid() },
            cancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Admin_invite_creates_pending_user_and_sends_invite_email()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var admin = await SeedActiveUserAsync(UserRole.Admin, cancellationToken);
        var rosterRecord = await SeedRosterRecordAsync(cancellationToken);
        using var client = await SignedInClientAsync(admin, cancellationToken);

        var response = await client.PostAsJsonAsync(
            "/admin/invites",
            new { RosterRecordId = rosterRecord.Id },
            cancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var scope = _fixture.Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var invitedUser = await dbContext.Users.SingleAsync(u => u.RosterRecordId == rosterRecord.Id, cancellationToken);

        Assert.Equal(UserStatus.Pending, invitedUser.Status);
        Assert.Contains(_fixture.Factory.EmailSender.SentEmails, e => e.ToEmail == rosterRecord.Email);
    }

    [Fact]
    public async Task Accepting_an_invite_activates_the_account_and_signs_in()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var admin = await SeedActiveUserAsync(UserRole.Admin, cancellationToken);
        var rosterRecord = await SeedRosterRecordAsync(cancellationToken);

        using var adminClient = await SignedInClientAsync(admin, cancellationToken);
        await adminClient.PostAsJsonAsync("/admin/invites", new { RosterRecordId = rosterRecord.Id }, cancellationToken);

        var rawToken = ExtractToken(_fixture.Factory.EmailSender.SentEmails.Single(e => e.ToEmail == rosterRecord.Email).HtmlBody);

        using var playerClient = _fixture.Factory.CreateClient();
        var consumeResponse = await playerClient.PostAsJsonAsync("/auth/consume", new { Token = rawToken }, cancellationToken);
        Assert.Equal(HttpStatusCode.OK, consumeResponse.StatusCode);

        var meResponse = await playerClient.GetAsync("/auth/me", cancellationToken);
        Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);
        var me = await meResponse.Content.ReadFromJsonAsync<MeEndpoint.MeResponse>(cancellationToken);
        Assert.Equal(rosterRecord.Email, me!.Email);

        using var scope = _fixture.Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var activatedUser = await dbContext.Users.SingleAsync(u => u.RosterRecordId == rosterRecord.Id, cancellationToken);
        Assert.Equal(UserStatus.Active, activatedUser.Status);
        Assert.NotNull(activatedUser.ActivatedAt);
    }

    [Fact]
    public async Task Requesting_a_login_link_for_an_unknown_email_still_returns_ok_and_sends_nothing()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = _fixture.Factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/auth/login/request",
            new { Email = "nobody@example.com" },
            cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain(_fixture.Factory.EmailSender.SentEmails, e => e.ToEmail == "nobody@example.com");
    }

    [Fact]
    public async Task Login_link_signs_the_active_user_in()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var player = await SeedActiveUserAsync(UserRole.Player, cancellationToken);
        using var client = _fixture.Factory.CreateClient();

        await client.PostAsJsonAsync("/auth/login/request", new { Email = player.Email }, cancellationToken);
        var rawToken = ExtractToken(_fixture.Factory.EmailSender.SentEmails.Last(e => e.ToEmail == player.Email).HtmlBody);

        var consumeResponse = await client.PostAsJsonAsync("/auth/consume", new { Token = rawToken }, cancellationToken);
        Assert.Equal(HttpStatusCode.OK, consumeResponse.StatusCode);

        var meResponse = await client.GetAsync("/auth/me", cancellationToken);
        Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);
    }

    [Fact]
    public async Task A_consumed_token_cannot_be_reused()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var player = await SeedActiveUserAsync(UserRole.Player, cancellationToken);
        using var client = _fixture.Factory.CreateClient();

        await client.PostAsJsonAsync("/auth/login/request", new { Email = player.Email }, cancellationToken);
        var rawToken = ExtractToken(_fixture.Factory.EmailSender.SentEmails.Last(e => e.ToEmail == player.Email).HtmlBody);

        await client.PostAsJsonAsync("/auth/consume", new { Token = rawToken }, cancellationToken);

        using var secondClient = _fixture.Factory.CreateClient();
        var secondAttempt = await secondClient.PostAsJsonAsync("/auth/consume", new { Token = rawToken }, cancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, secondAttempt.StatusCode);
    }

    private static string ExtractToken(string htmlBody)
    {
        var match = Regex.Match(htmlBody, "token=([a-f0-9]+)");
        Assert.True(match.Success, $"No token found in email body: {htmlBody}");
        return match.Groups[1].Value;
    }

    private async Task<User> SeedActiveUserAsync(UserRole role, CancellationToken cancellationToken)
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user = new User
        {
            Email = $"{Guid.NewGuid()}@example.com",
            Phone = "555-0100",
            Name = "Test User",
            Role = role,
            Status = UserStatus.Active,
            ActivatedAt = DateTimeOffset.UtcNow
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);
        return user;
    }

    private async Task<RosterRecord> SeedRosterRecordAsync(CancellationToken cancellationToken)
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var rosterRecord = new RosterRecord
        {
            Name = "Imported Player",
            Email = $"{Guid.NewGuid()}@example.com",
            Phone = "555-0101",
            LegacyBalance = 0m
        };
        dbContext.RosterRecords.Add(rosterRecord);
        await dbContext.SaveChangesAsync(cancellationToken);
        return rosterRecord;
    }

    private async Task<HttpClient> SignedInClientAsync(User user, CancellationToken cancellationToken)
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var (rawToken, hash) = Infrastructure.Auth.MagicLinkTokenGenerator.Generate();
        dbContext.MagicLinkTokens.Add(new MagicLinkToken
        {
            UserId = user.Id,
            TokenHash = hash,
            Purpose = MagicLinkPurpose.Login,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(15)
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        var client = _fixture.Factory.CreateClient();
        var response = await client.PostAsJsonAsync("/auth/consume", new { Token = rawToken }, cancellationToken);
        response.EnsureSuccessStatusCode();
        return client;
    }
}
