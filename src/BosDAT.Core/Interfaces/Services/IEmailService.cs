namespace BosDAT.Core.Interfaces.Services;

public interface IEmailService
{
    Task QueueEmailAsync(string to, string subject, string templateName,
        object templateData, CancellationToken cancellationToken = default);

    Task QueueEmailAsync(string to, string subject, string templateName,
        object templateData, IReadOnlyList<EmailAttachment>? attachments,
        CancellationToken cancellationToken = default);
}
