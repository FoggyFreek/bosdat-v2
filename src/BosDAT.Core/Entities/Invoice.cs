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

    public DateOnly IssueDate { get; set; }
    public DateOnly DueDate { get; set; }

    public decimal Subtotal { get; set; }
    public decimal VatAmount { get; set; }
    public decimal Total { get; set; }
    public decimal DiscountAmount { get; set; }

    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
    public DateTime? PaidAt { get; set; }
    public string? PaymentMethod { get; set; }

    public string? Notes { get; set; }

    // Navigation properties
    public virtual Student Student { get; set; } = null!;
    public virtual ICollection<InvoiceLine> Lines { get; set; } = new List<InvoiceLine>();
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public virtual ICollection<StudentLedgerApplication> LedgerApplications { get; set; } = new List<StudentLedgerApplication>();
}
