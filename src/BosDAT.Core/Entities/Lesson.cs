namespace BosDAT.Core.Entities;

public enum LessonStatus
{
    Scheduled,
    Completed,
    Cancelled,
    NoShow
}

public class Lesson : BaseEntity
{
    public Guid CourseId { get; set; }
    public Guid? StudentId { get; set; }
    public Guid TeacherId { get; set; }
    public int? RoomId { get; set; }

    public DateOnly ScheduledDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }

    public LessonStatus Status { get; set; } = LessonStatus.Scheduled;
    public string? CancellationReason { get; set; }

    public bool IsInvoiced { get; set; }
    public bool IsPaidToTeacher { get; set; }

    public string? Notes { get; set; }

    // Navigation properties
    public virtual Course Course { get; set; } = null!;
    public virtual Student? Student { get; set; }
    public virtual Teacher Teacher { get; set; } = null!;
    public virtual Room? Room { get; set; }
    public virtual ICollection<InvoiceLine> InvoiceLines { get; set; } = new List<InvoiceLine>();
}
