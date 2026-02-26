using BosDAT.Core.DTOs;

namespace BosDAT.Core.Interfaces;

public interface ICreditInvoiceService
{
    Task<InvoiceDto> CreateCreditInvoiceAsync(Guid originalInvoiceId, CreateCreditInvoiceDto dto, Guid userId, CancellationToken ct = default);
    Task<InvoiceDto> ConfirmCreditInvoiceAsync(Guid creditInvoiceId, Guid userId, CancellationToken ct = default);
    Task<InvoiceDto> ApplyCreditInvoicesAsync(Guid invoiceId, Guid userId, CancellationToken ct = default);
    Task<decimal> GetAvailableCreditAsync(Guid studentId, CancellationToken ct = default);
}
