using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;

namespace BosDAT.Core.Interfaces;

public interface IInvoiceLedgerService
{
    Task<InvoiceDto> ApplyLedgerCorrectionAsync(Guid invoiceId, Guid ledgerEntryId, decimal amount, Guid userId, CancellationToken ct = default);
    Task ApplyLedgerCorrectionsToInvoiceAsync(Invoice invoice, Guid studentId, Guid userId, CancellationToken ct = default);
    Task RevertLedgerApplicationsAsync(Invoice invoice, CancellationToken ct = default);
}
