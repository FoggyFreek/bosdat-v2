using BosDAT.Core.Enums;

namespace BosDAT.Core.Entities;

public class StudentTransaction : BaseEntity
{
    public required Guid StudentId { get; set; }
    public required DateOnly TransactionDate { get; set; }
    public required TransactionType Type { get; set; }
    public required string Description { get; set; }
    public required string ReferenceNumber { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public Guid? InvoiceId { get; set; }
    public Guid? PaymentId { get; set; }
    public Guid? LedgerEntryId { get; set; }
    public required Guid CreatedById { get; set; }

    // Navigation properties
    public virtual Student Student { get; set; } = null!;
    public virtual Invoice? Invoice { get; set; }
    public virtual Payment? Payment { get; set; }
    public virtual ApplicationUser CreatedBy { get; set; } = null!;
}
