namespace BosDAT.Core.Entities;

public enum LessonTypeCategory
{
    Individual,
    Group,
    Workshop
}

public class LessonType
{
    public int Id { get; set; }
    public int InstrumentId { get; set; }
    public required string Name { get; set; }
    public int DurationMinutes { get; set; } = 30;
    public LessonTypeCategory Type { get; set; } = LessonTypeCategory.Individual;
    public decimal PriceAdult { get; set; }
    public decimal PriceChild { get; set; }
    public int MaxStudents { get; set; } = 1;
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual Instrument Instrument { get; set; } = null!;
    public virtual ICollection<Course> Courses { get; set; } = new List<Course>();
    public virtual ICollection<TeacherLessonType> TeacherLessonTypes { get; set; } = new List<TeacherLessonType>();
}
