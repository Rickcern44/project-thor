using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace ProjectThor.Api.Features.Auth;

public static class LogoutEndpoint
{
    public static void MapLogout(this IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/logout", Handle).RequireAuthorization();
    }

    private static async Task<IResult> Handle(HttpContext httpContext, CancellationToken cancellationToken)
    {
        await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Results.Ok();
    }
}
