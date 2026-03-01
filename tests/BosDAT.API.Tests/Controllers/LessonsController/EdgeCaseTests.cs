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
/// Edge case tests for lesson generation, covering boundary conditions,
/// day-of-week alignment scenarios, and combined complex scenarios.
/// </summary>
public class EdgeCaseTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ILessonService> _mockLessonService;
    private readonly BosDAT.API.Controllers.LessonGenerationController _controller;
    private readonly List<Lesson> _createdLessons;

    public EdgeCaseTests()
    {
        _mockUnitOfWork = MockHelpers.CreateMockUnitOfWork();
        _mockLessonService = new Mock<ILessonService>();
        var lessonGenerationService = new LessonGenerationService(_mockUnitOfWork.Object);
        _controller = new BosDAT.API.Controllers.LessonGenerationController(lessonGenerationService, _mockUnitOfWork.Object);
        _createdLessons = new List<Lesson>();
    }

    #region Day-of-Week Alignment

    [Fact]
    public async Task GenerateLessons_StartDateNotMatchingDayOfWeek_FindsFirstCorrectDay()
    {
        // Arrange - Course is on Monday, but start date is Wednesday
        var course = CreateTestCourse(CourseFrequency.Weekly, DayOfWeek.Monday);
        SetupMocksForCourse(course, new List<Lesson>(), new List<Holiday>());

        var dto = new GenerateLessonsDto
        {
            CourseId = course.Id,
            StartDate = new DateOnly(2024, 3, 6),  // Wednesday
            EndDate = new DateOnly(2024, 3, 25),  // Monday
            SkipHolidays = true
        };

        // Act
        var result = await _controller.GenerateLessons(dto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var generateResult = Assert.IsType<GenerateLessonsResultDto>(okResult.Value);

        // First Monday after March 6 is March 11, then 18, 25 = 3 lessons
        Assert.Equal(3, generateResult.LessonsCreated);
    }

    [Fact]
    public async Task GenerateLessons_StartDateIsSaturday_CourseOnMonday_FindsNextMonday()
    {
        // Arrange
        var course = CreateTestCourse(CourseFrequency.Weekly, DayOfWeek.Monday);
        SetupMocksForCourse(course, new List<Lesson>(), new List<Holiday>());

        var dto = new GenerateLessonsDto
        {
            CourseId = course.Id,
            StartDate = new DateOnly(2024, 3, 2),  // Saturday
            EndDate = new DateOnly(2024, 3, 18),  // Monday
            SkipHolidays = true
        };

        // Act
        var result = await _controller.GenerateLessons(dto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var generateResult = Assert.IsType<GenerateLessonsResultDto>(okResult.Value);

        // Next Monday is March 4, then 11, 18 = 3 lessons
        Assert.Equal(3, generateResult.LessonsCreated);
    }

    #endregion

    #region Boundary Conditions

    [Fact]
    public async Task GenerateLessons_EmptyDateRange_CreatesZeroLessons()
    {
        // Arrange
        var course = CreateTestCourse(CourseFrequency.Weekly, DayOfWeek.Monday);
        SetupMocksForCourse(course, new List<Lesson>(), new List<Holiday>());

        // Start > End
        var dto = new GenerateLessonsDto
        {
            CourseId = course.Id,
            StartDate = new DateOnly(2024, 3, 25),
            EndDate = new DateOnly(2024, 3, 4),  // Before start!
            SkipHolidays = true
        };

        // Act
        var result = await _controller.GenerateLessons(dto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var generateResult = Assert.IsType<GenerateLessonsResultDto>(okResult.Value);

        Assert.Equal(0, generateResult.LessonsCreated);
        Assert.Equal(0, generateResult.LessonsSkipped);
    }

    [Fact]
    public async Task GenerateLessons_SingleDay_MatchingDayOfWeek_CreatesOneLesson()
    {
        // Arrange
        var course = CreateTestCourse(CourseFrequency.Weekly, DayOfWeek.Monday);
        SetupMocksForCourse(course, new List<Lesson>(), new List<Holiday>());

        var dto = new GenerateLessonsDto
        {
            CourseId = course.Id,
            StartDate = new DateOnly(2024, 3, 4),  // Monday
            EndDate = new DateOnly(2024, 3, 4),   // Same day
            SkipHolidays = true
        };

        // Act
        var result = await _controller.GenerateLessons(dto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var generateResult = Assert.IsType<GenerateLessonsResultDto>(okResult.Value);

        Assert.Equal(1, generateResult.LessonsCreated);
    }

    [Fact]
    public async Task GenerateLessons_SingleDay_NotMatchingDayOfWeek_CreatesZeroLessons()
    {
        // Arrange
        var course = CreateTestCourse(CourseFrequency.Weekly, DayOfWeek.Monday);
        SetupMocksForCourse(course, new List<Lesson>(), new List<Holiday>());

        var dto = new GenerateLessonsDto
        {
            CourseId = course.Id,
            StartDate = new DateOnly(2024, 3, 5),  // Tuesday
            EndDate = new DateOnly(2024, 3, 5),   // Same day
            SkipHolidays = true
        };

        // Act
        var result = await _controller.GenerateLessons(dto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var generateResult = Assert.IsType<GenerateLessonsResultDto>(okResult.Value);

        Assert.Equal(0, generateResult.LessonsCreated);
    }

    [Fact]
    public async Task GenerateLessons_CourseWithNoCourseType_DefaultsToSingleLesson()
    {
        // Arrange - Course without CourseType
        var courseId = Guid.NewGuid();
        var teacherId = Guid.NewGuid();

        var course = new Course
        {
            Id = courseId,
            TeacherId = teacherId,
            CourseTypeId = Guid.NewGuid(),
            RoomId = 1,
            DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(10, 30),
            Frequency = CourseFrequency.Weekly,
            Status = CourseStatus.Active,
            CourseType = null!,  // No CourseType!
            Enrollments = new List<Enrollment>()
        };

        SetupMocksForCourse(course, new List<Lesson>(), new List<Holiday>());

        var dto = new GenerateLessonsDto
        {
            CourseId = course.Id,
            StartDate = new DateOnly(2024, 3, 4),
            EndDate = new DateOnly(2024, 3, 11),
            SkipHolidays = true
        };

        // Act
        var result = await _controller.GenerateLessons(dto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var generateResult = Assert.IsType<GenerateLessonsResultDto>(okResult.Value);

        // Should default to creating one lesson per date (group behavior)
        Assert.Equal(2, generateResult.LessonsCreated);
    }

    #endregion

    #region Combined Scenarios

    [Fact]
    public async Task GenerateLessons_BiweeklyOddParity_WithHoliday_SkipsCorrectly()
    {
        // Arrange
        var course = CreateTestCourseWithParity(
            CourseFrequency.Biweekly,
            DayOfWeek.Monday,
            WeekParity.Odd);

        var holidays = new List<Holiday>
        {
            new Holiday
            {
                Id = 1,
                Name = "Week 3 Holiday",
                StartDate = new DateOnly(2024, 1, 15),  // Week 3 (odd), Monday
                EndDate = new DateOnly(2024, 1, 15)
            }
        };
        SetupMocksForCourse(course, new List<Lesson>(), holidays);

        var dto = new GenerateLessonsDto
        {
            CourseId = course.Id,
            StartDate = new DateOnly(2024, 1, 1),  // Week 1 (odd)
            EndDate = new DateOnly(2024, 2, 5),   // Week 6
            SkipHolidays = true
        };

        // Act
        var result = await _controller.GenerateLessons(dto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var generateResult = Assert.IsType<GenerateLessonsResultDto>(okResult.Value);

        // Odd weeks: 1, 3, 5 = 3 occurrences, but Week 3 is holiday = 2 created, 1 skipped
        Assert.Equal(2, generateResult.LessonsCreated);
        Assert.Equal(1, generateResult.LessonsSkipped);
    }

    [Fact]
    public async Task GenerateLessons_IndividualCourse_WithHolidayAndExisting_CalculatesCorrectly()
    {
        // Arrange
        var course = CreateTestCourseWithStudents(
            CourseFrequency.Weekly,
            DayOfWeek.Monday,
            CourseTypeCategory.Individual,
            studentCount: 2);

        // Create existing lessons for both students on March 4
        var studentIds = course.Enrollments.Select(e => e.StudentId).ToList();
        var existingLessons = new List<Lesson>
        {
            CreateLessonWithStudent(course.Id, course.TeacherId, new DateOnly(2024, 3, 4), studentIds[0]),
            CreateLessonWithStudent(course.Id, course.TeacherId, new DateOnly(2024, 3, 4), studentIds[1])
        };

        var holidays = new List<Holiday>
        {
            new Holiday
            {
                Id = 1,
                Name = "Test Holiday",
                StartDate = new DateOnly(2024, 3, 18),
                EndDate = new DateOnly(2024, 3, 18)
            }
        };

        SetupMocksForCourse(course, existingLessons, holidays);

        var dto = new GenerateLessonsDto
        {
            CourseId = course.Id,
            StartDate = new DateOnly(2024, 3, 4),
            EndDate = new DateOnly(2024, 3, 25),  // 4 Mondays
            SkipHolidays = true
        };

        // Act
        var result = await _controller.GenerateLessons(dto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var generateResult = Assert.IsType<GenerateLessonsResultDto>(okResult.Value);

        // 4 Mondays: March 4 (existing x2), 11 (create 2), 18 (holiday), 25 (create 2)
        // Created: 2 + 2 = 4, Skipped: 2 (existing) + 1 (holiday) = 3
        Assert.Equal(4, generateResult.LessonsCreated);
        Assert.Equal(3, generateResult.LessonsSkipped);
    }

    [Fact]
    public async Task GenerateLessons_BiweeklyAllParity_StartingMidWeek_FindsCorrectFirstDay()
    {
        // Arrange - Course on Friday, start date is Tuesday
        var course = CreateTestCourseWithParity(
            CourseFrequency.Biweekly,
            DayOfWeek.Friday,
            WeekParity.All);
        SetupMocksForCourse(course, new List<Lesson>(), new List<Holiday>());

        var dto = new GenerateLessonsDto
        {
            CourseId = course.Id,
            StartDate = new DateOnly(2024, 1, 2),   // Tuesday
            EndDate = new DateOnly(2024, 2, 16),   // Friday
            SkipHolidays = true
        };

        // Act
        var result = await _controller.GenerateLessons(dto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var generateResult = Assert.IsType<GenerateLessonsResultDto>(okResult.Value);

        // First Friday is Jan 5, then every 14 days: Jan 19, Feb 2, Feb 16 = 4 lessons
        Assert.Equal(4, generateResult.LessonsCreated);
    }

    [Fact]
    public async Task GenerateLessons_MonthlyFrequency_WithHolidayAndMismatchedStartDay_SkipsCorrectly()
    {
        // Arrange - Course on first Monday, start date is Wednesday
        var course = CreateTestCourse(CourseFrequency.Once, DayOfWeek.Monday);
        var holidays = new List<Holiday>
        {
            new Holiday
            {
                Id = 1,
                Name = "April Holiday",
                StartDate = new DateOnly(2024, 4, 1),  // First Monday in April
                EndDate = new DateOnly(2024, 4, 1)
            }
        };
        SetupMocksForCourse(course, new List<Lesson>(), holidays);

        var dto = new GenerateLessonsDto
        {
            CourseId = course.Id,
            StartDate = new DateOnly(2024, 2, 28),  // Wednesday, end of February
            EndDate = new DateOnly(2024, 5, 31),   // End of May
            SkipHolidays = true
        };

        // Act
        var result = await _controller.GenerateLessons(dto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var generateResult = Assert.IsType<GenerateLessonsResultDto>(okResult.Value);

        // First Monday after Feb 28 is March 4
        // Monthly: March 4 → April 4 → May 4 (all within range)
        // Holiday is April 1, but April 4 is NOT in holiday range
        Assert.Equal(3, generateResult.LessonsCreated);
        Assert.Equal(0, generateResult.LessonsSkipped);
    }

    #endregion

    #region Helper Methods

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

    private static Course CreateTestCourseWithParity(
        CourseFrequency frequency,
        DayOfWeek dayOfWeek,
        WeekParity weekParity)
    {
        var course = CreateTestCourse(frequency, dayOfWeek);
        course.WeekParity = weekParity;
        return course;
    }

    private static Course CreateTestCourseWithStudents(
        CourseFrequency frequency,
        DayOfWeek dayOfWeek,
        CourseTypeCategory category,
        int studentCount)
    {
        var courseId = Guid.NewGuid();
        var teacherId = Guid.NewGuid();
        var instrument = CreateTestInstrument();
        var courseType = CreateTestCourseType(instrument, category);

        var enrollments = new List<Enrollment>();
        for (var i = 0; i < studentCount; i++)
        {
            var studentId = Guid.NewGuid();
            enrollments.Add(new Enrollment
            {
                Id = Guid.NewGuid(),
                StudentId = studentId,
                CourseId = courseId,
                Status = EnrollmentStatus.Active,
                EnrolledAt = new DateTime(2024, 1, 1), // Enrolled before test period
                Student = new Student
                {
                    Id = studentId,
                    FirstName = $"Student{i + 1}",
                    LastName = "Test",
                    Email = $"student{i + 1}@test.com"
                }
            });
        }

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
            Enrollments = enrollments
        };
    }

    private static Instrument CreateTestInstrument()
    {
        return new Instrument
        {
            Id = 1,
            Name = "Piano",
            Category = InstrumentCategory.Keyboard
        };
    }

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

    private static Lesson CreateLessonWithStudent(Guid courseId, Guid teacherId, DateOnly date, Guid studentId)
    {
        return new Lesson
        {
            Id = Guid.NewGuid(),
            CourseId = courseId,
            TeacherId = teacherId,
            StudentId = studentId,
            ScheduledDate = date,
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(10, 30),
            Status = LessonStatus.Scheduled
        };
    }

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

    #endregion
}
