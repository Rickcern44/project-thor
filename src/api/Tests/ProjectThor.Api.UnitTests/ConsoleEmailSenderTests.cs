using Microsoft.Extensions.Logging;
using ProjectThor.Api.Infrastructure.Email;

namespace ProjectThor.Api.UnitTests;

public class ConsoleEmailSenderTests
{
    [Fact]
    public async Task SendAsync_logs_the_bare_link_rather_than_the_raw_html()
    {
        var logger = new CapturingLogger<ConsoleEmailSender>();
        var sender = new ConsoleEmailSender(logger);
        var link = "http://localhost:5173/auth/consume?token=abc123";
        var html = $"<p>Click to sign in: <a href=\"{link}\">{link}</a></p>";

        await sender.SendAsync("player@example.com", "Your sign-in link", html, CancellationToken.None);

        Assert.Contains(link, logger.LastMessage);
        Assert.DoesNotContain("<a href", logger.LastMessage);
    }

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public string? LastMessage { get; private set; }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            LastMessage = formatter(state, exception);
    }
}
