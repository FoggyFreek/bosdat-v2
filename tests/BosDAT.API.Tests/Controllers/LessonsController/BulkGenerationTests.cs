using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using BosDAT.API.Controllers;
using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Enums;
using BosDAT.Core.Interfaces;
using BosDAT.API.Tests.Helpers;

namespace BosDAT.API.Tests.Controllers.LessonsController;

/// <summary>
/// Tests for bulk lesson generation functionality.
/// Bulk generation processes multiple active courses in a single operation,
/// aggregating results across all courses while respecting individual course settings.
/// </summary>
public class BulkGenerationTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly BosDAT.API.Controllers.LessonsController _controller;
    private readonly List<Lesson> _createdLessons;

    public BulkGenerationTests()
    {
        _mockUnitOfWork = MockHelpers.CreateMockUnitOfWork();
        _controller = new BosDAT.API.Controllers.LessonsController(_mockUnitOfWork.Object);
        _createdLessons = new List<Lesson>();
    }

    [Fact]
    public async Task BulkGenerateLessons_OnlyProcessesActiveCourses()
    {
        // Arrange
        var teacherId = Guid.NewGuid();
        var instrument = CreateInstrument();
        var courseType = CreateCourseType(instrument, CourseTypeCategory.Group);

        var courses = new List<Course>
        {
            CreateCourseWithStatus(CourseStatus.Active, DayOfWeek.Monday, courseType, teacherId),
            CreateCourseWithStatus(CourseStatus.Active, DayOfWeek.Wednesday, courseType, teacherId),
            CreateCourseWithStatus(CourseStatus.Paused, DayOfWeek.Friday, courseType, teacherId),
            CreateCourseWithStatus(CourseStatus.Completed, DayOfWeek.Saturday, courseType, teacherId)
        };

        SetupBulkGenerationMocks(courses, new List<Lesson>(), new List<Holiday>());

        var dto = CreateBulkDto(
            new DateOnly(2024, 3, 4),
            new DateOnly(2024, 3, 17),  // 2 weeks
            skipHolidays: true);

        // Act
        var result = await _controller.GenerateLessonsBulk(dto, CancellationToken.None);

        // Assert
        var bulkResult = AssertBulkGenerationResult(result);

        // Only Active courses (2 of 4)
        Assert.Equal(2, bulkResult.TotalCoursesProcessed);
        // 2 active courses × 2 weeks = 4 lessons exactly (for weekly courses)
        Assert.Equal(4, bulkResult.TotalLessonsCreated);
    }

    [Fact]
    public async Task BulkGenerateLessons_AggregatesResultsCorrectly()
    {
        // Arrange
        var teacherId = Guid.NewGuid();
        var instrument = CreateInstrument();
        var courseType = CreateCourseType(instrument, CourseTypeCategory.Group);

        var courses = new List<Course>
        {
            CreateCourseWithStatus(CourseStatus.Active, DayOfWeek.Monday, courseType, teacherId),
            CreateCourseWithStatus(CourseStatus.Active, DayOfWeek.Tuesday, courseType, teacherId),
            CreateCourseWithStatus(CourseStatus.Active, DayOfWeek.Wednesday, courseType, teacherId)
        };

        var holidays = new List<Holiday>
        {
            new Holiday
            {
                Id = 1,
                Name = "Test Holiday",
                StartDate = new DateOnly(2024, 3, 11),  // Monday
                EndDate = new DateOnly(2024, 3, 11)
            }
        };

        SetupBulkGenerationMocks(courses, new List<Lesson>(), holidays);

        var dto = CreateBulkDto(
            new DateOnly(2024, 3, 4),
            new DateOnly(2024, 3, 17),
            skipHolidays: true);

        // Act
        var result = await _controller.GenerateLessonsBulk(dto, CancellationToken.None);

        // Assert
        var bulkResult = AssertBulkGenerationResult(result);

        Assert.Equal(3, bulkResult.TotalCoursesProcessed);
        Assert.Equal(3, bulkResult.CourseResults.Count);

        // Monday course: 1 created (March 4), 1 skipped (March 11 holiday)
        // Tuesday course: 2 created (March 5, 12)
        // Wednesday course: 2 created (March 6, 13)
        Assert.Equal(5, bulkResult.TotalLessonsCreated);
        Assert.Equal(1, bulkResult.TotalLessonsSkipped);
    }

    [Fact]
    public async Task BulkGenerateLessons_NoActiveCourses_ReturnsEmptyResult()
    {
        // Arrange
        var teacherId = Guid.NewGuid();
        var instrument = CreateInstrument();
        var courseType = CreateCourseType(instrument, CourseTypeCategory.Group);

        var courses = new List<Course>
        {
            CreateCourseWithStatus(CourseStatus.Paused, DayOfWeek.Monday, courseType, teacherId),
            CreateCourseWithStatus(CourseStatus.Completed, DayOfWeek.Tuesday, courseType, teacherId)
        };

        SetupBulkGenerationMocks(courses, new List<Lesson>(), new List<Holiday>());

        var dto = CreateBulkDto(
            new DateOnly(2024, 3, 4),
            new DateOnly(2024, 3, 17),
            skipHolidays: true);

        // Act
        var result = await _controller.GenerateLessonsBulk(dto, CancellationToken.None);

        // Assert
        var bulkResult = AssertBulkGenerationResult(result);

        Assert.Equal(0, bulkResult.TotalCoursesProcessed);
        Assert.Equal(0, bulkResult.TotalLessonsCreated);
        Assert.Empty(bulkResult.CourseResults);
    }

    #region Helper Methods

    /// <summary>
    /// Creates a test instrument for course generation.
    /// </summary>
    private static Instrument CreateInstrument()
    {
        return new Instrument
        {
            Id = 1,
            Name = "Piano",
            Category = InstrumentCategory.Keyboard
        };
    }

    /// <summary>
    /// Creates a test course type with the specified category.
    /// </summary>
    private static CourseType CreateCourseType(Instrument instrument, CourseTypeCategory category)
    {
        return new CourseType
        {
            Id = Guid.NewGuid(),
            Name = $"Test {category} Course",
            InstrumentId = instrument.Id,
            Instrument = instrument,
            Type = category,
            DurationMinutes = 30,
            MaxStudents = category == CourseTypeCategory.Individual ? 1 : 10
        };
    }

    /// <summary>
    /// Creates a test course with the specified status and day of week.
    /// </summary>
    private static Course CreateCourseWithStatus(
        CourseStatus status,
        DayOfWeek dayOfWeek,
        CourseType courseType,
        Guid teacherId)
    {
        return new Course
        {
            Id = Guid.NewGuid(),
            TeacherId = teacherId,
            CourseTypeId = courseType.Id,
            RoomId = 1,
            DayOfWeek = dayOfWeek,
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(10, 30),
            Frequency = CourseFrequency.Weekly,
            WeekParity = WeekParity.All,
            Status = status,
            CourseType = courseType,
            Enrollments = new List<Enrollment>()
        };
    }

    /// <summary>
    /// Creates a bulk generation DTO with the specified parameters.
    /// </summary>
    private static BulkGenerateLessonsDto CreateBulkDto(
        DateOnly startDate,
        DateOnly endDate,
        bool skipHolidays)
    {
        return new BulkGenerateLessonsDto
        {
            StartDate = startDate,
            EndDate = endDate,
            SkipHolidays = skipHolidays
        };
    }

    /// <summary>
    /// Asserts that the result is a valid bulk generation result and returns it.
    /// </summary>
    private static BulkGenerateLessonsResultDto AssertBulkGenerationResult(
        ActionResult<BulkGenerateLessonsResultDto> result)
    {
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        return Assert.IsType<BulkGenerateLessonsResultDto>(okResult.Value);
    }

    /// <summary>
    /// Sets up mocks for bulk generation testing with the specified courses, lessons, and holidays.
    /// </summary>
    private void SetupBulkGenerationMocks(
        List<Course> courses,
        List<Lesson> existingLessons,
        List<Holiday> holidays)
    {
        var mockCourseRepo = new Mock<ICourseRepository>();
        mockCourseRepo.Setup(r => r.Query())
            .Returns(courses.AsQueryable().BuildMockDbSet().Object);

        var mockLessonRepo = new Mock<ILessonRepository>();
        mockLessonRepo.Setup(r => r.Query())
            .Returns(existingLessons.AsQueryable().BuildMockDbSet().Object);
        mockLessonRepo.Setup(r => r.AddAsync(It.IsAny<Lesson>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Lesson l, CancellationToken _) =>
            {
                _createdLessons.Add(l);
                return l;
            });

        var mockHolidayRepo = MockHelpers.CreateMockRepository(holidays);

        _mockUnitOfWork.Setup(u => u.Courses).Returns(mockCourseRepo.Object);
        _mockUnitOfWork.Setup(u => u.Lessons).Returns(mockLessonRepo.Object);
        _mockUnitOfWork.Setup(u => u.Repository<Holiday>()).Returns(mockHolidayRepo.Object);
    }

    #endregion
}
