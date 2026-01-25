namespace BosDAT.Core.Entities;

public class StudentLedgerApplication : BaseEntity
{
    public required Guid LedgerEntryId { get; set; }
    public required Guid InvoiceId { get; set; }
    public required decimal AppliedAmount { get; set; }
    public required Guid AppliedById { get; set; }

    // Navigation properties
    public virtual StudentLedgerEntry LedgerEntry { get; set; } = null!;
    public virtual Invoice Invoice { get; set; } = null!;
    public virtual ApplicationUser AppliedBy { get; set; } = null!;
}
