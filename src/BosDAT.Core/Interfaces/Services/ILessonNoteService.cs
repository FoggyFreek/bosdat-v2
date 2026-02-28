using BosDAT.Core.Common;
using BosDAT.Core.DTOs;

namespace BosDAT.Core.Interfaces.Services;

public interface ILessonNoteService
{
    Task<Result<IEnumerable<LessonNoteDto>>> GetByLessonCourseAsync(Guid lessonId, CancellationToken ct = default);
    Task<Result<LessonNoteDto>> CreateAsync(Guid lessonId, CreateLessonNoteDto dto, CancellationToken ct = default);
    Task<Result<LessonNoteDto>> UpdateAsync(Guid noteId, UpdateLessonNoteDto dto, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(Guid noteId, CancellationToken ct = default);
    Task<Result<NoteAttachmentDto>> AddAttachmentAsync(Guid noteId, Stream fileStream, string fileName, string contentType, long fileSize, CancellationToken ct = default);
    Task<Result<bool>> DeleteAttachmentAsync(Guid attachmentId, CancellationToken ct = default);
}
