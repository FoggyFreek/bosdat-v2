using BosDAT.Core.DTOs;

namespace BosDAT.Core.Interfaces.Services;

public interface IInvoicePdfService
{
    Task<byte[]> GeneratePdfAsync(InvoiceDto invoice, SchoolBillingInfoDto schoolInfo, CancellationToken ct = default);
}
