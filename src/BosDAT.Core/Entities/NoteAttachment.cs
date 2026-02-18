namespace BosDAT.Core.Entities;

public class NoteAttachment : BaseEntity
{
    public Guid NoteId { get; set; }
    public string FileName { get; set; } = string.Empty;       // original name
    public string StoredFileName { get; set; } = string.Empty; // unique stored name
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }

    // Navigation properties
    public virtual LessonNote Note { get; set; } = null!;
}
