using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ProjectThor.Api.Infrastructure.Import;
using ProjectThor.Data;
using ProjectThor.Data.Entities;

namespace ProjectThor.Api.Features.Import;

/// <summary>
/// Admin supplies the missing email/phone for a flagged row, creating the real RosterRecord +
/// Player User + historical SignUp/Charge records against the Games already created at import
/// time. HistoricalChargeReconciler decides which of the attended games are Paid vs Owed.
/// </summary>
public static class ResolveFlaggedRowEndpoint
{
    public static void MapResolveFlaggedRow(this IEndpointRouteBuilder app)
    {
        app.MapPost("/admin/import/flagged-rows/{id:guid}/resolve", Handle).RequireAuthorization("AdminOnly");
    }

    private static async Task<IResult> Handle(
        Guid id,
        ResolveFlaggedRowRequest request,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var flaggedRow = await dbContext.FlaggedImportRows.FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
        if (flaggedRow is null || flaggedRow.Status != ImportRowStatus.Pending)
        {
            return Results.NotFound();
        }

        // User.Email and RosterRecord.Email both carry a unique index - check up front so a
        // collision (e.g. two spelling variants of the same person, resolved in different
        // sessions) comes back as a clean 409 instead of a raw constraint-violation 500.
        var emailAlreadyExists = await dbContext.Users.AnyAsync(u => u.Email == request.Email, cancellationToken)
            || await dbContext.RosterRecords.AnyAsync(r => r.Email == request.Email, cancellationToken);
        if (emailAlreadyExists)
        {
            return Results.Conflict("A player with this email already exists.");
        }

        var pending = JsonSerializer.Deserialize<PendingRosterRow>(flaggedRow.RawData)!;
        var attendedDates = pending.AttendedDates.OrderBy(d => d).ToList();
        var perGameRate = attendedDates.Count > 0 ? Math.Round(pending.TotalDue / attendedDates.Count, 2) : 0m;
        var reconciliation = HistoricalChargeReconciler.Reconcile(attendedDates.Count, perGameRate, pending.AmountPaid, pending.TotalDue);

        var rosterRecord = new RosterRecord
        {
            Name = request.Name,
            Email = request.Email,
            Phone = request.Phone,
            LegacyBalance = reconciliation.LegacyBalanceAdjustment
        };
        dbContext.RosterRecords.Add(rosterRecord);

        var user = new User
        {
            Email = request.Email,
            Phone = request.Phone,
            Name = request.Name,
            Role = UserRole.Player,
            Status = UserStatus.Pending,
            RosterRecordId = rosterRecord.Id
        };
        dbContext.Users.Add(user);

        for (var i = 0; i < attendedDates.Count; i++)
        {
            var date = attendedDates[i];
            var startsAt = new DateTimeOffset(date.ToDateTime(new TimeOnly(19, 0)), TimeSpan.Zero);
            var game = await dbContext.Games.FirstOrDefaultAsync(g => g.StartsAt == startsAt, cancellationToken);
            if (game is null)
            {
                continue; // Defensive: should always exist from import time.
            }

            var isPaid = i < reconciliation.PaidGameCount;
            dbContext.SignUps.Add(new SignUp
            {
                GameId = game.Id,
                PlayerUserId = user.Id,
                Status = SignUpStatus.Rostered,
                SignedUpAt = startsAt
            });
            dbContext.Charges.Add(new Charge
            {
                GameId = game.Id,
                PlayerUserId = user.Id,
                Amount = perGameRate,
                Status = isPaid ? ChargeStatus.Paid : ChargeStatus.Owed,
                CreatedAt = startsAt,
                ResolvedAt = isPaid ? startsAt : null
            });
        }

        flaggedRow.Status = ImportRowStatus.Resolved;
        flaggedRow.ResolvedRosterRecordId = rosterRecord.Id;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(new { RosterRecordId = rosterRecord.Id, UserId = user.Id });
    }

    public sealed record ResolveFlaggedRowRequest(string Name, string Email, string Phone);
}
