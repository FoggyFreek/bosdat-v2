namespace BosDAT.Core.Entities;

public enum CourseTypeCategory
{
    Individual,
    Group,
    Workshop
}

public class CourseType : BaseEntity
{
    public int InstrumentId { get; set; }
    public required string Name { get; set; }
    public int DurationMinutes { get; set; } = 30;
    public CourseTypeCategory Type { get; set; } = CourseTypeCategory.Individual;
    public decimal PriceAdult { get; set; }
    public decimal PriceChild { get; set; }
    public int MaxStudents { get; set; } = 1;
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual Instrument Instrument { get; set; } = null!;
    public virtual ICollection<Course> Courses { get; set; } = new List<Course>();
    public virtual ICollection<TeacherCourseType> TeacherCourseTypes { get; set; } = new List<TeacherCourseType>();
}
