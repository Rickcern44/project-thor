using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProjectThor.Api.Features.Charges;
using ProjectThor.Api.Features.SignUps;
using ProjectThor.Api.Infrastructure.Payments;
using ProjectThor.Data;
using ProjectThor.Data.Entities;

namespace ProjectThor.Api.IntegrationTests;

/// <summary>
/// Task 8.2: a single dry run through the whole player lifecycle, chaining the flows that
/// are otherwise only verified in isolation elsewhere: import -> invite -> sign up -> waitlist
/// -> promote -> reconcile -> mark paid.
/// </summary>
public class EndToEndDryRunTests : IAsyncLifetime
{
    private readonly ApiFixture _fixture = new();

    private const string SampleCsv =
        "Name,8-Jan,Attendance,Total Due ($8/night), Amount Paid ,Balance,,,,\n" +
        "Alice Admiree,x,1, $ 8.00 , $ 0.00 , $ 8.00 ,,,,\n" +
        "Bob Bench,x,1, $ 8.00 , $ 0.00 , $ 8.00 ,,,,\n";

    public ValueTask InitializeAsync() => _fixture.InitializeAsync();

    public ValueTask DisposeAsync() => _fixture.DisposeAsync();

    [Fact]
    public async Task Full_lifecycle_import_invite_signup_waitlist_promote_reconcile_pay()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var adminClient = await TestUsers.ActiveClientAsync(_fixture.Factory, UserRole.Admin, cancellationToken);

