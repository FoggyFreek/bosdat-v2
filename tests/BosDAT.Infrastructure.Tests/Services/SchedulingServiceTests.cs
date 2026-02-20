using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using BosDAT.Core.Entities;
using BosDAT.Core.Enums;
using BosDAT.Core.Interfaces;
using BosDAT.Infrastructure.Data;
using BosDAT.Infrastructure.Repositories;
using BosDAT.Infrastructure.Services;

namespace BosDAT.Infrastructure.Tests.Services;

public class SchedulingServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly UnitOfWork _unitOfWork;
    private readonly Mock<ILessonGenerationService> _mockGenerationService;
    private readonly SchedulingService _service;

    public SchedulingServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"SchedulingServiceTest_{Guid.NewGuid()}")
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new ApplicationDbContext(options);
        _unitOfWork = new UnitOfWork(_context);
        _mockGenerationService = new Mock<ILessonGenerationService>();
        _service = new SchedulingService(_unitOfWork, _mockGenerationService.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    #region GetSchedulingStatusAsync

    [Fact]
    public async Task GetSchedulingStatusAsync_WithNoLessonsOrCourses_ReturnsZeroDefaults()
    {
        var result = await _service.GetSchedulingStatusAsync();

        Assert.Null(result.LastScheduledDate);
        Assert.Equal(0, result.DaysAhead);
        Assert.Equal(0, result.ActiveCourseCount);
    }

    [Fact]
    public async Task GetSchedulingStatusAsync_WithActiveCourses_ReturnsCorrectCount()
    {
        _context.Courses.AddRange(
            CreateCourse(CourseStatus.Active),
            CreateCourse(CourseStatus.Active),
            CreateCourse(CourseStatus.Paused));
        await _context.SaveChangesAsync();

        var result = await _service.GetSchedulingStatusAsync();

        Assert.Equal(2, result.ActiveCourseCount);
    }

    [Fact]
    public async Task GetSchedulingStatusAsync_WithFutureLessons_ReturnsDaysAheadGreaterThanZero()
    {
        var futureDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30));
        _context.Lessons.Add(CreateLesson(futureDate));
        await _context.SaveChangesAsync();

        var result = await _service.GetSchedulingStatusAsync();

        Assert.Equal(futureDate, result.LastScheduledDate);
        Assert.True(result.DaysAhead > 0);
    }

    [Fact]
    public async Task GetSchedulingStatusAsync_WithPastLessonsOnly_ReturnsDaysAheadZero()
    {
        var pastDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10));
        _context.Lessons.Add(CreateLesson(pastDate));
        await _context.SaveChangesAsync();

        var result = await _service.GetSchedulingStatusAsync();

        Assert.Equal(pastDate, result.LastScheduledDate);
        Assert.Equal(0, result.DaysAhead);
    }

    [Fact]
    public async Task GetSchedulingStatusAsync_ReturnsLatestLessonDate()
    {
        var earlier = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10));
        var later = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(20));
        _context.Lessons.AddRange(CreateLesson(earlier), CreateLesson(later));
        await _context.SaveChangesAsync();

        var result = await _service.GetSchedulingStatusAsync();

        Assert.Equal(later, result.LastScheduledDate);
    }

    #endregion

    #region GetScheduleRunsAsync

    [Fact]
    public async Task GetScheduleRunsAsync_WithNoRuns_ReturnsEmptyPage()
    {
        var result = await _service.GetScheduleRunsAsync(1, 5);

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(1, result.Page);
        Assert.Equal(5, result.PageSize);
    }

    [Fact]
    public async Task GetScheduleRunsAsync_ReturnsPagedResults()
    {
        for (int i = 0; i < 7; i++)
        {
            _context.Set<ScheduleRun>().Add(CreateScheduleRun());
        }
        await _context.SaveChangesAsync();

        var result = await _service.GetScheduleRunsAsync(1, 5);

        Assert.Equal(5, result.Items.Count);
        Assert.Equal(7, result.TotalCount);
        Assert.Equal(1, result.Page);
    }

    [Fact]
    public async Task GetScheduleRunsAsync_Page2_ReturnsCorrectItems()
    {
        for (int i = 0; i < 7; i++)
        {
            _context.Set<ScheduleRun>().Add(CreateScheduleRun());
        }
        await _context.SaveChangesAsync();

        var result = await _service.GetScheduleRunsAsync(2, 5);

        Assert.Equal(2, result.Items.Count);
        Assert.Equal(7, result.TotalCount);
        Assert.Equal(2, result.Page);
    }

    [Theory]
    [InlineData(0, 5, 5)]   // page < 1 → clamp to 1
    [InlineData(-1, 5, 5)]
    [InlineData(1, 0, 5)]   // pageSize < 1 → clamp to 5
    [InlineData(1, 100, 5)] // pageSize > 50 → clamp to 5
    public async Task GetScheduleRunsAsync_ClampsInvalidPagingParameters(int page, int pageSize, int expectedPageSize)
    {
        var result = await _service.GetScheduleRunsAsync(page, pageSize);

        Assert.Equal(expectedPageSize, result.PageSize);
    }

    #endregion

    #region ExecuteManualRunAsync

    [Fact]
    public async Task ExecuteManualRunAsync_OnSuccess_PersistsSuccessfulScheduleRun()
    {
        var start = new DateOnly(2025, 1, 1);
        var end = new DateOnly(2025, 1, 31);
        var generationResult = new BulkLessonGenerationResult(start, end, 3, 10, 2, []);

        _mockGenerationService
            .Setup(s => s.GenerateBulkAsync(start, end, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(generationResult);

        var result = await _service.ExecuteManualRunAsync(start, end);

        Assert.Equal(ScheduleRunStatus.Success, result.Status);
        Assert.Equal(3, result.TotalCoursesProcessed);
        Assert.Equal(10, result.TotalLessonsCreated);
        Assert.Equal(2, result.TotalLessonsSkipped);
        Assert.Equal(start, result.StartDate);
        Assert.Equal(end, result.EndDate);

        var saved = await _context.Set<ScheduleRun>().FindAsync(result.ScheduleRunId);
        Assert.NotNull(saved);
        Assert.Equal(ScheduleRunStatus.Success, saved.Status);
        Assert.Equal("Manual", saved.InitiatedBy);
    }

    [Fact]
    public async Task ExecuteManualRunAsync_OnGenerationFailure_PersistsFailedScheduleRun()
    {
        var start = new DateOnly(2025, 1, 1);
        var end = new DateOnly(2025, 1, 31);

        _mockGenerationService
            .Setup(s => s.GenerateBulkAsync(start, end, true, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Generation failed"));

        var result = await _service.ExecuteManualRunAsync(start, end);

        Assert.Equal(ScheduleRunStatus.Failed, result.Status);

        var saved = await _context.Set<ScheduleRun>().FindAsync(result.ScheduleRunId);
        Assert.NotNull(saved);
        Assert.Equal(ScheduleRunStatus.Failed, saved.Status);
        Assert.Equal("Generation failed", saved.ErrorMessage);
    }

    #endregion

    #region ExecuteSingleCourseRunAsync

    [Fact]
    public async Task ExecuteSingleCourseRunAsync_OnSuccess_PersistsSuccessfulScheduleRun()
    {
        var courseId = Guid.NewGuid();
        var start = new DateOnly(2025, 2, 1);
        var end = new DateOnly(2025, 2, 28);
        var generationResult = new LessonGenerationResult(courseId, start, end, 4, 1);

        _mockGenerationService
            .Setup(s => s.GenerateForCourseAsync(courseId, start, end, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(generationResult);

        var result = await _service.ExecuteSingleCourseRunAsync(courseId, start, end);

        Assert.Equal(ScheduleRunStatus.Success, result.Status);
        Assert.Equal(1, result.TotalCoursesProcessed);
        Assert.Equal(4, result.TotalLessonsCreated);
        Assert.Equal(1, result.TotalLessonsSkipped);

        var saved = await _context.Set<ScheduleRun>().FindAsync(result.ScheduleRunId);
        Assert.NotNull(saved);
        Assert.Equal("RunManualSingle", saved.InitiatedBy);
    }

    [Fact]
    public async Task ExecuteSingleCourseRunAsync_OnGenerationFailure_PersistsFailedScheduleRun()
    {
        var courseId = Guid.NewGuid();
        var start = new DateOnly(2025, 2, 1);
        var end = new DateOnly(2025, 2, 28);

        _mockGenerationService
            .Setup(s => s.GenerateForCourseAsync(courseId, start, end, true, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Course not found"));

        var result = await _service.ExecuteSingleCourseRunAsync(courseId, start, end);

        Assert.Equal(ScheduleRunStatus.Failed, result.Status);
        Assert.Equal(1, result.TotalCoursesProcessed);
        Assert.Equal(0, result.TotalLessonsCreated);

        var saved = await _context.Set<ScheduleRun>().FindAsync(result.ScheduleRunId);
        Assert.NotNull(saved);
        Assert.Equal("Course not found", saved.ErrorMessage);
    }

    #endregion

    #region Helpers

    private static Course CreateCourse(CourseStatus status = CourseStatus.Active) => new()
    {
        Id = Guid.NewGuid(),
        CourseTypeId = Guid.NewGuid(),
        TeacherId = Guid.NewGuid(),
        DayOfWeek = DayOfWeek.Monday,
        StartTime = new TimeOnly(10, 0),
        EndTime = new TimeOnly(10, 30),
        StartDate = new DateOnly(2025, 1, 1),
        Status = status,
        Frequency = CourseFrequency.Weekly
    };

    private static Lesson CreateLesson(DateOnly scheduledDate) => new()
    {
        Id = Guid.NewGuid(),
        CourseId = Guid.NewGuid(),
        TeacherId = Guid.NewGuid(),
        ScheduledDate = scheduledDate,
        StartTime = new TimeOnly(10, 0),
        EndTime = new TimeOnly(10, 30),
        Status = LessonStatus.Scheduled
    };

    private static ScheduleRun CreateScheduleRun() => new()
    {
        Id = Guid.NewGuid(),
        StartDate = new DateOnly(2025, 1, 1),
        EndDate = new DateOnly(2025, 1, 31),
        TotalCoursesProcessed = 1,
        TotalLessonsCreated = 4,
        TotalLessonsSkipped = 0,
        SkipHolidays = true,
        Status = ScheduleRunStatus.Success,
        InitiatedBy = "Manual"
    };

    #endregion
}
