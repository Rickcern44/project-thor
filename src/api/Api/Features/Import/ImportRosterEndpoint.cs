using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectThor.Api.Infrastructure.Import;
using ProjectThor.Data;
using ProjectThor.Data.Entities;

namespace ProjectThor.Api.Features.Import;

/// <summary>
/// One-time spreadsheet import (spec: roster-import). Shared Games are created once per unique
/// attended date (capacity = actual headcount for that date); per-player identity/history is
/// deferred to ResolveFlaggedRowEndpoint since every row here is missing email/phone (D11) and
/// can't become a real RosterRecord/User yet.
/// </summary>
public static class ImportRosterEndpoint
{
    public static void MapImportRoster(this IEndpointRouteBuilder app)
    {
        app.MapPost("/admin/import/roster", Handle).RequireAuthorization("AdminOnly").DisableAntiforgery();
    }

    private static async Task<IResult> Handle(
        IFormFile file,
        [FromForm] int seasonYear,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        string csvContent;
        using (var reader = new StreamReader(file.OpenReadStream()))
        {
            csvContent = await reader.ReadToEndAsync(cancellationToken);
        }

        var rows = CsvRosterParser.Parse(csvContent, seasonYear);

        var allDates = rows.SelectMany(r => r.AttendedDates).Distinct().OrderBy(d => d).ToList();
        var gamesByDate = new Dictionary<DateOnly, Game>();
        foreach (var date in allDates)
        {
            var startsAt = new DateTimeOffset(date.ToDateTime(new TimeOnly(19, 0)), TimeSpan.Zero);
            var existing = await dbContext.Games.FirstOrDefaultAsync(g => g.StartsAt == startsAt, cancellationToken);
            if (existing is not null)
            {
                gamesByDate[date] = existing;
                continue;
            }

            var attendeeCount = rows.Count(r => r.AttendedDates.Contains(date));
            var game = new Game
            {
                StartsAt = startsAt,
                SignupOpensAt = startsAt.AddDays(-1),
                Capacity = attendeeCount,
                Fee = 0m, // Historical charges use each player's own computed per-game rate, not a single game-level fee.
                IsAdHoc = true,
                SignupOpenNotifiedAt = startsAt // Historical games never trigger the "sign-ups open" notification.
            };
            dbContext.Games.Add(game);
            gamesByDate[date] = game;
        }

        var rosterRecordNames = await dbContext.RosterRecords.Select(r => r.Name).ToListAsync(cancellationToken);
        var pendingRowRawData = await dbContext.FlaggedImportRows
            .Where(f => f.Status == ImportRowStatus.Pending)
            .Select(f => f.RawData)
            .ToListAsync(cancellationToken);
        var pendingRowNames = pendingRowRawData.Select(raw => JsonSerializer.Deserialize<PendingRosterRow>(raw)!.Name);

        var existingNames = rosterRecordNames
            .Concat(pendingRowNames)
            .Select(n => n.Trim().ToLowerInvariant())
            .ToHashSet();

        var flaggedCount = 0;
        var skippedCount = 0;
        foreach (var row in rows)
        {
            if (existingNames.Contains(row.Name.Trim().ToLowerInvariant()))
            {
                skippedCount++;
                continue;
            }

            var pending = new PendingRosterRow(row.Name, [.. row.AttendedDates], row.TotalDue, row.AmountPaid);
            dbContext.FlaggedImportRows.Add(new FlaggedImportRow
            {
                RawData = JsonSerializer.Serialize(pending),
                Reason = "Missing email and phone (not present in source spreadsheet)."
            });
            flaggedCount++;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(new { GamesCreated = gamesByDate.Count, RowsFlagged = flaggedCount, RowsSkippedAsDuplicate = skippedCount });
    }
}
