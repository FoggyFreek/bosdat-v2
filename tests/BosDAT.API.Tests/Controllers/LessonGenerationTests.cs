using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using BosDAT.API.Controllers;
using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Interfaces;
using BosDAT.Core.Interfaces.Services;
using BosDAT.Core.Interfaces.Repositories;
using BosDAT.Infrastructure.Services;
using BosDAT.API.Tests.Helpers;

namespace BosDAT.API.Tests.Controllers;

public class LessonGenerationTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ILessonService> _mockLessonService;
    private readonly BosDAT.API.Controllers.LessonsController _controller;

    public LessonGenerationTests()
    {
        _mockUnitOfWork = MockHelpers.CreateMockUnitOfWork();
        _mockLessonService = new Mock<ILessonService>();
        var lessonGenerationService = new LessonGenerationService(_mockUnitOfWork.Object);
        _controller = new BosDAT.API.Controllers.LessonsController(_mockLessonService.Object, lessonGenerationService, _mockUnitOfWork.Object);
    }

    [Fact]
    public async Task GenerateLessons_WithWeeklyCourse_CreatesWeeklyLessons()
    {
        // Arrange
        var courseId = Guid.NewGuid();
        var teacherId = Guid.NewGuid();

        var instrument = new Instrument { Id = 1, Name = "Piano", Category = InstrumentCategory.Keyboard };
        var courseType = new CourseType
        {
            Id = Guid.NewGuid(),
            Name = "Piano Individual",
            InstrumentId = 1,
            Instrument = instrument,
            Type = CourseTypeCategory.Individual
        };

        var studentId = Guid.NewGuid();
        var student = new Student { Id = studentId, FirstName = "John", LastName = "Doe", Email = "john@test.com" };
        var enrollment = new Enrollment
        {
            Id = Guid.NewGuid(),
            StudentId = studentId,
            CourseId = courseId,
            Status = EnrollmentStatus.Active,
            Student = student
        };

        var course = new Course
        {
            Id = courseId,
            TeacherId = teacherId,
            CourseTypeId = courseType.Id,
            RoomId = 1,
            DayOfWeek = DayOfWeek.Monday, // Monday
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(10, 30),
            Frequency = CourseFrequency.Weekly,
            Status = CourseStatus.Active,
            CourseType = courseType,
            Enrollments = new List<Enrollment> { enrollment }
        };

        var existingLessons = new List<Lesson>();
        var holidays = new List<Holiday>();

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

        _mockUnitOfWork.Setup(u => u.Courses).Returns(mockCourseRepo.Object);
        _mockUnitOfWork.Setup(u => u.Lessons).Returns(mockLessonRepo.Object);
        _mockUnitOfWork.Setup(u => u.Repository<Holiday>()).Returns(mockHolidayRepo.Object);
        _mockUnitOfWork.Setup(u => u.Repository<Absence>()).Returns(mockAbsenceRepo.Object);

        // Generate lessons for 4 weeks (should create 4 lessons on Mondays)
        var startDate = new DateOnly(2024, 3, 4); // Monday
        var endDate = new DateOnly(2024, 3, 25); // Monday (4 weeks later)

        var dto = new GenerateLessonsDto
        {
            CourseId = courseId,
            StartDate = startDate,
            EndDate = endDate,
            SkipHolidays = true
        };

        // Act
        var result = await _controller.GenerateLessons(dto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var generateResult = Assert.IsType<GenerateLessonsResultDto>(okResult.Value);

        // For individual lessons, one lesson per student per week = 4 lessons
        Assert.Equal(4, generateResult.LessonsCreated);
        Assert.Equal(0, generateResult.LessonsSkipped);
    }

    [Fact]
    public async Task GenerateLessons_WithBiweeklyCourse_CreatesBiweeklyLessons()
    {
        // Arrange
        var courseId = Guid.NewGuid();
        var teacherId = Guid.NewGuid();

        var instrument = new Instrument { Id = 1, Name = "Piano", Category = InstrumentCategory.Keyboard };
        var courseType = new CourseType
        {
            Id = Guid.NewGuid(),
            Name = "Piano Group",
            InstrumentId = 1,
            Instrument = instrument,
            Type = CourseTypeCategory.Group
        };

        var course = new Course
        {
            Id = courseId,
            TeacherId = teacherId,
            CourseTypeId = courseType.Id,
            RoomId = 1,
            DayOfWeek = DayOfWeek.Wednesday,
            StartTime = new TimeOnly(14, 0),
            EndTime = new TimeOnly(15, 0),
            Frequency = CourseFrequency.Biweekly,
            Status = CourseStatus.Active,
            CourseType = courseType,
            Enrollments = new List<Enrollment>()
        };

        var existingLessons = new List<Lesson>();
        var holidays = new List<Holiday>();

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

        _mockUnitOfWork.Setup(u => u.Courses).Returns(mockCourseRepo.Object);
        _mockUnitOfWork.Setup(u => u.Lessons).Returns(mockLessonRepo.Object);
        _mockUnitOfWork.Setup(u => u.Repository<Holiday>()).Returns(mockHolidayRepo.Object);
        _mockUnitOfWork.Setup(u => u.Repository<Absence>()).Returns(mockAbsenceRepo.Object);

        // Generate lessons for 8 weeks (biweekly = 4 lessons)
        var startDate = new DateOnly(2024, 3, 6); // Wednesday
        var endDate = new DateOnly(2024, 4, 30); // End of April

        var dto = new GenerateLessonsDto
        {
            CourseId = courseId,
            StartDate = startDate,
            EndDate = endDate,
            SkipHolidays = true
        };

        // Act
        var result = await _controller.GenerateLessons(dto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var generateResult = Assert.IsType<GenerateLessonsResultDto>(okResult.Value);

        // Biweekly: March 6, 20, April 3, 17 = 4 lessons
        Assert.Equal(4, generateResult.LessonsCreated);
    }

    [Fact]
    public async Task GenerateLessons_SkipsHolidays_WhenEnabled()
    {
        // Arrange
        var courseId = Guid.NewGuid();
        var teacherId = Guid.NewGuid();

        var instrument = new Instrument { Id = 1, Name = "Piano", Category = InstrumentCategory.Keyboard };
        var courseType = new CourseType
        {
            Id = Guid.NewGuid(),
            Name = "Piano Group",
            InstrumentId = 1,
            Instrument = instrument,
            Type = CourseTypeCategory.Group
        };

        var course = new Course
        {
            Id = courseId,
            TeacherId = teacherId,
            CourseTypeId = courseType.Id,
            RoomId = 1,
            DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(10, 30),
            Frequency = CourseFrequency.Weekly,
            Status = CourseStatus.Active,
            CourseType = courseType,
            Enrollments = new List<Enrollment>()
        };

        var existingLessons = new List<Lesson>();

        // Holiday covering one of the Mondays
        var holidays = new List<Holiday>
        {
            new Holiday
            {
                Id = 1,
                Name = "Spring Break",
                StartDate = new DateOnly(2024, 3, 11), // Monday
                EndDate = new DateOnly(2024, 3, 15)    // Friday
            }
        };

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

        _mockUnitOfWork.Setup(u => u.Courses).Returns(mockCourseRepo.Object);
        _mockUnitOfWork.Setup(u => u.Lessons).Returns(mockLessonRepo.Object);
        _mockUnitOfWork.Setup(u => u.Repository<Holiday>()).Returns(mockHolidayRepo.Object);
        _mockUnitOfWork.Setup(u => u.Repository<Absence>()).Returns(mockAbsenceRepo.Object);

        // Generate lessons for 4 Mondays, one falls on holiday
        var startDate = new DateOnly(2024, 3, 4);  // Monday
        var endDate = new DateOnly(2024, 3, 25);   // Monday

        var dto = new GenerateLessonsDto
        {
            CourseId = courseId,
            StartDate = startDate,
            EndDate = endDate,
            SkipHolidays = true
        };

        // Act
        var result = await _controller.GenerateLessons(dto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var generateResult = Assert.IsType<GenerateLessonsResultDto>(okResult.Value);

        // 4 Mondays, 1 skipped due to holiday = 3 created, 1 skipped
        Assert.Equal(3, generateResult.LessonsCreated);
        Assert.Equal(1, generateResult.LessonsSkipped);
    }

    [Fact]
    public async Task GenerateLessons_SkipsExistingLessons()
    {
        // Arrange
        var courseId = Guid.NewGuid();
        var teacherId = Guid.NewGuid();

        var instrument = new Instrument { Id = 1, Name = "Piano", Category = InstrumentCategory.Keyboard };
        var courseType = new CourseType
        {
            Id = Guid.NewGuid(),
            Name = "Piano Group",
            InstrumentId = 1,
            Instrument = instrument,
            Type = CourseTypeCategory.Group
        };

        var course = new Course
        {
            Id = courseId,
            TeacherId = teacherId,
            CourseTypeId = courseType.Id,
            RoomId = 1,
            DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(10, 30),
            Frequency = CourseFrequency.Weekly,
            Status = CourseStatus.Active,
            CourseType = courseType,
            Enrollments = new List<Enrollment>()
        };

        // One lesson already exists
        var existingLessons = new List<Lesson>
        {
            new Lesson
            {
                Id = Guid.NewGuid(),
                CourseId = courseId,
                TeacherId = teacherId,
                ScheduledDate = new DateOnly(2024, 3, 11), // Second Monday
                StartTime = new TimeOnly(10, 0),
                EndTime = new TimeOnly(10, 30)
            }
        };

        var holidays = new List<Holiday>();

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

        _mockUnitOfWork.Setup(u => u.Courses).Returns(mockCourseRepo.Object);
        _mockUnitOfWork.Setup(u => u.Lessons).Returns(mockLessonRepo.Object);
        _mockUnitOfWork.Setup(u => u.Repository<Holiday>()).Returns(mockHolidayRepo.Object);
        _mockUnitOfWork.Setup(u => u.Repository<Absence>()).Returns(mockAbsenceRepo.Object);

        var startDate = new DateOnly(2024, 3, 4);  // Monday
        var endDate = new DateOnly(2024, 3, 25);   // Monday (4 weeks)

        var dto = new GenerateLessonsDto
        {
            CourseId = courseId,
            StartDate = startDate,
            EndDate = endDate,
            SkipHolidays = true
        };

        // Act
        var result = await _controller.GenerateLessons(dto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var generateResult = Assert.IsType<GenerateLessonsResultDto>(okResult.Value);

        // 4 Mondays, 1 already exists = 3 created, 1 skipped
        Assert.Equal(3, generateResult.LessonsCreated);
        Assert.Equal(1, generateResult.LessonsSkipped);
    }

    [Fact]
    public async Task GenerateLessons_WithNonExistentCourse_ReturnsBadRequest()
    {
        // Arrange
        var courseId = Guid.NewGuid();

        var mockCourseRepo = new Mock<ICourseRepository>();
        mockCourseRepo.Setup(r => r.Query())
            .Returns(new List<Course>().AsQueryable().BuildMockDbSet().Object);

        _mockUnitOfWork.Setup(u => u.Courses).Returns(mockCourseRepo.Object);

        var dto = new GenerateLessonsDto
        {
            CourseId = courseId,
            StartDate = new DateOnly(2024, 3, 4),
            EndDate = new DateOnly(2024, 3, 25)
        };

        // Act
        var result = await _controller.GenerateLessons(dto, CancellationToken.None);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task BulkGenerateLessons_GeneratesForAllActiveCourses()
    {
        // Arrange
        var teacherId = Guid.NewGuid();

        var instrument = new Instrument { Id = 1, Name = "Piano", Category = InstrumentCategory.Keyboard };
        var courseType = new CourseType
        {
            Id = Guid.NewGuid(),
            Name = "Piano Group",
            InstrumentId = 1,
            Instrument = instrument,
            Type = CourseTypeCategory.Group
        };

        var courses = new List<Course>
        {
            new Course
            {
                Id = Guid.NewGuid(),
                TeacherId = teacherId,
                CourseTypeId = courseType.Id,
                DayOfWeek = DayOfWeek.Monday,
                StartTime = new TimeOnly(10, 0),
                EndTime = new TimeOnly(10, 30),
                Frequency = CourseFrequency.Weekly,
                Status = CourseStatus.Active,
                CourseType = courseType,
                Enrollments = new List<Enrollment>()
            },
            new Course
            {
                Id = Guid.NewGuid(),
                TeacherId = teacherId,
                CourseTypeId = courseType.Id,
                DayOfWeek = DayOfWeek.Wednesday,
                StartTime = new TimeOnly(14, 0),
                EndTime = new TimeOnly(14, 30),
                Frequency = CourseFrequency.Weekly,
                Status = CourseStatus.Active,
                CourseType = courseType,
                Enrollments = new List<Enrollment>()
            },
            new Course
            {
                Id = Guid.NewGuid(),
                TeacherId = teacherId,
                CourseTypeId = courseType.Id,
                DayOfWeek = DayOfWeek.Friday,
                StartTime = new TimeOnly(9, 0),
                EndTime = new TimeOnly(9, 30),
                Frequency = CourseFrequency.Weekly,
                Status = CourseStatus.Paused, // Not active, should be excluded
                CourseType = courseType,
                Enrollments = new List<Enrollment>()
            }
        };

        var existingLessons = new List<Lesson>();
        var holidays = new List<Holiday>();

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

        _mockUnitOfWork.Setup(u => u.Courses).Returns(mockCourseRepo.Object);
        _mockUnitOfWork.Setup(u => u.Lessons).Returns(mockLessonRepo.Object);
        _mockUnitOfWork.Setup(u => u.Repository<Holiday>()).Returns(mockHolidayRepo.Object);
        _mockUnitOfWork.Setup(u => u.Repository<Absence>()).Returns(mockAbsenceRepo.Object);

        // Generate for 2 weeks
        var startDate = new DateOnly(2024, 3, 4);  // Monday
        var endDate = new DateOnly(2024, 3, 17);   // Sunday

        var dto = new BulkGenerateLessonsDto
        {
            StartDate = startDate,
            EndDate = endDate,
            SkipHolidays = true
        };

        // Act
        var result = await _controller.GenerateLessonsBulk(dto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var bulkResult = Assert.IsType<BulkGenerateLessonsResultDto>(okResult.Value);

        // 2 active courses, 2 weeks each = at least 4 lessons per course
        Assert.Equal(2, bulkResult.TotalCoursesProcessed);
        Assert.True(bulkResult.TotalLessonsCreated >= 4);
    }
}
