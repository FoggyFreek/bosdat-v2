using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Enums;
using BosDAT.Core.Interfaces;
using BosDAT.Infrastructure.Data;

namespace BosDAT.Infrastructure.Services;

public class StudentTransactionService(
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

    public async Task RecordCreditInvoiceAsync(Invoice creditInvoice, Invoice originalInvoice, Guid userId, CancellationToken ct = default)
    {
        var transaction = new StudentTransaction
        {
            Id = Guid.NewGuid(),
            StudentId = creditInvoice.StudentId,
            TransactionDate = creditInvoice.IssueDate,
            Type = TransactionType.CreditInvoice,
            Description = $"Credit invoice {creditInvoice.InvoiceNumber} for invoice {originalInvoice.InvoiceNumber}",
            ReferenceNumber = creditInvoice.InvoiceNumber,
            Debit = 0,
            Credit = Math.Abs(creditInvoice.Total),
            InvoiceId = creditInvoice.Id,
            CreatedById = userId
        };

        await transactionRepository.AddAsync(transaction, ct);
        await unitOfWork.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<StudentTransactionDto>> GetTransactionsAsync(Guid studentId, CancellationToken ct = default)
    {
        var transactions = await transactionRepository.GetByStudentAsync(studentId, ct);

        var runningBalance = 0m;
        var dtos = new List<StudentTransactionDto>();
        foreach (var t in transactions)
        {
            runningBalance += t.Debit - t.Credit;
            dtos.Add(new StudentTransactionDto
            {
                Id = t.Id,
                StudentId = t.StudentId,
                TransactionDate = t.TransactionDate,
                Type = t.Type,
                Description = t.Description,
                ReferenceNumber = t.ReferenceNumber,
                Debit = t.Debit,
                Credit = t.Credit,
                RunningBalance = runningBalance,
                InvoiceId = t.InvoiceId,
                PaymentId = t.PaymentId,
                LedgerEntryId = t.LedgerEntryId,
                CreatedAt = t.CreatedAt,
                CreatedByName = ResolveUserName(t.CreatedBy),
            });
        }
        return dtos;
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
