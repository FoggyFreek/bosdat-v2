using System.Text.Json;
using BosDAT.Core.Enums;

namespace BosDAT.Core.Entities;

public class EmailOutboxMessage : BaseEntity
{
    public string To { get; private set; } = string.Empty;
    public string Subject { get; private set; } = string.Empty;
    public string TemplateName { get; private set; } = string.Empty;
    public string TemplateDataJson { get; private set; } = string.Empty;

    public EmailStatus Status { get; private set; } = EmailStatus.Pending;
    public int RetryCount { get; private set; }
    public DateTime? NextAttemptAtUtc { get; private set; }

    public string? ProviderMessageId { get; private set; }
    public string? LastError { get; private set; }
    public DateTime? SentAtUtc { get; private set; }

    public uint ConcurrencyToken { get; set; }

    private const int MaxRetries = 5;

    private EmailOutboxMessage() { }

    public static EmailOutboxMessage Create(string to, string subject, string templateName, object templateData)
    {
        return new EmailOutboxMessage
        {
            Id = Guid.NewGuid(),
            To = to,
            Subject = subject,
            TemplateName = templateName,
            TemplateDataJson = JsonSerializer.Serialize(templateData),
            Status = EmailStatus.Pending,
            RetryCount = 0,
            NextAttemptAtUtc = null
        };
    }

    public void MarkProcessing()
    {
        Status = EmailStatus.Processing;
    }

    public void MarkSent(string providerMessageId)
    {
        Status = EmailStatus.Sent;
        ProviderMessageId = providerMessageId;
        SentAtUtc = DateTime.UtcNow;
        LastError = null;
    }

    public void MarkFailed(string error)
    {
        RetryCount++;
        LastError = error;

        if (RetryCount >= MaxRetries)
        {
            MarkDeadLetter();
            return;
        }

        Status = EmailStatus.Pending;
        var delayMinutes = Math.Pow(5, RetryCount);
        NextAttemptAtUtc = DateTime.UtcNow.AddMinutes(delayMinutes);
    }

    public void MarkDeadLetter()
    {
        Status = EmailStatus.DeadLetter;
    }
}
