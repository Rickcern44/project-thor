using Microsoft.EntityFrameworkCore;
using ProjectThor.Data;

namespace ProjectThor.Api.Features.GameTemplates;

public static class GetActiveGameTemplateEndpoint
{
    public static void MapGetActiveGameTemplate(this IEndpointRouteBuilder app)
    {
        app.MapGet("/admin/game-templates/active", Handle).RequireAuthorization("AdminOnly");
    }

    private static async Task<IResult> Handle(AppDbContext dbContext, CancellationToken cancellationToken)
    {
        var template = await dbContext.GameTemplates.FirstOrDefaultAsync(t => t.IsActive, cancellationToken);
        return template is null ? Results.NotFound() : Results.Ok(GameTemplateResponse.From(template));
    }
}
