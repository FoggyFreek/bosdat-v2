using BosDAT.Core.Entities;
using BosDAT.Core.Enums;
using BosDAT.Core.Interfaces.Repositories;
using BosDAT.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BosDAT.Infrastructure.Repositories;

public class EmailOutboxRepository(ApplicationDbContext context)
    : Repository<EmailOutboxMessage>(context), IEmailOutboxRepository
{
    public async Task<IReadOnlyList<EmailOutboxMessage>> GetPendingBatchAsync(
        int batchSize, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(e => e.Status == EmailStatus.Pending
                && (e.NextAttemptAtUtc == null || e.NextAttemptAtUtc <= DateTime.UtcNow))
            .OrderBy(e => e.CreatedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }
}
