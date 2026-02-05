using System.Net.Http.Json;
using System.Web;
using BosDAT.Worker.Models;

namespace BosDAT.Worker.Services;

public class BosApiClient(
    HttpClient httpClient,
    ILogger<BosApiClient> logger) : IBosApiClient
{
    public async Task<BulkGenerateLessonsResult?> GenerateLessonsBulkAsync(
        DateOnly startDate,
        DateOnly endDate,
        bool skipHolidays,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Calling API to generate lessons in bulk from {StartDate} to {EndDate}, skipHolidays: {SkipHolidays}",
            startDate, endDate, skipHolidays);

        var request = new BulkGenerateLessonsRequest
        {
            StartDate = startDate.ToString("yyyy-MM-dd"),
            EndDate = endDate.ToString("yyyy-MM-dd"),
            SkipHolidays = skipHolidays
        };

        var response = await httpClient.PostAsJsonAsync("api/lessons/generate-bulk", request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogError("Failed to generate lessons: {StatusCode} - {Error}", response.StatusCode, error);
            return null;
        }

        var result = await response.Content.ReadFromJsonAsync<BulkGenerateLessonsResult>(cancellationToken);
        logger.LogInformation(
            "Bulk lesson generation completed: {CoursesProcessed} courses, {LessonsCreated} lessons created, {LessonsSkipped} skipped",
            result?.TotalCoursesProcessed, result?.TotalLessonsCreated, result?.TotalLessonsSkipped);

        return result;
    }

    public async Task<List<LessonDto>> GetLessonsAsync(
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        var queryParams = HttpUtility.ParseQueryString(string.Empty);

        if (startDate.HasValue)
            queryParams["startDate"] = startDate.Value.ToString("yyyy-MM-dd");
        if (endDate.HasValue)
            queryParams["endDate"] = endDate.Value.ToString("yyyy-MM-dd");
        if (!string.IsNullOrEmpty(status))
            queryParams["status"] = status;

        var url = $"api/lessons?{queryParams}";
        logger.LogDebug("Fetching lessons from {Url}", url);

        var response = await httpClient.GetAsync(url, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogError("Failed to fetch lessons: {StatusCode} - {Error}", response.StatusCode, error);
            return [];
        }

        var lessons = await response.Content.ReadFromJsonAsync<List<LessonDto>>(cancellationToken);
        logger.LogDebug("Retrieved {Count} lessons", lessons?.Count ?? 0);

        return lessons ?? [];
    }

    public async Task<LessonDto?> UpdateLessonStatusAsync(
        Guid lessonId,
        string status,
        string? cancellationReason = null,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Updating lesson {LessonId} status to {Status}", lessonId, status);

        var request = new UpdateLessonStatusRequest
        {
            Status = status,
            CancellationReason = cancellationReason
        };

        var response = await httpClient.PutAsJsonAsync($"api/lessons/{lessonId}/status", request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogError("Failed to update lesson {LessonId} status: {StatusCode} - {Error}", lessonId, response.StatusCode, error);
            return null;
        }

        return await response.Content.ReadFromJsonAsync<LessonDto>(cancellationToken);
    }

    public async Task<InvoiceRunResult?> TriggerInvoiceRunAsync(
        int month,
        int year,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Triggering invoice run for {Month}/{Year}", month, year);

        var request = new InvoiceRunRequest
        {
            Month = month,
            Year = year
        };

        var response = await httpClient.PostAsJsonAsync("api/invoices/generate", request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogError("Failed to trigger invoice run: {StatusCode} - {Error}", response.StatusCode, error);
            return null;
        }

        var result = await response.Content.ReadFromJsonAsync<InvoiceRunResult>(cancellationToken);
        logger.LogInformation(
            "Invoice run completed: {InvoicesGenerated} invoices generated, total amount: {TotalAmount}",
            result?.InvoicesGenerated, result?.TotalAmount);

        return result;
    }
}
