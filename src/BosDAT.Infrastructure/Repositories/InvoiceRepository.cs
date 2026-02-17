using Microsoft.EntityFrameworkCore;
using BosDAT.Core.Entities;
using BosDAT.Core.Interfaces;
using BosDAT.Infrastructure.Data;

namespace BosDAT.Infrastructure.Repositories;

public class InvoiceRepository : Repository<Invoice>, IInvoiceRepository
{
    public InvoiceRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Invoice?> GetByInvoiceNumberAsync(string invoiceNumber, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(i => i.Student)
            .Include(i => i.Lines)
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.InvoiceNumber == invoiceNumber, cancellationToken);
    }

    public async Task<Invoice?> GetWithLinesAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(i => i.Student)
            .Include(i => i.Lines)
                .ThenInclude(l => l.Lesson)
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
    }

    public async Task<Invoice?> GetWithLinesAndEnrollmentsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(i => i.Lines)
                .ThenInclude(l => l.Lesson)
            .Include(i => i.Payments)
            .Include(i => i.Enrollment)
                .ThenInclude(e => e!.Student)
            .Include(i => i.Enrollment)
                .ThenInclude(e => e!.Course)
                    .ThenInclude(c => c.CourseType)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Invoice>> GetByStudentAsync(Guid studentId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(i => i.StudentId == studentId)
            .Include(i => i.Lines)
            .Include(i => i.Payments)
            .OrderByDescending(i => i.IssueDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Invoice>> GetByStatusAsync(InvoiceStatus status, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(i => i.Status == status)
            .Include(i => i.Student)
            .OrderByDescending(i => i.IssueDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Invoice>> GetOverdueInvoicesAsync(CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return await _dbSet
            .Where(i => i.Status == InvoiceStatus.Sent && i.DueDate < today)
            .Include(i => i.Student)
            .OrderBy(i => i.DueDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<string> GenerateInvoiceNumberAsync(CancellationToken cancellationToken = default)
    {
        var year = DateTime.UtcNow.Year;
        var yearPrefix = year.ToString();

        var lastInvoice = await _dbSet
            .Where(i => i.InvoiceNumber.StartsWith(yearPrefix))
            .OrderByDescending(i => i.InvoiceNumber)
            .FirstOrDefaultAsync(cancellationToken);

        int nextNumber = 1;
        if (lastInvoice != null)
        {
            // Invoice number format: YYYYNN (e.g., 202601, 202602)
            var numberPart = lastInvoice.InvoiceNumber[4..];
            if (int.TryParse(numberPart, out var lastNumber))
            {
                nextNumber = lastNumber + 1;
            }
        }

        // Format: YYYYNN with at least 2 digits for the sequence number
        return $"{year}{nextNumber:D2}";
    }

    public async Task<IReadOnlyList<Invoice>> GetByEnrollmentAsync(Guid enrollmentId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(i => i.EnrollmentId == enrollmentId)
            .Include(i => i.Lines)
            .Include(i => i.Payments)
            .OrderByDescending(i => i.IssueDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<Invoice?> GetByPeriodAsync(Guid studentId, Guid enrollmentId, DateOnly periodStart, DateOnly periodEnd, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(i => i.StudentId == studentId &&
                        i.EnrollmentId == enrollmentId &&
                        i.PeriodStart == periodStart &&
                        i.PeriodEnd == periodEnd)
            .Include(i => i.Lines)
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Invoice>> GetUnpaidInvoicesAsync(Guid studentId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(i => i.StudentId == studentId &&
                        (i.Status == InvoiceStatus.Draft || i.Status == InvoiceStatus.Sent || i.Status == InvoiceStatus.Overdue))
            .Include(i => i.Lines)
            .Include(i => i.Payments)
            .OrderBy(i => i.IssueDate)
            .ToListAsync(cancellationToken);
    }
}
