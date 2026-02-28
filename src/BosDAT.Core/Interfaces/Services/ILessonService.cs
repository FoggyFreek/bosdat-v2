using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;

namespace BosDAT.Core.Interfaces.Services;

public interface ILessonService
{
    Task<List<LessonDto>> GetAllAsync(LessonFilterCriteria criteria, CancellationToken ct = default);

    Task<LessonDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<List<LessonDto>> GetByStudentAsync(Guid studentId, CancellationToken ct = default);

    Task<(LessonDto? Lesson, string? Error)> CreateAsync(CreateLessonDto dto, CancellationToken ct = default);

    Task<(LessonDto? Lesson, bool NotFound)> UpdateAsync(Guid id, UpdateLessonDto dto, CancellationToken ct = default);

    Task<(LessonDto? Lesson, bool NotFound)> UpdateStatusAsync(Guid id, LessonStatus status, string? cancellationReason, CancellationToken ct = default);

    Task<(int LessonsUpdated, bool NotFound)> UpdateGroupStatusAsync(
        Guid courseId, DateOnly scheduledDate, LessonStatus status, string? cancellationReason,
        CancellationToken ct = default);

    Task<(bool Success, string? Error)> DeleteAsync(Guid id, CancellationToken ct = default);
}
