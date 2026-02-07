using Moq;
using Xunit;
using BosDAT.Core.Entities;
using BosDAT.Core.Enums;
using BosDAT.Core.Interfaces;
using BosDAT.Infrastructure.Services;
using BosDAT.API.Tests.Helpers;
using BosDAT.API.Tests.Controllers.LessonsController;

namespace BosDAT.API.Tests.Services;

public class LessonGenerationServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly LessonGenerationService _service;
    private readonly List<Lesson> _createdLessons = new();

    public LessonGenerationServiceTests()
    {
        _mockUnitOfWork = MockHelpers.CreateMockUnitOfWork();
        _service = new LessonGenerationService(_mockUnitOfWork.Object);
    }

    #region GenerateForCourseAsync Tests

    [Fact]
    public async Task GenerateForCourse_WeeklyCourse_CreatesCorrectNumberOfLessons()
    {
        // Arrange
        var course = new CourseBuilder()
            .WithFrequency(CourseFrequency.Weekly)
            .WithDayOfWeek(DayOfWeek.Monday)
            .WithStudents(1, CourseTypeCategory.Individual)
            .Build();

        foreach (var e in course.Enrollments)
            e.EnrolledAt = new DateTime(2024, 1, 1);

        SetupMocks(new List<Course> { course }, new List<Lesson>(), new List<Holiday>());

        // Act - 4 Mondays in March 2024: 4th, 11th, 18th, 25th
        var result = await _service.GenerateForCourseAsync(
            course.Id,
            new DateOnly(2024, 3, 4),
            new DateOnly(2024, 3, 31),
            skipHolidays: true);

        // Assert
        Assert.Equal(4, result.LessonsCreated);
        Assert.Equal(0, result.LessonsSkipped);
    }

    [Fact]
    public async Task GenerateForCourse_SkipsHolidays()
    {
        // Arrange
        var course = new CourseBuilder()
            .WithFrequency(CourseFrequency.Weekly)
            .WithDayOfWeek(DayOfWeek.Monday)
            .WithStudents(1, CourseTypeCategory.Individual)
            .Build();

        foreach (var e in course.Enrollments)
            e.EnrolledAt = new DateTime(2024, 1, 1);

        var holidays = new List<Holiday>
        {
            new Holiday { Id = 1, Name = "Spring Break", StartDate = new DateOnly(2024, 3, 11), EndDate = new DateOnly(2024, 3, 11) }
        };

        SetupMocks(new List<Course> { course }, new List<Lesson>(), holidays);

        // Act
        var result = await _service.GenerateForCourseAsync(
            course.Id,
            new DateOnly(2024, 3, 4),
            new DateOnly(2024, 3, 31),
            skipHolidays: true);

        // Assert - 3 created, 1 skipped (March 11 is holiday)
        Assert.Equal(3, result.LessonsCreated);
        Assert.Equal(1, result.LessonsSkipped);
    }

    [Fact]
    public async Task GenerateForCourse_SkipsDuplicates()
    {
        // Arrange
        var course = new CourseBuilder()
            .WithFrequency(CourseFrequency.Weekly)
            .WithDayOfWeek(DayOfWeek.Monday)
            .WithStudents(1, CourseTypeCategory.Individual)
            .Build();

        // Set enrollment date before the test range so the mid-enrollment filter passes
        foreach (var e in course.Enrollments)
            e.EnrolledAt = new DateTime(2024, 1, 1);

        var studentId = course.Enrollments.First().StudentId;
        var existingLessons = new List<Lesson>
        {
            new Lesson
            {
                Id = Guid.NewGuid(),
                CourseId = course.Id,
                TeacherId = course.TeacherId,
                StudentId = studentId,
                ScheduledDate = new DateOnly(2024, 3, 4),
                StartTime = new TimeOnly(10, 0),
                EndTime = new TimeOnly(10, 30),
                Status = LessonStatus.Scheduled
            }
        };

        SetupMocks(new List<Course> { course }, existingLessons, new List<Holiday>());

        // Act
        var result = await _service.GenerateForCourseAsync(
            course.Id,
            new DateOnly(2024, 3, 4),
            new DateOnly(2024, 3, 31),
            skipHolidays: true);

        // Assert - 3 created, 1 skipped (March 4 already exists)
        Assert.Equal(3, result.LessonsCreated);
        Assert.Equal(1, result.LessonsSkipped);
    }

    [Fact]
    public async Task GenerateForCourse_InvalidCourseId_ReturnsZeroResults()
    {
        // Arrange
        SetupMocks(new List<Course>(), new List<Lesson>(), new List<Holiday>());

        // Act
        var result = await _service.GenerateForCourseAsync(
            Guid.NewGuid(),
            new DateOnly(2024, 3, 4),
            new DateOnly(2024, 3, 31),
            skipHolidays: true);

        // Assert
        Assert.Equal(0, result.LessonsCreated);
        Assert.Equal(0, result.LessonsSkipped);
    }

    #endregion

    #region GenerateBulkAsync Tests

    [Fact]
    public async Task GenerateBulk_ProcessesOnlyActiveCourses()
    {
        // Arrange
        var instrument = CreateInstrument();
        var courseType = CreateCourseType(instrument, CourseTypeCategory.Group);
        var teacherId = Guid.NewGuid();

        var courses = new List<Course>
        {
            CreateCourseWithStatus(CourseStatus.Active, DayOfWeek.Monday, courseType, teacherId),
            CreateCourseWithStatus(CourseStatus.Active, DayOfWeek.Wednesday, courseType, teacherId),
            CreateCourseWithStatus(CourseStatus.Paused, DayOfWeek.Friday, courseType, teacherId),
        };

        SetupMocks(courses, new List<Lesson>(), new List<Holiday>());

        // Act - 2 weeks
        var result = await _service.GenerateBulkAsync(
            new DateOnly(2024, 3, 4),
            new DateOnly(2024, 3, 17),
            skipHolidays: true);

        // Assert
        Assert.Equal(2, result.TotalCoursesProcessed);
        Assert.Equal(4, result.TotalLessonsCreated); // 2 active courses x 2 weeks
    }

    [Fact]
    public async Task GenerateBulk_AggregatesHolidaySkips()
    {
        // Arrange
        var instrument = CreateInstrument();
        var courseType = CreateCourseType(instrument, CourseTypeCategory.Group);
        var teacherId = Guid.NewGuid();

        var courses = new List<Course>
        {
            CreateCourseWithStatus(CourseStatus.Active, DayOfWeek.Monday, courseType, teacherId),
            CreateCourseWithStatus(CourseStatus.Active, DayOfWeek.Tuesday, courseType, teacherId),
        };

        var holidays = new List<Holiday>
        {
            new Holiday { Id = 1, Name = "Holiday", StartDate = new DateOnly(2024, 3, 11), EndDate = new DateOnly(2024, 3, 11) }
        };

        SetupMocks(courses, new List<Lesson>(), holidays);

        // Act
        var result = await _service.GenerateBulkAsync(
            new DateOnly(2024, 3, 4),
            new DateOnly(2024, 3, 17),
            skipHolidays: true);

        // Assert
        Assert.Equal(2, result.TotalCoursesProcessed);
        // Monday: 1 created (Mar 4) + 1 skipped (Mar 11 holiday)
        // Tuesday: 2 created (Mar 5, 12)
        Assert.Equal(3, result.TotalLessonsCreated);
        Assert.Equal(1, result.TotalLessonsSkipped);
    }

    [Fact]
    public async Task GenerateBulk_NoActiveCourses_ReturnsEmpty()
    {
        // Arrange
        var instrument = CreateInstrument();
        var courseType = CreateCourseType(instrument, CourseTypeCategory.Group);
        var teacherId = Guid.NewGuid();

        var courses = new List<Course>
        {
            CreateCourseWithStatus(CourseStatus.Paused, DayOfWeek.Monday, courseType, teacherId),
        };

        SetupMocks(courses, new List<Lesson>(), new List<Holiday>());

        // Act
        var result = await _service.GenerateBulkAsync(
            new DateOnly(2024, 3, 4),
            new DateOnly(2024, 3, 17),
            skipHolidays: true);

        // Assert
        Assert.Equal(0, result.TotalCoursesProcessed);
        Assert.Equal(0, result.TotalLessonsCreated);
        Assert.Empty(result.CourseResults);
    }

    #endregion

    #region Helpers

    private void SetupMocks(List<Course> courses, List<Lesson> existingLessons, List<Holiday> holidays)
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

    private static Instrument CreateInstrument() => new()
    {
        Id = 1,
        Name = "Piano",
        Category = InstrumentCategory.Keyboard
    };

    private static CourseType CreateCourseType(Instrument instrument, CourseTypeCategory category) => new()
    {
        Id = Guid.NewGuid(),
        Name = $"Test {category} Course",
        InstrumentId = instrument.Id,
        Instrument = instrument,
        Type = category,
        DurationMinutes = 30,
        MaxStudents = category == CourseTypeCategory.Individual ? 1 : 10
    };

    private static Course CreateCourseWithStatus(CourseStatus status, DayOfWeek dayOfWeek, CourseType courseType, Guid teacherId) => new()
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

    #endregion
}
