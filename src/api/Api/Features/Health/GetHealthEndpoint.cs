using ProjectThor.Data;

namespace ProjectThor.Api.Features.Health;

public static class GetHealthEndpoint
{
    public static void MapGetHealth(this IEndpointRouteBuilder app)
    {
        // Intentionally unauthenticated so k8s liveness/readiness probes can call it without credentials.
        app.MapGet("/health", Handle);
    }

    private static async Task<IResult> Handle(AppDbContext dbContext, ILoggerFactory loggerFactory)
    {
        var databaseReachable = await TryConnectAsync(dbContext, loggerFactory.CreateLogger(nameof(GetHealthEndpoint)));
        var response = new HealthResponse("healthy", DateTimeOffset.UtcNow, databaseReachable);
        return Results.Ok(response);
    }

    private static async Task<bool> TryConnectAsync(AppDbContext dbContext, ILogger logger)
    {
        try
        {
            return await dbContext.Database.CanConnectAsync();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Health check could not reach the database.");
            return false;
        }
    }
}

public sealed record HealthResponse(string Status, DateTimeOffset Timestamp, bool DatabaseReachable);
