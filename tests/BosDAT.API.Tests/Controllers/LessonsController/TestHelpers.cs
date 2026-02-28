using Microsoft.AspNetCore.Mvc;
using Moq;
using BosDAT.API.Controllers;
using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Enums;
using BosDAT.Core.Interfaces;
using BosDAT.Core.Interfaces.Services;
using BosDAT.Core.Interfaces.Repositories;
using BosDAT.Infrastructure.Services;
using BosDAT.API.Tests.Helpers;
using Xunit;

namespace BosDAT.API.Tests.Controllers.LessonsController;

/// <summary>
/// Shared test helpers, builders, and utilities for lesson generation tests.
/// </summary>
public static class TestHelpers
{
    #region Assertion Helpers

    /// <summary>
    /// Asserts that a GenerateLessons result matches expected values.
    /// </summary>
    public static void AssertGenerationResult(
        ActionResult<GenerateLessonsResultDto> result,
        int expectedCreated,
        int expectedSkipped)
    {
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var generateResult = Assert.IsType<GenerateLessonsResultDto>(okResult.Value);

        Assert.Equal(expectedCreated, generateResult.LessonsCreated);
        Assert.Equal(expectedSkipped, generateResult.LessonsSkipped);
    }

    /// <summary>
    /// Asserts that a BulkGenerateLessons result matches expected values.
    /// </summary>
    public static void AssertBulkGenerationResult(
        ActionResult<BulkGenerateLessonsResultDto> result,
        int expectedCoursesProcessed,
        int expectedLessonsCreated,
        int expectedLessonsSkipped)
    {
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var bulkResult = Assert.IsType<BulkGenerateLessonsResultDto>(okResult.Value);

        Assert.Equal(expectedCoursesProcessed, bulkResult.TotalCoursesProcessed);
        Assert.Equal(expectedLessonsCreated, bulkResult.TotalLessonsCreated);
        Assert.Equal(expectedLessonsSkipped, bulkResult.TotalLessonsSkipped);
    }

    #endregion

    #region DTO Builders

    /// <summary>
    /// Creates a GenerateLessonsDto with standard defaults.
    /// </summary>
    public static GenerateLessonsDto CreateGenerateDto(
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
    /// Creates a BulkGenerateLessonsDto with standard defaults.
    /// </summary>
    public static BulkGenerateLessonsDto CreateBulkDto(
        DateOnly startDate,
        DateOnly endDate,
        bool skipHolidays = true)
    {
        return new BulkGenerateLessonsDto
        {
            StartDate = startDate,
            EndDate = endDate,
            SkipHolidays = skipHolidays
        };
    }

    #endregion

    #region Entity Factory Methods

    /// <summary>
    /// Creates a test Instrument entity.
    /// </summary>
    public static Instrument CreateTestInstrument()
    {
        return new Instrument
        {
            Id = 1,
            Name = "Piano",
            Category = InstrumentCategory.Keyboard
        };
    }

    /// <summary>
    /// Creates a test CourseType entity.
    /// </summary>
    public static CourseType CreateTestCourseType(Instrument instrument, CourseTypeCategory category)
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
    /// Creates a test Lesson entity.
    /// </summary>
    public static Lesson CreateLesson(Guid courseId, Guid teacherId, DateOnly date)
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

    #endregion

    #region Test Data Constants

    /// <summary>
    /// Common test dates used across tests.
    /// </summary>
    public static class TestDates
    {
        // March 2024 - standard test range
        public static readonly DateOnly March4_2024 = new(2024, 3, 4);   // Monday
        public static readonly DateOnly March5_2024 = new(2024, 3, 5);   // Tuesday
        public static readonly DateOnly March6_2024 = new(2024, 3, 6);   // Wednesday
        public static readonly DateOnly March7_2024 = new(2024, 3, 7);   // Thursday
        public static readonly DateOnly March11_2024 = new(2024, 3, 11); // Monday
        public static readonly DateOnly March18_2024 = new(2024, 3, 18); // Monday
        public static readonly DateOnly March25_2024 = new(2024, 3, 25); // Monday

        // January 2024 - week parity tests
        public static readonly DateOnly Jan1_2024 = new(2024, 1, 1);     // Week 1 (odd)
        public static readonly DateOnly Jan8_2024 = new(2024, 1, 8);     // Week 2 (even)
        public static readonly DateOnly Jan15_2024 = new(2024, 1, 15);   // Week 3 (odd)

        // Year transition - 53-week year test
        public static readonly DateOnly Dec21_2026 = new(2026, 12, 21);  // Week 52 (even)
        public static readonly DateOnly Jan31_2027 = new(2027, 1, 31);

        // Month-end handling
        public static readonly DateOnly Jan31_2024 = new(2024, 1, 31);
        public static readonly DateOnly Mar31_2024 = new(2024, 3, 31);
    }

    /// <summary>
    /// Common test times used across tests.
    /// </summary>
    public static class TestTimes
    {
        public static readonly TimeOnly Morning10AM = new(10, 0);
        public static readonly TimeOnly Morning10_30AM = new(10, 30);
        public static readonly TimeOnly Afternoon2PM = new(14, 0);
        public static readonly TimeOnly Afternoon2_30PM = new(14, 30);
    }

    #endregion
}

/// <summary>
/// Fluent builder for creating Course test entities with various configurations.
/// </summary>
public class CourseBuilder
{
    private Guid _id = Guid.NewGuid();
    private Guid _teacherId = Guid.NewGuid();
    private Guid? _courseTypeId;
    private int _roomId = 1;
    private DayOfWeek _dayOfWeek = DayOfWeek.Monday;
    private TimeOnly _startTime = new(10, 0);
    private TimeOnly _endTime = new(10, 30);
    private CourseFrequency _frequency = CourseFrequency.Weekly;
    private WeekParity _weekParity = WeekParity.All;
    private CourseStatus _status = CourseStatus.Active;
    private CourseType? _courseType;
    private List<Enrollment> _enrollments = new();

