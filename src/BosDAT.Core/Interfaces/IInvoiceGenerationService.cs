using BosDAT.Core.DTOs;

namespace BosDAT.Core.Interfaces;

public interface IInvoiceGenerationService
{
    Task<InvoiceDto> GenerateInvoiceAsync(GenerateInvoiceDto dto, Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<InvoiceDto>> GenerateBatchInvoicesAsync(GenerateBatchInvoicesDto dto, Guid userId, CancellationToken ct = default);
    Task<InvoiceDto> RecalculateInvoiceAsync(Guid invoiceId, Guid userId, CancellationToken ct = default);
}
