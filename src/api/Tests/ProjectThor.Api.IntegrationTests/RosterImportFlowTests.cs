using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProjectThor.Data;
using ProjectThor.Data.Entities;

namespace ProjectThor.Api.IntegrationTests;

public class RosterImportFlowTests : IAsyncLifetime
{
    private readonly ApiFixture _fixture = new();

    private const string SampleCsv =
        "Name,8-Jan,15-Jan,22-Jan,Attendance,Total Due ($8/night), Amount Paid ,Balance,,,,\n" +
        "John Cooke,x,x,,2, $ 14.00 , $ 21.00 , $ 7.00 ,,,,\n";

    public ValueTask InitializeAsync() => _fixture.InitializeAsync();

    public ValueTask DisposeAsync() => _fixture.DisposeAsync();

    [Fact]
    public async Task Import_flags_every_row_and_resolve_creates_history()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var adminClient = await TestUsers.ActiveClientAsync(_fixture.Factory, UserRole.Admin, cancellationToken);

        using var form = new MultipartFormDataContent();
        form.Add(new StringContent("2026"), "seasonYear");
        form.Add(new ByteArrayContent(Encoding.UTF8.GetBytes(SampleCsv)), "file", "roster.csv");

        var importResponse = await adminClient.PostAsync("/admin/import/roster", form, cancellationToken);
        Assert.Equal(HttpStatusCode.OK, importResponse.StatusCode);

        var flaggedResponse = await adminClient.GetAsync("/admin/import/flagged-rows", cancellationToken);
        var flaggedRows = JsonSerializer.Deserialize<JsonElement[]>(
            await flaggedResponse.Content.ReadAsStringAsync(cancellationToken))!;
        Assert.Single(flaggedRows);
        var flaggedId = flaggedRows[0].GetProperty("id").GetGuid();

        var resolveResponse = await adminClient.PostAsJsonAsync(
            $"/admin/import/flagged-rows/{flaggedId}/resolve",
            new { Name = "John Cooke", Email = "john@example.com", Phone = "555-0100" },
            cancellationToken);
        Assert.Equal(HttpStatusCode.OK, resolveResponse.StatusCode);

        using var scope = _fixture.Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var rosterRecord = await dbContext.RosterRecords.SingleAsync(r => r.Email == "john@example.com", cancellationToken);
        // TotalDue $14 (2 games @ $7), Paid $21 -> overpaid by $7, all games Paid, LegacyBalance = -7 credit.
        Assert.Equal(-7.00m, rosterRecord.LegacyBalance);

        var user = await dbContext.Users.SingleAsync(u => u.RosterRecordId == rosterRecord.Id, cancellationToken);
        Assert.Equal(UserStatus.Pending, user.Status);

        var charges = await dbContext.Charges.Where(c => c.PlayerUserId == user.Id).ToListAsync(cancellationToken);
        Assert.Equal(2, charges.Count);
        Assert.All(charges, c => Assert.Equal(ChargeStatus.Paid, c.Status));
        Assert.All(charges, c => Assert.Equal(7.00m, c.Amount));

        var signUps = await dbContext.SignUps.Where(s => s.PlayerUserId == user.Id).ToListAsync(cancellationToken);
        Assert.Equal(2, signUps.Count);
    }

    [Fact]
    public async Task Re_running_import_does_not_duplicate_flagged_rows()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var adminClient = await TestUsers.ActiveClientAsync(_fixture.Factory, UserRole.Admin, cancellationToken);

        for (var i = 0; i < 2; i++)
        {
            using var form = new MultipartFormDataContent();
            form.Add(new StringContent("2026"), "seasonYear");
            form.Add(new ByteArrayContent(Encoding.UTF8.GetBytes(SampleCsv)), "file", "roster.csv");
            await adminClient.PostAsync("/admin/import/roster", form, cancellationToken);
        }

        var flaggedResponse = await adminClient.GetAsync("/admin/import/flagged-rows", cancellationToken);
        var flaggedRows = JsonSerializer.Deserialize<JsonElement[]>(
            await flaggedResponse.Content.ReadAsStringAsync(cancellationToken))!;

        Assert.Single(flaggedRows);
    }

    [Fact]
    public async Task Resolving_a_row_with_an_already_used_email_returns_conflict_not_a_server_error()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var adminClient = await TestUsers.ActiveClientAsync(_fixture.Factory, UserRole.Admin, cancellationToken);
        var (existingRosterRecord, _) = await TestUsers.SeedPendingPlayerAsync(_fixture.Factory, cancellationToken);

        using var form = new MultipartFormDataContent();
        form.Add(new StringContent("2026"), "seasonYear");
        form.Add(new ByteArrayContent(Encoding.UTF8.GetBytes(SampleCsv)), "file", "roster.csv");
        await adminClient.PostAsync("/admin/import/roster", form, cancellationToken);

        var flaggedResponse = await adminClient.GetAsync("/admin/import/flagged-rows", cancellationToken);
        var flaggedRows = JsonSerializer.Deserialize<JsonElement[]>(
            await flaggedResponse.Content.ReadAsStringAsync(cancellationToken))!;
        var flaggedId = flaggedRows[0].GetProperty("id").GetGuid();

        var resolveResponse = await adminClient.PostAsJsonAsync(
            $"/admin/import/flagged-rows/{flaggedId}/resolve",
            new { Name = "John Cooke", Email = existingRosterRecord.Email, Phone = "555-0100" },
            cancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, resolveResponse.StatusCode);

        using var scope = _fixture.Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var rosterRecordCount = await dbContext.RosterRecords.CountAsync(
            r => r.Email == existingRosterRecord.Email, cancellationToken);
        Assert.Equal(1, rosterRecordCount);

        var stillPendingRow = await dbContext.FlaggedImportRows.SingleAsync(f => f.Id == flaggedId, cancellationToken);
        Assert.Equal(ImportRowStatus.Pending, stillPendingRow.Status);
    }
}
