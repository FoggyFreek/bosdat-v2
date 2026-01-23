namespace BosDAT.Core.Entities;

public enum CourseStatus
{
    Active,
    Paused,
    Completed,
    Cancelled
}

public enum CourseFrequency
{
    Weekly,
    Biweekly,
    Monthly
}

public class Course : BaseEntity
{
    public Guid TeacherId { get; set; }
    public Guid CourseTypeId { get; set; }
    public int? RoomId { get; set; }

    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }

    public CourseFrequency Frequency { get; set; } = CourseFrequency.Weekly;
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }

    public CourseStatus Status { get; set; } = CourseStatus.Active;
    public bool IsWorkshop { get; set; }
    public bool IsTrial { get; set; }

    public string? Notes { get; set; }

    // Navigation properties
    public virtual Teacher Teacher { get; set; } = null!;
    public virtual CourseType CourseType { get; set; } = null!;
    public virtual Room? Room { get; set; }
    public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    public virtual ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
}
