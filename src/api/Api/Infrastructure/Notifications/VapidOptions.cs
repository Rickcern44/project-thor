namespace ProjectThor.Api.Infrastructure.Notifications;

public class VapidOptions
{
    public const string SectionName = "Vapid";

    public required string PublicKey { get; set; }
    public required string PrivateKey { get; set; }
    public required string Subject { get; set; }
}
