using Microsoft.EntityFrameworkCore;
using ProjectThor.Data;
using ProjectThor.Data.Entities;

namespace ProjectThor.Api.Features.Charges;

/// <summary>Admin marks a charge as paid, reducing the player's outstanding balance.</summary>
public static class PayChargeEndpoint
{
    public static void MapPayCharge(this IEndpointRouteBuilder app)
    {
        app.MapPost("/admin/charges/{chargeId:guid}/pay", Handle).RequireAuthorization("AdminOnly");
    }

    private static async Task<IResult> Handle(Guid chargeId, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        var charge = await dbContext.Charges.FirstOrDefaultAsync(c => c.Id == chargeId, cancellationToken);
        if (charge is null)
        {
            return Results.NotFound();
        }

        if (charge.Status != ChargeStatus.Owed)
        {
            return Results.Conflict($"Charge is {charge.Status}, not Owed - nothing to mark paid.");
        }

        charge.Status = ChargeStatus.Paid;
        charge.ResolvedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(ChargeResponse.From(charge));
    }
}
