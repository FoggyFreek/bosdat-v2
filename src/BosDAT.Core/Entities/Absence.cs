using BosDAT.Core.Enums;

namespace BosDAT.Core.Entities;

public class Absence : BaseEntity
{
    public Guid? StudentId { get; set; }
    public Guid? TeacherId { get; set; }

    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }

    public AbsenceReason Reason { get; set; }
    public string? Notes { get; set; }

    /// <summary>
    /// Whether affected lessons should still be invoiced. Default: false (don't invoice).
    /// </summary>
    public bool InvoiceLesson { get; set; }

    // Navigation properties
    public virtual Student? Student { get; set; }
    public virtual Teacher? Teacher { get; set; }
}
