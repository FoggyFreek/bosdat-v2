using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using BosDAT.API.Controllers;
using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Enums;
using BosDAT.Core.Interfaces;
using BosDAT.API.Tests.Helpers;

namespace BosDAT.API.Tests.Controllers;

public class SchedulingControllerTests
{
    private readonly Mock<ISchedulingService> _mockSchedulingService;
    private readonly SchedulingController _controller;

    public SchedulingControllerTests()
    {
        _mockSchedulingService = new Mock<ISchedulingService>();
        _controller = new SchedulingController(_mockSchedulingService.Object);
    }

    #region GetStatus Tests

    [Fact]
    public async Task GetStatus_WithLessons_ReturnsCorrectStatus()
    {
        // Arrange
        var futureDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(60));
        var expectedStatus = new SchedulingStatusDto
        {
            LastScheduledDate = futureDate,
            DaysAhead = 60,
            ActiveCourseCount = 1
        };

        _mockSchedulingService
            .Setup(s => s.GetSchedulingStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedStatus);

        // Act
        var result = await _controller.GetStatus(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var status = Assert.IsType<SchedulingStatusDto>(okResult.Value);

        Assert.Equal(futureDate, status.LastScheduledDate);
        Assert.True(status.DaysAhead > 0);
        Assert.Equal(1, status.ActiveCourseCount);
    }

