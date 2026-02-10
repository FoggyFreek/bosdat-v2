using BosDAT.Core.Enums;

namespace BosDAT.Core.Entities;

public class InvoiceRun : BaseEntity
{
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public InvoicingPreference PeriodType { get; set; }
    public int TotalEnrollmentsProcessed { get; set; }
    public int TotalInvoicesGenerated { get; set; }
    public int TotalSkipped { get; set; }
    public int TotalFailed { get; set; }
    public decimal TotalAmount { get; set; }
    public long DurationMs { get; set; }
    public InvoiceRunStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
    public required string InitiatedBy { get; set; }
}
