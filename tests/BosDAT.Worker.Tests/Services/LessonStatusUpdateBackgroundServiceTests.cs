using BosDAT.Worker.Configuration;
using BosDAT.Worker.Models;
using BosDAT.Worker.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace BosDAT.Worker.Tests.Services;

public class LessonStatusUpdateBackgroundServiceTests
{
    private readonly Mock<IBosApiClient> _mockApiClient;
    private readonly Mock<ILogger<LessonStatusUpdateBackgroundService>> _mockLogger;
    private readonly IServiceProvider _serviceProvider;

    public LessonStatusUpdateBackgroundServiceTests()
    {
        _mockApiClient = new Mock<IBosApiClient>();
        _mockLogger = new Mock<ILogger<LessonStatusUpdateBackgroundService>>();

        var services = new ServiceCollection();
        services.AddScoped(_ => _mockApiClient.Object);
        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public async Task ExecuteAsync_WhenDisabled_DoesNotRunJob()
    {
        // Arrange
        var settings = CreateSettings(enabled: false);
        var service = CreateService(settings);
        var cts = new CancellationTokenSource();

        // Act
        var task = service.StartAsync(cts.Token);
        await Task.Delay(100);
        cts.Cancel();
        await task;

        // Assert
        _mockApiClient.Verify(
            x => x.GetLessonsAsync(
                It.IsAny<DateOnly?>(),
                It.IsAny<DateOnly?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WhenEnabled_LogsStartup()
    {
        // Arrange
        var settings = CreateSettings(enabled: true);
        var service = CreateService(settings);
        var cts = new CancellationTokenSource();

        // Act
        var task = service.StartAsync(cts.Token);
        await Task.Delay(50);
        cts.Cancel();
        await task;

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Lesson status update background service started")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LessonStatusUpdate_UpdatesYesterdaysLessons()
    {
        // Arrange
        var today = DateOnly.FromDateTime(DateTime.Today);
        var expectedEndDate = today.AddDays(-1);

        // Act
        var yesterday = today.AddDays(-1);

        // Assert
        Assert.Equal(expectedEndDate, yesterday);
    }

    [Fact]
    public void ShouldRunJob_AtMidnight_ReturnsTrue()
    {
        // Arrange
        var settings = CreateSettings(enabled: true, executionTime: new TimeOnly(0, 0));
        var currentTime = new TimeOnly(0, 1);

        // Act
        var shouldRun = ShouldRunJobInternal(settings.LessonStatusUpdateJob.ExecutionTime, false, currentTime);

        // Assert
        Assert.True(shouldRun);
    }

    [Fact]
    public void ShouldRunJob_BeforeMidnight_ReturnsFalse()
    {
        // Arrange
        var settings = CreateSettings(enabled: true, executionTime: new TimeOnly(0, 0));
        var currentTime = new TimeOnly(23, 59);

        // Act
        var shouldRun = ShouldRunJobInternal(settings.LessonStatusUpdateJob.ExecutionTime, false, currentTime);

        // Assert
        Assert.False(shouldRun);
    }

    [Fact]
    public void ShouldRunJob_WhenAlreadyRanToday_ReturnsFalse()
    {
        // Arrange
        var settings = CreateSettings(enabled: true, executionTime: new TimeOnly(0, 0));
        var currentTime = new TimeOnly(1, 0);

        // Act
        var shouldRun = ShouldRunJobInternal(settings.LessonStatusUpdateJob.ExecutionTime, hasRunToday: true, currentTime);

        // Assert
        Assert.False(shouldRun);
    }

    private static bool ShouldRunJobInternal(TimeOnly executionTime, bool hasRunToday, TimeOnly currentTime)
    {
        if (hasRunToday)
            return false;

        return currentTime >= executionTime;
    }

    private LessonStatusUpdateBackgroundService CreateService(WorkerSettings settings)
    {
        var options = Options.Create(settings);
        return new LessonStatusUpdateBackgroundService(_serviceProvider, options, _mockLogger.Object);
    }

    private static WorkerSettings CreateSettings(
        bool enabled = true,
        TimeOnly? executionTime = null)
    {
        return new WorkerSettings
        {
            Api = new ApiSettings { BaseUrl = "http://localhost:5000", TimeoutSeconds = 30, RetryCount = 3 },
            Credentials = new WorkerCredentials { Email = "worker@test.com", Password = "test123" },
            InvoiceJob = new InvoiceJobSettings
            {
                Enabled = false,
                DayOfMonth = 1,
                ExecutionTime = new TimeOnly(8, 0)
            },
            LessonGenerationJob = new LessonGenerationJobSettings
            {
                Enabled = false,
                DaysAhead = 90,
                ExecutionTime = new TimeOnly(2, 0),
                SkipHolidays = true
            },
            LessonStatusUpdateJob = new LessonStatusUpdateJobSettings
            {
                Enabled = enabled,
                ExecutionTime = executionTime ?? new TimeOnly(0, 0)
            }
        };
    }
}