    /// <summary>
    /// Sets the course frequency.
    /// </summary>
    public CourseBuilder WithFrequency(CourseFrequency frequency)
    {
        _frequency = frequency;
        return this;
    }

    /// <summary>
    /// Sets the day of week when the course occurs.
    /// </summary>
    public CourseBuilder WithDayOfWeek(DayOfWeek dayOfWeek)
    {
        _dayOfWeek = dayOfWeek;
        return this;
    }

    /// <summary>
    /// Sets the week parity (for biweekly courses).
    /// </summary>
    public CourseBuilder WithParity(WeekParity parity)
    {
        _weekParity = parity;
        return this;
    }

    /// <summary>
    /// Sets the course status.
    /// </summary>
    public CourseBuilder WithStatus(CourseStatus status)
    {
        _status = status;
        return this;
    }

    /// <summary>
    /// Sets the course ID.
    /// </summary>
    public CourseBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    /// <summary>
    /// Sets the teacher ID.
    /// </summary>
    public CourseBuilder WithTeacherId(Guid teacherId)
    {
        _teacherId = teacherId;
        return this;
    }

    /// <summary>
    /// Sets the room ID.
    /// </summary>
    public CourseBuilder WithRoomId(int roomId)
    {
        _roomId = roomId;
        return this;
    }

    /// <summary>
    /// Sets the course time range.
    /// </summary>
    public CourseBuilder WithTimeRange(TimeOnly startTime, TimeOnly endTime)
    {
        _startTime = startTime;
        _endTime = endTime;
        return this;
    }

    /// <summary>
    /// Sets the course type directly.
    /// </summary>
    public CourseBuilder WithCourseType(CourseType courseType)
    {
        _courseType = courseType;
        _courseTypeId = courseType.Id;
        return this;
    }

    /// <summary>
    /// Generates students and enrollments for the course.
    /// </summary>
    public CourseBuilder WithStudents(int count, CourseTypeCategory category)
    {
        // Ensure course type matches the category
        var instrument = TestHelpers.CreateTestInstrument();
        _courseType = TestHelpers.CreateTestCourseType(instrument, category);
        _courseTypeId = _courseType.Id;

        _enrollments = new List<Enrollment>();
        for (var i = 0; i < count; i++)
        {
            var studentId = Guid.NewGuid();
            _enrollments.Add(new Enrollment
            {
                Id = Guid.NewGuid(),
                StudentId = studentId,
                CourseId = _id,
                Status = EnrollmentStatus.Active,
                Student = new Student
                {
                    Id = studentId,
                    FirstName = $"Student{i + 1}",
                    LastName = "Test",
                    Email = $"student{i + 1}@test.com"
                }
            });
        }

        return this;
    }

