namespace BosDAT.Core.Entities;

public class CourseTask : BaseEntity
{
    public Guid CourseId { get; set; }
    public string Title { get; set; } = string.Empty;

    // Navigation properties
    public virtual Course Course { get; set; } = null!;
}
