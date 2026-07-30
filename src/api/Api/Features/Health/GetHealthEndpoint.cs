namespace ProjectThor.Api.Features.Health;

public static class GetHealthEndpoint
{
    public static void MapGetHealth(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", Handle);
    }

    private static IResult Handle()
    {
        var response = new HealthResponse("healthy", DateTimeOffset.UtcNow);
        return Results.Ok(response);
    }
}

public sealed record HealthResponse(string Status, DateTimeOffset Timestamp);
