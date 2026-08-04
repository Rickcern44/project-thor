using Microsoft.EntityFrameworkCore;
using ProjectThor.Api.Infrastructure.Auth;
using ProjectThor.Api.Infrastructure.Email;
using ProjectThor.Data;
using ProjectThor.Data.Entities;

namespace ProjectThor.Api.Features.Invites;

public static class IssueInviteEndpoint
{
    public static void MapIssueInvite(this IEndpointRouteBuilder app)
    {
        app.MapPost("/admin/invites", Handle).RequireAuthorization("AdminOnly");
    }

    private static async Task<IResult> Handle(
        IssueInviteRequest request,
        AppDbContext dbContext,
        IEmailSender emailSender,
        IConfiguration configuration,
        CancellationToken cancellationToken)
    {
        // The User is created when the RosterRecord is resolved (roster-import), not here - this
        // endpoint only sends the credential to an identity that already exists (design.md D2's
        // "invite is the first magic link" still holds; it's just decoupled from record creation
        // now that historical roster-import needs a User to exist before anyone's been invited).
        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.RosterRecordId == request.RosterRecordId, cancellationToken);

        if (user is null)
        {
            return Results.NotFound();
        }

        if (user.Status != UserStatus.Pending)
        {
            return Results.Conflict("This player has already activated their account.");
        }

        var (rawToken, hash) = MagicLinkTokenGenerator.Generate();
        dbContext.MagicLinkTokens.Add(new MagicLinkToken
        {
            UserId = user.Id,
            TokenHash = hash,
            Purpose = MagicLinkPurpose.Invite,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7)
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        var frontendOrigin = configuration["Frontend:Origin"] ?? "http://localhost:5173";
        var link = $"{frontendOrigin}/auth/consume?token={rawToken}";
        await emailSender.SendAsync(
            user.Email,
            "You're invited to the league app",
            $"<p>You've been invited. Click to activate your account: <a href=\"{link}\">{link}</a></p>",
            cancellationToken);

        return Results.Created(
            $"/admin/invites/{user.Id}",
            new IssueInviteResponse(user.Id, user.Email, user.Status.ToString()));
    }

    public sealed record IssueInviteRequest(Guid RosterRecordId);

    public sealed record IssueInviteResponse(Guid UserId, string Email, string Status);
}
