using Microsoft.EntityFrameworkCore;
using ProjectThor.Data;
using ProjectThor.Data.Entities;

namespace ProjectThor.Api.Features.GameTemplates;

public static class CreateGameTemplateEndpoint
{
    // Weekly recurring template - the interval between generated games is always 7 days.
    private static readonly TimeSpan WeeklyInterval = TimeSpan.FromDays(7);

    public static void MapCreateGameTemplate(this IEndpointRouteBuilder app)
    {
        app.MapPost("/admin/game-templates", Handle).RequireAuthorization("AdminOnly");
    }

    private static async Task<IResult> Handle(
        CreateGameTemplateRequest request,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (request.SignupLeadTime >= WeeklyInterval)
        {
            return Results.BadRequest("Sign-up lead time must be shorter than the interval between recurring games.");
        }

        var activeExists = await dbContext.GameTemplates.AnyAsync(t => t.IsActive, cancellationToken);
        if (activeExists)
        {
            return Results.Conflict("An active recurring template already exists. Update or deactivate it first.");
        }

        var template = new GameTemplate
        {
            DayOfWeek = request.DayOfWeek,
            TimeOfDay = request.TimeOfDay,
            DefaultCapacity = request.DefaultCapacity,
            Fee = request.Fee,
            SignupLeadTime = request.SignupLeadTime,
            IsActive = true
        };
        dbContext.GameTemplates.Add(template);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Created($"/admin/game-templates/{template.Id}", GameTemplateResponse.From(template));
    }

    public sealed record CreateGameTemplateRequest(
        DayOfWeek DayOfWeek,
        TimeOnly TimeOfDay,
        int DefaultCapacity,
        decimal Fee,
        TimeSpan SignupLeadTime);
}
