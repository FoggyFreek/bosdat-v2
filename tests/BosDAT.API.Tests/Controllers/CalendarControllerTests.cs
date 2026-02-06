using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using BosDAT.API.Controllers;
using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Interfaces;
using BosDAT.Core.Utilities;
using BosDAT.API.Tests.Helpers;

namespace BosDAT.API.Tests.Controllers;

public class CalendarControllerTests
{
    private readonly Mock<ICalendarService> _mockCalendarService;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly CalendarController _controller;

    public CalendarControllerTests()
    {
        _mockCalendarService = new Mock<ICalendarService>();
        _mockUnitOfWork = MockHelpers.CreateMockUnitOfWork();
        _controller = new CalendarController(_mockCalendarService.Object, _mockUnitOfWork.Object);
    }

    [Fact]
    public async Task GetWeek_WithNoDate_ReturnsCurrentWeek()
    {
        // Arrange
        var today = DateOnly.FromDateTime(DateTime.Today);
        var expectedWeekStart = IsoDateHelper.GetWeekStart(today);
        var expectedWeekEnd = expectedWeekStart.AddDays(6);

        _mockCalendarService
            .Setup(s => s.GetLessonsForRangeAsync(expectedWeekStart, expectedWeekEnd, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CalendarLessonDto>());
        _mockCalendarService
            .Setup(s => s.GetHolidaysForRangeAsync(expectedWeekStart, expectedWeekEnd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<HolidayDto>());

        // Act
        var result = await _controller.GetWeek(null, null, null, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var weekCalendar = Assert.IsType<WeekCalendarDto>(okResult.Value);
        Assert.Equal(expectedWeekStart, weekCalendar.WeekStart);
        Assert.Equal(expectedWeekEnd, weekCalendar.WeekEnd);
    }

    [Fact]
    public async Task GetWeek_WithSpecificDate_ReturnsCorrectWeek()
    {
        // Arrange
        var targetDate = new DateOnly(2024, 3, 15); // A Friday
        var expectedWeekStart = new DateOnly(2024, 3, 11); // Monday
        var expectedWeekEnd = new DateOnly(2024, 3, 17); // Sunday

        _mockCalendarService
            .Setup(s => s.GetLessonsForRangeAsync(expectedWeekStart, expectedWeekEnd, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CalendarLessonDto>());
        _mockCalendarService
            .Setup(s => s.GetHolidaysForRangeAsync(expectedWeekStart, expectedWeekEnd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<HolidayDto>());

        // Act
        var result = await _controller.GetWeek(targetDate, null, null, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var weekCalendar = Assert.IsType<WeekCalendarDto>(okResult.Value);
        Assert.Equal(expectedWeekStart, weekCalendar.WeekStart);
        Assert.Equal(expectedWeekEnd, weekCalendar.WeekEnd);
    }

    [Fact]
    public async Task GetWeek_WithLessons_ReturnsLessonsFromService()
    {
        // Arrange
        var targetDate = new DateOnly(2024, 3, 15);
        var weekStart = new DateOnly(2024, 3, 11);
        var weekEnd = new DateOnly(2024, 3, 17);

        var lessons = new List<CalendarLessonDto>
        {
            new CalendarLessonDto
            {
                Id = Guid.NewGuid(),
                Date = new DateOnly(2024, 3, 12),
                Title = "Piano - John Doe",
                StartTime = new TimeOnly(10, 0),
                EndTime = new TimeOnly(10, 30)
            }
        };

        _mockCalendarService
            .Setup(s => s.GetLessonsForRangeAsync(weekStart, weekEnd, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lessons);
        _mockCalendarService
            .Setup(s => s.GetHolidaysForRangeAsync(weekStart, weekEnd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<HolidayDto>());

        // Act
        var result = await _controller.GetWeek(targetDate, null, null, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var weekCalendar = Assert.IsType<WeekCalendarDto>(okResult.Value);
        Assert.Single(weekCalendar.Lessons);
        Assert.Equal(new DateOnly(2024, 3, 12), weekCalendar.Lessons[0].Date);
    }

    [Fact]
    public async Task GetWeek_WithHolidays_ReturnsHolidaysFromService()
    {
        // Arrange
        var targetDate = new DateOnly(2024, 3, 15);
        var weekStart = new DateOnly(2024, 3, 11);
        var weekEnd = new DateOnly(2024, 3, 17);

        var holidays = new List<HolidayDto>
        {
            new HolidayDto
            {
                Id = 1,
                Name = "Spring Break",
                StartDate = new DateOnly(2024, 3, 11),
                EndDate = new DateOnly(2024, 3, 15)
            }
        };

        _mockCalendarService
            .Setup(s => s.GetLessonsForRangeAsync(weekStart, weekEnd, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CalendarLessonDto>());
        _mockCalendarService
            .Setup(s => s.GetHolidaysForRangeAsync(weekStart, weekEnd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(holidays);

        // Act
        var result = await _controller.GetWeek(targetDate, null, null, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var weekCalendar = Assert.IsType<WeekCalendarDto>(okResult.Value);
        Assert.Single(weekCalendar.Holidays);
        Assert.Equal("Spring Break", weekCalendar.Holidays[0].Name);
    }

    [Fact]
    public async Task GetTeacherSchedule_WithValidTeacherId_ReturnsTeacherLessons()
    {
        // Arrange
        var teacherId = Guid.NewGuid();
        var teacher = new Teacher { Id = teacherId, FirstName = "Jane", LastName = "Smith", Email = "jane@test.com" };

        var today = DateOnly.FromDateTime(DateTime.Today);
        var weekStart = IsoDateHelper.GetWeekStart(today);
        var weekEnd = weekStart.AddDays(6);

        var mockTeacherRepo = new Mock<ITeacherRepository>();
        mockTeacherRepo.Setup(r => r.GetByIdAsync(teacherId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(teacher);
        _mockUnitOfWork.Setup(u => u.Teachers).Returns(mockTeacherRepo.Object);

        var lessons = new List<CalendarLessonDto>
        {
            new CalendarLessonDto
            {
                Id = Guid.NewGuid(),
                Date = today,
                Title = "Piano - Student",
                StartTime = new TimeOnly(10, 0),
                EndTime = new TimeOnly(10, 30)
            }
        };

        _mockCalendarService
            .Setup(s => s.GetLessonsForRangeAsync(weekStart, weekEnd, teacherId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lessons);
        _mockCalendarService
            .Setup(s => s.GetHolidaysForRangeAsync(weekStart, weekEnd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<HolidayDto>());

        // Act
        var result = await _controller.GetTeacherSchedule(teacherId, null, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var weekCalendar = Assert.IsType<WeekCalendarDto>(okResult.Value);
        Assert.Single(weekCalendar.Lessons);
    }

    [Fact]
    public async Task GetTeacherSchedule_WithInvalidTeacherId_ReturnsNotFound()
    {
        // Arrange
        var teacherId = Guid.NewGuid();

        var mockTeacherRepo = new Mock<ITeacherRepository>();
        mockTeacherRepo.Setup(r => r.GetByIdAsync(teacherId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Teacher?)null);
        _mockUnitOfWork.Setup(u => u.Teachers).Returns(mockTeacherRepo.Object);

        // Act
        var result = await _controller.GetTeacherSchedule(teacherId, null, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task CheckAvailability_WithNoConflicts_ReturnsAvailable()
    {
        // Arrange
        var teacherId = Guid.NewGuid();
        var date = DateOnly.FromDateTime(DateTime.Today.AddDays(1));
        var startTime = new TimeOnly(14, 0);
        var endTime = new TimeOnly(15, 0);

        _mockCalendarService
            .Setup(s => s.CheckConflictsAsync(date, startTime, endTime, teacherId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ConflictDto>());

        // Act
        var result = await _controller.CheckAvailability(date, startTime, endTime, teacherId, null, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var availability = Assert.IsType<AvailabilityDto>(okResult.Value);
        Assert.True(availability.IsAvailable);
        Assert.Empty(availability.Conflicts);
    }

    [Fact]
    public async Task CheckAvailability_WithTeacherConflict_ReturnsConflict()
    {
        // Arrange
        var teacherId = Guid.NewGuid();
        var date = DateOnly.FromDateTime(DateTime.Today.AddDays(1));
        var startTime = new TimeOnly(10, 0);
        var endTime = new TimeOnly(11, 0);

        var conflicts = new List<ConflictDto>
        {
            new ConflictDto
            {
                Type = "Teacher",
                Description = "Teacher has another lesson from 10:30 to 11:30"
            }
        };

        _mockCalendarService
            .Setup(s => s.CheckConflictsAsync(date, startTime, endTime, teacherId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conflicts);

        // Act
        var result = await _controller.CheckAvailability(date, startTime, endTime, teacherId, null, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var availability = Assert.IsType<AvailabilityDto>(okResult.Value);
        Assert.False(availability.IsAvailable);
        Assert.Single(availability.Conflicts);
        Assert.Equal("Teacher", availability.Conflicts[0].Type);
    }

    [Fact]
    public async Task CheckAvailability_WithHoliday_ReturnsConflict()
    {
        // Arrange
        var date = new DateOnly(2024, 12, 25);
        var startTime = new TimeOnly(10, 0);
        var endTime = new TimeOnly(11, 0);

        var conflicts = new List<ConflictDto>
        {
            new ConflictDto
            {
                Type = "Holiday",
                Description = "Date falls within holiday: Christmas"
            }
        };

        _mockCalendarService
            .Setup(s => s.CheckConflictsAsync(date, startTime, endTime, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conflicts);

        // Act
        var result = await _controller.CheckAvailability(date, startTime, endTime, null, null, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var availability = Assert.IsType<AvailabilityDto>(okResult.Value);
        Assert.False(availability.IsAvailable);
        Assert.Single(availability.Conflicts);
        Assert.Equal("Holiday", availability.Conflicts[0].Type);
    }
}
