using Moq;
using Xunit;
using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Enums;
using BosDAT.Core.Interfaces;
using BosDAT.Core.Interfaces.Services;
using BosDAT.Infrastructure.Services;
using BosDAT.API.Tests.Helpers;
using static BosDAT.API.Tests.Helpers.TestDataFactory;

namespace BosDAT.API.Tests.Services;

public class EnrollmentServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IScheduleConflictService> _mockScheduleConflictService;
    private readonly Mock<IRegistrationFeeService> _mockRegistrationFeeService;
    private readonly EnrollmentService _service;

    public EnrollmentServiceTests()
    {
        _mockUnitOfWork = MockHelpers.CreateMockUnitOfWork();
        _mockScheduleConflictService = new Mock<IScheduleConflictService>();
        _mockRegistrationFeeService = MockHelpers.CreateMockRegistrationFeeService();
        _service = new EnrollmentService(
            _mockUnitOfWork.Object,
            _mockScheduleConflictService.Object);
    }

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_WithNoFilters_ReturnsAllEnrollments()
    {
        // Arrange
        var student = CreateStudent();
        var teacher = CreateTeacher();
        var instrument = CreateInstrument();
        var courseType = CreateCourseType(instrument);
        var course = CreateCourse(teacher, courseType);
        var enrollments = new List<Enrollment>
        {
            CreateEnrollment(student, course),
            CreateEnrollment(student, course, EnrollmentStatus.Trail)
        };

        var mockEnrollmentRepo = MockHelpers.CreateMockRepository(enrollments);
        _mockUnitOfWork.Setup(u => u.Repository<Enrollment>()).Returns(mockEnrollmentRepo.Object);

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GetAllAsync_WithStudentFilter_ReturnsMatchingEnrollments()
    {
        // Arrange
        var student1 = CreateStudent("Jane", "Smith");
        var student2 = CreateStudent("John", "Doe");
        var teacher = CreateTeacher();
        var instrument = CreateInstrument();
        var courseType = CreateCourseType(instrument);
        var course = CreateCourse(teacher, courseType);
        var enrollments = new List<Enrollment>
        {
            CreateEnrollment(student1, course),
            CreateEnrollment(student2, course)
        };

        var mockEnrollmentRepo = MockHelpers.CreateMockRepository(enrollments);
        _mockUnitOfWork.Setup(u => u.Repository<Enrollment>()).Returns(mockEnrollmentRepo.Object);

        // Act
        var result = await _service.GetAllAsync(studentId: student1.Id);

        // Assert
        Assert.Single(result);
        Assert.All(result, e => Assert.Equal(student1.Id, e.StudentId));
    }

    [Fact]
    public async Task GetAllAsync_WithCourseFilter_ReturnsMatchingEnrollments()
    {
        // Arrange
        var student = CreateStudent();
        var teacher = CreateTeacher();
        var instrument = CreateInstrument();
        var courseType = CreateCourseType(instrument);
        var course1 = CreateCourse(teacher, courseType);
        var course2 = CreateCourse(teacher, courseType);
        var enrollments = new List<Enrollment>
        {
            CreateEnrollment(student, course1),
            CreateEnrollment(student, course2)
        };

        var mockEnrollmentRepo = MockHelpers.CreateMockRepository(enrollments);
        _mockUnitOfWork.Setup(u => u.Repository<Enrollment>()).Returns(mockEnrollmentRepo.Object);

        // Act
        var result = await _service.GetAllAsync(courseId: course1.Id);

        // Assert
        Assert.Single(result);
        Assert.All(result, e => Assert.Equal(course1.Id, e.CourseId));
    }

    [Fact]
    public async Task GetAllAsync_WithStatusFilter_ReturnsMatchingEnrollments()
    {
        // Arrange
        var student = CreateStudent();
        var teacher = CreateTeacher();
        var instrument = CreateInstrument();
        var courseType = CreateCourseType(instrument);
        var course = CreateCourse(teacher, courseType);
        var enrollments = new List<Enrollment>
        {
            CreateEnrollment(student, course, EnrollmentStatus.Active),
            CreateEnrollment(student, course, EnrollmentStatus.Trail),
            CreateEnrollment(student, course, EnrollmentStatus.Withdrawn)
        };

        var mockEnrollmentRepo = MockHelpers.CreateMockRepository(enrollments);
        _mockUnitOfWork.Setup(u => u.Repository<Enrollment>()).Returns(mockEnrollmentRepo.Object);

        // Act
        var result = await _service.GetAllAsync(status: EnrollmentStatus.Active);

        // Assert
        Assert.Single(result);
        Assert.All(result, e => Assert.Equal(EnrollmentStatus.Active, e.Status));
    }

    [Fact]
    public async Task GetAllAsync_WithMultipleFilters_ReturnsMatchingEnrollments()
    {
        // Arrange
        var student1 = CreateStudent("Jane", "Smith");
        var student2 = CreateStudent("John", "Doe");
        var teacher = CreateTeacher();
        var instrument = CreateInstrument();
        var courseType = CreateCourseType(instrument);
        var course1 = CreateCourse(teacher, courseType);
        var course2 = CreateCourse(teacher, courseType);
        var enrollments = new List<Enrollment>
        {
            CreateEnrollment(student1, course1, EnrollmentStatus.Active),
            CreateEnrollment(student1, course2, EnrollmentStatus.Active),
            CreateEnrollment(student2, course1, EnrollmentStatus.Active),
            CreateEnrollment(student1, course1, EnrollmentStatus.Trail)
        };

        var mockEnrollmentRepo = MockHelpers.CreateMockRepository(enrollments);
        _mockUnitOfWork.Setup(u => u.Repository<Enrollment>()).Returns(mockEnrollmentRepo.Object);

        // Act
        var result = await _service.GetAllAsync(
            studentId: student1.Id,
            courseId: course1.Id,
            status: EnrollmentStatus.Active);

        // Assert
        Assert.Single(result);
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsEnrollmentDetailDto()
    {
        // Arrange
        var student = CreateStudent();
        var teacher = CreateTeacher();
        var instrument = CreateInstrument();
        var courseType = CreateCourseType(instrument);
        var room = CreateRoom();
        var course = CreateCourse(teacher, courseType, room: room);
        var enrollment = CreateEnrollment(student, course);
        var enrollments = new List<Enrollment> { enrollment };

        var mockEnrollmentRepo = MockHelpers.CreateMockRepository(enrollments);
        _mockUnitOfWork.Setup(u => u.Repository<Enrollment>()).Returns(mockEnrollmentRepo.Object);

        // Act
        var result = await _service.GetByIdAsync(enrollment.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(enrollment.Id, result.Id);
        Assert.Equal(student.FullName, result.StudentName);
        Assert.Equal(teacher.FullName, result.TeacherName);
        Assert.Equal(instrument.Name, result.InstrumentName);
        Assert.Equal(room.Name, result.RoomName);
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ReturnsNull()
    {
        // Arrange
        var enrollments = new List<Enrollment>();
        var mockEnrollmentRepo = MockHelpers.CreateMockRepository(enrollments);
        _mockUnitOfWork.Setup(u => u.Repository<Enrollment>()).Returns(mockEnrollmentRepo.Object);

        // Act
        var result = await _service.GetByIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetByStudentAsync Tests

    [Fact]
    public async Task GetByStudentAsync_WithValidStudent_ReturnsEnrollments()
    {
        // Arrange
        var student = CreateStudent();
        var teacher = CreateTeacher();
        var instrument = CreateInstrument();
        var courseType = CreateCourseType(instrument);
        var course = CreateCourse(teacher, courseType);
        var enrollments = new List<Enrollment>
        {
            CreateEnrollment(student, course)
        };

        var mockStudentRepo = MockHelpers.CreateMockStudentRepository(new List<Student> { student });
        var mockEnrollmentRepo = MockHelpers.CreateMockRepository(enrollments);
        _mockUnitOfWork.Setup(u => u.Students).Returns(mockStudentRepo.Object);
        _mockUnitOfWork.Setup(u => u.Repository<Enrollment>()).Returns(mockEnrollmentRepo.Object);

        // Act
        var result = await _service.GetByStudentAsync(student.Id);

        // Assert
        Assert.Single(result);
        Assert.Equal(enrollments[0].Id, result.First().Id);
    }

    [Fact]
    public async Task GetByStudentAsync_WithInvalidStudent_ReturnsEmpty()
    {
        // Arrange
        var mockStudentRepo = MockHelpers.CreateMockStudentRepository(new List<Student>());
        _mockUnitOfWork.Setup(u => u.Students).Returns(mockStudentRepo.Object);

        // Act
        var result = await _service.GetByStudentAsync(Guid.NewGuid());

        // Assert
        Assert.Empty(result);
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithValidData_CreatesActiveEnrollment()
    {
        // Arrange
        var student = CreateStudent();
        var teacher = CreateTeacher();
        var instrument = CreateInstrument();
        var courseType = CreateCourseType(instrument);
        var course = CreateCourse(teacher, courseType, isTrial: false);
        var enrollments = new List<Enrollment>();

        var mockCourseRepo = MockHelpers.CreateMockCourseRepository(new List<Course> { course });
        var mockStudentRepo = MockHelpers.CreateMockStudentRepository(new List<Student> { student });
        var mockEnrollmentRepo = MockHelpers.CreateMockRepository(enrollments);

        _mockUnitOfWork.Setup(u => u.Courses).Returns(mockCourseRepo.Object);
        _mockUnitOfWork.Setup(u => u.Students).Returns(mockStudentRepo.Object);
        _mockUnitOfWork.Setup(u => u.Repository<Enrollment>()).Returns(mockEnrollmentRepo.Object);

        _mockScheduleConflictService
            .Setup(s => s.HasConflictAsync(student.Id, course.Id))
            .ReturnsAsync(new ConflictCheckResult { HasConflict = false });

        _mockRegistrationFeeService
            .Setup(s => s.IsStudentEligibleForFeeAsync(student.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var dto = new CreateEnrollmentDto
        {
            StudentId = student.Id,
            CourseId = course.Id,
            DiscountPercent = 10,
            DiscountType = DiscountType.Family,
            InvoicingPreference = InvoicingPreference.Monthly
        };

        // Act
        var (result, notFound, error) = await _service.CreateAsync(course.Id, dto);

        // Assert
        Assert.NotNull(result);
        Assert.False(notFound);
        Assert.Null(error);
        Assert.Equal(EnrollmentStatus.Active, result.Status);
        Assert.Equal(student.Id, result.StudentId);
        Assert.Equal(course.Id, result.CourseId);
        Assert.Equal(10, result.DiscountPercent);
    }

    [Fact]
    public async Task CreateAsync_WithTrialCourse_CreatesTrailEnrollment()
    {
        // Arrange
        var student = CreateStudent();
        var teacher = CreateTeacher();
        var instrument = CreateInstrument();
        var courseType = CreateCourseType(instrument);
        var course = CreateCourse(teacher, courseType, isTrial: true);
        var enrollments = new List<Enrollment>();

        var mockCourseRepo = MockHelpers.CreateMockCourseRepository(new List<Course> { course });
        var mockStudentRepo = MockHelpers.CreateMockStudentRepository(new List<Student> { student });
        var mockEnrollmentRepo = MockHelpers.CreateMockRepository(enrollments);

        _mockUnitOfWork.Setup(u => u.Courses).Returns(mockCourseRepo.Object);
        _mockUnitOfWork.Setup(u => u.Students).Returns(mockStudentRepo.Object);
        _mockUnitOfWork.Setup(u => u.Repository<Enrollment>()).Returns(mockEnrollmentRepo.Object);

        _mockScheduleConflictService
            .Setup(s => s.HasConflictAsync(student.Id, course.Id))
            .ReturnsAsync(new ConflictCheckResult { HasConflict = false });

        var dto = new CreateEnrollmentDto
        {
            StudentId = student.Id,
            CourseId = course.Id,
            InvoicingPreference = InvoicingPreference.Monthly
        };

        // Act
        var (result, notFound, error) = await _service.CreateAsync(course.Id, dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnrollmentStatus.Trail, result.Status);
    }

    [Fact]
    public async Task CreateAsync_WithInvalidCourse_ReturnsNotFound()
    {
        // Arrange
        var mockCourseRepo = MockHelpers.CreateMockCourseRepository(new List<Course>());
        _mockUnitOfWork.Setup(u => u.Courses).Returns(mockCourseRepo.Object);

        var dto = new CreateEnrollmentDto
        {
            StudentId = Guid.NewGuid(),
            CourseId = Guid.NewGuid(),
            InvoicingPreference = InvoicingPreference.Monthly
        };

        // Act
        var (result, notFound, error) = await _service.CreateAsync(dto.CourseId, dto);

        // Assert
        Assert.Null(result);
        Assert.True(notFound);
        Assert.Equal("Course not found", error);
    }

    [Fact]
    public async Task CreateAsync_WithInvalidStudent_ReturnsNotFound()
    {
        // Arrange
        var teacher = CreateTeacher();
        var instrument = CreateInstrument();
        var courseType = CreateCourseType(instrument);
        var course = CreateCourse(teacher, courseType);

        var mockCourseRepo = MockHelpers.CreateMockCourseRepository(new List<Course> { course });
        var mockStudentRepo = MockHelpers.CreateMockStudentRepository(new List<Student>());

        _mockUnitOfWork.Setup(u => u.Courses).Returns(mockCourseRepo.Object);
        _mockUnitOfWork.Setup(u => u.Students).Returns(mockStudentRepo.Object);

        var dto = new CreateEnrollmentDto
        {
            StudentId = Guid.NewGuid(),
            CourseId = course.Id,
            InvoicingPreference = InvoicingPreference.Monthly
        };

        // Act
        var (result, notFound, error) = await _service.CreateAsync(course.Id, dto);

        // Assert
        Assert.Null(result);
        Assert.True(notFound);
        Assert.Equal("Student not found", error);
    }

    [Fact]
    public async Task CreateAsync_WithExistingEnrollment_ReturnsError()
    {
        // Arrange
        var student = CreateStudent();
        var teacher = CreateTeacher();
        var instrument = CreateInstrument();
        var courseType = CreateCourseType(instrument);
        var course = CreateCourse(teacher, courseType);
        var existingEnrollment = CreateEnrollment(student, course);

        var mockCourseRepo = MockHelpers.CreateMockCourseRepository(new List<Course> { course });
        var mockStudentRepo = MockHelpers.CreateMockStudentRepository(new List<Student> { student });
        var mockEnrollmentRepo = MockHelpers.CreateMockRepository(new List<Enrollment> { existingEnrollment });

        _mockUnitOfWork.Setup(u => u.Courses).Returns(mockCourseRepo.Object);
        _mockUnitOfWork.Setup(u => u.Students).Returns(mockStudentRepo.Object);
        _mockUnitOfWork.Setup(u => u.Repository<Enrollment>()).Returns(mockEnrollmentRepo.Object);

        var dto = new CreateEnrollmentDto
        {
            StudentId = student.Id,
            CourseId = course.Id,
            InvoicingPreference = InvoicingPreference.Monthly
        };

        // Act
        var (result, notFound, error) = await _service.CreateAsync(course.Id, dto);

        // Assert
        Assert.Null(result);
        Assert.False(notFound);
        Assert.Contains("already enrolled", error);
    }

    [Fact]
    public async Task CreateAsync_WithScheduleConflict_ReturnsError()
    {
        // Arrange
        var student = CreateStudent();
        var teacher = CreateTeacher();
        var instrument = CreateInstrument();
        var courseType = CreateCourseType(instrument);
        var course = CreateCourse(teacher, courseType);

        var mockCourseRepo = MockHelpers.CreateMockCourseRepository(new List<Course> { course });
        var mockStudentRepo = MockHelpers.CreateMockStudentRepository(new List<Student> { student });
        var mockEnrollmentRepo = MockHelpers.CreateMockRepository(new List<Enrollment>());

        _mockUnitOfWork.Setup(u => u.Courses).Returns(mockCourseRepo.Object);
        _mockUnitOfWork.Setup(u => u.Students).Returns(mockStudentRepo.Object);
        _mockUnitOfWork.Setup(u => u.Repository<Enrollment>()).Returns(mockEnrollmentRepo.Object);

        _mockScheduleConflictService
            .Setup(s => s.HasConflictAsync(student.Id, course.Id))
            .ReturnsAsync(new ConflictCheckResult { HasConflict = true });

        var dto = new CreateEnrollmentDto
        {
            StudentId = student.Id,
            CourseId = course.Id,
            InvoicingPreference = InvoicingPreference.Monthly
        };

        // Act
        var (result, notFound, error) = await _service.CreateAsync(course.Id, dto);

        // Assert
        Assert.Null(result);
        Assert.False(notFound);
        Assert.Contains("Schedule conflict", error);
    }
    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithValidData_UpdatesEnrollment()
    {
        // Arrange
        var student = CreateStudent();
        var teacher = CreateTeacher();
        var instrument = CreateInstrument();
        var courseType = CreateCourseType(instrument);
        var course = CreateCourse(teacher, courseType);
        var enrollment = CreateEnrollment(student, course);
        var enrollments = new List<Enrollment> { enrollment };

        var mockEnrollmentRepo = MockHelpers.CreateMockRepository(enrollments);
        _mockUnitOfWork.Setup(u => u.Repository<Enrollment>()).Returns(mockEnrollmentRepo.Object);

        var dto = new UpdateEnrollmentDto
        {
            DiscountPercent = 15,
            Status = EnrollmentStatus.Active,
            InvoicingPreference = InvoicingPreference.Monthly,
            Notes = "Updated notes"
        };

        // Act
        var result = await _service.UpdateAsync(enrollment.Id, dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(15, result.DiscountPercent);
        Assert.Equal(EnrollmentStatus.Active, result.Status);
        Assert.Equal(InvoicingPreference.Monthly, result.InvoicingPreference);
        Assert.Equal("Updated notes", result.Notes);
    }

    [Fact]
    public async Task UpdateAsync_WithInvalidId_ReturnsNull()
    {
        // Arrange
        var mockEnrollmentRepo = MockHelpers.CreateMockRepository(new List<Enrollment>());
        _mockUnitOfWork.Setup(u => u.Repository<Enrollment>()).Returns(mockEnrollmentRepo.Object);

        var dto = new UpdateEnrollmentDto
        {
            DiscountPercent = 10,
            Status = EnrollmentStatus.Active,
            InvoicingPreference = InvoicingPreference.Monthly
        };

        // Act
        var result = await _service.UpdateAsync(Guid.NewGuid(), dto);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region PromoteFromTrailAsync Tests

    [Fact]
    public async Task PromoteFromTrailAsync_WithValidTrailEnrollment_PromotesToActive()
    {
        // Arrange
        var student = CreateStudent();
        var teacher = CreateTeacher();
        var instrument = CreateInstrument();
        var courseType = CreateCourseType(instrument);
        var course = CreateCourse(teacher, courseType, isTrial: true);
        var enrollment = CreateEnrollment(student, course, EnrollmentStatus.Trail);
        var enrollments = new List<Enrollment> { enrollment };

        var mockEnrollmentRepo = MockHelpers.CreateMockRepository(enrollments);
        _mockUnitOfWork.Setup(u => u.Repository<Enrollment>()).Returns(mockEnrollmentRepo.Object);

        _mockRegistrationFeeService
            .Setup(s => s.IsStudentEligibleForFeeAsync(student.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _service.PromoteFromTrailAsync(enrollment.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnrollmentStatus.Active, result.Status);
    }

    [Fact]
    public async Task PromoteFromTrailAsync_WithInvalidId_ReturnsNull()
    {
        // Arrange
        var mockEnrollmentRepo = MockHelpers.CreateMockRepository(new List<Enrollment>());
        _mockUnitOfWork.Setup(u => u.Repository<Enrollment>()).Returns(mockEnrollmentRepo.Object);

        // Act
        var result = await _service.PromoteFromTrailAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task PromoteFromTrailAsync_WithNonTrailEnrollment_ThrowsException()
    {
        // Arrange
        var student = CreateStudent();
        var teacher = CreateTeacher();
        var instrument = CreateInstrument();
        var courseType = CreateCourseType(instrument);
        var course = CreateCourse(teacher, courseType);
        var enrollment = CreateEnrollment(student, course, EnrollmentStatus.Active);
        var enrollments = new List<Enrollment> { enrollment };

        var mockEnrollmentRepo = MockHelpers.CreateMockRepository(enrollments);
        _mockUnitOfWork.Setup(u => u.Repository<Enrollment>()).Returns(mockEnrollmentRepo.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.PromoteFromTrailAsync(enrollment.Id));
        Assert.Contains("Only trial enrollments can be promoted", exception.Message);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithValidId_SetsStatusToWithdrawn()
    {
        // Arrange
        var student = CreateStudent();
        var teacher = CreateTeacher();
        var instrument = CreateInstrument();
        var courseType = CreateCourseType(instrument);
        var course = CreateCourse(teacher, courseType);
        var enrollment = CreateEnrollment(student, course);
        var enrollments = new List<Enrollment> { enrollment };

        var mockEnrollmentRepo = MockHelpers.CreateMockRepository(enrollments);
        _mockUnitOfWork.Setup(u => u.Repository<Enrollment>()).Returns(mockEnrollmentRepo.Object);

        mockEnrollmentRepo.Setup(r => r.GetByIdAsync(enrollment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(enrollment);

        // Act
        var result = await _service.DeleteAsync(enrollment.Id);

        // Assert
        Assert.True(result);
        Assert.Equal(EnrollmentStatus.Withdrawn, enrollment.Status);
    }

    [Fact]
    public async Task DeleteAsync_WithInvalidId_ReturnsFalse()
    {
        // Arrange
        var mockEnrollmentRepo = MockHelpers.CreateMockRepository(new List<Enrollment>());
        _mockUnitOfWork.Setup(u => u.Repository<Enrollment>()).Returns(mockEnrollmentRepo.Object);

        mockEnrollmentRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Enrollment?)null);

        // Act
        var result = await _service.DeleteAsync(Guid.NewGuid());

        // Assert
        Assert.False(result);
    }

    #endregion
}