    [Fact]
    public async Task GetStatus_NoLessons_ReturnsZeroDaysAhead()
    {
        // Arrange
        var expectedStatus = new SchedulingStatusDto
        {
            LastScheduledDate = null,
            DaysAhead = 0,
            ActiveCourseCount = 0
        };

        _mockSchedulingService
            .Setup(s => s.GetSchedulingStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedStatus);

        // Act
        var result = await _controller.GetStatus(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var status = Assert.IsType<SchedulingStatusDto>(okResult.Value);

        Assert.Null(status.LastScheduledDate);
        Assert.Equal(0, status.DaysAhead);
        Assert.Equal(0, status.ActiveCourseCount);
    }

    #endregion

    #region GetRuns Tests

    [Fact]
    public async Task GetRuns_ReturnsPaginatedResults()
    {
        // Arrange
        var items = new List<ScheduleRunDto>();
        for (var i = 0; i < 5; i++)
        {
            items.Add(new ScheduleRunDto
            {
                Id = Guid.NewGuid(),
                StartDate = new DateOnly(2024, 3, 1),
                EndDate = new DateOnly(2024, 5, 31),
                TotalCoursesProcessed = 5,
                TotalLessonsCreated = 20,
                TotalLessonsSkipped = 2,
                SkipHolidays = true,
                Status = ScheduleRunStatus.Success,
                InitiatedBy = "Manual",
                CreatedAt = DateTime.UtcNow.AddHours(-i)
            });
        }

        var expectedPage = new ScheduleRunsPageDto
        {
            Items = items,
            TotalCount = 10,
            Page = 1,
            PageSize = 5
        };

        _mockSchedulingService
            .Setup(s => s.GetScheduleRunsAsync(1, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPage);

        // Act
        var result = await _controller.GetRuns(page: 1, pageSize: 5);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var page = Assert.IsType<ScheduleRunsPageDto>(okResult.Value);

        Assert.Equal(10, page.TotalCount);
        Assert.Equal(5, page.Items.Count);
        Assert.Equal(1, page.Page);
        Assert.Equal(5, page.PageSize);
    }

    [Fact]
    public async Task GetRuns_EmptyResults_ReturnsEmptyPage()
    {
        // Arrange
        var expectedPage = new ScheduleRunsPageDto
        {
            Items = new List<ScheduleRunDto>(),
            TotalCount = 0,
            Page = 1,
            PageSize = 5
        };

        _mockSchedulingService
            .Setup(s => s.GetScheduleRunsAsync(1, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPage);

        // Act
        var result = await _controller.GetRuns();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var page = Assert.IsType<ScheduleRunsPageDto>(okResult.Value);

        Assert.Equal(0, page.TotalCount);
        Assert.Empty(page.Items);
    }

    #endregion

    #region RunManual Tests

    [Fact]
    public async Task RunManual_Success_ReturnsResultAndPersistsRun()
    {
        // Arrange
        var expectedResult = new ManualRunResultDto
        {
            ScheduleRunId = Guid.NewGuid(),
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(90)),
            TotalCoursesProcessed = 5,
            TotalLessonsCreated = 42,
            TotalLessonsSkipped = 3,
            Status = ScheduleRunStatus.Success
        };

        _mockSchedulingService
            .Setup(s => s.ExecuteManualRunAsync(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.RunManual(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var manualResult = Assert.IsType<ManualRunResultDto>(okResult.Value);

        Assert.Equal(5, manualResult.TotalCoursesProcessed);
        Assert.Equal(42, manualResult.TotalLessonsCreated);
        Assert.Equal(3, manualResult.TotalLessonsSkipped);
        Assert.Equal(ScheduleRunStatus.Success, manualResult.Status);

        _mockSchedulingService.Verify(s => s.ExecuteManualRunAsync(
            It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RunManual_GenerationFails_PersistsFailedRun()
    {
        // Arrange
        var expectedResult = new ManualRunResultDto
        {
            ScheduleRunId = Guid.NewGuid(),
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(90)),
            TotalCoursesProcessed = 0,
            TotalLessonsCreated = 0,
            TotalLessonsSkipped = 0,
            Status = ScheduleRunStatus.Failed
        };

        _mockSchedulingService
            .Setup(s => s.ExecuteManualRunAsync(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.RunManual(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var manualResult = Assert.IsType<ManualRunResultDto>(okResult.Value);

        Assert.Equal(ScheduleRunStatus.Failed, manualResult.Status);
        Assert.Equal(0, manualResult.TotalLessonsCreated);

        _mockSchedulingService.Verify(s => s.ExecuteManualRunAsync(
            It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region RunSingle Tests

    [Fact]
    public async Task RunSingle_Success_ReturnsResultAndPersistsRun()
    {
        // Arrange
        var courseId = Guid.NewGuid();
        var expectedResult = new ManualRunResultDto
        {
            ScheduleRunId = Guid.NewGuid(),
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(90)),
            TotalCoursesProcessed = 1,
            TotalLessonsCreated = 10,
            TotalLessonsSkipped = 1,
            Status = ScheduleRunStatus.Success
        };

        _mockSchedulingService
            .Setup(s => s.ExecuteSingleCourseRunAsync(courseId, It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.RunSingle(courseId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var manualResult = Assert.IsType<ManualRunResultDto>(okResult.Value);

        Assert.Equal(1, manualResult.TotalCoursesProcessed);
        Assert.Equal(10, manualResult.TotalLessonsCreated);
        Assert.Equal(1, manualResult.TotalLessonsSkipped);
        Assert.Equal(ScheduleRunStatus.Success, manualResult.Status);

        _mockSchedulingService.Verify(s => s.ExecuteSingleCourseRunAsync(
            courseId, It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RunSingle_GenerationFails_PersistsFailedRun()
    {
        // Arrange
        var courseId = Guid.NewGuid();
        var expectedResult = new ManualRunResultDto
        {
            ScheduleRunId = Guid.NewGuid(),
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(90)),
            TotalCoursesProcessed = 1,
            TotalLessonsCreated = 0,
            TotalLessonsSkipped = 0,
            Status = ScheduleRunStatus.Failed
        };

        _mockSchedulingService
            .Setup(s => s.ExecuteSingleCourseRunAsync(courseId, It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.RunSingle(courseId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var manualResult = Assert.IsType<ManualRunResultDto>(okResult.Value);

        Assert.Equal(ScheduleRunStatus.Failed, manualResult.Status);
        Assert.Equal(0, manualResult.TotalLessonsCreated);

        _mockSchedulingService.Verify(s => s.ExecuteSingleCourseRunAsync(
            courseId, It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}
