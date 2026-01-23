namespace BosDAT.Core.Entities;

public enum EnrollmentStatus
{
    Trail,
    Active,
    Withdrawn,
    Completed,
    Suspended
}

public class Enrollment : BaseEntity
{
    public Guid StudentId { get; set; }
    public Guid CourseId { get; set; }

    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
    public decimal DiscountPercent { get; set; }
    public EnrollmentStatus Status { get; set; } = EnrollmentStatus.Active;

    public string? Notes { get; set; }

    // Navigation properties
    public virtual Student Student { get; set; } = null!;
    public virtual Course Course { get; set; } = null!;
}
