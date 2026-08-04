using Microsoft.EntityFrameworkCore;
using ProjectThor.Data;
using ProjectThor.Data.Entities;

namespace ProjectThor.Api.Features.Import;

public static class ListFlaggedRowsEndpoint
{
    public static void MapListFlaggedRows(this IEndpointRouteBuilder app)
    {
        app.MapGet("/admin/import/flagged-rows", Handle).RequireAuthorization("AdminOnly");
    }

    private static async Task<IResult> Handle(AppDbContext dbContext, CancellationToken cancellationToken)
    {
        var rows = await dbContext.FlaggedImportRows
            .Where(f => f.Status == ImportRowStatus.Pending)
            .OrderBy(f => f.CreatedAt)
            .Select(f => new { f.Id, f.RawData, f.Reason, f.CreatedAt })
            .ToListAsync(cancellationToken);

        return Results.Ok(rows);
    }
}
