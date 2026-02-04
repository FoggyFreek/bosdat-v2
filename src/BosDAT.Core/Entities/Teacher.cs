namespace BosDAT.Core.Entities;

public enum TeacherRole
{
    Teacher,
    Admin,
    Staff
}

public class Teacher : BaseEntity
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public string? Prefix { get; set; }

    public required string Email { get; set; }
    public string? Phone { get; set; }

    public string? Address { get; set; }
    public string? PostalCode { get; set; }
    public string? City { get; set; }

    public decimal HourlyRate { get; set; }
    public bool IsActive { get; set; } = true;
    public TeacherRole Role { get; set; } = TeacherRole.Teacher;

    public string? Notes { get; set; }

    // Navigation properties
    public virtual ICollection<TeacherInstrument> TeacherInstruments { get; set; } = new List<TeacherInstrument>();
    public virtual ICollection<TeacherCourseType> TeacherCourseTypes { get; set; } = new List<TeacherCourseType>();
    public virtual ICollection<Course> Courses { get; set; } = new List<Course>();
    public virtual ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
    public virtual ICollection<TeacherPayment> Payments { get; set; } = new List<TeacherPayment>();
    public virtual ICollection<TeacherAvailability> Availability { get; set; } = new List<TeacherAvailability>();

    // Link to Identity user (optional)
    public Guid? UserId { get; set; }

    public string FullName => string.IsNullOrEmpty(Prefix)
        ? $"{FirstName} {LastName}"
        : $"{FirstName} {Prefix} {LastName}";
}
