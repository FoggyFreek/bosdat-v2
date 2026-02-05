using BosDAT.Worker.Configuration;
using Microsoft.Extensions.Options;

namespace BosDAT.Worker.Services;

public class LessonGenerationBackgroundService(
    IServiceProvider serviceProvider,
    IOptions<WorkerSettings> settings,
    ILogger<LessonGenerationBackgroundService> logger) : BackgroundService
{
    private readonly WorkerSettings _settings = settings.Value;
    private bool _hasRunToday;
    private DateOnly _lastRunDate = DateOnly.MinValue;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.LessonGenerationJob.Enabled)
        {
            logger.LogInformation("Lesson generation background service is disabled");
            return;
        }

        logger.LogInformation(
            "Lesson generation background service started. Configured to generate {DaysAhead} days ahead at {Time}",
            _settings.LessonGenerationJob.DaysAhead,
            _settings.LessonGenerationJob.ExecutionTime);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.Now;
                var today = DateOnly.FromDateTime(now);
                var currentTime = TimeOnly.FromDateTime(now);

                if (today != _lastRunDate)
                {
                    _hasRunToday = false;
                    _lastRunDate = today;
                }

                if (ShouldRunJob(currentTime))
                {
                    await RunLessonGenerationJobAsync(today, stoppingToken);
                    _hasRunToday = true;
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in lesson generation background service");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        logger.LogInformation("Lesson generation background service stopped");
    }

    private bool ShouldRunJob(TimeOnly currentTime)
    {
        if (_hasRunToday)
            return false;

        return currentTime >= _settings.LessonGenerationJob.ExecutionTime;
    }

    private async Task RunLessonGenerationJobAsync(DateOnly today, CancellationToken stoppingToken)
    {
        var startDate = today;
        var endDate = today.AddDays(_settings.LessonGenerationJob.DaysAhead);

        logger.LogInformation(
            "Starting lesson generation from {StartDate} to {EndDate} ({DaysAhead} days ahead)",
            startDate,
            endDate,
            _settings.LessonGenerationJob.DaysAhead);

        using var scope = serviceProvider.CreateScope();
        var apiClient = scope.ServiceProvider.GetRequiredService<IBosApiClient>();

        var result = await apiClient.GenerateLessonsBulkAsync(
            startDate,
            endDate,
            _settings.LessonGenerationJob.SkipHolidays,
            stoppingToken);

        if (result != null)
        {
            logger.LogInformation(
                "Lesson generation completed successfully: {CoursesProcessed} courses processed, {LessonsCreated} lessons created, {LessonsSkipped} skipped",
                result.TotalCoursesProcessed,
                result.TotalLessonsCreated,
                result.TotalLessonsSkipped);
        }
        else
        {
            logger.LogWarning("Lesson generation returned no result");
        }
    }
}
