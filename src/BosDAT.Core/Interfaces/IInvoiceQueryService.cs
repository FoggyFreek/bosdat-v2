using BosDAT.Core.DTOs;
using BosDAT.Core.Enums;

namespace BosDAT.Core.Interfaces;

public interface IInvoiceQueryService
{
    Task<InvoiceDto?> GetInvoiceAsync(Guid invoiceId, CancellationToken ct = default);
    Task<InvoiceDto?> GetByInvoiceNumberAsync(string invoiceNumber, CancellationToken ct = default);
    Task<IReadOnlyList<InvoiceListDto>> GetStudentInvoicesAsync(Guid studentId, CancellationToken ct = default);
    Task<IReadOnlyList<InvoiceListDto>> GetByStatusAsync(Entities.InvoiceStatus status, CancellationToken ct = default);
    Task<SchoolBillingInfoDto> GetSchoolBillingInfoAsync(CancellationToken ct = default);
    string GeneratePeriodDescription(DateOnly periodStart, DateOnly periodEnd, InvoicingPreference periodType);
    Task<decimal> GetVatRateAsync(CancellationToken ct = default);
    Task<int> GetPaymentDueDaysAsync(CancellationToken ct = default);
}
