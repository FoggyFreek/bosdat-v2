namespace BosDAT.Core.Entities;

public class TeacherPayment : BaseEntity
{
    public Guid TeacherId { get; set; }

    public int PeriodMonth { get; set; }
    public int PeriodYear { get; set; }

    public int LessonCount { get; set; }
    public int TotalMinutes { get; set; }
    public decimal HourlyRate { get; set; }
    public decimal GrossAmount { get; set; }

    public bool IsPaid { get; set; }
    public DateTime? PaidAt { get; set; }

    public string? Notes { get; set; }

    // Navigation properties
    public virtual Teacher Teacher { get; set; } = null!;
}
