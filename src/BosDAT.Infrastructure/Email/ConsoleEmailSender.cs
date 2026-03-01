using BosDAT.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace BosDAT.Infrastructure.Email;

public class ConsoleEmailSender(ILogger<ConsoleEmailSender> logger) : IEmailSender
{
    public Task<string> SendAsync(string to, string subject, string htmlBody,
        CancellationToken cancellationToken = default)
    {
        var messageId = $"console-{Guid.NewGuid():N}";
        var bodyLength = htmlBody.Length;

        logger.LogInformation(
            "=== EMAIL (Console Provider) ===\n" +
            "To: {To}\n" +
            "Subject: {Subject}\n" +
            "MessageId: {MessageId}\n" +
            "Body Length: {BodyLength} chars\n" +
            "================================",
            to, subject, messageId, bodyLength);

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

            var batchBodyLength = message.HtmlBody.Length;
            logger.LogInformation(
                "=== EMAIL BATCH (Console Provider) ===\n" +
                "To: {To}\n" +
                "Subject: {Subject}\n" +
                "MessageId: {MessageId}\n" +
                "Body Length: {BodyLength} chars\n" +
                "======================================",
                message.To, message.Subject, messageId, batchBodyLength);
        }

        var emailCount = messages.Count;
        logger.LogInformation("Batch complete: {Count} emails logged", emailCount);
        return Task.FromResult<IReadOnlyList<string>>(messageIds);
    }
}
