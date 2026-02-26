using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;

namespace BosDAT.Core.Interfaces;

public interface IStudentTransactionService
{
    Task RecordInvoiceChargeAsync(Invoice invoice, Guid userId, CancellationToken ct = default);
    Task RecordPaymentAsync(Payment payment, Invoice invoice, Guid userId, CancellationToken ct = default);
    Task RecordInvoiceAdjustmentAsync(Invoice invoice, decimal oldTotal, Guid userId, CancellationToken ct = default);
    Task RecordInvoiceCancellationAsync(Invoice invoice, decimal originalTotal, Guid userId, CancellationToken ct = default);
    Task RecordCreditInvoiceAsync(Invoice creditInvoice, Invoice originalInvoice, Guid userId, CancellationToken ct = default);
    Task RecordCreditAppliedAsync(Invoice creditInvoice, Invoice targetInvoice, Payment payment, Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<StudentTransactionDto>> GetTransactionsAsync(Guid studentId, CancellationToken ct = default);
    Task<decimal> GetStudentBalanceAsync(Guid studentId, CancellationToken ct = default);
}
