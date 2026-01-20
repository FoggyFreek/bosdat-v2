using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using BosDAT.API.Controllers;
using BosDAT.Core.Entities;
using BosDAT.Core.Interfaces;
using BosDAT.API.Tests.Helpers;

namespace BosDAT.API.Tests.Controllers;

public class CalendarControllerTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly CalendarController _controller;

    public CalendarControllerTests()
    {
        _mockUnitOfWork = MockHelpers.CreateMockUnitOfWork();
        _controller = new CalendarController(_mockUnitOfWork.Object);
    }

    [Fact]
    public async Task GetWeek_WithNoDate_ReturnsCurrentWeek()
    {
        // Arrange
        var today = DateTime.Today;
        var daysFromMonday = ((int)today.DayOfWeek - 1 + 7) % 7;
        var expectedWeekStart = DateOnly.FromDateTime(today.AddDays(-daysFromMonday));
        var expectedWeekEnd = expectedWeekStart.AddDays(6);

        var lessons = new List<Lesson>();
        var holidays = new List<Holiday>();

        var mockLessonRepo = new Mock<ILessonRepository>();
        mockLessonRepo.Setup(r => r.Query())
            .Returns(lessons.AsQueryable().BuildMockDbSet().Object);

        var mockHolidayRepo = MockHelpers.CreateMockRepository(holidays);

        _mockUnitOfWork.Setup(u => u.Lessons).Returns(mockLessonRepo.Object);
        _mockUnitOfWork.Setup(u => u.Repository<Holiday>()).Returns(mockHolidayRepo.Object);

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

        var lessons = new List<Lesson>();
        var holidays = new List<Holiday>();

        var mockLessonRepo = new Mock<ILessonRepository>();
        mockLessonRepo.Setup(r => r.Query())
            .Returns(lessons.AsQueryable().BuildMockDbSet().Object);

        var mockHolidayRepo = MockHelpers.CreateMockRepository(holidays);

        _mockUnitOfWork.Setup(u => u.Lessons).Returns(mockLessonRepo.Object);
        _mockUnitOfWork.Setup(u => u.Repository<Holiday>()).Returns(mockHolidayRepo.Object);

        // Act
        var result = await _controller.GetWeek(targetDate, null, null, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var weekCalendar = Assert.IsType<WeekCalendarDto>(okResult.Value);
        Assert.Equal(expectedWeekStart, weekCalendar.WeekStart);
        Assert.Equal(expectedWeekEnd, weekCalendar.WeekEnd);
    }

    [Fact]
    public async Task GetWeek_WithLessons_ReturnsLessonsInRange()
    {
        // Arrange
        var targetDate = new DateOnly(2024, 3, 15);
        var weekStart = new DateOnly(2024, 3, 11);

        var instrument = new Instrument { Id = 1, Name = "Piano", Category = InstrumentCategory.Keyboard };
        var lessonType = new LessonType { Id = 1, Name = "Piano 30 min", InstrumentId = 1, Instrument = instrument };
        var teacher = new Teacher { Id = Guid.NewGuid(), FirstName = "Jane", LastName = "Smith", Email = "jane@test.com" };
        var student = new Student { Id = Guid.NewGuid(), FirstName = "John", LastName = "Doe", Email = "john@test.com" };
        var course = new Course { Id = Guid.NewGuid(), LessonType = lessonType, Teacher = teacher };

        var lessons = new List<Lesson>
        {
            new Lesson
            {
                Id = Guid.NewGuid(),
                CourseId = course.Id,
                StudentId = student.Id,
                TeacherId = teacher.Id,
                ScheduledDate = new DateOnly(2024, 3, 12), // Tuesday, within week
                StartTime = new TimeOnly(10, 0),
                EndTime = new TimeOnly(10, 30),
                Status = LessonStatus.Scheduled,
                Course = course,
                Student = student,
                Teacher = teacher
            },
            new Lesson
            {
                Id = Guid.NewGuid(),
                CourseId = course.Id,
                StudentId = student.Id,
                TeacherId = teacher.Id,
                ScheduledDate = new DateOnly(2024, 3, 20), // Outside week
                StartTime = new TimeOnly(10, 0),
                EndTime = new TimeOnly(10, 30),
                Status = LessonStatus.Scheduled,
                Course = course,
                Student = student,
                Teacher = teacher
            }
        };

        var holidays = new List<Holiday>();

        var mockLessonRepo = new Mock<ILessonRepository>();
        mockLessonRepo.Setup(r => r.Query())
            .Returns(lessons.AsQueryable().BuildMockDbSet().Object);

        var mockHolidayRepo = MockHelpers.CreateMockRepository(holidays);

        _mockUnitOfWork.Setup(u => u.Lessons).Returns(mockLessonRepo.Object);
        _mockUnitOfWork.Setup(u => u.Repository<Holiday>()).Returns(mockHolidayRepo.Object);

        // Act
        var result = await _controller.GetWeek(targetDate, null, null, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var weekCalendar = Assert.IsType<WeekCalendarDto>(okResult.Value);
        Assert.Single(weekCalendar.Lessons);
        Assert.Equal(new DateOnly(2024, 3, 12), weekCalendar.Lessons[0].Date);
    }

    [Fact]
    public async Task GetWeek_WithHolidays_ReturnsHolidaysInRange()
    {
        // Arrange
        var targetDate = new DateOnly(2024, 3, 15);

        var lessons = new List<Lesson>();
        var holidays = new List<Holiday>
        {
            new Holiday
            {
                Id = 1,
                Name = "Spring Break",
                StartDate = new DateOnly(2024, 3, 11),
                EndDate = new DateOnly(2024, 3, 15)
            }
        };

        var mockLessonRepo = new Mock<ILessonRepository>();
        mockLessonRepo.Setup(r => r.Query())
            .Returns(lessons.AsQueryable().BuildMockDbSet().Object);

        var mockHolidayRepo = MockHelpers.CreateMockRepository(holidays);

        _mockUnitOfWork.Setup(u => u.Lessons).Returns(mockLessonRepo.Object);
        _mockUnitOfWork.Setup(u => u.Repository<Holiday>()).Returns(mockHolidayRepo.Object);

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

        var instrument = new Instrument { Id = 1, Name = "Piano", Category = InstrumentCategory.Keyboard };
        var lessonType = new LessonType { Id = 1, Name = "Piano 30 min", InstrumentId = 1, Instrument = instrument };
        var course = new Course { Id = Guid.NewGuid(), LessonType = lessonType, Teacher = teacher };

        var lessons = new List<Lesson>
        {
            new Lesson
            {
                Id = Guid.NewGuid(),
                CourseId = course.Id,
                TeacherId = teacherId,
                ScheduledDate = DateOnly.FromDateTime(DateTime.Today),
                StartTime = new TimeOnly(10, 0),
                EndTime = new TimeOnly(10, 30),
                Status = LessonStatus.Scheduled,
                Course = course,
                Teacher = teacher
            }
        };

        var holidays = new List<Holiday>();

        var mockTeacherRepo = new Mock<ITeacherRepository>();
        mockTeacherRepo.Setup(r => r.GetByIdAsync(teacherId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(teacher);

        var mockLessonRepo = new Mock<ILessonRepository>();
        mockLessonRepo.Setup(r => r.Query())
            .Returns(lessons.AsQueryable().BuildMockDbSet().Object);

        var mockHolidayRepo = MockHelpers.CreateMockRepository(holidays);

        _mockUnitOfWork.Setup(u => u.Teachers).Returns(mockTeacherRepo.Object);
        _mockUnitOfWork.Setup(u => u.Lessons).Returns(mockLessonRepo.Object);
        _mockUnitOfWork.Setup(u => u.Repository<Holiday>()).Returns(mockHolidayRepo.Object);

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

        var lessons = new List<Lesson>(); // No existing lessons
        var holidays = new List<Holiday>(); // No holidays

        var mockLessonRepo = new Mock<ILessonRepository>();
        mockLessonRepo.Setup(r => r.Query())
            .Returns(lessons.AsQueryable().BuildMockDbSet().Object);

        var mockHolidayRepo = MockHelpers.CreateMockRepository(holidays);

        _mockUnitOfWork.Setup(u => u.Lessons).Returns(mockLessonRepo.Object);
        _mockUnitOfWork.Setup(u => u.Repository<Holiday>()).Returns(mockHolidayRepo.Object);

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

        var existingLesson = new Lesson
        {
            Id = Guid.NewGuid(),
            TeacherId = teacherId,
            ScheduledDate = date,
            StartTime = new TimeOnly(10, 30),
            EndTime = new TimeOnly(11, 30),
            Status = LessonStatus.Scheduled
        };

        var lessons = new List<Lesson> { existingLesson };
        var holidays = new List<Holiday>();

        var mockLessonRepo = new Mock<ILessonRepository>();
        mockLessonRepo.Setup(r => r.Query())
            .Returns(lessons.AsQueryable().BuildMockDbSet().Object);

        var mockHolidayRepo = MockHelpers.CreateMockRepository(holidays);

        _mockUnitOfWork.Setup(u => u.Lessons).Returns(mockLessonRepo.Object);
        _mockUnitOfWork.Setup(u => u.Repository<Holiday>()).Returns(mockHolidayRepo.Object);

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

        var lessons = new List<Lesson>();
        var holidays = new List<Holiday>
        {
            new Holiday
            {
                Id = 1,
                Name = "Christmas",
                StartDate = new DateOnly(2024, 12, 24),
                EndDate = new DateOnly(2024, 12, 26)
            }
        };

        var mockLessonRepo = new Mock<ILessonRepository>();
        mockLessonRepo.Setup(r => r.Query())
            .Returns(lessons.AsQueryable().BuildMockDbSet().Object);

        var mockHolidayRepo = MockHelpers.CreateMockRepository(holidays);

        _mockUnitOfWork.Setup(u => u.Lessons).Returns(mockLessonRepo.Object);
        _mockUnitOfWork.Setup(u => u.Repository<Holiday>()).Returns(mockHolidayRepo.Object);

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
