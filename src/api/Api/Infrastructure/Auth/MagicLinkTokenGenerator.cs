using System.Security.Cryptography;

namespace ProjectThor.Api.Infrastructure.Auth;

/// <summary>
/// Generates opaque magic-link tokens. Only the SHA-256 hash of a token is ever persisted;
/// the raw token exists solely in the emailed link, so a database read can never recover it.
/// </summary>
public static class MagicLinkTokenGenerator
{
    public static (string RawToken, string Hash) Generate()
    {
        var rawToken = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(32));
        return (rawToken, Hash(rawToken));
    }

    public static string Hash(string rawToken) =>
        Convert.ToHexStringLower(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(rawToken)));
}
