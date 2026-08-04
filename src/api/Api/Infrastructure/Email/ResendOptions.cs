namespace ProjectThor.Api.Infrastructure.Email;

public class ResendOptions
{
    public const string SectionName = "Resend";

    public required string ApiKey { get; set; }
    public required string FromAddress { get; set; }
}
