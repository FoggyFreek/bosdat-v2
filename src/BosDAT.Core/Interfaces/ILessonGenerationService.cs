using BosDAT.Core.Entities;

namespace BosDAT.Core.Interfaces;

public interface ILessonGenerationService
{
    Task<LessonGenerationResult> GenerateForCourseAsync(
        Guid courseId,
        DateOnly startDate,
        DateOnly endDate,
        bool skipHolidays,
        CancellationToken ct = default);

    Task<BulkLessonGenerationResult> GenerateBulkAsync(
        DateOnly startDate,
        DateOnly endDate,
        bool skipHolidays,
        CancellationToken ct = default);
}

public record LessonGenerationResult(
    Guid CourseId,
    DateOnly StartDate,
    DateOnly EndDate,
    int LessonsCreated,
    int LessonsSkipped);

public record BulkLessonGenerationResult(
    DateOnly StartDate,
    DateOnly EndDate,
    int TotalCoursesProcessed,
    int TotalLessonsCreated,
    int TotalLessonsSkipped,
    List<LessonGenerationResult> CourseResults);
