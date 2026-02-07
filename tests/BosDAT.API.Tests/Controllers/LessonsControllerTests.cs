using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using BosDAT.API.Controllers;
using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Interfaces;
using BosDAT.Infrastructure.Services;
using BosDAT.API.Tests.Helpers;

namespace BosDAT.API.Tests.Controllers;

public class LessonsControllerTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly BosDAT.API.Controllers.LessonsController _controller;

    public LessonsControllerTests()
    {
        _mockUnitOfWork = MockHelpers.CreateMockUnitOfWork();
        var lessonGenerationService = new LessonGenerationService(_mockUnitOfWork.Object);
        _controller = new BosDAT.API.Controllers.LessonsController(_mockUnitOfWork.Object, lessonGenerationService);
    }

    [Fact]
    public async Task GetByStudent_WithValidStudentId_ReturnsLessons()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var teacherId = Guid.NewGuid();
        var courseId = Guid.NewGuid();

        var instrument = new Instrument { Id = 1, Name = "Piano", Category = InstrumentCategory.Keyboard };
        var courseType = new CourseType
        {
            Id = Guid.NewGuid(),
            Name = "Piano 30 min",
            InstrumentId = 1,
            Instrument = instrument
        };
        var teacher = new Teacher { Id = teacherId, FirstName = "Jane", LastName = "Smith", Email = "jane@test.com" };
        var student = new Student { Id = studentId, FirstName = "John", LastName = "Doe", Email = "john@test.com" };
        var room = new Room { Id = 1, Name = "Room A" };
        var course = new Course
        {
            Id = courseId,
            CourseType = courseType,
            Teacher = teacher,
            Room = room
        };

        var lessons = new List<Lesson>
        {
            new Lesson
            {
                Id = Guid.NewGuid(),
                CourseId = courseId,
                StudentId = studentId,
                TeacherId = teacherId,
                RoomId = 1,
                ScheduledDate = DateOnly.FromDateTime(DateTime.Today),
                StartTime = new TimeOnly(10, 0),
                EndTime = new TimeOnly(10, 30),
                Status = LessonStatus.Scheduled,
                Course = course,
                Student = student,
                Teacher = teacher,
                Room = room
            }
        };

        var mockLessonRepo = new Mock<ILessonRepository>();
        mockLessonRepo.Setup(r => r.GetByStudentAsync(studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lessons);

        _mockUnitOfWork.Setup(u => u.Lessons).Returns(mockLessonRepo.Object);

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

        var instrument = new Instrument { Id = 1, Name = "Piano", Category = InstrumentCategory.Keyboard };
        var courseType = new CourseType { Id = Guid.NewGuid(), Name = "Piano 30 min", InstrumentId = 1, Instrument = instrument };
        var teacher = new Teacher { Id = teacherId, FirstName = "Jane", LastName = "Smith", Email = "jane@test.com" };
        var student = new Student { Id = studentId, FirstName = "John", LastName = "Doe", Email = "john@test.com" };
        var room = new Room { Id = 1, Name = "Room A" };

        var course = new Course
        {
            Id = courseId,
            TeacherId = teacherId,
            CourseTypeId = courseType.Id,
            CourseType = courseType,
            Teacher = teacher,
            Room = room
        };

        var mockCourseRepo = new Mock<ICourseRepository>();
        mockCourseRepo.Setup(r => r.GetByIdAsync(courseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(course);

        var mockTeacherRepo = new Mock<ITeacherRepository>();
        mockTeacherRepo.Setup(r => r.GetByIdAsync(teacherId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(teacher);

        var mockStudentRepo = new Mock<IStudentRepository>();
        mockStudentRepo.Setup(r => r.GetByIdAsync(studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(student);

        var createdLesson = new Lesson
        {
            Id = Guid.NewGuid(),
            CourseId = courseId,
            StudentId = studentId,
            TeacherId = teacherId,
            RoomId = 1,
            ScheduledDate = DateOnly.FromDateTime(DateTime.Today),
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(10, 30),
            Status = LessonStatus.Scheduled,
            Course = course,
            Student = student,
            Teacher = teacher,
            Room = room
        };

        var mockLessonRepo = new Mock<ILessonRepository>();
        mockLessonRepo.Setup(r => r.AddAsync(It.IsAny<Lesson>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdLesson);
        mockLessonRepo.Setup(r => r.Query())
            .Returns(new List<Lesson> { createdLesson }.AsQueryable().BuildMockDbSet().Object);

        _mockUnitOfWork.Setup(u => u.Courses).Returns(mockCourseRepo.Object);
        _mockUnitOfWork.Setup(u => u.Teachers).Returns(mockTeacherRepo.Object);
        _mockUnitOfWork.Setup(u => u.Students).Returns(mockStudentRepo.Object);
        _mockUnitOfWork.Setup(u => u.Lessons).Returns(mockLessonRepo.Object);

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
    }

    [Fact]
    public async Task Create_WithNonExistentCourse_ReturnsBadRequest()
    {
        // Arrange
        var courseId = Guid.NewGuid();
        var teacherId = Guid.NewGuid();

        var mockCourseRepo = new Mock<ICourseRepository>();
        mockCourseRepo.Setup(r => r.GetByIdAsync(courseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Course?)null);

        _mockUnitOfWork.Setup(u => u.Courses).Returns(mockCourseRepo.Object);

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
        var teacherId = Guid.NewGuid();

        var instrument = new Instrument { Id = 1, Name = "Piano", Category = InstrumentCategory.Keyboard };
        var courseType = new CourseType { Id = Guid.NewGuid(), Name = "Piano 30 min", InstrumentId = 1, Instrument = instrument };
        var teacher = new Teacher { Id = teacherId, FirstName = "Jane", LastName = "Smith", Email = "jane@test.com" };
        var course = new Course { Id = Guid.NewGuid(), CourseType = courseType, Teacher = teacher };

        var lesson = new Lesson
        {
            Id = lessonId,
            CourseId = course.Id,
            TeacherId = teacherId,
            ScheduledDate = DateOnly.FromDateTime(DateTime.Today),
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(10, 30),
            Status = LessonStatus.Scheduled,
            Course = course,
            Teacher = teacher
        };

        var mockLessonRepo = new Mock<ILessonRepository>();
        mockLessonRepo.Setup(r => r.GetByIdAsync(lessonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lesson);
        mockLessonRepo.Setup(r => r.Query())
            .Returns(new List<Lesson> { lesson }.AsQueryable().BuildMockDbSet().Object);

        _mockUnitOfWork.Setup(u => u.Lessons).Returns(mockLessonRepo.Object);

        var dto = new UpdateLessonStatusDto
        {
            Status = LessonStatus.Completed
        };

        // Act
        var result = await _controller.UpdateStatus(lessonId, dto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(LessonStatus.Completed, lesson.Status);
    }

    [Fact]
    public async Task UpdateStatus_WithCancellation_SetsReasonAndStatus()
    {
        // Arrange
        var lessonId = Guid.NewGuid();
        var teacherId = Guid.NewGuid();

        var instrument = new Instrument { Id = 1, Name = "Piano", Category = InstrumentCategory.Keyboard };
        var courseType = new CourseType { Id = Guid.NewGuid(), Name = "Piano 30 min", InstrumentId = 1, Instrument = instrument };
        var teacher = new Teacher { Id = teacherId, FirstName = "Jane", LastName = "Smith", Email = "jane@test.com" };
        var course = new Course { Id = Guid.NewGuid(), CourseType = courseType, Teacher = teacher };

        var lesson = new Lesson
        {
            Id = lessonId,
            CourseId = course.Id,
            TeacherId = teacherId,
            ScheduledDate = DateOnly.FromDateTime(DateTime.Today),
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(10, 30),
            Status = LessonStatus.Scheduled,
            Course = course,
            Teacher = teacher
        };

        var mockLessonRepo = new Mock<ILessonRepository>();
        mockLessonRepo.Setup(r => r.GetByIdAsync(lessonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lesson);
        mockLessonRepo.Setup(r => r.Query())
            .Returns(new List<Lesson> { lesson }.AsQueryable().BuildMockDbSet().Object);

        _mockUnitOfWork.Setup(u => u.Lessons).Returns(mockLessonRepo.Object);

        var dto = new UpdateLessonStatusDto
        {
            Status = LessonStatus.Cancelled,
            CancellationReason = "Student is sick"
        };

        // Act
        var result = await _controller.UpdateStatus(lessonId, dto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(LessonStatus.Cancelled, lesson.Status);
        Assert.Equal("Student is sick", lesson.CancellationReason);
    }

    [Fact]
    public async Task Delete_WithInvoicedLesson_ReturnsBadRequest()
    {
        // Arrange
        var lessonId = Guid.NewGuid();

        var lesson = new Lesson
        {
            Id = lessonId,
            CourseId = Guid.NewGuid(),
            TeacherId = Guid.NewGuid(),
            ScheduledDate = DateOnly.FromDateTime(DateTime.Today),
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(10, 30),
            IsInvoiced = true
        };

        var mockLessonRepo = new Mock<ILessonRepository>();
        mockLessonRepo.Setup(r => r.GetByIdAsync(lessonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lesson);

        _mockUnitOfWork.Setup(u => u.Lessons).Returns(mockLessonRepo.Object);

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

        var lesson = new Lesson
        {
            Id = lessonId,
            CourseId = Guid.NewGuid(),
            TeacherId = Guid.NewGuid(),
            ScheduledDate = DateOnly.FromDateTime(DateTime.Today),
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(10, 30),
            IsInvoiced = false
        };

        var mockLessonRepo = new Mock<ILessonRepository>();
        mockLessonRepo.Setup(r => r.GetByIdAsync(lessonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lesson);

        _mockUnitOfWork.Setup(u => u.Lessons).Returns(mockLessonRepo.Object);

        // Act
        var result = await _controller.Delete(lessonId, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
        mockLessonRepo.Verify(r => r.DeleteAsync(lesson, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Delete_WithNonExistentLesson_ReturnsNotFound()
    {
        // Arrange
        var lessonId = Guid.NewGuid();

        var mockLessonRepo = new Mock<ILessonRepository>();
        mockLessonRepo.Setup(r => r.GetByIdAsync(lessonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Lesson?)null);

        _mockUnitOfWork.Setup(u => u.Lessons).Returns(mockLessonRepo.Object);

        // Act
        var result = await _controller.Delete(lessonId, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetAll_WithNoFilters_ReturnsAllLessons()
    {
        // Arrange
        var lessons = CreateTestLessons();
        var mockLessonRepo = new Mock<ILessonRepository>();
        mockLessonRepo.Setup(r => r.Query())
            .Returns(lessons.AsQueryable().BuildMockDbSet().Object);

        _mockUnitOfWork.Setup(u => u.Lessons).Returns(mockLessonRepo.Object);

        // Act
        var result = await _controller.GetAll(null, null, null, null, null, null, null, null, CancellationToken.None);

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
        var lessons = CreateTestLessons();
        var mockLessonRepo = new Mock<ILessonRepository>();
        mockLessonRepo.Setup(r => r.Query())
            .Returns(lessons.AsQueryable().BuildMockDbSet().Object);

        _mockUnitOfWork.Setup(u => u.Lessons).Returns(mockLessonRepo.Object);

        // Act
        var result = await _controller.GetAll(startDate, null, null, null, null, null, null, null, CancellationToken.None);

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
        var lessons = CreateTestLessons(teacherId);
        var mockLessonRepo = new Mock<ILessonRepository>();
        mockLessonRepo.Setup(r => r.Query())
            .Returns(lessons.AsQueryable().BuildMockDbSet().Object);

        _mockUnitOfWork.Setup(u => u.Lessons).Returns(mockLessonRepo.Object);

        // Act
        var result = await _controller.GetAll(null, null, teacherId, null, null, null, null, null, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedLessons = Assert.IsAssignableFrom<IEnumerable<LessonDto>>(okResult.Value);
        Assert.All(returnedLessons, l => Assert.Equal(teacherId, l.TeacherId));
    }

    [Fact]
    public async Task GetAll_WithStatusFilter_ReturnsFilteredLessons()
    {
        // Arrange
        var lessons = CreateTestLessons();
        var mockLessonRepo = new Mock<ILessonRepository>();
        mockLessonRepo.Setup(r => r.Query())
            .Returns(lessons.AsQueryable().BuildMockDbSet().Object);

        _mockUnitOfWork.Setup(u => u.Lessons).Returns(mockLessonRepo.Object);

        // Act
        var result = await _controller.GetAll(null, null, null, null, null, null, LessonStatus.Completed, null, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedLessons = Assert.IsAssignableFrom<IEnumerable<LessonDto>>(okResult.Value);
        Assert.All(returnedLessons, l => Assert.Equal(LessonStatus.Completed, l.Status));
    }

    [Fact]
    public async Task GetById_WithValidId_ReturnsLesson()
    {
        // Arrange
        var lesson = CreateTestLessons().First();
        var mockLessonRepo = new Mock<ILessonRepository>();
        mockLessonRepo.Setup(r => r.Query())
            .Returns(new List<Lesson> { lesson }.AsQueryable().BuildMockDbSet().Object);

        _mockUnitOfWork.Setup(u => u.Lessons).Returns(mockLessonRepo.Object);

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
        var mockLessonRepo = new Mock<ILessonRepository>();
        mockLessonRepo.Setup(r => r.Query())
            .Returns(new List<Lesson>().AsQueryable().BuildMockDbSet().Object);

        _mockUnitOfWork.Setup(u => u.Lessons).Returns(mockLessonRepo.Object);

        // Act
        var result = await _controller.GetById(Guid.NewGuid(), CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Create_WithNonExistentTeacher_ReturnsBadRequest()
    {
        // Arrange
        var courseId = Guid.NewGuid();
        var course = new Course { Id = courseId, TeacherId = Guid.NewGuid() };

        var mockCourseRepo = new Mock<ICourseRepository>();
        mockCourseRepo.Setup(r => r.GetByIdAsync(courseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(course);

        var mockTeacherRepo = new Mock<ITeacherRepository>();
        mockTeacherRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Teacher?)null);

        _mockUnitOfWork.Setup(u => u.Courses).Returns(mockCourseRepo.Object);
        _mockUnitOfWork.Setup(u => u.Teachers).Returns(mockTeacherRepo.Object);

        var dto = new CreateLessonDto
        {
            CourseId = courseId,
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
        var courseId = Guid.NewGuid();
        var teacherId = Guid.NewGuid();
        var course = new Course { Id = courseId, TeacherId = teacherId };
        var teacher = new Teacher { Id = teacherId, FirstName = "John", LastName = "Doe", Email = "john@test.com" };

        var mockCourseRepo = new Mock<ICourseRepository>();
        mockCourseRepo.Setup(r => r.GetByIdAsync(courseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(course);

        var mockTeacherRepo = new Mock<ITeacherRepository>();
        mockTeacherRepo.Setup(r => r.GetByIdAsync(teacherId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(teacher);

        var mockStudentRepo = new Mock<IStudentRepository>();
        mockStudentRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Student?)null);

        _mockUnitOfWork.Setup(u => u.Courses).Returns(mockCourseRepo.Object);
        _mockUnitOfWork.Setup(u => u.Teachers).Returns(mockTeacherRepo.Object);
        _mockUnitOfWork.Setup(u => u.Students).Returns(mockStudentRepo.Object);

        var dto = new CreateLessonDto
        {
            CourseId = courseId,
            TeacherId = teacherId,
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
        var lesson = CreateTestLessons().First();
        lesson.Id = lessonId;

        var mockLessonRepo = new Mock<ILessonRepository>();
        mockLessonRepo.Setup(r => r.GetByIdAsync(lessonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lesson);
        mockLessonRepo.Setup(r => r.Query())
            .Returns(new List<Lesson> { lesson }.AsQueryable().BuildMockDbSet().Object);

        _mockUnitOfWork.Setup(u => u.Lessons).Returns(mockLessonRepo.Object);

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
        Assert.Equal(LessonStatus.Completed, lesson.Status);
    }

    [Fact]
    public async Task Update_WithNonExistentLesson_ReturnsNotFound()
    {
        // Arrange
        var lessonId = Guid.NewGuid();

        var mockLessonRepo = new Mock<ILessonRepository>();
        mockLessonRepo.Setup(r => r.GetByIdAsync(lessonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Lesson?)null);

        _mockUnitOfWork.Setup(u => u.Lessons).Returns(mockLessonRepo.Object);

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

        var mockLessonRepo = new Mock<ILessonRepository>();
        mockLessonRepo.Setup(r => r.GetByIdAsync(lessonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Lesson?)null);

        _mockUnitOfWork.Setup(u => u.Lessons).Returns(mockLessonRepo.Object);

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

        _mockUnitOfWork.Setup(u => u.Courses).Returns(mockCourseRepo.Object);
        _mockUnitOfWork.Setup(u => u.Lessons).Returns(mockLessonRepo.Object);
        _mockUnitOfWork.Setup(u => u.Repository<Holiday>()).Returns(mockHolidayRepo.Object);

        var dto = new GenerateLessonsDto
        {
            CourseId = courseId,
            StartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek + 1)), // Next Monday
            EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(14)),
            SkipHolidays = false
        };

        // Act
        var result = await _controller.GenerateLessons(dto, CancellationToken.None);

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
        var result = await _controller.GenerateLessons(dto, CancellationToken.None);

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

        _mockUnitOfWork.Setup(u => u.Courses).Returns(mockCourseRepo.Object);
        _mockUnitOfWork.Setup(u => u.Lessons).Returns(mockLessonRepo.Object);
        _mockUnitOfWork.Setup(u => u.Repository<Holiday>()).Returns(mockHolidayRepo.Object);

        var dto = new BulkGenerateLessonsDto
        {
            StartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek + 1)),
            EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(7)),
            SkipHolidays = false
        };

        // Act
        var result = await _controller.GenerateLessonsBulk(dto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var bulkResult = Assert.IsType<BulkGenerateLessonsResultDto>(okResult.Value);
        Assert.True(bulkResult.TotalCoursesProcessed >= 0);
    }

    private static List<Lesson> CreateTestLessons(Guid? specificTeacherId = null)
    {
        var teacherId = specificTeacherId ?? Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var courseId = Guid.NewGuid();

        var instrument = new Instrument { Id = 1, Name = "Piano", Category = InstrumentCategory.Keyboard };
        var courseType = new CourseType
        {
            Id = Guid.NewGuid(),
            Name = "Piano 30 min",
            InstrumentId = 1,
            Instrument = instrument
        };
        var teacher = new Teacher { Id = teacherId, FirstName = "Jane", LastName = "Smith", Email = "jane@test.com" };
        var student = new Student { Id = studentId, FirstName = "John", LastName = "Doe", Email = "john@test.com" };
        var room = new Room { Id = 1, Name = "Room A" };
        var course = new Course
        {
            Id = courseId,
            CourseType = courseType,
            Teacher = teacher,
            Room = room
        };

        return new List<Lesson>
        {
            new()
            {
                Id = Guid.NewGuid(),
                CourseId = courseId,
                StudentId = studentId,
                TeacherId = teacherId,
                RoomId = 1,
                ScheduledDate = DateOnly.FromDateTime(DateTime.Today),
                StartTime = new TimeOnly(10, 0),
                EndTime = new TimeOnly(10, 30),
                Status = LessonStatus.Scheduled,
                Course = course,
                Student = student,
                Teacher = teacher,
                Room = room
            },
            new()
            {
                Id = Guid.NewGuid(),
                CourseId = courseId,
                StudentId = studentId,
                TeacherId = teacherId,
                RoomId = 1,
                ScheduledDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
                StartTime = new TimeOnly(11, 0),
                EndTime = new TimeOnly(11, 30),
                Status = LessonStatus.Completed,
                Course = course,
                Student = student,
                Teacher = teacher,
                Room = room
            }
        };
    }
}
