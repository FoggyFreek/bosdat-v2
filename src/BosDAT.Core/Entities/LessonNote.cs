namespace BosDAT.Core.Entities;

public class LessonNote : BaseEntity
{
    public Guid LessonId { get; set; }
    public string Content { get; set; } = string.Empty; // Lexical JSON

    // Navigation properties
    public virtual Lesson Lesson { get; set; } = null!;
    public virtual ICollection<NoteAttachment> Attachments { get; set; } = [];
}
