using Microsoft.EntityFrameworkCore;
using ProjectThor.Data;
using ProjectThor.Data.Entities;

namespace ProjectThor.Api.Features.Charges;

/// <summary>Admin post-game waiver - the no-show reconciliation mechanism (design.md D4).</summary>
public static class WaiveChargeEndpoint
{
    public static void MapWaiveCharge(this IEndpointRouteBuilder app)
    {
        app.MapPost("/admin/charges/{chargeId:guid}/waive", Handle).RequireAuthorization("AdminOnly");
    }

    private static async Task<IResult> Handle(Guid chargeId, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        var charge = await dbContext.Charges
            .Include(c => c.Game)
            .FirstOrDefaultAsync(c => c.Id == chargeId, cancellationToken);

        if (charge is null)
        {
            return Results.NotFound();
        }

        if (charge.Status != ChargeStatus.Owed)
        {
            return Results.Conflict($"Charge is {charge.Status}, not Owed - nothing to waive.");
        }

        if (charge.Game!.StartsAt > DateTimeOffset.UtcNow)
        {
            return Results.BadRequest("Charges can only be waived after the game has started.");
        }

        charge.Status = ChargeStatus.Waived;
        charge.ResolvedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(ChargeResponse.From(charge));
    }
}
