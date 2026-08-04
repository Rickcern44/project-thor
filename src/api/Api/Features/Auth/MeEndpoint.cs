using System.Security.Claims;

namespace ProjectThor.Api.Features.Auth;

/// <summary>Lets the frontend check current session state (part of "login/session" in 2.1).</summary>
public static class MeEndpoint
{
    public static void MapMe(this IEndpointRouteBuilder app)
    {
        app.MapGet("/auth/me", Handle).RequireAuthorization();
    }

    private static IResult Handle(ClaimsPrincipal user)
    {
        var id = user.FindFirstValue(ClaimTypes.NameIdentifier);
        var name = user.FindFirstValue(ClaimTypes.Name);
        var email = user.FindFirstValue(ClaimTypes.Email);
        var role = user.FindFirstValue(ClaimTypes.Role);

        return Results.Ok(new MeResponse(Guid.Parse(id!), name!, email!, role!));
    }

    public sealed record MeResponse(Guid Id, string Name, string Email, string Role);
}
