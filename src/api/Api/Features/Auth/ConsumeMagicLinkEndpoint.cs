using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using ProjectThor.Api.Infrastructure.Auth;
using ProjectThor.Data;
using ProjectThor.Data.Entities;

namespace ProjectThor.Api.Features.Auth;

public static class ConsumeMagicLinkEndpoint
{
    public static void MapConsumeMagicLink(this IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/consume", Handle);
    }

    private static async Task<IResult> Handle(
        ConsumeMagicLinkRequest request,
        HttpContext httpContext,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var hash = MagicLinkTokenGenerator.Hash(request.Token);
        var now = DateTimeOffset.UtcNow;

        var token = await dbContext.MagicLinkTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);

        if (token is null || token.ConsumedAt is not null || token.ExpiresAt < now || token.User is null)
        {
            return Results.Unauthorized();
        }

        token.ConsumedAt = now;

        var user = token.User;
        if (token.Purpose == MagicLinkPurpose.Invite && user.Status == UserStatus.Pending)
        {
            user.Status = UserStatus.Active;
            user.ActivatedAt = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Role, user.Role.ToString())
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

        return Results.Ok(new ConsumeMagicLinkResponse(user.Id, user.Name, user.Email, user.Role.ToString()));
    }

    public sealed record ConsumeMagicLinkRequest(string Token);

    public sealed record ConsumeMagicLinkResponse(Guid Id, string Name, string Email, string Role);
}
