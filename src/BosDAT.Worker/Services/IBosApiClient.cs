using BosDAT.Worker.Models;

namespace BosDAT.Worker.Services;

public interface IBosApiClient
{
    Task<BulkGenerateLessonsResult?> GenerateLessonsBulkAsync(
        DateOnly startDate,
        DateOnly endDate,
        bool skipHolidays,
        CancellationToken cancellationToken = default);

    Task<List<LessonDto>> GetLessonsAsync(
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        string? status = null,
        CancellationToken cancellationToken = default);

    Task<LessonDto?> UpdateLessonStatusAsync(
        Guid lessonId,
        string status,
        string? cancellationReason = null,
        CancellationToken cancellationToken = default);

    Task<InvoiceRunResult?> TriggerInvoiceRunAsync(
        int month,
        int year,
        CancellationToken cancellationToken = default);
}
