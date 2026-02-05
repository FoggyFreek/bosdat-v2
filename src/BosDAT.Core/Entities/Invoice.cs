using BosDAT.Core.Enums;

namespace BosDAT.Core.Entities;

public enum InvoiceStatus
{
    Draft,
    Sent,
    Paid,
    Overdue,
    Cancelled
}

public class Invoice : BaseEntity
{
    public required string InvoiceNumber { get; set; }
    public Guid StudentId { get; set; }
    public Guid? EnrollmentId { get; set; }

    public DateOnly IssueDate { get; set; }
    public DateOnly DueDate { get; set; }

    /// <summary>
    /// Invoice period description (e.g., "jan26" for monthly, "jan-mar26" for quarterly)
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Start of the billing period
    /// </summary>
    public DateOnly? PeriodStart { get; set; }

    /// <summary>
    /// End of the billing period
    /// </summary>
    public DateOnly? PeriodEnd { get; set; }

    /// <summary>
    /// Type of billing period (Monthly/Quarterly)
    /// </summary>
    public InvoicingPreference? PeriodType { get; set; }

    public decimal Subtotal { get; set; }
    public decimal VatAmount { get; set; }
    public decimal Total { get; set; }
    public decimal DiscountAmount { get; set; }

    /// <summary>
    /// Amount already paid through ledger credit applications
    /// </summary>
    public decimal LedgerCreditsApplied { get; set; }

    /// <summary>
    /// Amount added from outstanding ledger debits
    /// </summary>
    public decimal LedgerDebitsApplied { get; set; }

    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
    public DateTime? PaidAt { get; set; }
    public string? PaymentMethod { get; set; }

    public string? Notes { get; set; }

    // Navigation properties
    public virtual Student Student { get; set; } = null!;
    public virtual Enrollment? Enrollment { get; set; }
    public virtual ICollection<InvoiceLine> Lines { get; set; } = new List<InvoiceLine>();
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public virtual ICollection<StudentLedgerApplication> LedgerApplications { get; set; } = new List<StudentLedgerApplication>();
}
