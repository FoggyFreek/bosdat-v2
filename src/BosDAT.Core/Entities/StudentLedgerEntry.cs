namespace BosDAT.Core.Entities;

public enum LedgerEntryType
{
    Credit,
    Debit
}

public enum LedgerEntryStatus
{
    Open,
    PartiallyApplied,
    FullyApplied
}

public class StudentLedgerEntry : BaseEntity
{
    public required string CorrectionRefName { get; set; }
    public required string Description { get; set; }
    public required Guid StudentId { get; set; }
    public Guid? CourseId { get; set; }
    public required decimal Amount { get; set; }
    public required LedgerEntryType EntryType { get; set; }
    public LedgerEntryStatus Status { get; set; } = LedgerEntryStatus.Open;
    public required Guid CreatedById { get; set; }

    // Navigation properties
    public virtual Student Student { get; set; } = null!;
    public virtual Course? Course { get; set; }
    public virtual ApplicationUser CreatedBy { get; set; } = null!;
    public virtual ICollection<StudentLedgerApplication> Applications { get; set; } = new List<StudentLedgerApplication>();
}
