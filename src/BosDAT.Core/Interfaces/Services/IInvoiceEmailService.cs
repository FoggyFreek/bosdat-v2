using BosDAT.Core.DTOs;

namespace BosDAT.Core.Interfaces.Services;

public interface IInvoiceEmailService
{
    Task<InvoiceEmailPreviewDto> PreviewAsync(Guid invoiceId, CancellationToken ct = default);
    Task<InvoiceDto> SendAsync(Guid invoiceId, CancellationToken ct = default);
}
