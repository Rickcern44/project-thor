using ProjectThor.Data;

namespace ProjectThor.Api.Features.Health;

public static class GetHealthEndpoint
{
    public static void MapGetHealth(this IEndpointRouteBuilder app)
    {
        // Intentionally unauthenticated so k8s liveness/readiness probes can call it without credentials.
        app.MapGet("/health", Handle);
    }

    private static async Task<IResult> Handle(AppDbContext dbContext)
    {
        var databaseReachable = await TryConnectAsync(dbContext);
        var response = new HealthResponse("healthy", DateTimeOffset.UtcNow, databaseReachable);
        return Results.Ok(response);
    }

    private static async Task<bool> TryConnectAsync(AppDbContext dbContext)
    {
        try
        {
            return await dbContext.Database.CanConnectAsync();
        }
        catch
        {
            return false;
        }
    }
}

public sealed record HealthResponse(string Status, DateTimeOffset Timestamp, bool DatabaseReachable);
