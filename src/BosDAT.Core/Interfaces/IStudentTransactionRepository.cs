using BosDAT.Core.Entities;
using BosDAT.Core.Enums;

namespace BosDAT.Core.Interfaces;

public interface IStudentTransactionRepository : IRepository<StudentTransaction>
{
    Task<IReadOnlyList<StudentTransaction>> GetByStudentAsync(Guid studentId, CancellationToken cancellationToken = default);
    Task<decimal> GetBalanceAsync(Guid studentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StudentTransaction>> GetByStudentFilteredAsync(
        Guid studentId,
        TransactionType? type = null,
        DateOnly? from = null,
        DateOnly? to = null,
        CancellationToken cancellationToken = default);
}
