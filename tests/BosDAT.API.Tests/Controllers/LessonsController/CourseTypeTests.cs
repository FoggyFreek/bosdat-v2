using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using BosDAT.API.Controllers;
using BosDAT.API.Tests.Helpers;
using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Enums;
using BosDAT.Core.Interfaces;

namespace BosDAT.API.Tests.Controllers.LessonsController;

/// <summary>
/// Tests for lesson generation behavior across different course types (Individual, Group, Workshop).
/// Verifies that all course types create one lesson per enrolled student per date.
/// </summary>
public class CourseTypeTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly BosDAT.API.Controllers.LessonsController _controller;
    private readonly List<Lesson> _createdLessons;

    public CourseTypeTests()
    {
        _mockUnitOfWork = MockHelpers.CreateMockUnitOfWork();
        _controller = new BosDAT.API.Controllers.LessonsController(_mockUnitOfWork.Object);
        _createdLessons = new List<Lesson>();
    }

    /// <summary>
    /// Tests that lesson generation creates the correct number of lessons based on course type category.
    /// All course types (Individual, Group, Workshop) create one lesson per enrolled student per date.
    /// </summary>
    /// <param name="category">The course type category (Individual, Group, or Workshop).</param>
    /// <param name="studentCount">Number of students enrolled in the course.</param>
    /// <param name="expectedLessonCount">Expected total number of lessons to be created.</param>
    [Theory]
    [InlineData(CourseTypeCategory.Individual, 3, 12)] // 4 weeks × 3 students = 12
    [InlineData(CourseTypeCategory.Group, 5, 20)]      // 4 weeks × 5 students = 20
    [InlineData(CourseTypeCategory.Workshop, 10, 50)]  // 5 weeks × 10 students = 50
    public async Task GenerateLessons_CourseType_CreatesCorrectLessonCount(
        CourseTypeCategory category,
        int studentCount,
        int expectedLessonCount)
    {
        // Arrange
        var course = CreateTestCourseWithStudents(
            CourseFrequency.Weekly,
            category == CourseTypeCategory.Workshop ? DayOfWeek.Saturday : DayOfWeek.Tuesday,
            category,
            studentCount);
        SetupMocksForCourse(course, new List<Lesson>(), new List<Holiday>());

        var dto = CreateGenerateDto(
            course.Id,
            category == CourseTypeCategory.Workshop
                ? new DateOnly(2024, 3, 2)  // Saturday for Workshop (5 weeks)
                : new DateOnly(2024, 3, 5),  // Tuesday for Individual/Group (4 weeks)
            category == CourseTypeCategory.Workshop
                ? new DateOnly(2024, 3, 30)
                : new DateOnly(2024, 3, 26));

        // Act
        var result = await _controller.GenerateLessons(dto, CancellationToken.None);

        // Assert
        AssertGenerationResult(result, expectedLessonCount, 0);
    }

    [Fact]
    public async Task GenerateLessons_IndividualCourse_ThreeStudents_CreatesLessonPerStudent()
    {
        // Arrange
        var course = CreateTestCourseWithStudents(
            CourseFrequency.Weekly,
            DayOfWeek.Tuesday,
            CourseTypeCategory.Individual,
            studentCount: 3);
        SetupMocksForCourse(course, new List<Lesson>(), new List<Holiday>());

        var dto = CreateGenerateDto(
            course.Id,
            new DateOnly(2024, 3, 5),   // Tuesday
            new DateOnly(2024, 3, 26)); // Tuesday (4 weeks)

        // Act
        var result = await _controller.GenerateLessons(dto, CancellationToken.None);

        // Assert
        // 4 weeks × 3 students = 12 lessons
        AssertGenerationResult(result, lessonsCreated: 12, lessonsSkipped: 0);
    }

    [Fact]
    public async Task GenerateLessons_GroupCourse_FiveStudents_CreatesLessonPerStudentPerDate()
    {
        // Arrange
        var course = CreateTestCourseWithStudents(
            CourseFrequency.Weekly,
            DayOfWeek.Thursday,
            CourseTypeCategory.Group,
            studentCount: 5);
        SetupMocksForCourse(course, new List<Lesson>(), new List<Holiday>());

        var dto = CreateGenerateDto(
            course.Id,
            new DateOnly(2024, 3, 7),   // Thursday
            new DateOnly(2024, 3, 28)); // Thursday (4 weeks)

        // Act
        var result = await _controller.GenerateLessons(dto, CancellationToken.None);

        // Assert
        // 4 weeks × 5 students = 20 lessons
        AssertGenerationResult(result, lessonsCreated: 20, lessonsSkipped: 0);
    }

    [Fact]
    public async Task GenerateLessons_WorkshopCourse_TenStudents_CreatesLessonPerStudentPerDate()
    {
        // Arrange
        var course = CreateTestCourseWithStudents(
            CourseFrequency.Weekly,
            DayOfWeek.Saturday,
            CourseTypeCategory.Workshop,
            studentCount: 10);
        SetupMocksForCourse(course, new List<Lesson>(), new List<Holiday>());

        var dto = CreateGenerateDto(
            course.Id,
            new DateOnly(2024, 3, 2),   // Saturday
            new DateOnly(2024, 3, 30)); // Saturday (5 weeks)

        // Act
        var result = await _controller.GenerateLessons(dto, CancellationToken.None);

        // Assert
        // 5 weeks × 10 students = 50 lessons
        AssertGenerationResult(result, lessonsCreated: 50, lessonsSkipped: 0);
    }

    [Fact]
    public async Task GenerateLessons_NoEnrollments_CreatesOneLessonWithNullStudent()
    {
        // Arrange
        var course = CreateTestCourse(CourseFrequency.Weekly, DayOfWeek.Monday);
        // No enrollments added
        SetupMocksForCourse(course, new List<Lesson>(), new List<Holiday>());

        var dto = CreateGenerateDto(
            course.Id,
            new DateOnly(2024, 3, 4),
            new DateOnly(2024, 3, 25));

        // Act
        var result = await _controller.GenerateLessons(dto, CancellationToken.None);

        // Assert
        // 4 weeks, no students = still creates 4 lessons with null StudentId
        AssertGenerationResult(result, lessonsCreated: 4, lessonsSkipped: 0);
    }

    #region Helper Methods

    /// <summary>
    /// Creates a DTO for lesson generation with the specified parameters.
    /// </summary>
    private static GenerateLessonsDto CreateGenerateDto(
        Guid courseId,
        DateOnly startDate,
        DateOnly endDate,
        bool skipHolidays = true)
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
    /// Asserts that the generation result matches expected values.
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

        _mockUnitOfWork.Setup(u => u.Courses).Returns(mockCourseRepo.Object);
        _mockUnitOfWork.Setup(u => u.Lessons).Returns(mockLessonRepo.Object);
        _mockUnitOfWork.Setup(u => u.Repository<Holiday>()).Returns(mockHolidayRepo.Object);
    }

    #endregion
}
