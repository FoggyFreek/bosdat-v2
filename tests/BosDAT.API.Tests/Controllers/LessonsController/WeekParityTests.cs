using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using BosDAT.API.Controllers;
using BosDAT.API.Tests.Helpers;
using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Enums;
using BosDAT.Core.Interfaces;

namespace BosDAT.API.Tests.Controllers;

/// <summary>
/// Tests for lesson generation with week parity logic (Odd/Even/All).
/// Week parity uses ISO 8601 week numbers to determine which weeks lessons should be created in.
///
/// Key concepts:
/// - ISO weeks are numbered 1-53, with most years having 52 weeks and some having 53.
/// - Odd parity: lessons only in weeks 1, 3, 5, 7, etc.
/// - Even parity: lessons only in weeks 2, 4, 6, 8, etc.
/// - All parity: lessons every week (simple 14-day interval for biweekly).
/// - 53-week years (like 2026) create edge cases where Week 53 (odd) is followed by Week 1 (odd).
/// </summary>
public class WeekParityTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly BosDAT.API.Controllers.LessonsController _controller;
    private readonly List<Lesson> _createdLessons;

    public WeekParityTests()
    {
        _mockUnitOfWork = MockHelpers.CreateMockUnitOfWork();
        _controller = new BosDAT.API.Controllers.LessonsController(_mockUnitOfWork.Object);
        _createdLessons = new List<Lesson>();
    }

    [Fact]
    public async Task GenerateLessons_BiweeklyOddParity_OnlyCreatesInOddWeeks()
    {
        // Arrange
        var course = CreateTestCourseWithParity(
            CourseFrequency.Biweekly,
            DayOfWeek.Monday,
            WeekParity.Odd);
        SetupMocksForCourse(course, new List<Lesson>(), new List<Holiday>());

        // Start in Week 1 of 2024 (odd) - spans ~8 weeks
        // Week calculations:
        // Jan 1 (Week 1 - Odd) ✓
        // Jan 8 (Week 2 - Even) ✗
        // Jan 15 (Week 3 - Odd) ✓
        // Jan 22 (Week 4 - Even) ✗
        // Jan 29 (Week 5 - Odd) ✓
        // Feb 5 (Week 6 - Even) ✗
        // Feb 12 (Week 7 - Odd) ✓
        // Feb 19 (Week 8 - Even) ✗
        // Feb 26 (Week 9 - Odd) ✓
        // Expected: 5 lessons
        var dto = new GenerateLessonsDto
        {
            CourseId = course.Id,
            StartDate = new DateOnly(2024, 1, 1),  // Week 1 (odd)
            EndDate = new DateOnly(2024, 2, 29),  // ~9 weeks
            SkipHolidays = true
        };

        // Act
        var result = await _controller.GenerateLessons(dto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var generateResult = Assert.IsType<GenerateLessonsResultDto>(okResult.Value);

        Assert.Equal(5, generateResult.LessonsCreated);
        Assert.Equal(0, generateResult.LessonsSkipped);
    }

    [Fact]
    public async Task GenerateLessons_BiweeklyEvenParity_OnlyCreatesInEvenWeeks()
    {
        // Arrange
        var course = CreateTestCourseWithParity(
            CourseFrequency.Biweekly,
            DayOfWeek.Monday,
            WeekParity.Even);
        SetupMocksForCourse(course, new List<Lesson>(), new List<Holiday>());

        // Start in Week 2 of 2024 (even)
        // Week calculations:
        // Jan 8 (Week 2 - Even) ✓
        // Jan 15 (Week 3 - Odd) ✗
        // Jan 22 (Week 4 - Even) ✓
        // Jan 29 (Week 5 - Odd) ✗
        // Feb 5 (Week 6 - Even) ✓
        // Feb 12 (Week 7 - Odd) ✗
        // Feb 19 (Week 8 - Even) ✓
        // Feb 26 (Week 9 - Odd) ✗
        // Expected: 4 lessons
        var dto = new GenerateLessonsDto
        {
            CourseId = course.Id,
            StartDate = new DateOnly(2024, 1, 8),  // Week 2 (even)
            EndDate = new DateOnly(2024, 2, 29),  // ~7 weeks from here
            SkipHolidays = true
        };

        // Act
        var result = await _controller.GenerateLessons(dto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var generateResult = Assert.IsType<GenerateLessonsResultDto>(okResult.Value);

        Assert.Equal(4, generateResult.LessonsCreated);
        Assert.Equal(0, generateResult.LessonsSkipped);
    }

    [Fact]
    public async Task GenerateLessons_BiweeklyOddParity_StartingInEvenWeek_JumpsToOddWeek()
    {
        // Arrange
        var course = CreateTestCourseWithParity(
            CourseFrequency.Biweekly,
            DayOfWeek.Monday,
            WeekParity.Odd);
        SetupMocksForCourse(course, new List<Lesson>(), new List<Holiday>());

        // Start in Week 2 (even) - should skip to Week 3 (odd)
        // Week calculations:
        // Jan 8 (Week 2 - Even) - Start date, but parity doesn't match ✗
        // Jan 15 (Week 3 - Odd) - First match ✓
        // Jan 22 (Week 4 - Even) ✗
        // Jan 29 (Week 5 - Odd) ✓
        // Feb 5 (Week 6 - Even) - Outside range
        // Expected: 2 lessons
        var dto = new GenerateLessonsDto
        {
            CourseId = course.Id,
            StartDate = new DateOnly(2024, 1, 8),  // Week 2 (even)
            EndDate = new DateOnly(2024, 2, 5),   // ~4 weeks
            SkipHolidays = true
        };

        // Act
        var result = await _controller.GenerateLessons(dto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var generateResult = Assert.IsType<GenerateLessonsResultDto>(okResult.Value);

        // First lesson should be Week 3 (Jan 15), then Week 5 (Jan 29) = 2 lessons
        Assert.Equal(2, generateResult.LessonsCreated);
        Assert.Equal(0, generateResult.LessonsSkipped);
    }

    [Fact]
    public async Task GenerateLessons_53WeekYear_BiweeklyOddParity_HandlesYearTransition()
    {
        // 2026 is a 53-week year. Week 53 is odd, Week 1 of 2027 is also odd.
        // This tests the edge case of consecutive odd weeks at year boundary.

        // Arrange
        var course = CreateTestCourseWithParity(
            CourseFrequency.Biweekly,
            DayOfWeek.Monday,
            WeekParity.Odd);
        SetupMocksForCourse(course, new List<Lesson>(), new List<Holiday>());

        // Start late December 2026, cross into January 2027
        // Week calculations:
        // Dec 21, 2026 (Monday) - ISO Year 2026, Week 52 (Even) ✗
        // Dec 28, 2026 (Monday) - ISO Year 2026, Week 53 (Odd) ✓
        // Jan 4, 2027 (Monday) - ISO Year 2027, Week 1 (Odd) ✓
        // Jan 11, 2027 (Monday) - ISO Year 2027, Week 2 (Even) ✗
        // Jan 18, 2027 (Monday) - ISO Year 2027, Week 3 (Odd) ✓
        // Jan 25, 2027 (Monday) - ISO Year 2027, Week 4 (Even) ✗
        // Expected: 3 lessons (Week 53, Week 1, Week 3)
        var dto = new GenerateLessonsDto
        {
            CourseId = course.Id,
            StartDate = new DateOnly(2026, 12, 21),  // Week 52 (even)
            EndDate = new DateOnly(2027, 1, 31),
            SkipHolidays = true
        };

        // Act
        var result = await _controller.GenerateLessons(dto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var generateResult = Assert.IsType<GenerateLessonsResultDto>(okResult.Value);

        // Week 53 (Dec 28) and Week 1 of 2027 (Jan 4) are both odd weeks,
        // which are only 7 days apart. Week 3 (Jan 18) is also included.
        Assert.Equal(3, generateResult.LessonsCreated);
        Assert.Equal(0, generateResult.LessonsSkipped);
    }

    [Fact]
    public async Task GenerateLessons_BiweeklyAllParity_SimpleFourteenDayJump()
    {
        // Arrange
        var course = CreateTestCourseWithParity(
            CourseFrequency.Biweekly,
            DayOfWeek.Monday,
            WeekParity.All);
        SetupMocksForCourse(course, new List<Lesson>(), new List<Holiday>());

        // When parity is "All", biweekly should use simple 14-day intervals
        // Jan 1, Jan 15, Jan 29, Feb 12 = 4 lessons (every 14 days)
        var dto = new GenerateLessonsDto
        {
            CourseId = course.Id,
            StartDate = new DateOnly(2024, 1, 1),  // Monday
            EndDate = new DateOnly(2024, 2, 12),  // Monday, 6 weeks
            SkipHolidays = true
        };

        // Act
        var result = await _controller.GenerateLessons(dto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var generateResult = Assert.IsType<GenerateLessonsResultDto>(okResult.Value);

        // Jan 1, Jan 15, Jan 29, Feb 12 = 4 lessons (every 14 days)
        Assert.Equal(4, generateResult.LessonsCreated);
        Assert.Equal(0, generateResult.LessonsSkipped);
    }

    [Fact]
    public async Task GenerateLessons_Weekly_IgnoresWeekParity()
    {
        // Weekly frequency should ignore parity setting
        // Arrange
        var course = CreateTestCourseWithParity(
            CourseFrequency.Weekly,
            DayOfWeek.Monday,
            WeekParity.Odd);  // Should be ignored for weekly
        SetupMocksForCourse(course, new List<Lesson>(), new List<Holiday>());

        var dto = new GenerateLessonsDto
        {
            CourseId = course.Id,
            StartDate = new DateOnly(2024, 1, 1),
            EndDate = new DateOnly(2024, 1, 29),  // 5 Mondays
            SkipHolidays = true
        };

        // Act
        var result = await _controller.GenerateLessons(dto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var generateResult = Assert.IsType<GenerateLessonsResultDto>(okResult.Value);

        // All 5 Mondays should have lessons, parity is ignored
        Assert.Equal(5, generateResult.LessonsCreated);
        Assert.Equal(0, generateResult.LessonsSkipped);
    }

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
