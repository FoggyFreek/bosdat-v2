namespace BosDAT.Core.Interfaces.Services;

public interface IEmailSender
{
    Task<string> SendAsync(string to, string subject, string htmlBody,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> SendBatchAsync(IReadOnlyList<EmailMessage> messages,
        CancellationToken cancellationToken = default);
}

public record EmailMessage(string To, string Subject, string HtmlBody);
