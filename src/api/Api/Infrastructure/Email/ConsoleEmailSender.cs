using System.Text.RegularExpressions;

namespace ProjectThor.Api.Infrastructure.Email;

/// <summary>Logs emails instead of sending them, so a magic link shows up as a structured entry
/// in Aspire's dashboard without a real Resend API key. Local dev only, see Program.cs.</summary>
public partial class ConsoleEmailSender(ILogger<ConsoleEmailSender> logger) : IEmailSender
{
    public Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken)
    {
        var link = LinkPattern().Match(htmlBody) is { Success: true } match ? match.Groups[1].Value : htmlBody;

        logger.LogInformation(
            "Dev email — To: {ToEmail}, Subject: {Subject}, Link: {Link}", toEmail, subject, link);

        return Task.CompletedTask;
    }

    [GeneratedRegex("href=\"([^\"]+)\"")]
    private static partial Regex LinkPattern();
}
