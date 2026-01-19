namespace BosDAT.Core.Entities;

public enum CancellationStatus
{
    Pending,
    Approved,
    Rejected
}

public class Cancellation : BaseEntity
{
    public Guid StudentId { get; set; }

    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string? Reason { get; set; }

    public CancellationStatus Status { get; set; } = CancellationStatus.Pending;

    // Navigation properties
    public virtual Student Student { get; set; } = null!;
}
