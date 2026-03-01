namespace BosDAT.Infrastructure.Email;

public class EmailSettings
{
    public const string SectionName = "EmailSettings";

    public string Provider { get; set; } = "Console";
    public required string FromEmail { get; set; }
    public required string FromName { get; set; }
    public BrevoSettings Brevo { get; set; } = new();
}

public class BrevoSettings
{
    public string ApiKey { get; set; } = string.Empty;
}
