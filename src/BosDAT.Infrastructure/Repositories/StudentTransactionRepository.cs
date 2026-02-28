using Microsoft.EntityFrameworkCore;
using BosDAT.Core.Entities;
using BosDAT.Core.Enums;
using BosDAT.Core.Interfaces;
using BosDAT.Core.Interfaces.Repositories;
using BosDAT.Infrastructure.Data;

namespace BosDAT.Infrastructure.Repositories;

public class StudentTransactionRepository : Repository<StudentTransaction>, IStudentTransactionRepository
{
    public StudentTransactionRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<StudentTransaction>> GetByStudentAsync(
        Guid studentId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(t => t.StudentId == studentId)
            .Include(t => t.Student)
            .Include(t => t.Invoice)
            .Include(t => t.Payment)
            .Include(t => t.CreatedBy)
            .OrderBy(t => t.TransactionDate)
            .ThenBy(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<decimal> GetBalanceAsync(
        Guid studentId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(t => t.StudentId == studentId)
            .SumAsync(t => t.Debit - t.Credit, cancellationToken);
    }

    public async Task<IReadOnlyList<StudentTransaction>> GetByStudentFilteredAsync(
        Guid studentId,
        TransactionType? type = null,
        DateOnly? from = null,
        DateOnly? to = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .AsNoTracking()
            .Where(t => t.StudentId == studentId);

        if (type.HasValue)
            query = query.Where(t => t.Type == type.Value);

        if (from.HasValue)
            query = query.Where(t => t.TransactionDate >= from.Value);

        if (to.HasValue)
            query = query.Where(t => t.TransactionDate <= to.Value);

        return await query
            .Include(t => t.Student)
            .Include(t => t.Invoice)
            .Include(t => t.Payment)
            .Include(t => t.CreatedBy)
            .OrderBy(t => t.TransactionDate)
            .ThenBy(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<decimal> GetAppliedCreditAmountAsync(Guid creditInvoiceId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(t => t.InvoiceId == creditInvoiceId && t.Type == TransactionType.CreditOffset)
            .SumAsync(t => t.Debit, cancellationToken);
    }
}
