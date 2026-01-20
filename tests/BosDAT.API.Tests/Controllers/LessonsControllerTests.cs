using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using BosDAT.API.Controllers;
using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Interfaces;
using BosDAT.API.Tests.Helpers;

namespace BosDAT.API.Tests.Controllers;

public class LessonsControllerTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly LessonsController _controller;

    public LessonsControllerTests()
    {
        _mockUnitOfWork = MockHelpers.CreateMockUnitOfWork();
        _controller = new LessonsController(_mockUnitOfWork.Object);
    }

    [Fact]
    public async Task GetByStudent_WithValidStudentId_ReturnsLessons()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var teacherId = Guid.NewGuid();
        var courseId = Guid.NewGuid();

        var instrument = new Instrument { Id = 1, Name = "Piano", Category = InstrumentCategory.Keyboard };
        var lessonType = new LessonType
        {
            Id = 1,
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
            LessonType = lessonType,
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
        var lessonType = new LessonType { Id = 1, Name = "Piano 30 min", InstrumentId = 1, Instrument = instrument };
        var teacher = new Teacher { Id = teacherId, FirstName = "Jane", LastName = "Smith", Email = "jane@test.com" };
        var student = new Student { Id = studentId, FirstName = "John", LastName = "Doe", Email = "john@test.com" };
        var room = new Room { Id = 1, Name = "Room A" };

        var course = new Course
        {
            Id = courseId,
            TeacherId = teacherId,
            LessonTypeId = 1,
            LessonType = lessonType,
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
        var lessonType = new LessonType { Id = 1, Name = "Piano 30 min", InstrumentId = 1, Instrument = instrument };
        var teacher = new Teacher { Id = teacherId, FirstName = "Jane", LastName = "Smith", Email = "jane@test.com" };
        var course = new Course { Id = Guid.NewGuid(), LessonType = lessonType, Teacher = teacher };

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
        var lessonType = new LessonType { Id = 1, Name = "Piano 30 min", InstrumentId = 1, Instrument = instrument };
        var teacher = new Teacher { Id = teacherId, FirstName = "Jane", LastName = "Smith", Email = "jane@test.com" };
        var course = new Course { Id = Guid.NewGuid(), LessonType = lessonType, Teacher = teacher };

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
        mockLessonRepo.Verify(r => r.Delete(lesson), Times.Once);
    }
}
