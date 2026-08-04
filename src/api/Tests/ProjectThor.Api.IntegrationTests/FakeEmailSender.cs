using System.Collections.Concurrent;
using ProjectThor.Api.Infrastructure.Email;

namespace ProjectThor.Api.IntegrationTests;

/// <summary>Captures emails instead of sending them, so tests can extract magic-link tokens.</summary>
public class FakeEmailSender : IEmailSender
{
    public ConcurrentBag<SentEmail> SentEmails { get; } = [];

    public Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken)
    {
        SentEmails.Add(new SentEmail(toEmail, subject, htmlBody));
        return Task.CompletedTask;
    }

    public record SentEmail(string ToEmail, string Subject, string HtmlBody);
}
