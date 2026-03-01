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

public class LessonsControllerTests
{
    private readonly Mock<ILessonService> _mockLessonService;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly BosDAT.API.Controllers.LessonsController _controller;
    private readonly BosDAT.API.Controllers.LessonGenerationController _generationController;

    public LessonsControllerTests()
    {
        _mockLessonService = new Mock<ILessonService>();
        _mockUnitOfWork = MockHelpers.CreateMockUnitOfWork();
        var lessonGenerationService = new LessonGenerationService(_mockUnitOfWork.Object);
        _controller = new BosDAT.API.Controllers.LessonsController(_mockLessonService.Object);
        _generationController = new BosDAT.API.Controllers.LessonGenerationController(lessonGenerationService, _mockUnitOfWork.Object);
    }

    [Fact]
    public async Task GetByStudent_WithValidStudentId_ReturnsLessons()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var lessons = new List<LessonDto>
        {
            new LessonDto
            {
                Id = Guid.NewGuid(),
                CourseId = Guid.NewGuid(),
                StudentId = studentId,
                StudentName = "John Doe",
                TeacherId = Guid.NewGuid(),
                TeacherName = "Jane Smith",
                RoomId = 1,
                RoomName = "Room A",
                CourseTypeName = "Piano 30 min",
                InstrumentName = "Piano",
                ScheduledDate = DateOnly.FromDateTime(DateTime.Today),
                StartTime = new TimeOnly(10, 0),
                EndTime = new TimeOnly(10, 30),
                Status = LessonStatus.Scheduled
            }
        };

