using BosDAT.Core.Entities;

namespace BosDAT.Core.Interfaces.Repositories;

public interface IEmailOutboxRepository : IRepository<EmailOutboxMessage>
{
    Task<IReadOnlyList<EmailOutboxMessage>> GetPendingBatchAsync(int batchSize, CancellationToken cancellationToken = default);
}
