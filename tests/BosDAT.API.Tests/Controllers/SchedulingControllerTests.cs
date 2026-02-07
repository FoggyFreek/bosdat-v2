using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using BosDAT.API.Controllers;
using BosDAT.Core.Entities;
using BosDAT.Core.Enums;
using BosDAT.Core.Interfaces;
using BosDAT.API.Tests.Helpers;

namespace BosDAT.API.Tests.Controllers;

public class SchedulingControllerTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ILessonGenerationService> _mockLessonGenerationService;
    private readonly SchedulingController _controller;

    public SchedulingControllerTests()
    {
        _mockUnitOfWork = MockHelpers.CreateMockUnitOfWork();
        _mockLessonGenerationService = new Mock<ILessonGenerationService>();
        _controller = new SchedulingController(
            _mockUnitOfWork.Object,
            _mockLessonGenerationService.Object);
    }

    #region GetStatus Tests

    [Fact]
    public async Task GetStatus_WithLessons_ReturnsCorrectStatus()
    {
        // Arrange
        var futureDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(60));
        var lessons = new List<Lesson>
        {
            new Lesson
            {
                Id = Guid.NewGuid(),
                CourseId = Guid.NewGuid(),
                TeacherId = Guid.NewGuid(),
                ScheduledDate = futureDate,
                StartTime = new TimeOnly(10, 0),
                EndTime = new TimeOnly(10, 30),
                Status = LessonStatus.Scheduled
            }
        };

        var courses = new List<Course>
        {
            new Course
            {
                Id = Guid.NewGuid(),
                TeacherId = Guid.NewGuid(),
                CourseTypeId = Guid.NewGuid(),
                DayOfWeek = DayOfWeek.Monday,
                StartTime = new TimeOnly(10, 0),
                EndTime = new TimeOnly(10, 30),
                Frequency = CourseFrequency.Weekly,
                WeekParity = WeekParity.All,
                Status = CourseStatus.Active
            },
            new Course
            {
                Id = Guid.NewGuid(),
                TeacherId = Guid.NewGuid(),
                CourseTypeId = Guid.NewGuid(),
                DayOfWeek = DayOfWeek.Tuesday,
                StartTime = new TimeOnly(11, 0),
                EndTime = new TimeOnly(11, 30),
                Frequency = CourseFrequency.Weekly,
                WeekParity = WeekParity.All,
                Status = CourseStatus.Paused
            }
        };

        var mockLessonRepo = new Mock<ILessonRepository>();
        mockLessonRepo.Setup(r => r.Query())
            .Returns(lessons.AsQueryable().BuildMockDbSet().Object);

        var mockCourseRepo = new Mock<ICourseRepository>();
        mockCourseRepo.Setup(r => r.Query())
            .Returns(courses.AsQueryable().BuildMockDbSet().Object);

        _mockUnitOfWork.Setup(u => u.Lessons).Returns(mockLessonRepo.Object);
        _mockUnitOfWork.Setup(u => u.Courses).Returns(mockCourseRepo.Object);

        // Act
        var result = await _controller.GetStatus(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var status = Assert.IsType<SchedulingStatusDto>(okResult.Value);

        Assert.Equal(futureDate, status.LastScheduledDate);
        Assert.True(status.DaysAhead > 0);
        Assert.Equal(1, status.ActiveCourseCount); // Only 1 Active course
    }

    [Fact]
    public async Task GetStatus_NoLessons_ReturnsZeroDaysAhead()
    {
        // Arrange
        var mockLessonRepo = new Mock<ILessonRepository>();
        mockLessonRepo.Setup(r => r.Query())
            .Returns(new List<Lesson>().AsQueryable().BuildMockDbSet().Object);

        var mockCourseRepo = new Mock<ICourseRepository>();
        mockCourseRepo.Setup(r => r.Query())
            .Returns(new List<Course>().AsQueryable().BuildMockDbSet().Object);

        _mockUnitOfWork.Setup(u => u.Lessons).Returns(mockLessonRepo.Object);
        _mockUnitOfWork.Setup(u => u.Courses).Returns(mockCourseRepo.Object);

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
        var runs = new List<ScheduleRun>();
        for (var i = 0; i < 10; i++)
        {
            runs.Add(new ScheduleRun
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

        var mockScheduleRunRepo = MockHelpers.CreateMockRepository(runs);
        _mockUnitOfWork.Setup(u => u.Repository<ScheduleRun>()).Returns(mockScheduleRunRepo.Object);

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
        var mockScheduleRunRepo = MockHelpers.CreateMockRepository(new List<ScheduleRun>());
        _mockUnitOfWork.Setup(u => u.Repository<ScheduleRun>()).Returns(mockScheduleRunRepo.Object);

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
        var bulkResult = new BulkLessonGenerationResult(
            DateOnly.FromDateTime(DateTime.UtcNow),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(90)),
            TotalCoursesProcessed: 5,
            TotalLessonsCreated: 42,
            TotalLessonsSkipped: 3,
            CourseResults: new List<LessonGenerationResult>());

        _mockLessonGenerationService
            .Setup(s => s.GenerateBulkAsync(
                It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bulkResult);

        var mockScheduleRunRepo = MockHelpers.CreateMockRepository(new List<ScheduleRun>());
        _mockUnitOfWork.Setup(u => u.Repository<ScheduleRun>()).Returns(mockScheduleRunRepo.Object);

        // Act
        var result = await _controller.RunManual(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var manualResult = Assert.IsType<ManualRunResultDto>(okResult.Value);

        Assert.Equal(5, manualResult.TotalCoursesProcessed);
        Assert.Equal(42, manualResult.TotalLessonsCreated);
        Assert.Equal(3, manualResult.TotalLessonsSkipped);
        Assert.Equal(ScheduleRunStatus.Success, manualResult.Status);

        // Verify a ScheduleRun was persisted
        mockScheduleRunRepo.Verify(r => r.AddAsync(
            It.Is<ScheduleRun>(sr => sr.InitiatedBy == "Manual" && sr.Status == ScheduleRunStatus.Success),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RunManual_GenerationFails_PersistsFailedRun()
    {
        // Arrange
        _mockLessonGenerationService
            .Setup(s => s.GenerateBulkAsync(
                It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), true, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        var mockScheduleRunRepo = MockHelpers.CreateMockRepository(new List<ScheduleRun>());
        _mockUnitOfWork.Setup(u => u.Repository<ScheduleRun>()).Returns(mockScheduleRunRepo.Object);

        // Act
        var result = await _controller.RunManual(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var manualResult = Assert.IsType<ManualRunResultDto>(okResult.Value);

        Assert.Equal(ScheduleRunStatus.Failed, manualResult.Status);
        Assert.Equal(0, manualResult.TotalLessonsCreated);

        // Verify a failed ScheduleRun was persisted
        mockScheduleRunRepo.Verify(r => r.AddAsync(
            It.Is<ScheduleRun>(sr => sr.Status == ScheduleRunStatus.Failed && sr.ErrorMessage == "Database error"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}
