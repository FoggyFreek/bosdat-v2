using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;

namespace BosDAT.Core.Interfaces;

public interface IStudentTransactionService
{
    Task RecordInvoiceChargeAsync(Invoice invoice, Guid userId, CancellationToken ct = default);
    Task RecordPaymentAsync(Payment payment, Invoice invoice, Guid userId, CancellationToken ct = default);
    Task RecordCorrectionAsync(StudentLedgerEntry entry, Guid userId, CancellationToken ct = default);
    Task RecordReversalAsync(StudentLedgerEntry reversal, StudentLedgerEntry original, Guid userId, CancellationToken ct = default);
    Task RecordInvoiceAdjustmentAsync(Invoice invoice, decimal oldTotal, Guid userId, CancellationToken ct = default);
    Task RecordInvoiceCancellationAsync(Invoice invoice, decimal originalTotal, Guid userId, CancellationToken ct = default);
    Task RecordCorrectionAppliedAsync(StudentLedgerEntry entry, Invoice invoice, decimal appliedAmount, Guid userId, CancellationToken ct = default);
    Task<StudentLedgerViewDto> GetStudentLedgerAsync(Guid studentId, CancellationToken ct = default);
    Task<decimal> GetStudentBalanceAsync(Guid studentId, CancellationToken ct = default);
}
