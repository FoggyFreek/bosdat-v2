using BosDAT.Core.Entities;

namespace BosDAT.Core.Interfaces;

public interface IStudentLedgerRepository : IRepository<StudentLedgerEntry>
{
    Task<IReadOnlyList<StudentLedgerEntry>> GetByStudentAsync(Guid studentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StudentLedgerEntry>> GetOpenEntriesForStudentAsync(Guid studentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StudentLedgerEntry>> GetByStatusAsync(LedgerEntryStatus status, CancellationToken cancellationToken = default);
    Task<StudentLedgerEntry?> GetWithApplicationsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<decimal> GetAvailableCreditAsync(Guid studentId, CancellationToken cancellationToken = default);
    Task<string> GenerateCorrectionRefNameAsync(CancellationToken cancellationToken = default);
}
