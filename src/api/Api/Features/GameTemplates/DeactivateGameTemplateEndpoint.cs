using Microsoft.EntityFrameworkCore;
using ProjectThor.Data;

namespace ProjectThor.Api.Features.GameTemplates;

public static class DeactivateGameTemplateEndpoint
{
    public static void MapDeactivateGameTemplate(this IEndpointRouteBuilder app)
    {
        app.MapDelete("/admin/game-templates/{id:guid}", Handle).RequireAuthorization("AdminOnly");
    }

    private static async Task<IResult> Handle(Guid id, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        var template = await dbContext.GameTemplates.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (template is null)
        {
            return Results.NotFound();
        }

        // Soft-delete: Games already generated from this template keep a valid TemplateId reference.
        template.IsActive = false;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.NoContent();
    }
}
