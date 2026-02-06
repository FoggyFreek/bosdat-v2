using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Enums;
using BosDAT.Core.Interfaces;

namespace BosDAT.Infrastructure.Services;

public class InvoiceService(
    IInvoiceGenerationService generation,
    IInvoiceLedgerService ledger,
    IInvoiceQueryService query) : IInvoiceService
{

    public Task<InvoiceDto> GenerateInvoiceAsync(GenerateInvoiceDto dto, Guid userId, CancellationToken ct = default)
        => generation.GenerateInvoiceAsync(dto, userId, ct);

    public Task<IReadOnlyList<InvoiceDto>> GenerateBatchInvoicesAsync(GenerateBatchInvoicesDto dto, Guid userId, CancellationToken ct = default)
        => generation.GenerateBatchInvoicesAsync(dto, userId, ct);

    public Task<InvoiceDto> RecalculateInvoiceAsync(Guid invoiceId, Guid userId, CancellationToken ct = default)
        => generation.RecalculateInvoiceAsync(invoiceId, userId, ct);

    public Task<InvoiceDto?> GetInvoiceAsync(Guid invoiceId, CancellationToken ct = default)
        => query.GetInvoiceAsync(invoiceId, ct);

    public Task<InvoiceDto?> GetByInvoiceNumberAsync(string invoiceNumber, CancellationToken ct = default)
        => query.GetByInvoiceNumberAsync(invoiceNumber, ct);

    public Task<IReadOnlyList<InvoiceListDto>> GetStudentInvoicesAsync(Guid studentId, CancellationToken ct = default)
        => query.GetStudentInvoicesAsync(studentId, ct);

    public Task<IReadOnlyList<InvoiceListDto>> GetByStatusAsync(InvoiceStatus status, CancellationToken ct = default)
        => query.GetByStatusAsync(status, ct);

    public Task<InvoiceDto> ApplyLedgerCorrectionAsync(Guid invoiceId, Guid ledgerEntryId, decimal amount, Guid userId, CancellationToken ct = default)
        => ledger.ApplyLedgerCorrectionAsync(invoiceId, ledgerEntryId, amount, userId, ct);

    public string GeneratePeriodDescription(DateOnly periodStart, DateOnly periodEnd, InvoicingPreference periodType)
        => query.GeneratePeriodDescription(periodStart, periodEnd, periodType);

    public Task<SchoolBillingInfoDto> GetSchoolBillingInfoAsync(CancellationToken ct = default)
        => query.GetSchoolBillingInfoAsync(ct);
}
