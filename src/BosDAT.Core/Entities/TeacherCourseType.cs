namespace BosDAT.Core.Entities;

public class TeacherCourseType
{
    public Guid TeacherId { get; set; }
    public Guid CourseTypeId { get; set; }

    // Navigation properties
    public virtual Teacher Teacher { get; set; } = null!;
    public virtual CourseType CourseType { get; set; } = null!;
}