    /// <summary>
    /// Builds the Course entity.
    /// </summary>
    public Course Build()
    {
        // If no course type was set, create a default Group course type
        if (_courseType == null)
        {
            var instrument = TestHelpers.CreateTestInstrument();
            _courseType = TestHelpers.CreateTestCourseType(instrument, CourseTypeCategory.Group);
            _courseTypeId = _courseType.Id;
        }

        return new Course
        {
            Id = _id,
            TeacherId = _teacherId,
            CourseTypeId = _courseTypeId!.Value,
            RoomId = _roomId,
            DayOfWeek = _dayOfWeek,
            StartTime = _startTime,
            EndTime = _endTime,
            Frequency = _frequency,
            WeekParity = _weekParity,
            Status = _status,
            CourseType = _courseType,
            Enrollments = _enrollments
        };
    }
}

/// <summary>
/// Base class for lesson generation tests with shared setup and mock configuration.
/// </summary>
public abstract class LessonGenerationTestBase
{
    protected readonly Mock<IUnitOfWork> MockUnitOfWork;
    protected readonly Mock<ILessonService> MockLessonService;
    protected readonly API.Controllers.LessonsController Controller;

    protected LessonGenerationTestBase()
    {
        MockUnitOfWork = MockHelpers.CreateMockUnitOfWork();
        MockLessonService = new Mock<ILessonService>();
        var lessonGenerationService = new LessonGenerationService(MockUnitOfWork.Object);
        Controller = new API.Controllers.LessonsController(MockLessonService.Object, lessonGenerationService, MockUnitOfWork.Object);
    }

    /// <summary>
    /// Sets up mocks for a single course scenario.
    /// </summary>
    protected void SetupMocks(Course course, List<Lesson> existingLessons, List<Holiday> holidays)
    {
        var mockCourseRepo = new Mock<ICourseRepository>();
        mockCourseRepo.Setup(r => r.Query())
            .Returns(new List<Course> { course }.AsQueryable().BuildMockDbSet().Object);

        var mockLessonRepo = new Mock<ILessonRepository>();
        mockLessonRepo.Setup(r => r.Query())
            .Returns(existingLessons.AsQueryable().BuildMockDbSet().Object);
        mockLessonRepo.Setup(r => r.AddAsync(It.IsAny<Lesson>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Lesson l, CancellationToken _) => l);

        var mockHolidayRepo = MockHelpers.CreateMockRepository(holidays);
        var mockAbsenceRepo = MockHelpers.CreateMockRepository(new List<Absence>());

        MockUnitOfWork.Setup(u => u.Courses).Returns(mockCourseRepo.Object);
        MockUnitOfWork.Setup(u => u.Lessons).Returns(mockLessonRepo.Object);
        MockUnitOfWork.Setup(u => u.Repository<Holiday>()).Returns(mockHolidayRepo.Object);
        MockUnitOfWork.Setup(u => u.Repository<Absence>()).Returns(mockAbsenceRepo.Object);
    }

    /// <summary>
    /// Sets up mocks for bulk generation scenario with multiple courses.
    /// </summary>
    protected void SetupMocks(List<Course> courses, List<Lesson> existingLessons, List<Holiday> holidays)
    {
        var mockCourseRepo = new Mock<ICourseRepository>();
        mockCourseRepo.Setup(r => r.Query())
            .Returns(courses.AsQueryable().BuildMockDbSet().Object);

        var mockLessonRepo = new Mock<ILessonRepository>();
        mockLessonRepo.Setup(r => r.Query())
            .Returns(existingLessons.AsQueryable().BuildMockDbSet().Object);
        mockLessonRepo.Setup(r => r.AddAsync(It.IsAny<Lesson>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Lesson l, CancellationToken _) => l);

        var mockHolidayRepo = MockHelpers.CreateMockRepository(holidays);
        var mockAbsenceRepo = MockHelpers.CreateMockRepository(new List<Absence>());

        MockUnitOfWork.Setup(u => u.Courses).Returns(mockCourseRepo.Object);
        MockUnitOfWork.Setup(u => u.Lessons).Returns(mockLessonRepo.Object);
        MockUnitOfWork.Setup(u => u.Repository<Holiday>()).Returns(mockHolidayRepo.Object);
        MockUnitOfWork.Setup(u => u.Repository<Absence>()).Returns(mockAbsenceRepo.Object);
    }
}
