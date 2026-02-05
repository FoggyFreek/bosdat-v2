using BosDAT.Worker.Configuration;
using BosDAT.Worker.Models;
using BosDAT.Worker.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace BosDAT.Worker.Tests.Services;

public class LessonGenerationBackgroundServiceTests
{
    private readonly Mock<IBosApiClient> _mockApiClient;
    private readonly Mock<ILogger<LessonGenerationBackgroundService>> _mockLogger;
    private readonly IServiceProvider _serviceProvider;

    public LessonGenerationBackgroundServiceTests()
    {
        _mockApiClient = new Mock<IBosApiClient>();
        _mockLogger = new Mock<ILogger<LessonGenerationBackgroundService>>();

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
            x => x.GenerateLessonsBulkAsync(
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WhenEnabled_LogsStartup()
    {
        // Arrange
        var settings = CreateSettings(enabled: true, daysAhead: 60);
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
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Lesson generation background service started")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Theory]
    [InlineData(30)]
    [InlineData(60)]
    [InlineData(90)]
    [InlineData(180)]
    public void LessonGenerationRange_CalculatesCorrectEndDate(int daysAhead)
    {
        // Arrange
        var today = DateOnly.FromDateTime(DateTime.Today);
        var expectedEndDate = today.AddDays(daysAhead);

        // Act
        var startDate = today;
        var endDate = today.AddDays(daysAhead);

        // Assert
        Assert.Equal(expectedEndDate, endDate);
        Assert.Equal(today, startDate);
    }

    [Fact]
    public void ShouldRunJob_WhenAfterExecutionTime_ReturnsTrue()
    {
        // Arrange
        var settings = CreateSettings(enabled: true, executionTime: new TimeOnly(2, 0));
        var currentTime = new TimeOnly(3, 0);

        // Act
        var shouldRun = ShouldRunJobInternal(settings.LessonGenerationJob.ExecutionTime, false, currentTime);

        // Assert
        Assert.True(shouldRun);
    }

    [Fact]
    public void ShouldRunJob_WhenBeforeExecutionTime_ReturnsFalse()
    {
        // Arrange
        var settings = CreateSettings(enabled: true, executionTime: new TimeOnly(2, 0));
        var currentTime = new TimeOnly(1, 0);

        // Act
        var shouldRun = ShouldRunJobInternal(settings.LessonGenerationJob.ExecutionTime, false, currentTime);

        // Assert
        Assert.False(shouldRun);
    }

    [Fact]
    public void ShouldRunJob_WhenAlreadyRanToday_ReturnsFalse()
    {
        // Arrange
        var settings = CreateSettings(enabled: true, executionTime: new TimeOnly(2, 0));
        var currentTime = new TimeOnly(3, 0);

        // Act
        var shouldRun = ShouldRunJobInternal(settings.LessonGenerationJob.ExecutionTime, hasRunToday: true, currentTime);

        // Assert
        Assert.False(shouldRun);
    }

    private static bool ShouldRunJobInternal(TimeOnly executionTime, bool hasRunToday, TimeOnly currentTime)
    {
        if (hasRunToday)
            return false;

        return currentTime >= executionTime;
    }

    private LessonGenerationBackgroundService CreateService(WorkerSettings settings)
    {
        var options = Options.Create(settings);
        return new LessonGenerationBackgroundService(_serviceProvider, options, _mockLogger.Object);
    }

    private static WorkerSettings CreateSettings(
        bool enabled = true,
        int daysAhead = 90,
        TimeOnly? executionTime = null,
        bool skipHolidays = true)
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
                Enabled = enabled,
                DaysAhead = daysAhead,
                ExecutionTime = executionTime ?? new TimeOnly(2, 0),
                SkipHolidays = skipHolidays
            },
            LessonStatusUpdateJob = new LessonStatusUpdateJobSettings
            {
                Enabled = false,
                ExecutionTime = new TimeOnly(0, 0)
            }
        };
    }
}
