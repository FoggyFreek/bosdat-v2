using BosDAT.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace BosDAT.Infrastructure.Email;

public class ConsoleEmailSender(ILogger<ConsoleEmailSender> logger) : IEmailSender
{
    public Task<string> SendAsync(string to, string subject, string htmlBody,
        CancellationToken cancellationToken = default)
    {
        var messageId = $"console-{Guid.NewGuid():N}";

        logger.LogInformation(
            "=== EMAIL (Console Provider) ===\n" +
            "To: {To}\n" +
            "Subject: {Subject}\n" +
            "MessageId: {MessageId}\n" +
            "Body Length: {BodyLength} chars\n" +
            "================================",
            to, subject, messageId, htmlBody.Length);

        return Task.FromResult(messageId);
    }

    public Task<IReadOnlyList<string>> SendBatchAsync(IReadOnlyList<EmailMessage> messages,
        CancellationToken cancellationToken = default)
    {
        var messageIds = new List<string>(messages.Count);

        foreach (var message in messages)
        {
            var messageId = $"console-{Guid.NewGuid():N}";
            messageIds.Add(messageId);

            logger.LogInformation(
                "=== EMAIL BATCH (Console Provider) ===\n" +
                "To: {To}\n" +
                "Subject: {Subject}\n" +
                "MessageId: {MessageId}\n" +
                "Body Length: {BodyLength} chars\n" +
                "======================================",
                message.To, message.Subject, messageId, message.HtmlBody.Length);
        }

        logger.LogInformation("Batch complete: {Count} emails logged", messages.Count);
        return Task.FromResult<IReadOnlyList<string>>(messageIds);
    }
}
