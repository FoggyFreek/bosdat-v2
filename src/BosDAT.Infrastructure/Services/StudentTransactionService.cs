using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Enums;
using BosDAT.Core.Interfaces;
using BosDAT.Infrastructure.Data;

namespace BosDAT.Infrastructure.Services;

public class StudentTransactionService(
    ApplicationDbContext context,
    IStudentTransactionRepository transactionRepository,
    IUnitOfWork unitOfWork) : IStudentTransactionService
{
    public async Task RecordInvoiceChargeAsync(Invoice invoice, Guid userId, CancellationToken ct = default)
    {
        var transaction = new StudentTransaction
        {
            Id = Guid.NewGuid(),
            StudentId = invoice.StudentId,
            TransactionDate = invoice.IssueDate,
            Type = TransactionType.InvoiceCharge,
            Description = $"Invoice {invoice.InvoiceNumber}",
            ReferenceNumber = invoice.InvoiceNumber,
            Debit = invoice.Total,
            Credit = 0,
            InvoiceId = invoice.Id,
            CreatedById = userId
        };

        await transactionRepository.AddAsync(transaction, ct);
        await unitOfWork.SaveChangesAsync(ct);
    }

    public async Task RecordPaymentAsync(Payment payment, Invoice invoice, Guid userId, CancellationToken ct = default)
    {
        var transaction = new StudentTransaction
        {
            Id = Guid.NewGuid(),
            StudentId = invoice.StudentId,
            TransactionDate = payment.PaymentDate,
            Type = TransactionType.Payment,
            Description = $"Payment for invoice {invoice.InvoiceNumber}",
            ReferenceNumber = payment.Reference ?? invoice.InvoiceNumber,
            Debit = 0,
            Credit = payment.Amount,
            InvoiceId = invoice.Id,
            PaymentId = payment.Id,
            CreatedById = userId
        };

        await transactionRepository.AddAsync(transaction, ct);
        await unitOfWork.SaveChangesAsync(ct);
    }

    public async Task RecordCorrectionAsync(StudentLedgerEntry entry, Guid userId, CancellationToken ct = default)
    {
        var type = entry.EntryType == LedgerEntryType.Credit
            ? TransactionType.CreditCorrection
            : TransactionType.DebitCorrection;

        var transaction = new StudentTransaction
        {
            Id = Guid.NewGuid(),
            StudentId = entry.StudentId,
            TransactionDate = DateOnly.FromDateTime(entry.CreatedAt),
            Type = type,
            Description = $"{entry.CorrectionRefName}: {entry.Description}",
            ReferenceNumber = entry.CorrectionRefName,
            Debit = entry.EntryType == LedgerEntryType.Debit ? entry.Amount : 0,
            Credit = entry.EntryType == LedgerEntryType.Credit ? entry.Amount : 0,
            LedgerEntryId = entry.Id,
            CreatedById = userId
        };

        await transactionRepository.AddAsync(transaction, ct);
        await unitOfWork.SaveChangesAsync(ct);
    }

    public async Task RecordReversalAsync(StudentLedgerEntry reversal, StudentLedgerEntry original, Guid userId, CancellationToken ct = default)
    {
        var transaction = new StudentTransaction
        {
            Id = Guid.NewGuid(),
            StudentId = reversal.StudentId,
            TransactionDate = DateOnly.FromDateTime(reversal.CreatedAt),
            Type = TransactionType.Reversal,
            Description = $"Reversal of {original.CorrectionRefName}",
            ReferenceNumber = reversal.CorrectionRefName,
            Debit = reversal.EntryType == LedgerEntryType.Debit ? reversal.Amount : 0,
            Credit = reversal.EntryType == LedgerEntryType.Credit ? reversal.Amount : 0,
            LedgerEntryId = reversal.Id,
            CreatedById = userId
        };

        await transactionRepository.AddAsync(transaction, ct);
        await unitOfWork.SaveChangesAsync(ct);
    }

    public async Task RecordInvoiceAdjustmentAsync(Invoice invoice, decimal oldTotal, Guid userId, CancellationToken ct = default)
    {
        var difference = invoice.Total - oldTotal;
        if (difference == 0) return;

        var transaction = new StudentTransaction
        {
            Id = Guid.NewGuid(),
            StudentId = invoice.StudentId,
            TransactionDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Type = TransactionType.InvoiceAdjustment,
            Description = $"Invoice {invoice.InvoiceNumber} recalculated ({(difference > 0 ? "+" : "")}{difference:F2})",
            ReferenceNumber = invoice.InvoiceNumber,
            Debit = difference > 0 ? difference : 0,
            Credit = difference < 0 ? Math.Abs(difference) : 0,
            InvoiceId = invoice.Id,
            CreatedById = userId
        };

        await transactionRepository.AddAsync(transaction, ct);
        await unitOfWork.SaveChangesAsync(ct);
    }

    public async Task RecordInvoiceCancellationAsync(Invoice invoice, decimal originalTotal, Guid userId, CancellationToken ct = default)
    {
        if (originalTotal <= 0) return;

        var transaction = new StudentTransaction
        {
            Id = Guid.NewGuid(),
            StudentId = invoice.StudentId,
            TransactionDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Type = TransactionType.InvoiceCancellation,
            Description = $"Invoice {invoice.InvoiceNumber} cancelled",
            ReferenceNumber = invoice.InvoiceNumber,
            Debit = 0,
            Credit = originalTotal,
            InvoiceId = invoice.Id,
            CreatedById = userId
        };

        await transactionRepository.AddAsync(transaction, ct);
        await unitOfWork.SaveChangesAsync(ct);
    }

    public async Task RecordCorrectionAppliedAsync(StudentLedgerEntry entry, Invoice invoice, decimal appliedAmount, Guid userId, CancellationToken ct = default)
    {
        var description = entry.EntryType == LedgerEntryType.Credit
            ? $"{entry.CorrectionRefName} applied to invoice {invoice.InvoiceNumber} (-{appliedAmount:F2})"
            : $"{entry.CorrectionRefName} applied to invoice {invoice.InvoiceNumber} (+{appliedAmount:F2})";

        //do the INVERSE to book the correction
        var transaction = new StudentTransaction
        {
            Id = Guid.NewGuid(),
            StudentId = invoice.StudentId,
            TransactionDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Type = TransactionType.CorrectionApplied,
            Description = description,
            ReferenceNumber = entry.CorrectionRefName,
            Debit = entry.EntryType == LedgerEntryType.Credit ? appliedAmount : 0,
            Credit = entry.EntryType == LedgerEntryType.Debit ? appliedAmount : 0,
            InvoiceId = invoice.Id,
            LedgerEntryId = entry.Id,
            CreatedById = userId
        };  

        await transactionRepository.AddAsync(transaction, ct);
        await unitOfWork.SaveChangesAsync(ct);
    }

    public async Task<StudentLedgerViewDto> GetStudentLedgerAsync(Guid studentId, CancellationToken ct = default)
    {
        var student = await context.Students.FindAsync([studentId], ct)
            ?? throw new InvalidOperationException($"Student with ID {studentId} not found.");

        var transactions = await transactionRepository.GetByStudentAsync(studentId, ct);

        var totalDebited = transactions.Sum(t => t.Debit);
        var totalCredited = transactions.Sum(t => t.Credit);

        var transactionDtos = new List<StudentTransactionDto>();
        var runningBalance = 0m;

        foreach (var tx in transactions)
        {
            runningBalance += tx.Debit - tx.Credit;
            transactionDtos.Add(new StudentTransactionDto
            {
                Id = tx.Id,
                StudentId = tx.StudentId,
                TransactionDate = tx.TransactionDate,
                Type = tx.Type,
                Description = tx.Description,
                ReferenceNumber = tx.ReferenceNumber,
                Debit = tx.Debit,
                Credit = tx.Credit,
                RunningBalance = runningBalance,
                InvoiceId = tx.InvoiceId,
                PaymentId = tx.PaymentId,
                LedgerEntryId = tx.LedgerEntryId,
                CreatedAt = tx.CreatedAt,
                CreatedByName = ResolveUserName(tx.CreatedBy)
            });
        }

        return new StudentLedgerViewDto
        {
            StudentId = studentId,
            StudentName = student.FullName,
            CurrentBalance = runningBalance,
            TotalDebited = totalDebited,
            TotalCredited = totalCredited,
            Transactions = transactionDtos
        };
    }

    public async Task<decimal> GetStudentBalanceAsync(Guid studentId, CancellationToken ct = default)
    {
        return await transactionRepository.GetBalanceAsync(studentId, ct);
    }

    private static string ResolveUserName(ApplicationUser? user)
    {
        if (user == null)
            return "Unknown";

        var name = $"{user.FirstName} {user.LastName}".Trim();
        return string.IsNullOrWhiteSpace(name)
            ? user.Email ?? "Unknown"
            : name;
    }
}
