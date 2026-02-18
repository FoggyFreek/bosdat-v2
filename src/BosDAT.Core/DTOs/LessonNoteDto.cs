namespace BosDAT.Core.DTOs;

public record NoteAttachmentDto
{
    public Guid Id { get; init; }
    public string FileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public long FileSize { get; init; }
    public string Url { get; init; } = string.Empty;
}

public record LessonNoteDto
{
    public Guid Id { get; init; }
    public Guid LessonId { get; init; }
    public string Content { get; init; } = string.Empty;
    public DateOnly LessonDate { get; init; }
    public IEnumerable<NoteAttachmentDto> Attachments { get; init; } = [];
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public record CreateLessonNoteDto
{
    public required string Content { get; init; }
}

public record UpdateLessonNoteDto
{
    public required string Content { get; init; }
}
