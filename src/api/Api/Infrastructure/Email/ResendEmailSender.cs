using System.Net.Http.Json;
using Microsoft.Extensions.Options;

namespace ProjectThor.Api.Infrastructure.Email;

/// <summary>Sends email via the Resend REST API (https://resend.com/docs/api-reference/emails/send-email).</summary>
public class ResendEmailSender(HttpClient httpClient, IOptions<ResendOptions> options) : IEmailSender
{
    public async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken)
    {
        var request = new ResendSendRequest(options.Value.FromAddress, [toEmail], subject, htmlBody);

        using var response = await httpClient.PostAsJsonAsync("emails", request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private sealed record ResendSendRequest(string From, string[] To, string Subject, string Html);
}
