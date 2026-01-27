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
        var prefix = await _context.Settings
            .Where(s => s.Key == "invoice_prefix")
            .Select(s => s.Value)
            .FirstOrDefaultAsync(cancellationToken) ?? "NMI";

        var lastInvoice = await _dbSet
            .Where(i => i.InvoiceNumber.StartsWith($"{prefix}-{year}-"))
            .OrderByDescending(i => i.InvoiceNumber)
            .FirstOrDefaultAsync(cancellationToken);

        int nextNumber = 1;
        if (lastInvoice != null)
        {
            var parts = lastInvoice.InvoiceNumber.Split('-');
            if (parts.Length == 3 && int.TryParse(parts[2], out var lastNumber))
            {
                nextNumber = lastNumber + 1;
            }
        }

        return $"{prefix}-{year}-{nextNumber:D5}";
    }
}
