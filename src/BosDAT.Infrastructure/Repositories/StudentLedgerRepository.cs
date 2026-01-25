using Microsoft.EntityFrameworkCore;
using BosDAT.Core.Entities;
using BosDAT.Core.Interfaces;
using BosDAT.Infrastructure.Data;

namespace BosDAT.Infrastructure.Repositories;

public class StudentLedgerRepository : Repository<StudentLedgerEntry>, IStudentLedgerRepository
{
    public StudentLedgerRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<StudentLedgerEntry>> GetByStudentAsync(Guid studentId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(e => e.StudentId == studentId)
            .Include(e => e.Student)
            .Include(e => e.Course)
            .Include(e => e.CreatedBy)
            .Include(e => e.Applications)
                .ThenInclude(a => a.Invoice)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<StudentLedgerEntry>> GetOpenEntriesForStudentAsync(Guid studentId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(e => e.StudentId == studentId &&
                       e.EntryType == LedgerEntryType.Credit &&
                       (e.Status == LedgerEntryStatus.Open || e.Status == LedgerEntryStatus.PartiallyApplied))
            .Include(e => e.Applications)
            .OrderBy(e => e.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<StudentLedgerEntry>> GetByStatusAsync(LedgerEntryStatus status, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(e => e.Status == status)
            .Include(e => e.Student)
            .Include(e => e.CreatedBy)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<StudentLedgerEntry?> GetWithApplicationsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(e => e.Student)
            .Include(e => e.Course)
            .Include(e => e.CreatedBy)
            .Include(e => e.Applications)
                .ThenInclude(a => a.Invoice)
            .Include(e => e.Applications)
                .ThenInclude(a => a.AppliedBy)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<decimal> GetAvailableCreditAsync(Guid studentId, CancellationToken cancellationToken = default)
    {
        var entries = await _dbSet
            .Where(e => e.StudentId == studentId &&
                       e.EntryType == LedgerEntryType.Credit &&
                       (e.Status == LedgerEntryStatus.Open || e.Status == LedgerEntryStatus.PartiallyApplied))
            .Include(e => e.Applications)
            .ToListAsync(cancellationToken);

        return entries.Sum(e => e.Amount - e.Applications.Sum(a => a.AppliedAmount));
    }

    public async Task<string> GenerateCorrectionRefNameAsync(CancellationToken cancellationToken = default)
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"CR-{year}-";

        var lastEntry = await _dbSet
            .Where(e => e.CorrectionRefName.StartsWith(prefix))
            .OrderByDescending(e => e.CorrectionRefName)
            .FirstOrDefaultAsync(cancellationToken);

        int nextNumber = 1;
        if (lastEntry != null)
        {
            var parts = lastEntry.CorrectionRefName.Split('-');
            if (parts.Length == 3 && int.TryParse(parts[2], out var lastNumber))
            {
                nextNumber = lastNumber + 1;
            }
        }

        return $"CR-{year}-{nextNumber:D4}";
    }
}
