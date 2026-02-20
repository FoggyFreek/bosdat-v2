using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using BosDAT.API.Controllers;
using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Enums;
using BosDAT.Core.Interfaces;
using BosDAT.Infrastructure.Services;
using BosDAT.API.Tests.Helpers;

namespace BosDAT.API.Tests.Controllers.LessonsController;

/// <summary>
/// Tests for error handling and validation scenarios in LessonsController.
/// Covers: invalid inputs, missing entities, cancellation, edge cases, and bulk operation partial failures.
/// </summary>
public class ErrorHandlingTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ILessonService> _mockLessonService;
    private readonly BosDAT.API.Controllers.LessonsController _controller;

    public ErrorHandlingTests()
    {
        _mockUnitOfWork = MockHelpers.CreateMockUnitOfWork();
        _mockLessonService = new Mock<ILessonService>();
        var lessonGenerationService = new LessonGenerationService(_mockUnitOfWork.Object);
        _controller = new BosDAT.API.Controllers.LessonsController(_mockLessonService.Object, lessonGenerationService, _mockUnitOfWork.Object);
    }

    #region Course Not Found Tests

    [Fact]
    public async Task GenerateLessons_CourseNotFound_ReturnsBadRequest()
    {
        // Arrange
        var nonExistentCourseId = Guid.NewGuid();

        // Setup empty course query result
        var mockCourseRepo = new Mock<ICourseRepository>();
        mockCourseRepo.Setup(r => r.Query())
            .Returns(new List<Course>().AsQueryable().BuildMockDbSet().Object);

        _mockUnitOfWork.Setup(u => u.Courses).Returns(mockCourseRepo.Object);

        var dto = new GenerateLessonsDto
        {
            CourseId = nonExistentCourseId,
            StartDate = new DateOnly(2024, 3, 4),
            EndDate = new DateOnly(2024, 3, 25),
            SkipHolidays = true
        };

        // Act
        var result = await _controller.GenerateLessons(dto, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var errorResponse = badRequestResult.Value;

        Assert.NotNull(errorResponse);
        var messageProperty = errorResponse.GetType().GetProperty("message");
        Assert.NotNull(messageProperty);
        Assert.Equal("Course not found", messageProperty.GetValue(errorResponse));
    }

    [Fact]
    public async Task GenerateLessons_EmptyGuidCourseId_ReturnsBadRequest()
    {
        // Arrange
        var emptyCourseId = Guid.Empty;

        // Setup empty course query result
        var mockCourseRepo = new Mock<ICourseRepository>();
        mockCourseRepo.Setup(r => r.Query())
            .Returns(new List<Course>().AsQueryable().BuildMockDbSet().Object);

        _mockUnitOfWork.Setup(u => u.Courses).Returns(mockCourseRepo.Object);

        var dto = new GenerateLessonsDto
        {
            CourseId = emptyCourseId,
            StartDate = new DateOnly(2024, 3, 4),
            EndDate = new DateOnly(2024, 3, 25),
            SkipHolidays = true
        };

        // Act
        var result = await _controller.GenerateLessons(dto, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var errorResponse = badRequestResult.Value;

        Assert.NotNull(errorResponse);
        var messageProperty = errorResponse.GetType().GetProperty("message");
        Assert.NotNull(messageProperty);
        Assert.Equal("Course not found", messageProperty.GetValue(errorResponse));
    }

    #endregion

    #region Invalid Date Range Tests

    [Fact]
    public async Task GenerateLessons_StartDateAfterEndDate_ReturnsEmptyResult()
    {
        // Arrange
        var course = CreateTestCourse(CourseFrequency.Weekly, DayOfWeek.Monday);
        SetupMocksForCourse(course, new List<Lesson>(), new List<Holiday>());

        // Start date is AFTER end date (invalid range)
        var dto = new GenerateLessonsDto
        {
            CourseId = course.Id,
            StartDate = new DateOnly(2024, 3, 25),
            EndDate = new DateOnly(2024, 3, 4),  // Before start date
            SkipHolidays = true
        };

        // Act
        var result = await _controller.GenerateLessons(dto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var generateResult = Assert.IsType<GenerateLessonsResultDto>(okResult.Value);

        // Should return valid response with zero lessons (graceful handling)
        Assert.Equal(0, generateResult.LessonsCreated);
        Assert.Equal(0, generateResult.LessonsSkipped);
    }

    [Fact]
    public async Task GenerateLessons_VeryLargeDateRange_HandlesCorrectly()
    {
        // Arrange
        var course = CreateTestCourse(CourseFrequency.Weekly, DayOfWeek.Monday);
        SetupMocksForCourse(course, new List<Lesson>(), new List<Holiday>());

        // Very large range: 1 year
        var dto = new GenerateLessonsDto
        {
            CourseId = course.Id,
            StartDate = new DateOnly(2024, 1, 1),
            EndDate = new DateOnly(2024, 12, 31),
            SkipHolidays = true
        };

        // Act
        var result = await _controller.GenerateLessons(dto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var generateResult = Assert.IsType<GenerateLessonsResultDto>(okResult.Value);

        // 52 weeks should create ~52 lessons
        Assert.InRange(generateResult.LessonsCreated, 50, 53);
    }

    #endregion

    #region Cancellation Token Tests

    [Fact]
    public async Task GenerateLessons_CancellationRequested_ThrowsOperationCanceledException()
    {
        // Arrange
        var course = CreateTestCourse(CourseFrequency.Weekly, DayOfWeek.Monday);

        // Setup mocks to respond to cancellation
        var mockCourseRepo = new Mock<ICourseRepository>();
        mockCourseRepo.Setup(r => r.Query())
            .Returns(() =>
            {
                // Simulate cancellation during query
                var cts = new CancellationTokenSource();
                cts.Cancel();
                cts.Token.ThrowIfCancellationRequested();
                return new List<Course> { course }.AsQueryable().BuildMockDbSet().Object;
            });

        _mockUnitOfWork.Setup(u => u.Courses).Returns(mockCourseRepo.Object);

        var dto = new GenerateLessonsDto
        {
            CourseId = course.Id,
            StartDate = new DateOnly(2024, 3, 4),
            EndDate = new DateOnly(2024, 3, 25),
            SkipHolidays = true
        };

        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await _controller.GenerateLessons(dto, cancellationTokenSource.Token));
    }

    [Fact]
    public async Task GetAll_CancellationRequested_ThrowsOperationCanceledException()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        _mockLessonService.Setup(s => s.GetAllAsync(
                It.IsAny<LessonFilterCriteria>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await _controller.GetAll(null, null, null, null, null, null, null, null, cancellationTokenSource.Token));
    }

    #endregion

    #region All Dates Are Holidays Tests

    [Fact]
    public async Task GenerateLessons_AllDatesAreHolidays_ReturnsZeroCreatedAllSkipped()
    {
        // Arrange
        var course = CreateTestCourse(CourseFrequency.Weekly, DayOfWeek.Monday);

        // Create holiday covering entire date range
        var holidays = new List<Holiday>
        {
            new Holiday
            {
                Id = 1,
                Name = "Extended Holiday",
                StartDate = new DateOnly(2024, 3, 1),
                EndDate = new DateOnly(2024, 3, 31)
            }
        };

        SetupMocksForCourse(course, new List<Lesson>(), holidays);

        var dto = new GenerateLessonsDto
        {
            CourseId = course.Id,
            StartDate = new DateOnly(2024, 3, 4),  // Monday
            EndDate = new DateOnly(2024, 3, 25),  // Monday (4 weeks)
            SkipHolidays = true
        };

        // Act
        var result = await _controller.GenerateLessons(dto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var generateResult = Assert.IsType<GenerateLessonsResultDto>(okResult.Value);

        // All dates are holidays, so no lessons created, all skipped
        Assert.Equal(0, generateResult.LessonsCreated);
        Assert.Equal(4, generateResult.LessonsSkipped);
    }

    [Fact]
    public async Task GenerateLessons_MultipleOverlappingHolidays_CountsCorrectly()
    {
        // Arrange
        var course = CreateTestCourse(CourseFrequency.Weekly, DayOfWeek.Monday);

        // Multiple overlapping holidays covering all dates
        var holidays = new List<Holiday>
        {
            new Holiday
            {
                Id = 1,
                Name = "Holiday 1",
                StartDate = new DateOnly(2024, 3, 4),
                EndDate = new DateOnly(2024, 3, 15)
            },
            new Holiday
            {
                Id = 2,
                Name = "Holiday 2",
                StartDate = new DateOnly(2024, 3, 10),
                EndDate = new DateOnly(2024, 3, 25)
            }
        };

        SetupMocksForCourse(course, new List<Lesson>(), holidays);

        var dto = new GenerateLessonsDto
        {
            CourseId = course.Id,
            StartDate = new DateOnly(2024, 3, 4),
            EndDate = new DateOnly(2024, 3, 25),
            SkipHolidays = true
        };

        // Act
        var result = await _controller.GenerateLessons(dto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var generateResult = Assert.IsType<GenerateLessonsResultDto>(okResult.Value);

        // All dates covered by holidays
        Assert.Equal(0, generateResult.LessonsCreated);
        Assert.Equal(4, generateResult.LessonsSkipped);
    }

    #endregion

    #region Bulk Generation Error Handling Tests

    [Fact]
    public async Task BulkGenerateLessons_NoActiveCourses_ReturnsEmptyResult()
    {
        // Arrange
        var teacherId = Guid.NewGuid();
        var instrument = CreateTestInstrument();
        var courseType = CreateTestCourseType(instrument, CourseTypeCategory.Group);

        // All courses are inactive
        var courses = new List<Course>
        {
            CreateTestCourseWithStatus(CourseStatus.Paused, DayOfWeek.Monday, courseType, teacherId),
            CreateTestCourseWithStatus(CourseStatus.Completed, DayOfWeek.Tuesday, courseType, teacherId)
        };

        SetupMocksForBulkGeneration(courses, new List<Lesson>(), new List<Holiday>());

        var dto = new BulkGenerateLessonsDto
        {
            StartDate = new DateOnly(2024, 3, 4),
            EndDate = new DateOnly(2024, 3, 17),
            SkipHolidays = true
        };

        // Act
        var result = await _controller.GenerateLessonsBulk(dto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var bulkResult = Assert.IsType<BulkGenerateLessonsResultDto>(okResult.Value);

        // No active courses, so zero lessons
        Assert.Equal(0, bulkResult.TotalCoursesProcessed);
        Assert.Equal(0, bulkResult.TotalLessonsCreated);
        Assert.Equal(0, bulkResult.TotalLessonsSkipped);
        Assert.Empty(bulkResult.CourseResults);
    }

    [Fact]
    public async Task BulkGenerateLessons_SomeCoursesHaveNoEnrollments_ContinuesProcessing()
    {
        // Arrange
        var teacherId = Guid.NewGuid();
        var instrument = CreateTestInstrument();
        var courseType = CreateTestCourseType(instrument, CourseTypeCategory.Group);

        // Mix of courses with and without enrollments
        var courseWithEnrollments = CreateTestCourseWithStudents(
            CourseStatus.Active,
            DayOfWeek.Monday,
            courseType,
            teacherId,
            studentCount: 3);

        var courseWithoutEnrollments = CreateTestCourseWithStatus(
            CourseStatus.Active,
            DayOfWeek.Tuesday,
            courseType,
            teacherId);

        var courses = new List<Course> { courseWithEnrollments, courseWithoutEnrollments };

        SetupMocksForBulkGeneration(courses, new List<Lesson>(), new List<Holiday>());

        var dto = new BulkGenerateLessonsDto
        {
            StartDate = new DateOnly(2024, 3, 4),
            EndDate = new DateOnly(2024, 3, 11),
            SkipHolidays = true
        };

        // Act
        var result = await _controller.GenerateLessonsBulk(dto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var bulkResult = Assert.IsType<BulkGenerateLessonsResultDto>(okResult.Value);

        // Both courses processed
        Assert.Equal(2, bulkResult.TotalCoursesProcessed);

        // Monday: March 4, March 11 = 2 lessons
        // Tuesday: March 5 only (March 12 is outside range) = 1 lesson
        // Total = 3 lessons
        Assert.Equal(3, bulkResult.TotalLessonsCreated);
    }

    [Fact]
    public async Task BulkGenerateLessons_AllCoursesHaveExistingLessons_SkipsAll()
    {
        // Arrange
        var teacherId = Guid.NewGuid();
        var instrument = CreateTestInstrument();
        var courseType = CreateTestCourseType(instrument, CourseTypeCategory.Group);

        var course1 = CreateTestCourseWithStatus(CourseStatus.Active, DayOfWeek.Monday, courseType, teacherId);
        var course2 = CreateTestCourseWithStatus(CourseStatus.Active, DayOfWeek.Tuesday, courseType, teacherId);

        var courses = new List<Course> { course1, course2 };

        // Existing lessons for all dates
        var existingLessons = new List<Lesson>
        {
            CreateLesson(course1.Id, teacherId, new DateOnly(2024, 3, 4)),
            CreateLesson(course1.Id, teacherId, new DateOnly(2024, 3, 11)),
            CreateLesson(course2.Id, teacherId, new DateOnly(2024, 3, 5)),
            CreateLesson(course2.Id, teacherId, new DateOnly(2024, 3, 12))
        };

        SetupMocksForBulkGeneration(courses, existingLessons, new List<Holiday>());

        var dto = new BulkGenerateLessonsDto
        {
            StartDate = new DateOnly(2024, 3, 4),
            EndDate = new DateOnly(2024, 3, 12),
            SkipHolidays = true
        };

        // Act
        var result = await _controller.GenerateLessonsBulk(dto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var bulkResult = Assert.IsType<BulkGenerateLessonsResultDto>(okResult.Value);

        // All lessons already exist
        Assert.Equal(2, bulkResult.TotalCoursesProcessed);
        Assert.Equal(0, bulkResult.TotalLessonsCreated);
        Assert.Equal(4, bulkResult.TotalLessonsSkipped);
    }

    [Fact]
    public async Task BulkGenerateLessons_MixOfSuccessAndSkips_AggregatesCorrectly()
    {
        // Arrange
        var teacherId = Guid.NewGuid();
        var instrument = CreateTestInstrument();
        var courseType = CreateTestCourseType(instrument, CourseTypeCategory.Group);

        var course1 = CreateTestCourseWithStatus(CourseStatus.Active, DayOfWeek.Monday, courseType, teacherId);
        var course2 = CreateTestCourseWithStatus(CourseStatus.Active, DayOfWeek.Tuesday, courseType, teacherId);
        var course3 = CreateTestCourseWithStatus(CourseStatus.Active, DayOfWeek.Wednesday, courseType, teacherId);

        var courses = new List<Course> { course1, course2, course3 };

        // Course 1 has existing lesson on March 11
        var existingLessons = new List<Lesson>
        {
            CreateLesson(course1.Id, teacherId, new DateOnly(2024, 3, 11))
        };

        // Course 2 has holiday on March 12
        var holidays = new List<Holiday>
        {
            new Holiday
            {
                Id = 1,
                Name = "Tuesday Holiday",
                StartDate = new DateOnly(2024, 3, 12),
                EndDate = new DateOnly(2024, 3, 12)
            }
        };

        SetupMocksForBulkGeneration(courses, existingLessons, holidays);

        var dto = new BulkGenerateLessonsDto
        {
            StartDate = new DateOnly(2024, 3, 4),
            EndDate = new DateOnly(2024, 3, 13),
            SkipHolidays = true
        };

        // Act
        var result = await _controller.GenerateLessonsBulk(dto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var bulkResult = Assert.IsType<BulkGenerateLessonsResultDto>(okResult.Value);

        // Course 1 (Monday): March 4 (created), March 11 (existing/skipped) = 1 created, 1 skipped
        // Course 2 (Tuesday): March 5 (created), March 12 (holiday/skipped) = 1 created, 1 skipped
        // Course 3 (Wednesday): March 6 (created), March 13 (created) = 2 created, 0 skipped

        Assert.Equal(3, bulkResult.TotalCoursesProcessed);
        Assert.Equal(4, bulkResult.TotalLessonsCreated);
        Assert.Equal(2, bulkResult.TotalLessonsSkipped);
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public async Task GenerateLessons_CourseWithNullCourseType_HandlesGracefully()
    {
        // Arrange
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
            WeekParity = WeekParity.All,
            Status = CourseStatus.Active,
            CourseType = null!,  // Null CourseType
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

        // Should default to group behavior (one lesson per date)
        Assert.Equal(2, generateResult.LessonsCreated);
    }

    [Fact]
    public async Task GenerateLessons_NoMatchingDayOfWeekInRange_CreatesZeroLessons()
    {
        // Arrange
        var course = CreateTestCourse(CourseFrequency.Weekly, DayOfWeek.Monday);
        SetupMocksForCourse(course, new List<Lesson>(), new List<Holiday>());

        // Range from Tuesday to Friday (no Mondays)
        var dto = new GenerateLessonsDto
        {
            CourseId = course.Id,
            StartDate = new DateOnly(2024, 3, 5),  // Tuesday
            EndDate = new DateOnly(2024, 3, 8),   // Friday
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
    public async Task GenerateLessons_ZeroLessonsCreated_ExcludedFromCourseResults()
    {
        // Arrange
        var teacherId = Guid.NewGuid();
        var instrument = CreateTestInstrument();
        var courseType = CreateTestCourseType(instrument, CourseTypeCategory.Group);

        var course = CreateTestCourseWithStatus(CourseStatus.Active, DayOfWeek.Monday, courseType, teacherId);

        var courses = new List<Course> { course };

        // All dates are holidays
        var holidays = new List<Holiday>
        {
            new Holiday
            {
                Id = 1,
                Name = "Extended Holiday",
                StartDate = new DateOnly(2024, 3, 1),
                EndDate = new DateOnly(2024, 3, 31)
            }
        };

        SetupMocksForBulkGeneration(courses, new List<Lesson>(), holidays);

        var dto = new BulkGenerateLessonsDto
        {
            StartDate = new DateOnly(2024, 3, 4),
            EndDate = new DateOnly(2024, 3, 11),
            SkipHolidays = true
        };

        // Act
        var result = await _controller.GenerateLessonsBulk(dto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var bulkResult = Assert.IsType<BulkGenerateLessonsResultDto>(okResult.Value);

        // Course is processed but creates zero lessons
        Assert.Equal(1, bulkResult.TotalCoursesProcessed);
        Assert.Equal(0, bulkResult.TotalLessonsCreated);

        // CourseResults should be empty because no lessons were created
        Assert.Empty(bulkResult.CourseResults);
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

    private static Course CreateTestCourseWithStatus(
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

    private static Course CreateTestCourseWithStudents(
        CourseStatus status,
        DayOfWeek dayOfWeek,
        CourseType courseType,
        Guid teacherId,
        int studentCount)
    {
        var courseId = Guid.NewGuid();
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
            Frequency = CourseFrequency.Weekly,
            WeekParity = WeekParity.All,
            Status = status,
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

    private void SetupMocksForCourse(Course course, List<Lesson> existingLessons, List<Holiday> holidays)
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

        _mockUnitOfWork.Setup(u => u.Courses).Returns(mockCourseRepo.Object);
        _mockUnitOfWork.Setup(u => u.Lessons).Returns(mockLessonRepo.Object);
        _mockUnitOfWork.Setup(u => u.Repository<Holiday>()).Returns(mockHolidayRepo.Object);
    }

    private void SetupMocksForBulkGeneration(List<Course> courses, List<Lesson> existingLessons, List<Holiday> holidays)
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

        _mockUnitOfWork.Setup(u => u.Courses).Returns(mockCourseRepo.Object);
        _mockUnitOfWork.Setup(u => u.Lessons).Returns(mockLessonRepo.Object);
        _mockUnitOfWork.Setup(u => u.Repository<Holiday>()).Returns(mockHolidayRepo.Object);
    }

    #endregion
}
