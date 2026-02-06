using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Enums;
using BosDAT.Core.Interfaces;
using BosDAT.Infrastructure.Data;

namespace BosDAT.Infrastructure.Services;

public class InvoiceService : IInvoiceService
{
    private readonly IInvoiceGenerationService _generation;
    private readonly IInvoiceLedgerService _ledger;
    private readonly IInvoiceQueryService _query;

    // DI constructor
    public InvoiceService(
        IInvoiceGenerationService generation,
        IInvoiceLedgerService ledger,
        IInvoiceQueryService query)
    {
        _generation = generation;
        _ledger = ledger;
        _query = query;
    }

    // Backwards-compatible constructor for tests
    public InvoiceService(
        ApplicationDbContext context,
        IUnitOfWork unitOfWork,
        ICourseTypePricingService pricingService)
    {
        _query = new InvoiceQueryService(context);
        _ledger = new InvoiceLedgerService(context, unitOfWork, _query);
        _generation = new InvoiceGenerationService(context, unitOfWork, pricingService, _ledger, _query);
    }

    public Task<InvoiceDto> GenerateInvoiceAsync(GenerateInvoiceDto dto, Guid userId, CancellationToken ct = default)
        => _generation.GenerateInvoiceAsync(dto, userId, ct);

    public Task<IReadOnlyList<InvoiceDto>> GenerateBatchInvoicesAsync(GenerateBatchInvoicesDto dto, Guid userId, CancellationToken ct = default)
        => _generation.GenerateBatchInvoicesAsync(dto, userId, ct);

    public Task<InvoiceDto> RecalculateInvoiceAsync(Guid invoiceId, Guid userId, CancellationToken ct = default)
        => _generation.RecalculateInvoiceAsync(invoiceId, userId, ct);

    public Task<InvoiceDto?> GetInvoiceAsync(Guid invoiceId, CancellationToken ct = default)
        => _query.GetInvoiceAsync(invoiceId, ct);

    public Task<InvoiceDto?> GetByInvoiceNumberAsync(string invoiceNumber, CancellationToken ct = default)
        => _query.GetByInvoiceNumberAsync(invoiceNumber, ct);

    public Task<IReadOnlyList<InvoiceListDto>> GetStudentInvoicesAsync(Guid studentId, CancellationToken ct = default)
        => _query.GetStudentInvoicesAsync(studentId, ct);

    public Task<IReadOnlyList<InvoiceListDto>> GetByStatusAsync(InvoiceStatus status, CancellationToken ct = default)
        => _query.GetByStatusAsync(status, ct);

    public Task<InvoiceDto> ApplyLedgerCorrectionAsync(Guid invoiceId, Guid ledgerEntryId, decimal amount, Guid userId, CancellationToken ct = default)
        => _ledger.ApplyLedgerCorrectionAsync(invoiceId, ledgerEntryId, amount, userId, ct);

    public string GeneratePeriodDescription(DateOnly periodStart, DateOnly periodEnd, InvoicingPreference periodType)
        => _query.GeneratePeriodDescription(periodStart, periodEnd, periodType);

    public Task<SchoolBillingInfoDto> GetSchoolBillingInfoAsync(CancellationToken ct = default)
        => _query.GetSchoolBillingInfoAsync(ct);
}
