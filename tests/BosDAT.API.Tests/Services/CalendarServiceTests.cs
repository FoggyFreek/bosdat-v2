using Moq;
using Xunit;
using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Interfaces;
using BosDAT.Core.Interfaces.Services;
using BosDAT.Core.Interfaces.Repositories;
using BosDAT.Infrastructure.Services;
using BosDAT.API.Tests.Helpers;

namespace BosDAT.API.Tests.Services;

public class CalendarServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IAbsenceService> _mockAbsenceService;
    private readonly CalendarService _service;

    public CalendarServiceTests()
    {
        _mockUnitOfWork = MockHelpers.CreateMockUnitOfWork();
        _mockAbsenceService = new Mock<IAbsenceService>();
        _service = new CalendarService(_mockUnitOfWork.Object, _mockAbsenceService.Object);
    }

    [Fact]
    public async Task GetLessonsForRangeAsync_ReturnsLessonsInRange()
    {
        // Arrange
        var instrument = new Instrument { Id = 1, Name = "Piano", Category = InstrumentCategory.Keyboard };
        var courseType = new CourseType { Id = Guid.NewGuid(), Name = "Piano 30 min", InstrumentId = 1, Instrument = instrument };
        var teacher = new Teacher { Id = Guid.NewGuid(), FirstName = "Jane", LastName = "Smith", Email = "jane@test.com" };
        var student = new Student { Id = Guid.NewGuid(), FirstName = "John", LastName = "Doe", Email = "john@test.com" };
        var course = new Course { Id = Guid.NewGuid(), CourseType = courseType, Teacher = teacher };

        var lessons = new List<Lesson>
        {
            new Lesson
            {
                Id = Guid.NewGuid(),
                CourseId = course.Id,
                StudentId = student.Id,
                TeacherId = teacher.Id,
                ScheduledDate = new DateOnly(2024, 3, 12),
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
                ScheduledDate = new DateOnly(2024, 3, 20),
                StartTime = new TimeOnly(10, 0),
                EndTime = new TimeOnly(10, 30),
                Status = LessonStatus.Scheduled,
                Course = course,
                Student = student,
                Teacher = teacher
            }
        };

        var mockLessonRepo = new Mock<ILessonRepository>();
        mockLessonRepo.Setup(r => r.Query())
            .Returns(lessons.AsQueryable().BuildMockDbSet().Object);
        _mockUnitOfWork.Setup(u => u.Lessons).Returns(mockLessonRepo.Object);

        // Act
        var result = await _service.GetLessonsForRangeAsync(
            new DateOnly(2024, 3, 11), new DateOnly(2024, 3, 17),
            null, null, CancellationToken.None);

        // Assert
        Assert.Single(result);
        Assert.Equal(new DateOnly(2024, 3, 12), result[0].Date);
        Assert.Equal("Piano - John Doe", result[0].Title);
    }

    [Fact]
    public async Task GetLessonsForRangeAsync_FiltersTeacherId()
    {
        // Arrange
        var instrument = new Instrument { Id = 1, Name = "Piano", Category = InstrumentCategory.Keyboard };
        var courseType = new CourseType { Id = Guid.NewGuid(), Name = "Piano 30 min", InstrumentId = 1, Instrument = instrument };
        var teacher1 = new Teacher { Id = Guid.NewGuid(), FirstName = "Jane", LastName = "Smith", Email = "jane@test.com" };
        var teacher2 = new Teacher { Id = Guid.NewGuid(), FirstName = "Bob", LastName = "Jones", Email = "bob@test.com" };
        var student = new Student { Id = Guid.NewGuid(), FirstName = "John", LastName = "Doe", Email = "john@test.com" };
        var course1 = new Course { Id = Guid.NewGuid(), CourseType = courseType, Teacher = teacher1 };
        var course2 = new Course { Id = Guid.NewGuid(), CourseType = courseType, Teacher = teacher2 };

        var lessons = new List<Lesson>
        {
            new Lesson
            {
                Id = Guid.NewGuid(),
                CourseId = course1.Id,
                StudentId = student.Id,
                TeacherId = teacher1.Id,
                ScheduledDate = new DateOnly(2024, 3, 12),
                StartTime = new TimeOnly(10, 0),
                EndTime = new TimeOnly(10, 30),
                Status = LessonStatus.Scheduled,
                Course = course1,
                Student = student,
                Teacher = teacher1
            },
            new Lesson
            {
                Id = Guid.NewGuid(),
                CourseId = course2.Id,
                StudentId = student.Id,
                TeacherId = teacher2.Id,
                ScheduledDate = new DateOnly(2024, 3, 12),
                StartTime = new TimeOnly(11, 0),
                EndTime = new TimeOnly(11, 30),
                Status = LessonStatus.Scheduled,
                Course = course2,
                Student = student,
                Teacher = teacher2
            }
        };

        var mockLessonRepo = new Mock<ILessonRepository>();
        mockLessonRepo.Setup(r => r.Query())
            .Returns(lessons.AsQueryable().BuildMockDbSet().Object);
        _mockUnitOfWork.Setup(u => u.Lessons).Returns(mockLessonRepo.Object);

        // Act
        var result = await _service.GetLessonsForRangeAsync(
            new DateOnly(2024, 3, 11), new DateOnly(2024, 3, 17),
            teacher1.Id, null, CancellationToken.None);

        // Assert
        Assert.Single(result);
        Assert.Equal("Jane Smith", result[0].TeacherName);
    }

    [Fact]
    public async Task GetLessonsForRangeAsync_GroupLesson_ShowsGroupTitle()
    {
        // Arrange
        var instrument = new Instrument { Id = 1, Name = "Guitar", Category = InstrumentCategory.String };
        var courseType = new CourseType { Id = Guid.NewGuid(), Name = "Guitar Group", InstrumentId = 1, Instrument = instrument };
        var teacher = new Teacher { Id = Guid.NewGuid(), FirstName = "Jane", LastName = "Smith", Email = "jane@test.com" };
        var course = new Course { Id = Guid.NewGuid(), CourseType = courseType, Teacher = teacher };

        var lessons = new List<Lesson>
        {
            new Lesson
            {
                Id = Guid.NewGuid(),
                CourseId = course.Id,
                StudentId = null,
                TeacherId = teacher.Id,
                ScheduledDate = new DateOnly(2024, 3, 12),
                StartTime = new TimeOnly(10, 0),
                EndTime = new TimeOnly(11, 0),
                Status = LessonStatus.Scheduled,
                Course = course,
                Student = null,
                Teacher = teacher
            }
        };

        var mockLessonRepo = new Mock<ILessonRepository>();
        mockLessonRepo.Setup(r => r.Query())
            .Returns(lessons.AsQueryable().BuildMockDbSet().Object);
        _mockUnitOfWork.Setup(u => u.Lessons).Returns(mockLessonRepo.Object);

        // Act
        var result = await _service.GetLessonsForRangeAsync(
            new DateOnly(2024, 3, 11), new DateOnly(2024, 3, 17),
            null, null, CancellationToken.None);

        // Assert
        Assert.Single(result);
        Assert.Equal("Guitar - Group", result[0].Title);
        Assert.Null(result[0].StudentName);
    }

    [Fact]
    public async Task GetHolidaysForRangeAsync_ReturnsHolidaysInRange()
    {
        // Arrange
        var holidays = new List<Holiday>
        {
            new Holiday { Id = 1, Name = "Spring Break", StartDate = new DateOnly(2024, 3, 11), EndDate = new DateOnly(2024, 3, 15) },
            new Holiday { Id = 2, Name = "Summer", StartDate = new DateOnly(2024, 7, 1), EndDate = new DateOnly(2024, 8, 31) }
        };

        var mockHolidayRepo = MockHelpers.CreateMockRepository(holidays);
        _mockUnitOfWork.Setup(u => u.Repository<Holiday>()).Returns(mockHolidayRepo.Object);

        // Act
        var result = await _service.GetHolidaysForRangeAsync(
            new DateOnly(2024, 3, 11), new DateOnly(2024, 3, 17), CancellationToken.None);

        // Assert
        Assert.Single(result);
        Assert.Equal("Spring Break", result[0].Name);
    }

    [Fact]
    public async Task GetHolidaysForRangeAsync_ReturnsEmpty_WhenNoHolidays()
    {
        // Arrange
        var holidays = new List<Holiday>();

        var mockHolidayRepo = MockHelpers.CreateMockRepository(holidays);
        _mockUnitOfWork.Setup(u => u.Repository<Holiday>()).Returns(mockHolidayRepo.Object);

        // Act
        var result = await _service.GetHolidaysForRangeAsync(
            new DateOnly(2024, 3, 11), new DateOnly(2024, 3, 17), CancellationToken.None);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task CheckConflictsAsync_WithTeacherConflict_ReturnsConflict()
    {
        // Arrange
        var teacherId = Guid.NewGuid();
        var date = new DateOnly(2024, 3, 12);

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
        var result = await _service.CheckConflictsAsync(
            date, new TimeOnly(10, 0), new TimeOnly(11, 0),
            teacherId, null, CancellationToken.None);

        // Assert
        Assert.Single(result);
        Assert.Equal("Teacher", result[0].Type);
    }

    [Fact]
    public async Task CheckConflictsAsync_WithRoomConflict_ReturnsConflict()
    {
        // Arrange
        var roomId = 1;
        var date = new DateOnly(2024, 3, 12);

        var existingLesson = new Lesson
        {
            Id = Guid.NewGuid(),
            RoomId = roomId,
            ScheduledDate = date,
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(11, 0),
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
        var result = await _service.CheckConflictsAsync(
            date, new TimeOnly(10, 30), new TimeOnly(11, 30),
            null, roomId, CancellationToken.None);

        // Assert
        Assert.Single(result);
        Assert.Equal("Room", result[0].Type);
    }

    [Fact]
    public async Task CheckConflictsAsync_WithHoliday_ReturnsConflict()
    {
        // Arrange
        var date = new DateOnly(2024, 12, 25);
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

        var lessons = new List<Lesson>();

        var mockLessonRepo = new Mock<ILessonRepository>();
        mockLessonRepo.Setup(r => r.Query())
            .Returns(lessons.AsQueryable().BuildMockDbSet().Object);
        var mockHolidayRepo = MockHelpers.CreateMockRepository(holidays);

        _mockUnitOfWork.Setup(u => u.Lessons).Returns(mockLessonRepo.Object);
        _mockUnitOfWork.Setup(u => u.Repository<Holiday>()).Returns(mockHolidayRepo.Object);

        // Act
        var result = await _service.CheckConflictsAsync(
            date, new TimeOnly(10, 0), new TimeOnly(11, 0),
            null, null, CancellationToken.None);

        // Assert
        Assert.Single(result);
        Assert.Equal("Holiday", result[0].Type);
        Assert.Contains("Christmas", result[0].Description);
    }

    [Fact]
    public async Task CheckConflictsAsync_WithNoConflicts_ReturnsEmpty()
    {
        // Arrange
        var teacherId = Guid.NewGuid();
        var date = new DateOnly(2024, 3, 12);

        var lessons = new List<Lesson>();
        var holidays = new List<Holiday>();

        var mockLessonRepo = new Mock<ILessonRepository>();
        mockLessonRepo.Setup(r => r.Query())
            .Returns(lessons.AsQueryable().BuildMockDbSet().Object);
        var mockHolidayRepo = MockHelpers.CreateMockRepository(holidays);

        _mockUnitOfWork.Setup(u => u.Lessons).Returns(mockLessonRepo.Object);
        _mockUnitOfWork.Setup(u => u.Repository<Holiday>()).Returns(mockHolidayRepo.Object);

        // Act
        var result = await _service.CheckConflictsAsync(
            date, new TimeOnly(14, 0), new TimeOnly(15, 0),
            teacherId, null, CancellationToken.None);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task CheckConflictsAsync_CancelledLessons_DoNotConflict()
    {
        // Arrange
        var teacherId = Guid.NewGuid();
        var date = new DateOnly(2024, 3, 12);

        var cancelledLesson = new Lesson
        {
            Id = Guid.NewGuid(),
            TeacherId = teacherId,
            ScheduledDate = date,
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(11, 0),
            Status = LessonStatus.Cancelled
        };

        var lessons = new List<Lesson> { cancelledLesson };
        var holidays = new List<Holiday>();

        var mockLessonRepo = new Mock<ILessonRepository>();
        mockLessonRepo.Setup(r => r.Query())
            .Returns(lessons.AsQueryable().BuildMockDbSet().Object);
        var mockHolidayRepo = MockHelpers.CreateMockRepository(holidays);

        _mockUnitOfWork.Setup(u => u.Lessons).Returns(mockLessonRepo.Object);
        _mockUnitOfWork.Setup(u => u.Repository<Holiday>()).Returns(mockHolidayRepo.Object);

        // Act
        var result = await _service.CheckConflictsAsync(
            date, new TimeOnly(10, 0), new TimeOnly(11, 0),
            teacherId, null, CancellationToken.None);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task CheckConflictsAsync_NonOverlappingTimes_DoNotConflict()
    {
        // Arrange
        var teacherId = Guid.NewGuid();
        var date = new DateOnly(2024, 3, 12);

        var existingLesson = new Lesson
        {
            Id = Guid.NewGuid(),
            TeacherId = teacherId,
            ScheduledDate = date,
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(11, 0),
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

        // Act - request time slot after existing lesson
        var result = await _service.CheckConflictsAsync(
            date, new TimeOnly(11, 0), new TimeOnly(12, 0),
            teacherId, null, CancellationToken.None);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task CheckConflictsAsync_MultipleConflicts_ReturnsAll()
    {
        // Arrange
        var teacherId = Guid.NewGuid();
        var roomId = 1;
        var date = new DateOnly(2024, 12, 25);

        var teacherLesson = new Lesson
        {
            Id = Guid.NewGuid(),
            TeacherId = teacherId,
            ScheduledDate = date,
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(11, 0),
            Status = LessonStatus.Scheduled
        };

        var roomLesson = new Lesson
        {
            Id = Guid.NewGuid(),
            RoomId = roomId,
            ScheduledDate = date,
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(11, 0),
            Status = LessonStatus.Scheduled
        };

        var lessons = new List<Lesson> { teacherLesson, roomLesson };
        var holidays = new List<Holiday>
        {
            new Holiday { Id = 1, Name = "Christmas", StartDate = new DateOnly(2024, 12, 24), EndDate = new DateOnly(2024, 12, 26) }
        };

        var mockLessonRepo = new Mock<ILessonRepository>();
        mockLessonRepo.Setup(r => r.Query())
            .Returns(lessons.AsQueryable().BuildMockDbSet().Object);
        var mockHolidayRepo = MockHelpers.CreateMockRepository(holidays);

        _mockUnitOfWork.Setup(u => u.Lessons).Returns(mockLessonRepo.Object);
        _mockUnitOfWork.Setup(u => u.Repository<Holiday>()).Returns(mockHolidayRepo.Object);

        // Act
        var result = await _service.CheckConflictsAsync(
            date, new TimeOnly(10, 0), new TimeOnly(11, 0),
            teacherId, roomId, CancellationToken.None);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Contains(result, c => c.Type == "Teacher");
        Assert.Contains(result, c => c.Type == "Room");
        Assert.Contains(result, c => c.Type == "Holiday");
    }
}
