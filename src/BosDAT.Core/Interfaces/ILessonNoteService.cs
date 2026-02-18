using BosDAT.Core.DTOs;

namespace BosDAT.Core.Interfaces;

public interface ILessonNoteService
{
    Task<IEnumerable<LessonNoteDto>> GetByCourseAsync(Guid lessonId, CancellationToken ct = default);
    Task<LessonNoteDto?> CreateAsync(Guid lessonId, CreateLessonNoteDto dto, CancellationToken ct = default);
    Task<LessonNoteDto?> UpdateAsync(Guid noteId, UpdateLessonNoteDto dto, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid noteId, CancellationToken ct = default);
    Task<NoteAttachmentDto?> AddAttachmentAsync(Guid noteId, Stream fileStream, string fileName, string contentType, long fileSize, CancellationToken ct = default);
    Task<bool> DeleteAttachmentAsync(Guid attachmentId, CancellationToken ct = default);
}
