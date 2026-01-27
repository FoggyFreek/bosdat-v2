using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using BosDAT.API.Controllers;
using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Interfaces;
using BosDAT.API.Tests.Helpers;

namespace BosDAT.API.Tests.Controllers;

public class EnrollmentsControllerTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IRegistrationFeeService> _mockRegistrationFeeService;
    private readonly Mock<IEnrollmentPricingService> _mockEnrollmentPricingService;
    private readonly EnrollmentsController _controller;

    public EnrollmentsControllerTests()
    {
        _mockUnitOfWork = MockHelpers.CreateMockUnitOfWork();
        _mockRegistrationFeeService = new Mock<IRegistrationFeeService>();
        _mockEnrollmentPricingService = new Mock<IEnrollmentPricingService>();
        _controller = new EnrollmentsController(_mockUnitOfWork.Object, _mockRegistrationFeeService.Object, _mockEnrollmentPricingService.Object);
    }

    [Fact]
    public async Task GetByStudent_WithValidStudentId_ReturnsEnrollments()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var teacherId = Guid.NewGuid();
        var instrumentId = 1;

        var student = new Student
        {
            Id = studentId,
            FirstName = "John",
            LastName = "Doe",
            Email = "john@test.com"
        };

        var instrument = new Instrument { Id = instrumentId, Name = "Piano", Category = InstrumentCategory.Keyboard };
        var courseType = new CourseType
        {
            Id = Guid.NewGuid(),
            InstrumentId = instrumentId,
            Name = "Piano 30 min",
            Instrument = instrument
        };

        var teacher = new Teacher
        {
            Id = teacherId,
            FirstName = "Jane",
            LastName = "Smith",
            Email = "jane@test.com"
        };

        var room = new Room { Id = 1, Name = "Room A" };

        var course = new Course
        {
            Id = courseId,
            TeacherId = teacherId,
            CourseTypeId = courseType.Id,
            RoomId = 1,
            DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(10, 30),
            Teacher = teacher,
            CourseType = courseType,
            Room = room
        };

        var enrollments = new List<Enrollment>
        {
            new Enrollment
            {
                Id = Guid.NewGuid(),
                StudentId = studentId,
                CourseId = courseId,
                Status = EnrollmentStatus.Active,
                Student = student,
                Course = course
            }
        };

        var mockStudentRepo = new Mock<IStudentRepository>();
        mockStudentRepo.Setup(r => r.GetByIdAsync(studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(student);

        var mockEnrollmentRepo = MockHelpers.CreateMockRepository(enrollments);

        _mockUnitOfWork.Setup(u => u.Students).Returns(mockStudentRepo.Object);
        _mockUnitOfWork.Setup(u => u.Repository<Enrollment>()).Returns(mockEnrollmentRepo.Object);

        // Act
        var result = await _controller.GetByStudent(studentId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedEnrollments = Assert.IsAssignableFrom<IEnumerable<StudentEnrollmentDto>>(okResult.Value);
        Assert.Single(returnedEnrollments);
    }

    [Fact]
    public async Task GetByStudent_WithInvalidStudentId_ReturnsNotFound()
    {
        // Arrange
        var studentId = Guid.NewGuid();

        var mockStudentRepo = new Mock<IStudentRepository>();
        mockStudentRepo.Setup(r => r.GetByIdAsync(studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Student?)null);

        _mockUnitOfWork.Setup(u => u.Students).Returns(mockStudentRepo.Object);

        // Act
        var result = await _controller.GetByStudent(studentId, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task Create_WithValidData_ReturnsCreatedEnrollment()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var courseId = Guid.NewGuid();

        var student = new Student
        {
            Id = studentId,
            FirstName = "John",
            LastName = "Doe",
            Email = "john@test.com"
        };

        var course = new Course
        {
            Id = courseId,
            TeacherId = Guid.NewGuid(),
            CourseTypeId = Guid.NewGuid(),
            Status = CourseStatus.Active
        };

        var mockStudentRepo = new Mock<IStudentRepository>();
        mockStudentRepo.Setup(r => r.GetByIdAsync(studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(student);

        var mockCourseRepo = new Mock<ICourseRepository>();
        mockCourseRepo.Setup(r => r.GetByIdAsync(courseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(course);

        var mockEnrollmentRepo = MockHelpers.CreateMockRepository(new List<Enrollment>());
        mockEnrollmentRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Enrollment, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Enrollment?)null);

        _mockUnitOfWork.Setup(u => u.Students).Returns(mockStudentRepo.Object);
        _mockUnitOfWork.Setup(u => u.Courses).Returns(mockCourseRepo.Object);
        _mockUnitOfWork.Setup(u => u.Repository<Enrollment>()).Returns(mockEnrollmentRepo.Object);

        var dto = new CreateEnrollmentDto
        {
            StudentId = studentId,
            CourseId = courseId,
            DiscountPercent = 10
        };

        // Act
        var result = await _controller.Create(dto, CancellationToken.None);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var enrollment = Assert.IsType<EnrollmentDto>(createdResult.Value);
        Assert.Equal(studentId, enrollment.StudentId);
        Assert.Equal(courseId, enrollment.CourseId);
        Assert.Equal(10, enrollment.DiscountPercent);
    }

    [Fact]
    public async Task Create_WithNonExistentStudent_ReturnsBadRequest()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var courseId = Guid.NewGuid();

        var mockStudentRepo = new Mock<IStudentRepository>();
        mockStudentRepo.Setup(r => r.GetByIdAsync(studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Student?)null);

        _mockUnitOfWork.Setup(u => u.Students).Returns(mockStudentRepo.Object);

        var dto = new CreateEnrollmentDto
        {
            StudentId = studentId,
            CourseId = courseId
        };

        // Act
        var result = await _controller.Create(dto, CancellationToken.None);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Create_WithAlreadyEnrolledStudent_ReturnsBadRequest()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var courseId = Guid.NewGuid();

        var student = new Student
        {
            Id = studentId,
            FirstName = "John",
            LastName = "Doe",
            Email = "john@test.com"
        };

        var course = new Course
        {
            Id = courseId,
            TeacherId = Guid.NewGuid(),
            CourseTypeId = Guid.NewGuid()
        };

        var existingEnrollment = new Enrollment
        {
            Id = Guid.NewGuid(),
            StudentId = studentId,
            CourseId = courseId
        };

        var mockStudentRepo = new Mock<IStudentRepository>();
        mockStudentRepo.Setup(r => r.GetByIdAsync(studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(student);

        var mockCourseRepo = new Mock<ICourseRepository>();
        mockCourseRepo.Setup(r => r.GetByIdAsync(courseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(course);

        var mockEnrollmentRepo = new Mock<IRepository<Enrollment>>();
        mockEnrollmentRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Enrollment, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEnrollment);

        _mockUnitOfWork.Setup(u => u.Students).Returns(mockStudentRepo.Object);
        _mockUnitOfWork.Setup(u => u.Courses).Returns(mockCourseRepo.Object);
        _mockUnitOfWork.Setup(u => u.Repository<Enrollment>()).Returns(mockEnrollmentRepo.Object);

        var dto = new CreateEnrollmentDto
        {
            StudentId = studentId,
            CourseId = courseId
        };

        // Act
        var result = await _controller.Create(dto, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Delete_WithValidId_ReturnsNoContent()
    {
        // Arrange
        var enrollmentId = Guid.NewGuid();
        var enrollment = new Enrollment
        {
            Id = enrollmentId,
            StudentId = Guid.NewGuid(),
            CourseId = Guid.NewGuid(),
            Status = EnrollmentStatus.Active
        };

        var mockEnrollmentRepo = new Mock<IRepository<Enrollment>>();
        mockEnrollmentRepo.Setup(r => r.GetByIdAsync(enrollmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(enrollment);

        _mockUnitOfWork.Setup(u => u.Repository<Enrollment>()).Returns(mockEnrollmentRepo.Object);

        // Act
        var result = await _controller.Delete(enrollmentId, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
        Assert.Equal(EnrollmentStatus.Withdrawn, enrollment.Status);
    }

    [Fact]
    public async Task Delete_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var enrollmentId = Guid.NewGuid();

        var mockEnrollmentRepo = new Mock<IRepository<Enrollment>>();
        mockEnrollmentRepo.Setup(r => r.GetByIdAsync(enrollmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Enrollment?)null);

        _mockUnitOfWork.Setup(u => u.Repository<Enrollment>()).Returns(mockEnrollmentRepo.Object);

        // Act
        var result = await _controller.Delete(enrollmentId, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }
}
