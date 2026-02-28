using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using BosDAT.API.Controllers;
using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Enums;
using BosDAT.Core.Interfaces;
using BosDAT.Core.Interfaces.Services;
using BosDAT.Core.Interfaces.Repositories;
using BosDAT.Infrastructure.Services;
using BosDAT.API.Tests.Helpers;

namespace BosDAT.API.Tests.Controllers.LessonsController;

/// <summary>
/// Tests for lesson generation duplication detection and handling.
/// Verifies that the system correctly identifies and skips existing lessons
/// when generating new lessons, preventing duplicate lesson creation.
/// </summary>
public class DuplicationTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ILessonService> _mockLessonService;
    private readonly BosDAT.API.Controllers.LessonsController _controller;
    private readonly List<Lesson> _createdLessons;

    public DuplicationTests()
    {
        _mockUnitOfWork = MockHelpers.CreateMockUnitOfWork();
        _mockLessonService = new Mock<ILessonService>();
        var lessonGenerationService = new LessonGenerationService(_mockUnitOfWork.Object);
        _controller = new BosDAT.API.Controllers.LessonsController(_mockLessonService.Object, lessonGenerationService, _mockUnitOfWork.Object);
        _createdLessons = new List<Lesson>();
    }

    [Fact]
    public async Task GenerateLessons_ExistingLesson_SkipsDuplicate()
    {
        // Arrange
        var course = CreateTestCourse(CourseFrequency.Weekly, DayOfWeek.Monday);
        var existingLessons = new List<Lesson>
        {
            CreateLesson(course.Id, course.TeacherId, new DateOnly(2024, 3, 11))  // Second Monday
        };
        SetupMocksForCourse(course, existingLessons, new List<Holiday>());

        var dto = CreateGenerateDto(
            course.Id,
            new DateOnly(2024, 3, 4),
            new DateOnly(2024, 3, 25),
            skipHolidays: true);

        // Act
        var result = await _controller.GenerateLessons(dto, CancellationToken.None);

        // Assert
        AssertGenerationResult(result, lessonsCreated: 3, lessonsSkipped: 1);
    }

    [Fact]
    public async Task GenerateLessons_CalledTwice_SkipsAllExisting()
    {
        // Arrange
        var course = CreateTestCourse(CourseFrequency.Weekly, DayOfWeek.Monday);
        var existingLessons = new List<Lesson>
        {
            CreateLesson(course.Id, course.TeacherId, new DateOnly(2024, 3, 4)),
            CreateLesson(course.Id, course.TeacherId, new DateOnly(2024, 3, 11)),
            CreateLesson(course.Id, course.TeacherId, new DateOnly(2024, 3, 18)),
            CreateLesson(course.Id, course.TeacherId, new DateOnly(2024, 3, 25))
        };
        SetupMocksForCourse(course, existingLessons, new List<Holiday>());

        var dto = CreateGenerateDto(
            course.Id,
            new DateOnly(2024, 3, 4),
            new DateOnly(2024, 3, 25),
            skipHolidays: true);

        // Act
        var result = await _controller.GenerateLessons(dto, CancellationToken.None);

        // Assert
        AssertGenerationResult(result, lessonsCreated: 0, lessonsSkipped: 4);
    }

    [Fact]
    public async Task GenerateLessons_OverlappingRanges_OnlySkipsOverlap()
    {
        // Arrange
        var course = CreateTestCourse(CourseFrequency.Weekly, DayOfWeek.Monday);
        var existingLessons = new List<Lesson>
        {
            CreateLesson(course.Id, course.TeacherId, new DateOnly(2024, 3, 11)),
            CreateLesson(course.Id, course.TeacherId, new DateOnly(2024, 3, 18))
        };
        SetupMocksForCourse(course, existingLessons, new List<Holiday>());

        // New range partially overlaps with existing
        var dto = CreateGenerateDto(
            course.Id,
            new DateOnly(2024, 3, 11),  // Starts at first existing
            new DateOnly(2024, 4, 1),   // Goes beyond existing
            skipHolidays: true);

        // Act
        var result = await _controller.GenerateLessons(dto, CancellationToken.None);

        // Assert
        // 4 Mondays in range: Mar 11, 18 (existing), 25, Apr 1 (new)
        AssertGenerationResult(result, lessonsCreated: 2, lessonsSkipped: 2);
    }

    [Fact]
    public async Task GenerateLessons_DifferentCourseExistingLesson_DoesNotSkip()
    {
        // Arrange
        var course = CreateTestCourse(CourseFrequency.Weekly, DayOfWeek.Monday);
        var otherCourseId = Guid.NewGuid();

        // Existing lesson is for a DIFFERENT course
        var existingLessons = new List<Lesson>
        {
            new Lesson
            {
                Id = Guid.NewGuid(),
                CourseId = otherCourseId,  // Different course!
                TeacherId = course.TeacherId,
                ScheduledDate = new DateOnly(2024, 3, 11),
                StartTime = new TimeOnly(10, 0),
                EndTime = new TimeOnly(10, 30)
            }
        };
        SetupMocksForCourse(course, existingLessons, new List<Holiday>());

        var dto = CreateGenerateDto(
            course.Id,
            new DateOnly(2024, 3, 4),
            new DateOnly(2024, 3, 25),
            skipHolidays: true);

        // Act
        var result = await _controller.GenerateLessons(dto, CancellationToken.None);

        // Assert
        // Should create all 4 lessons since existing is for different course
        AssertGenerationResult(result, lessonsCreated: 4, lessonsSkipped: 0);
    }

    #region Helper Methods

    /// <summary>
    /// Creates a test course with the specified frequency and day of week.
    /// </summary>
    private static Course CreateTestCourse(CourseFrequency frequency, DayOfWeek dayOfWeek)
    {
        var courseId = Guid.NewGuid();
        var teacherId = Guid.NewGuid();
        var instrument = CreateTestInstrument();
        var courseType = CreateTestCourseType(instrument, CourseTypeCategory.Group);

        return new Course
        {
            Id = courseId,
            TeacherId = teacherId,
            CourseTypeId = courseType.Id,
            RoomId = 1,
            DayOfWeek = dayOfWeek,
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(10, 30),
            Frequency = frequency,
            WeekParity = WeekParity.All,
            Status = CourseStatus.Active,
            CourseType = courseType,
            Enrollments = new List<Enrollment>()
        };
    }

    /// <summary>
    /// Creates a test instrument for course setup.
    /// </summary>
    private static Instrument CreateTestInstrument()
    {
        return new Instrument
        {
            Id = 1,
            Name = "Piano",
            Category = InstrumentCategory.Keyboard
        };
    }

    /// <summary>
    /// Creates a test course type with the specified instrument and category.
    /// </summary>
    private static CourseType CreateTestCourseType(Instrument instrument, CourseTypeCategory category)
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
    /// Creates a lesson for testing duplication scenarios.
    /// </summary>
    private static Lesson CreateLesson(Guid courseId, Guid teacherId, DateOnly date)
    {
        return new Lesson
        {
            Id = Guid.NewGuid(),
            CourseId = courseId,
            TeacherId = teacherId,
            ScheduledDate = date,
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(10, 30),
            Status = LessonStatus.Scheduled
        };
    }

    /// <summary>
    /// Creates a GenerateLessonsDto with the specified parameters.
    /// </summary>
    private static GenerateLessonsDto CreateGenerateDto(
        Guid courseId,
        DateOnly startDate,
        DateOnly endDate,
        bool skipHolidays)
    {
        return new GenerateLessonsDto
        {
            CourseId = courseId,
            StartDate = startDate,
            EndDate = endDate,
            SkipHolidays = skipHolidays
        };
    }

    /// <summary>
    /// Sets up mock repositories for course, lessons, and holidays.
    /// </summary>
    private void SetupMocksForCourse(Course course, List<Lesson> existingLessons, List<Holiday> holidays)
    {
        var mockCourseRepo = new Mock<ICourseRepository>();
        mockCourseRepo.Setup(r => r.Query())
            .Returns(new List<Course> { course }.AsQueryable().BuildMockDbSet().Object);

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
        var mockAbsenceRepo = MockHelpers.CreateMockRepository(new List<Absence>());

        _mockUnitOfWork.Setup(u => u.Courses).Returns(mockCourseRepo.Object);
        _mockUnitOfWork.Setup(u => u.Lessons).Returns(mockLessonRepo.Object);
        _mockUnitOfWork.Setup(u => u.Repository<Holiday>()).Returns(mockHolidayRepo.Object);
        _mockUnitOfWork.Setup(u => u.Repository<Absence>()).Returns(mockAbsenceRepo.Object);
    }

    /// <summary>
    /// Asserts the result of a lesson generation operation.
    /// </summary>
    private static void AssertGenerationResult(
        ActionResult<GenerateLessonsResultDto> result,
        int lessonsCreated,
        int lessonsSkipped)
    {
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var generateResult = Assert.IsType<GenerateLessonsResultDto>(okResult.Value);

        Assert.Equal(lessonsCreated, generateResult.LessonsCreated);
        Assert.Equal(lessonsSkipped, generateResult.LessonsSkipped);
    }

    #endregion
}
