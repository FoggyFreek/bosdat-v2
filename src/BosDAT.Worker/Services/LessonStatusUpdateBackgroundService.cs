using BosDAT.Worker.Configuration;
using Microsoft.Extensions.Options;

namespace BosDAT.Worker.Services;

public class LessonStatusUpdateBackgroundService(
    IServiceProvider serviceProvider,
    IOptions<WorkerSettings> settings,
    ILogger<LessonStatusUpdateBackgroundService> logger) : BackgroundService
{
    private readonly WorkerSettings _settings = settings.Value;
    private bool _hasRunToday;
    private DateOnly _lastRunDate = DateOnly.MinValue;

    private const string ScheduledStatus = "Scheduled";
    private const string CompletedStatus = "Completed";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.LessonStatusUpdateJob.Enabled)
        {
            logger.LogInformation("Lesson status update background service is disabled");
            return;
        }

        logger.LogInformation(
            "Lesson status update background service started. Configured to run at {Time}",
            _settings.LessonStatusUpdateJob.ExecutionTime);

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
                    await RunLessonStatusUpdateJobAsync(today, stoppingToken);
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
                logger.LogError(ex, "Error in lesson status update background service");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        logger.LogInformation("Lesson status update background service stopped");
    }

    private bool ShouldRunJob(TimeOnly currentTime)
    {
        if (_hasRunToday)
            return false;

        var elapsed = currentTime.ToTimeSpan() - _settings.LessonStatusUpdateJob.ExecutionTime.ToTimeSpan();

        return elapsed >= TimeSpan.Zero && elapsed < TimeSpan.FromHours(23);
    }

    private async Task RunLessonStatusUpdateJobAsync(DateOnly today, CancellationToken stoppingToken)
    {
        var yesterday = today.AddDays(-1);

        logger.LogInformation(
            "Starting lesson status update for lessons on or before {Date}",
            yesterday);

        using var scope = serviceProvider.CreateScope();
        var apiClient = scope.ServiceProvider.GetRequiredService<IBosApiClient>();

        var scheduledLessons = await apiClient.GetLessonsAsync(
            startDate: null,
            endDate: yesterday,
            status: ScheduledStatus,
            stoppingToken);

        if (scheduledLessons.Count == 0)
        {
            logger.LogInformation("No scheduled lessons found that need status update");
            return;
        }

        logger.LogInformation("Found {Count} scheduled lessons to mark as completed", scheduledLessons.Count);

        var successCount = 0;
        var failCount = 0;

        foreach (var lesson in scheduledLessons)
        {
            if (stoppingToken.IsCancellationRequested)
                break;

            var result = await apiClient.UpdateLessonStatusAsync(
                lesson.Id,
                CompletedStatus,
                cancellationToken: stoppingToken);

            if (result != null)
            {
                successCount++;
                logger.LogDebug("Updated lesson {LessonId} status to {Status}", lesson.Id, CompletedStatus);
            }
            else
            {
                failCount++;
                logger.LogWarning("Failed to update lesson {LessonId} status", lesson.Id);
            }
        }

        logger.LogInformation(
            "Lesson status update completed: {SuccessCount} lessons updated, {FailCount} failed",
            successCount,
            failCount);
    }
}
