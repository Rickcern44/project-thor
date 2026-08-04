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
        var rosterRecord = await dbContext.RosterRecords
            .FirstOrDefaultAsync(r => r.Id == request.RosterRecordId, cancellationToken);

        if (rosterRecord is null)
        {
            return Results.NotFound();
        }

        var alreadyInvited = await dbContext.Users
            .AnyAsync(u => u.RosterRecordId == rosterRecord.Id, cancellationToken);
        if (alreadyInvited)
        {
            return Results.Conflict("A user has already been invited for this roster record.");
        }

        var user = new User
        {
            Email = rosterRecord.Email,
            Phone = rosterRecord.Phone,
            Name = rosterRecord.Name,
            Role = UserRole.Player,
            Status = UserStatus.Pending,
            RosterRecordId = rosterRecord.Id
        };
        dbContext.Users.Add(user);

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
