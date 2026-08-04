using Microsoft.EntityFrameworkCore;
using ProjectThor.Api.Infrastructure.Auth;
using ProjectThor.Api.Infrastructure.Email;
using ProjectThor.Data;
using ProjectThor.Data.Entities;

namespace ProjectThor.Api.Features.Auth;

public static class RequestLoginLinkEndpoint
{
    public static void MapRequestLoginLink(this IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/login/request", Handle);
    }

    private static async Task<IResult> Handle(
        RequestLoginLinkRequest request,
        AppDbContext dbContext,
        IEmailSender emailSender,
        IConfiguration configuration,
        CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email && u.Status == UserStatus.Active, cancellationToken);

        // Always return 200 regardless of match, so this endpoint can't be used to enumerate registered emails.
        if (user is not null)
        {
            var (rawToken, hash) = MagicLinkTokenGenerator.Generate();
            dbContext.MagicLinkTokens.Add(new MagicLinkToken
            {
                UserId = user.Id,
                TokenHash = hash,
                Purpose = MagicLinkPurpose.Login,
                ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(15)
            });
            await dbContext.SaveChangesAsync(cancellationToken);

            var frontendOrigin = configuration["Frontend:Origin"] ?? "http://localhost:5173";
            var link = $"{frontendOrigin}/auth/consume?token={rawToken}";
            await emailSender.SendAsync(
                user.Email,
                "Your sign-in link",
                $"<p>Click to sign in: <a href=\"{link}\">{link}</a></p>",
                cancellationToken);
        }

        return Results.Ok();
    }

    public sealed record RequestLoginLinkRequest(string Email);
}