        // 1. Import: the real sheet's rows lack email/phone (D11), so both rows are flagged.
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent("2026"), "seasonYear");
        form.Add(new ByteArrayContent(Encoding.UTF8.GetBytes(SampleCsv)), "file", "roster.csv");
        var importResponse = await adminClient.PostAsync("/admin/import/roster", form, cancellationToken);
        Assert.Equal(HttpStatusCode.OK, importResponse.StatusCode);

        var flaggedResponse = await adminClient.GetAsync("/admin/import/flagged-rows", cancellationToken);
        var flaggedRows = JsonSerializer.Deserialize<JsonElement[]>(
            await flaggedResponse.Content.ReadAsStringAsync(cancellationToken))!;
        Assert.Equal(2, flaggedRows.Length);

        var aliceFlaggedId = flaggedRows.Single(r => RawDataName(r) == "Alice Admiree").GetProperty("id").GetGuid();
        var bobFlaggedId = flaggedRows.Single(r => RawDataName(r) == "Bob Bench").GetProperty("id").GetGuid();

        var aliceEmail = $"alice-{Guid.NewGuid()}@example.com";
        var bobEmail = $"bob-{Guid.NewGuid()}@example.com";

        await adminClient.PostAsJsonAsync(
            $"/admin/import/flagged-rows/{aliceFlaggedId}/resolve",
            new { Name = "Alice Admiree", Email = aliceEmail, Phone = "555-0100" },
            cancellationToken);
        await adminClient.PostAsJsonAsync(
            $"/admin/import/flagged-rows/{bobFlaggedId}/resolve",
            new { Name = "Bob Bench", Email = bobEmail, Phone = "555-0101" },
            cancellationToken);

        using var scope = _fixture.Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var aliceRosterRecord = await dbContext.RosterRecords.SingleAsync(r => r.Email == aliceEmail, cancellationToken);
        var bobRosterRecord = await dbContext.RosterRecords.SingleAsync(r => r.Email == bobEmail, cancellationToken);

        // 2. Invite: resolving a flagged row creates a Pending user; invite sends their first magic link.
        await adminClient.PostAsJsonAsync("/admin/invites", new { RosterRecordId = aliceRosterRecord.Id }, cancellationToken);
        await adminClient.PostAsJsonAsync("/admin/invites", new { RosterRecordId = bobRosterRecord.Id }, cancellationToken);

        var aliceToken = ExtractToken(_fixture.Factory.EmailSender.SentEmails.Single(e => e.ToEmail == aliceEmail).HtmlBody);
        var bobToken = ExtractToken(_fixture.Factory.EmailSender.SentEmails.Single(e => e.ToEmail == bobEmail).HtmlBody);

        using var aliceClient = _fixture.Factory.CreateClient();
        var aliceConsume = await aliceClient.PostAsJsonAsync("/auth/consume", new { Token = aliceToken }, cancellationToken);
        Assert.Equal(HttpStatusCode.OK, aliceConsume.StatusCode);

        using var bobClient = _fixture.Factory.CreateClient();
        var bobConsume = await bobClient.PostAsJsonAsync("/auth/consume", new { Token = bobToken }, cancellationToken);
        Assert.Equal(HttpStatusCode.OK, bobConsume.StatusCode);

        // 3. Sign up: a fresh live game with room for exactly one player.
        var game = new Game
        {
            StartsAt = DateTimeOffset.UtcNow.AddHours(2),
            SignupOpensAt = DateTimeOffset.UtcNow.AddHours(-1),
            Capacity = 1,
            Fee = 6.00m,
            IsAdHoc = true
        };
        dbContext.Games.Add(game);
        await dbContext.SaveChangesAsync(cancellationToken);

        var aliceSignUpResponse = await aliceClient.PostAsync($"/games/{game.Id}/signup", null, cancellationToken);
        var aliceSignUp = await aliceSignUpResponse.Content.ReadFromJsonAsync<SignUpResponse>(cancellationToken);
        Assert.Equal("Rostered", aliceSignUp!.Status);

        // 4. Waitlist: capacity is exhausted, so Bob overflows to the waitlist.
        var bobSignUpResponse = await bobClient.PostAsync($"/games/{game.Id}/signup", null, cancellationToken);
        var bobSignUp = await bobSignUpResponse.Content.ReadFromJsonAsync<SignUpResponse>(cancellationToken);
        Assert.Equal("Waitlisted", bobSignUp!.Status);

        // Alice cancels before the game, freeing her spot and erasing her charge with no penalty.
        await aliceClient.PostAsync($"/games/{game.Id}/cancel", null, cancellationToken);

        // 5. Promote: no auto-promotion - Bob stays waitlisted until the admin acts.
        var rosterAfterCancel = await adminClient.GetAsync($"/games/{game.Id}/roster", cancellationToken);
        var signUpsAfterCancel = await rosterAfterCancel.Content.ReadFromJsonAsync<List<SignUpResponse>>(cancellationToken);
        Assert.Contains(signUpsAfterCancel!, s => s.PlayerUserId == bobSignUp.PlayerUserId && s.Status == "Waitlisted");

        var promoteResponse = await adminClient.PostAsJsonAsync(
            $"/admin/games/{game.Id}/promote", new { PlayerUserId = bobSignUp.PlayerUserId }, cancellationToken);
        Assert.Equal(HttpStatusCode.OK, promoteResponse.StatusCode);
        var promoted = await promoteResponse.Content.ReadFromJsonAsync<SignUpResponse>(cancellationToken);
        Assert.Equal("Rostered", promoted!.Status);

        // 6. Reconcile: simulate the game's start time passing, then run the reconciliation pass
        // that decides which charges stand as owed (spec: payment-tracking).
        game.StartsAt = DateTimeOffset.UtcNow.AddMinutes(-5);
        await dbContext.SaveChangesAsync(cancellationToken);

        var reconciliationService = scope.ServiceProvider.GetRequiredService<PaymentReconciliationService>();
        await reconciliationService.ReconcilePastGameChargesAsync(cancellationToken);

        var bobCharge = await dbContext.Charges.SingleAsync(
            c => c.GameId == game.Id && c.PlayerUserId == bobSignUp.PlayerUserId, cancellationToken);
        Assert.Equal(ChargeStatus.Owed, bobCharge.Status);

        var aliceCharge = await dbContext.Charges.SingleAsync(
            c => c.GameId == game.Id && c.PlayerUserId == aliceSignUp.PlayerUserId, cancellationToken);
        Assert.Equal(ChargeStatus.Erased, aliceCharge.Status);

        // 7. Mark paid: Bob, still owing, gets marked paid by the admin.
        var payResponse = await adminClient.PostAsync($"/admin/charges/{bobCharge.Id}/pay", null, cancellationToken);
        Assert.Equal(HttpStatusCode.OK, payResponse.StatusCode);
        var paid = await payResponse.Content.ReadFromJsonAsync<ChargeResponse>(cancellationToken);
        Assert.Equal("Paid", paid!.Status);

        using var verifyScope = _fixture.Factory.Services.CreateScope();
        var verifyDbContext = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var finalCharge = await verifyDbContext.Charges.SingleAsync(c => c.Id == bobCharge.Id, cancellationToken);
        Assert.Equal(ChargeStatus.Paid, finalCharge.Status);
        Assert.NotNull(finalCharge.ResolvedAt);
    }

    private static string RawDataName(JsonElement flaggedRow) =>
        JsonDocument.Parse(flaggedRow.GetProperty("rawData").GetString()!).RootElement.GetProperty("Name").GetString()!;

    private static string ExtractToken(string htmlBody)
    {
        var match = Regex.Match(htmlBody, "token=([a-f0-9]+)");
        Assert.True(match.Success, $"No token found in email body: {htmlBody}");
        return match.Groups[1].Value;
    }
}
