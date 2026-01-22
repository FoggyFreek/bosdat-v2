namespace BosDAT.Core.Entities;

public class TeacherLessonType
{
    public Guid TeacherId { get; set; }
    public int LessonTypeId { get; set; }

    // Navigation properties
    public virtual Teacher Teacher { get; set; } = null!;
    public virtual LessonType LessonType { get; set; } = null!;
}
