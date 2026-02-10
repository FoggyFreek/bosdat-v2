using BosDAT.Core.Enums;

namespace BosDAT.Core.DTOs;

public record InvoiceRunDto
{
    public Guid Id { get; init; }
    public DateOnly PeriodStart { get; init; }
    public DateOnly PeriodEnd { get; init; }
    public InvoicingPreference PeriodType { get; init; }
    public int TotalEnrollmentsProcessed { get; init; }
    public int TotalInvoicesGenerated { get; init; }
    public int TotalSkipped { get; init; }
    public int TotalFailed { get; init; }
    public decimal TotalAmount { get; init; }
    public long DurationMs { get; init; }
    public InvoiceRunStatus Status { get; init; }
    public string? ErrorMessage { get; init; }
    public required string InitiatedBy { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record InvoiceRunsPageDto
{
    public List<InvoiceRunDto> Items { get; init; } = new();
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
}

public record StartInvoiceRunDto
{
    public DateOnly PeriodStart { get; init; }
    public DateOnly PeriodEnd { get; init; }
    public InvoicingPreference PeriodType { get; init; }
    public bool ApplyLedgerCorrections { get; init; } = true;
}

public record InvoiceRunResultDto
{
    public Guid InvoiceRunId { get; init; }
    public DateOnly PeriodStart { get; init; }
    public DateOnly PeriodEnd { get; init; }
    public InvoicingPreference PeriodType { get; init; }
    public int TotalEnrollmentsProcessed { get; init; }
    public int TotalInvoicesGenerated { get; init; }
    public int TotalSkipped { get; init; }
    public int TotalFailed { get; init; }
    public decimal TotalAmount { get; init; }
    public long DurationMs { get; init; }
    public InvoiceRunStatus Status { get; init; }
}
