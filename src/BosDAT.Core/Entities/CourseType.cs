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
    public int MaxStudents { get; set; } = 1;
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual Instrument Instrument { get; set; } = null!;
    public virtual ICollection<Course> Courses { get; set; } = new List<Course>();
    public virtual ICollection<TeacherCourseType> TeacherCourseTypes { get; set; } = new List<TeacherCourseType>();
    public virtual ICollection<CourseTypePricingVersion> PricingVersions { get; set; } = new List<CourseTypePricingVersion>();
}
