namespace BosDAT.Core.Entities;

public class TeacherAvailability : BaseEntity
{
    public Guid TeacherId { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly FromTime { get; set; }
    public TimeOnly UntilTime { get; set; }

    // Navigation property
    public virtual Teacher Teacher { get; set; } = null!;
}
