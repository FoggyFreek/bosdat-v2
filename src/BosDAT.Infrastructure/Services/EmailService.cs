using BosDAT.Core.Entities;
using BosDAT.Core.Interfaces;
using BosDAT.Core.Interfaces.Services;

namespace BosDAT.Infrastructure.Services;

public class EmailService(IUnitOfWork uow) : IEmailService
{
    public async Task QueueEmailAsync(string to, string subject, string templateName,
        object templateData, CancellationToken cancellationToken = default)
    {
        var message = EmailOutboxMessage.Create(to, subject, templateName, templateData);
        await uow.EmailOutboxMessages.AddAsync(message, cancellationToken);
    }
}
