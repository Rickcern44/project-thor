using Microsoft.EntityFrameworkCore;
using ProjectThor.Data;

namespace ProjectThor.Api.Features.GameTemplates;

public static class UpdateGameTemplateEndpoint
{
    private static readonly TimeSpan WeeklyInterval = TimeSpan.FromDays(7);

    public static void MapUpdateGameTemplate(this IEndpointRouteBuilder app)
    {
        app.MapPut("/admin/game-templates/{id:guid}", Handle).RequireAuthorization("AdminOnly");
    }

    private static async Task<IResult> Handle(
        Guid id,
        UpdateGameTemplateRequest request,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (request.SignupLeadTime >= WeeklyInterval)
        {
            return Results.BadRequest("Sign-up lead time must be shorter than the interval between recurring games.");
        }

        var template = await dbContext.GameTemplates.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (template is null)
        {
            return Results.NotFound();
        }

        template.DayOfWeek = request.DayOfWeek;
        template.TimeOfDay = request.TimeOfDay;
        template.DefaultCapacity = request.DefaultCapacity;
        template.Fee = request.Fee;
        template.SignupLeadTime = request.SignupLeadTime;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(GameTemplateResponse.From(template));
    }

    public sealed record UpdateGameTemplateRequest(
        DayOfWeek DayOfWeek,
        TimeOnly TimeOfDay,
        int DefaultCapacity,
        decimal Fee,
        TimeSpan SignupLeadTime);
}
