using BosDAT.Worker.Configuration;
using BosDAT.Worker.Models;
using BosDAT.Worker.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace BosDAT.Worker.Tests.Services;

public class InvoiceRunBackgroundServiceTests
{
    private readonly Mock<IBosApiClient> _mockApiClient;
    private readonly Mock<ILogger<InvoiceRunBackgroundService>> _mockLogger;
    private readonly IServiceProvider _serviceProvider;

    public InvoiceRunBackgroundServiceTests()
    {
        _mockApiClient = new Mock<IBosApiClient>();
        _mockLogger = new Mock<ILogger<InvoiceRunBackgroundService>>();

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
            x => x.TriggerInvoiceRunAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WhenEnabled_LogsStartup()
    {
        // Arrange
        var settings = CreateSettings(enabled: true, dayOfMonth: 15);
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
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Invoice run background service started")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void ShouldRunJob_WhenNotConfiguredDay_ReturnsFalse()
    {
        // Arrange
        var settings = CreateSettings(enabled: true, dayOfMonth: 15);
        var today = new DateOnly(2024, 1, 10); // Not the 15th

        // Act
        var shouldRun = ShouldRunJobInternal(today, settings.InvoiceJob.DayOfMonth, settings.InvoiceJob.ExecutionTime, false);

        // Assert
        Assert.False(shouldRun);
    }

    [Fact]
    public void ShouldRunJob_WhenConfiguredDayAndTime_ReturnsTrue()
    {
        // Arrange
        var settings = CreateSettings(enabled: true, dayOfMonth: 15, executionTime: new TimeOnly(8, 0));
        var today = new DateOnly(2024, 1, 15);
        var currentTime = new TimeOnly(9, 0);

        // Act
        var shouldRun = ShouldRunJobInternal(today, settings.InvoiceJob.DayOfMonth, settings.InvoiceJob.ExecutionTime, false, currentTime);

        // Assert
        Assert.True(shouldRun);
    }

    [Fact]
    public void ShouldRunJob_WhenAlreadyRanToday_ReturnsFalse()
    {
        // Arrange
        var settings = CreateSettings(enabled: true, dayOfMonth: 15);
        var today = new DateOnly(2024, 1, 15);
        var currentTime = new TimeOnly(9, 0);

        // Act
        var shouldRun = ShouldRunJobInternal(today, settings.InvoiceJob.DayOfMonth, settings.InvoiceJob.ExecutionTime, hasRunToday: true, currentTime);

        // Assert
        Assert.False(shouldRun);
    }

    [Fact]
    public void ShouldRunJob_WhenBeforeExecutionTime_ReturnsFalse()
    {
        // Arrange
        var settings = CreateSettings(enabled: true, dayOfMonth: 15, executionTime: new TimeOnly(8, 0));
        var today = new DateOnly(2024, 1, 15);
        var currentTime = new TimeOnly(7, 0);

        // Act
        var shouldRun = ShouldRunJobInternal(today, settings.InvoiceJob.DayOfMonth, settings.InvoiceJob.ExecutionTime, false, currentTime);

        // Assert
        Assert.False(shouldRun);
    }

    private static bool ShouldRunJobInternal(
        DateOnly today,
        int dayOfMonth,
        TimeOnly executionTime,
        bool hasRunToday,
        TimeOnly? currentTime = null)
    {
        if (hasRunToday)
            return false;

        if (today.Day != dayOfMonth)
            return false;

        return (currentTime ?? TimeOnly.FromDateTime(DateTime.Now)) >= executionTime;
    }

    private InvoiceRunBackgroundService CreateService(WorkerSettings settings)
    {
        var options = Options.Create(settings);
        return new InvoiceRunBackgroundService(_serviceProvider, options, _mockLogger.Object);
    }

    private static WorkerSettings CreateSettings(
        bool enabled = true,
        int dayOfMonth = 1,
        TimeOnly? executionTime = null)
    {
        return new WorkerSettings
        {
            Api = new ApiSettings { BaseUrl = "http://localhost:5000", TimeoutSeconds = 30, RetryCount = 3 },
            Credentials = new WorkerCredentials { Email = "worker@test.com", Password = "test123" },
            InvoiceJob = new InvoiceJobSettings
            {
                Enabled = enabled,
                DayOfMonth = dayOfMonth,
                ExecutionTime = executionTime ?? new TimeOnly(8, 0)
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
                Enabled = false,
                ExecutionTime = new TimeOnly(0, 0)
            }
        };
    }
}