        _mockLessonService.Setup(s => s.GetByStudentAsync(studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lessons);

        // Act
        var result = await _controller.GetByStudent(studentId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedLessons = Assert.IsAssignableFrom<IEnumerable<LessonDto>>(okResult.Value);
        Assert.Single(returnedLessons);
    }

    [Fact]
    public async Task Create_WithValidData_ReturnsCreatedLesson()
    {
        // Arrange
        var courseId = Guid.NewGuid();
        var teacherId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var lessonId = Guid.NewGuid();

        var createdLesson = new LessonDto
        {
            Id = lessonId,
            CourseId = courseId,
            StudentId = studentId,
            TeacherId = teacherId,
            RoomId = 1,
            ScheduledDate = DateOnly.FromDateTime(DateTime.Today),
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(10, 30),
            Status = LessonStatus.Scheduled
        };

        _mockLessonService.Setup(s => s.CreateAsync(It.IsAny<CreateLessonDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((createdLesson, (string?)null));

        var dto = new CreateLessonDto
        {
            CourseId = courseId,
            StudentId = studentId,
            TeacherId = teacherId,
            RoomId = 1,
            ScheduledDate = DateOnly.FromDateTime(DateTime.Today),
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(10, 30)
        };

        // Act
        var result = await _controller.Create(dto, CancellationToken.None);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(lessonId, ((LessonDto)createdResult.Value!).Id);
    }

    [Fact]
    public async Task Create_WithNonExistentCourse_ReturnsBadRequest()
    {
        // Arrange
        var courseId = Guid.NewGuid();
        var teacherId = Guid.NewGuid();

        _mockLessonService.Setup(s => s.CreateAsync(It.IsAny<CreateLessonDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((LessonDto?)null, "Course not found"));

        var dto = new CreateLessonDto
        {
            CourseId = courseId,
            TeacherId = teacherId,
            ScheduledDate = DateOnly.FromDateTime(DateTime.Today),
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(10, 30)
        };

        // Act
        var result = await _controller.Create(dto, CancellationToken.None);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpdateStatus_WithValidData_UpdatesLessonStatus()
    {
        // Arrange
        var lessonId = Guid.NewGuid();
        var updatedLesson = new LessonDto
        {
            Id = lessonId,
            CourseId = Guid.NewGuid(),
            TeacherId = Guid.NewGuid(),
            ScheduledDate = DateOnly.FromDateTime(DateTime.Today),
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(10, 30),
            Status = LessonStatus.Completed
        };

        _mockLessonService.Setup(s => s.UpdateStatusAsync(
            lessonId, LessonStatus.Completed, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((updatedLesson, false));

        var dto = new UpdateLessonStatusDto
        {
            Status = LessonStatus.Completed
        };

        // Act
        var result = await _controller.UpdateStatus(lessonId, dto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedLesson = Assert.IsType<LessonDto>(okResult.Value);
        Assert.Equal(LessonStatus.Completed, returnedLesson.Status);
    }

    [Fact]
    public async Task UpdateStatus_WithCancellation_SetsReasonAndStatus()
    {
        // Arrange
        var lessonId = Guid.NewGuid();
        var updatedLesson = new LessonDto
        {
            Id = lessonId,
            CourseId = Guid.NewGuid(),
            TeacherId = Guid.NewGuid(),
            ScheduledDate = DateOnly.FromDateTime(DateTime.Today),
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(10, 30),
            Status = LessonStatus.Cancelled,
            CancellationReason = "Student is sick"
        };

        _mockLessonService.Setup(s => s.UpdateStatusAsync(
            lessonId, LessonStatus.Cancelled, "Student is sick", It.IsAny<CancellationToken>()))
            .ReturnsAsync((updatedLesson, false));

        var dto = new UpdateLessonStatusDto
        {
            Status = LessonStatus.Cancelled,
            CancellationReason = "Student is sick"
        };

        // Act
        var result = await _controller.UpdateStatus(lessonId, dto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedLesson = Assert.IsType<LessonDto>(okResult.Value);
        Assert.Equal(LessonStatus.Cancelled, returnedLesson.Status);
        Assert.Equal("Student is sick", returnedLesson.CancellationReason);
    }

    [Fact]
    public async Task Delete_WithInvoicedLesson_ReturnsBadRequest()
    {
        // Arrange
        var lessonId = Guid.NewGuid();

        _mockLessonService.Setup(s => s.DeleteAsync(lessonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, "Cannot delete an invoiced lesson"));

        // Act
        var result = await _controller.Delete(lessonId, CancellationToken.None);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Delete_WithNonInvoicedLesson_ReturnsNoContent()
    {
        // Arrange
        var lessonId = Guid.NewGuid();

        _mockLessonService.Setup(s => s.DeleteAsync(lessonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, (string?)null));

        // Act
        var result = await _controller.Delete(lessonId, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_WithNonExistentLesson_ReturnsNotFound()
    {
        // Arrange
        var lessonId = Guid.NewGuid();

        _mockLessonService.Setup(s => s.DeleteAsync(lessonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, "Lesson not found"));

        // Act
        var result = await _controller.Delete(lessonId, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetAll_WithNoFilters_ReturnsAllLessons()
    {
        // Arrange
        var lessons = CreateTestLessonDtos();

        var criteria = new LessonFilterCriteria();

        _mockLessonService.Setup(s => s.GetAllAsync(
            It.IsAny<LessonFilterCriteria>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(lessons);

        // Act
        var result = await _controller.GetAll(criteria, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedLessons = Assert.IsAssignableFrom<IEnumerable<LessonDto>>(okResult.Value);
        Assert.Equal(2, returnedLessons.Count());
    }

    [Fact]
    public async Task GetAll_WithStartDateFilter_ReturnsFilteredLessons()
    {
        // Arrange
        var startDate = DateOnly.FromDateTime(DateTime.Today);
        var lessons = CreateTestLessonDtos().Where(l => l.ScheduledDate >= startDate).ToList();

        var criteria = new LessonFilterCriteria
        {
            StartDate = startDate
        };

        _mockLessonService.Setup(s => s.GetAllAsync(
            It.IsAny<LessonFilterCriteria>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(lessons);

        // Act
        var result = await _controller.GetAll(criteria, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedLessons = Assert.IsAssignableFrom<IEnumerable<LessonDto>>(okResult.Value);
        Assert.All(returnedLessons, l => Assert.True(l.ScheduledDate >= startDate));
    }

    [Fact]
    public async Task GetAll_WithTeacherIdFilter_ReturnsFilteredLessons()
    {
        // Arrange
        var teacherId = Guid.NewGuid();
        var lessons = CreateTestLessonDtos(teacherId);

        var criteria = new LessonFilterCriteria
        {
            TeacherId = teacherId
        };

        _mockLessonService.Setup(s => s.GetAllAsync(
            It.IsAny<LessonFilterCriteria>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(lessons);

        // Act
        var result = await _controller.GetAll(criteria, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedLessons = Assert.IsAssignableFrom<IEnumerable<LessonDto>>(okResult.Value);
        Assert.All(returnedLessons, l => Assert.Equal(teacherId, l.TeacherId));
    }

    [Fact]
    public async Task GetAll_WithStatusFilter_ReturnsFilteredLessons()
    {
        // Arrange
        var lessons = CreateTestLessonDtos().Where(l => l.Status == LessonStatus.Completed).ToList();

        var criteria = new LessonFilterCriteria
        {
            Status = LessonStatus.Completed
        };

        _mockLessonService.Setup(s => s.GetAllAsync(
            It.IsAny<LessonFilterCriteria>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(lessons);

        // Act
        var result = await _controller.GetAll(criteria, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedLessons = Assert.IsAssignableFrom<IEnumerable<LessonDto>>(okResult.Value);
        Assert.All(returnedLessons, l => Assert.Equal(LessonStatus.Completed, l.Status));
    }

    [Fact]
    public async Task GetById_WithValidId_ReturnsLesson()
    {
        // Arrange
        var lesson = CreateTestLessonDtos().First();

        _mockLessonService.Setup(s => s.GetByIdAsync(lesson.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lesson);

        // Act
        var result = await _controller.GetById(lesson.Id, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedLesson = Assert.IsType<LessonDto>(okResult.Value);
        Assert.Equal(lesson.Id, returnedLesson.Id);
    }

    [Fact]
    public async Task GetById_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var lessonId = Guid.NewGuid();

        _mockLessonService.Setup(s => s.GetByIdAsync(lessonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((LessonDto?)null);

        // Act
        var result = await _controller.GetById(lessonId, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Create_WithNonExistentTeacher_ReturnsBadRequest()
    {
        // Arrange
        _mockLessonService.Setup(s => s.CreateAsync(It.IsAny<CreateLessonDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((LessonDto?)null, "Teacher not found"));

        var dto = new CreateLessonDto
        {
            CourseId = Guid.NewGuid(),
            TeacherId = Guid.NewGuid(),
            ScheduledDate = DateOnly.FromDateTime(DateTime.Today),
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(10, 30)
        };

        // Act
        var result = await _controller.Create(dto, CancellationToken.None);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Create_WithNonExistentStudent_ReturnsBadRequest()
    {
        // Arrange
        _mockLessonService.Setup(s => s.CreateAsync(It.IsAny<CreateLessonDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((LessonDto?)null, "Student not found"));

        var dto = new CreateLessonDto
        {
            CourseId = Guid.NewGuid(),
            TeacherId = Guid.NewGuid(),
            StudentId = Guid.NewGuid(),
            ScheduledDate = DateOnly.FromDateTime(DateTime.Today),
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(10, 30)
        };

        // Act
        var result = await _controller.Create(dto, CancellationToken.None);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Update_WithValidData_ReturnsUpdatedLesson()
    {
        // Arrange
        var lessonId = Guid.NewGuid();
        var updatedLesson = new LessonDto
        {
            Id = lessonId,
            CourseId = Guid.NewGuid(),
            StudentId = Guid.NewGuid(),
            TeacherId = Guid.NewGuid(),
            RoomId = 2,
            ScheduledDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            StartTime = new TimeOnly(11, 0),
            EndTime = new TimeOnly(12, 0),
            Status = LessonStatus.Completed
        };

        _mockLessonService.Setup(s => s.UpdateAsync(lessonId, It.IsAny<UpdateLessonDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((updatedLesson, false));

        var dto = new UpdateLessonDto
        {
            StudentId = Guid.NewGuid(),
            TeacherId = Guid.NewGuid(),
            RoomId = 2,
            ScheduledDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            StartTime = new TimeOnly(11, 0),
            EndTime = new TimeOnly(12, 0),
            Status = LessonStatus.Completed
        };

        // Act
        var result = await _controller.Update(lessonId, dto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedLesson = Assert.IsType<LessonDto>(okResult.Value);
        Assert.Equal(LessonStatus.Completed, returnedLesson.Status);
    }

    [Fact]
    public async Task Update_WithNonExistentLesson_ReturnsNotFound()
    {
        // Arrange
        var lessonId = Guid.NewGuid();

        _mockLessonService.Setup(s => s.UpdateAsync(lessonId, It.IsAny<UpdateLessonDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((LessonDto?)null, true));

        var dto = new UpdateLessonDto
        {
            StudentId = Guid.NewGuid(),
            TeacherId = Guid.NewGuid(),
            RoomId = 1,
            ScheduledDate = DateOnly.FromDateTime(DateTime.Today),
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(11, 0),
            Status = LessonStatus.Scheduled
        };

        // Act
        var result = await _controller.Update(lessonId, dto, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task UpdateStatus_WithNonExistentLesson_ReturnsNotFound()
    {
        // Arrange
        var lessonId = Guid.NewGuid();

        _mockLessonService.Setup(s => s.UpdateStatusAsync(
            lessonId, It.IsAny<LessonStatus>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((LessonDto?)null, true));

        var dto = new UpdateLessonStatusDto
        {
            Status = LessonStatus.Cancelled,
            CancellationReason = "Test"
        };

        // Act
        var result = await _controller.UpdateStatus(lessonId, dto, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GenerateLessons_WithValidCourse_ReturnsGeneratedLessons()
    {
        // Arrange
        var courseId = Guid.NewGuid();
        var teacherId = Guid.NewGuid();
        var courseTypeId = Guid.NewGuid();

        var instrument = new Instrument { Id = 1, Name = "Piano", Category = InstrumentCategory.Keyboard };
        var courseType = new CourseType
        {
            Id = courseTypeId,
            Name = "Piano 30 min",
            InstrumentId = 1,
            Instrument = instrument,
            Type = CourseTypeCategory.Group,
            MaxStudents = 10,
            DurationMinutes = 30
        };

        var teacher = new Teacher { Id = teacherId, FirstName = "Jane", LastName = "Smith", Email = "jane@test.com" };
        var student = new Student { Id = Guid.NewGuid(), FirstName = "John", LastName = "Doe", Email = "john@test.com" };

        var course = new Course
        {
            Id = courseId,
            CourseTypeId = courseTypeId,
            CourseType = courseType,
            TeacherId = teacherId,
            Teacher = teacher,
            DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(10, 30),
            Frequency = CourseFrequency.Weekly,
            Status = CourseStatus.Active,
            Enrollments = new List<Enrollment>
            {
                new() { StudentId = student.Id, Student = student, Status = EnrollmentStatus.Active }
            }
        };

        var mockCourseRepo = new Mock<ICourseRepository>();
        mockCourseRepo.Setup(r => r.Query())
            .Returns(new List<Course> { course }.AsQueryable().BuildMockDbSet().Object);

        var mockLessonRepo = new Mock<ILessonRepository>();
        mockLessonRepo.Setup(r => r.Query())
            .Returns(new List<Lesson>().AsQueryable().BuildMockDbSet().Object);
        mockLessonRepo.Setup(r => r.AddAsync(It.IsAny<Lesson>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Lesson l, CancellationToken _) => l);

        var mockHolidayRepo = new Mock<IRepository<Holiday>>();
        mockHolidayRepo.Setup(r => r.Query())
            .Returns(new List<Holiday>().AsQueryable().BuildMockDbSet().Object);

        var mockAbsenceRepo = new Mock<IRepository<Absence>>();
        mockAbsenceRepo.Setup(r => r.Query())
            .Returns(new List<Absence>().AsQueryable().BuildMockDbSet().Object);

        _mockUnitOfWork.Setup(u => u.Courses).Returns(mockCourseRepo.Object);
        _mockUnitOfWork.Setup(u => u.Lessons).Returns(mockLessonRepo.Object);
        _mockUnitOfWork.Setup(u => u.Repository<Holiday>()).Returns(mockHolidayRepo.Object);
        _mockUnitOfWork.Setup(u => u.Repository<Absence>()).Returns(mockAbsenceRepo.Object);

        var dto = new GenerateLessonsDto
        {
            CourseId = courseId,
            StartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek + 1)),
            EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(14)),
            SkipHolidays = false
        };

        // Act
        var result = await _generationController.GenerateLessons(dto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var generatedResult = Assert.IsType<GenerateLessonsResultDto>(okResult.Value);
        Assert.True(generatedResult.LessonsCreated >= 0);
    }

    [Fact]
    public async Task GenerateLessons_WithNonExistentCourse_ReturnsBadRequest()
    {
        // Arrange
        var mockCourseRepo = new Mock<ICourseRepository>();
        mockCourseRepo.Setup(r => r.Query())
            .Returns(new List<Course>().AsQueryable().BuildMockDbSet().Object);

        _mockUnitOfWork.Setup(u => u.Courses).Returns(mockCourseRepo.Object);

        var dto = new GenerateLessonsDto
        {
            CourseId = Guid.NewGuid(),
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(7)),
            SkipHolidays = false
        };

        // Act
        var result = await _generationController.GenerateLessons(dto, CancellationToken.None);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GenerateLessonsBulk_WithActiveCourses_ReturnsGeneratedLessons()
    {
        // Arrange
        var teacherId = Guid.NewGuid();
        var courseTypeId = Guid.NewGuid();

        var instrument = new Instrument { Id = 1, Name = "Piano", Category = InstrumentCategory.Keyboard };
        var courseType = new CourseType
        {
            Id = courseTypeId,
            Name = "Piano 30 min",
            InstrumentId = 1,
            Instrument = instrument,
            Type = CourseTypeCategory.Individual,
            MaxStudents = 1,
            DurationMinutes = 30
        };

        var courses = new List<Course>
        {
            new()
            {
                Id = Guid.NewGuid(),
                CourseTypeId = courseTypeId,
                CourseType = courseType,
                TeacherId = teacherId,
                DayOfWeek = DayOfWeek.Monday,
                StartTime = new TimeOnly(10, 0),
                EndTime = new TimeOnly(10, 30),
                Frequency = CourseFrequency.Weekly,
                Status = CourseStatus.Active,
                Enrollments = new List<Enrollment>()
            }
        };

        var mockCourseRepo = new Mock<ICourseRepository>();
        mockCourseRepo.Setup(r => r.Query())
            .Returns(courses.AsQueryable().BuildMockDbSet().Object);

        var mockLessonRepo = new Mock<ILessonRepository>();
        mockLessonRepo.Setup(r => r.Query())
            .Returns(new List<Lesson>().AsQueryable().BuildMockDbSet().Object);
        mockLessonRepo.Setup(r => r.AddAsync(It.IsAny<Lesson>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Lesson l, CancellationToken _) => l);

        var mockHolidayRepo = new Mock<IRepository<Holiday>>();
        mockHolidayRepo.Setup(r => r.Query())
            .Returns(new List<Holiday>().AsQueryable().BuildMockDbSet().Object);

        var mockAbsenceRepo = new Mock<IRepository<Absence>>();
        mockAbsenceRepo.Setup(r => r.Query())
            .Returns(new List<Absence>().AsQueryable().BuildMockDbSet().Object);

        _mockUnitOfWork.Setup(u => u.Courses).Returns(mockCourseRepo.Object);
        _mockUnitOfWork.Setup(u => u.Lessons).Returns(mockLessonRepo.Object);
        _mockUnitOfWork.Setup(u => u.Repository<Holiday>()).Returns(mockHolidayRepo.Object);
        _mockUnitOfWork.Setup(u => u.Repository<Absence>()).Returns(mockAbsenceRepo.Object);

        var dto = new BulkGenerateLessonsDto
        {
            StartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek + 1)),
            EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(7)),
            SkipHolidays = false
        };

        // Act
        var result = await _generationController.GenerateLessonsBulk(dto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var bulkResult = Assert.IsType<BulkGenerateLessonsResultDto>(okResult.Value);
        Assert.True(bulkResult.TotalCoursesProcessed >= 0);
    }

    [Fact]
    public async Task UpdateGroupStatus_WithValidData_UpdatesMultipleLessons()
    {
        // Arrange
        var courseId = Guid.NewGuid();
        var scheduledDate = DateOnly.FromDateTime(DateTime.Today);

        _mockLessonService.Setup(s => s.UpdateGroupStatusAsync(
            courseId, scheduledDate, LessonStatus.Cancelled, "Holiday", It.IsAny<CancellationToken>()))
            .ReturnsAsync((2, false));

        var dto = new UpdateGroupLessonStatusDto
        {
            CourseId = courseId,
            ScheduledDate = scheduledDate,
            Status = LessonStatus.Cancelled,
            CancellationReason = "Holiday"
        };

        // Act
        var result = await _controller.UpdateGroupStatus(dto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var resultDto = Assert.IsType<UpdateGroupLessonStatusResultDto>(okResult.Value);
        Assert.Equal(2, resultDto.LessonsUpdated);
        Assert.Equal(LessonStatus.Cancelled, resultDto.Status);
    }

    [Fact]
    public async Task UpdateGroupStatus_WithNoLessons_ReturnsNotFound()
    {
        // Arrange
        var courseId = Guid.NewGuid();
        var scheduledDate = DateOnly.FromDateTime(DateTime.Today);

        _mockLessonService.Setup(s => s.UpdateGroupStatusAsync(
            courseId, scheduledDate, It.IsAny<LessonStatus>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((0, true));

        var dto = new UpdateGroupLessonStatusDto
        {
            CourseId = courseId,
            ScheduledDate = scheduledDate,
            Status = LessonStatus.Cancelled
        };

        // Act
        var result = await _controller.UpdateGroupStatus(dto, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    private static List<LessonDto> CreateTestLessonDtos(Guid? specificTeacherId = null)
    {
        var teacherId = specificTeacherId ?? Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var courseId = Guid.NewGuid();

        return new List<LessonDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                CourseId = courseId,
                StudentId = studentId,
                StudentName = "John Doe",
                TeacherId = teacherId,
                TeacherName = "Jane Smith",
                RoomId = 1,
                RoomName = "Room A",
                CourseTypeName = "Piano 30 min",
                InstrumentName = "Piano",
                ScheduledDate = DateOnly.FromDateTime(DateTime.Today),
                StartTime = new TimeOnly(10, 0),
                EndTime = new TimeOnly(10, 30),
                Status = LessonStatus.Scheduled
            },
            new()
            {
                Id = Guid.NewGuid(),
                CourseId = courseId,
                StudentId = studentId,
                StudentName = "John Doe",
                TeacherId = teacherId,
                TeacherName = "Jane Smith",
                RoomId = 1,
                RoomName = "Room A",
                CourseTypeName = "Piano 30 min",
                InstrumentName = "Piano",
                ScheduledDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
                StartTime = new TimeOnly(11, 0),
                EndTime = new TimeOnly(11, 30),
                Status = LessonStatus.Completed
            }
        };
    }
}
