using BosDAT.Core.DTOs;
using BosDAT.Core.Enums;

namespace BosDAT.Core.Interfaces;

public interface IInvoiceService
{
    /// <summary>
    /// Generates an invoice for lessons in the specified enrollment during the given period.
    /// </summary>
    Task<InvoiceDto> GenerateInvoiceAsync(GenerateInvoiceDto dto, Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Generates invoices for all active enrollments matching the specified period type.
    /// </summary>
    Task<IReadOnlyList<InvoiceDto>> GenerateBatchInvoicesAsync(GenerateBatchInvoicesDto dto, Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Recalculates an unpaid invoice based on current lessons and ledger corrections.
    /// </summary>
    Task<InvoiceDto> RecalculateInvoiceAsync(Guid invoiceId, Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Gets an invoice by ID with all related data.
    /// </summary>
    Task<InvoiceDto?> GetInvoiceAsync(Guid invoiceId, CancellationToken ct = default);

    /// <summary>
    /// Gets an invoice by invoice number.
    /// </summary>
    Task<InvoiceDto?> GetByInvoiceNumberAsync(string invoiceNumber, CancellationToken ct = default);

    /// <summary>
    /// Gets all invoices for a student.
    /// </summary>
    Task<IReadOnlyList<InvoiceListDto>> GetStudentInvoicesAsync(Guid studentId, CancellationToken ct = default);

    /// <summary>
    /// Gets all invoices with the specified status.
    /// </summary>
    Task<IReadOnlyList<InvoiceListDto>> GetByStatusAsync(Entities.InvoiceStatus status, CancellationToken ct = default);

    /// <summary>
    /// Applies a ledger correction to an unpaid invoice.
    /// </summary>
    Task<InvoiceDto> ApplyLedgerCorrectionAsync(Guid invoiceId, Guid ledgerEntryId, decimal amount, Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Generates the period description based on the period type.
    /// Monthly: "jan26", Quarterly: "jan-mar26"
    /// </summary>
    string GeneratePeriodDescription(DateOnly periodStart, DateOnly periodEnd, InvoicingPreference periodType);

    /// <summary>
    /// Gets school billing information from settings.
    /// </summary>
    Task<SchoolBillingInfoDto> GetSchoolBillingInfoAsync(CancellationToken ct = default);

    /// <summary
    /// Records a payment against an invoice and creates a ledger transaction.
    /// </summary>
    Task<PaymentDto> RecordPaymentandLedgerTransaction(Guid invoiceId, Guid userId, RecordPaymentDto dto, CancellationToken ct = default);

    /// <summary>
    /// Gets all payments for an invoice.
    /// </summary>
    Task<IEnumerable<PaymentDto>> GetPaymentsAsync(Guid invoiceId, CancellationToken ct = default);
}

public record SchoolBillingInfoDto
{
    public string Name { get; init; } = string.Empty;
    public string? Address { get; init; }
    public string? PostalCode { get; init; }
    public string? City { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? KvkNumber { get; init; }
    public string? Iban { get; init; }
    public decimal VatRate { get; init; }
}
