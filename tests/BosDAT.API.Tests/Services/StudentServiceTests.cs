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

public class StudentServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IRegistrationFeeService> _mockRegistrationFeeService;
    private readonly StudentService _service;

    public StudentServiceTests()
    {
        _mockUnitOfWork = MockHelpers.CreateMockUnitOfWork();
        _mockRegistrationFeeService = new Mock<IRegistrationFeeService>();
        _service = new StudentService(_mockUnitOfWork.Object, _mockRegistrationFeeService.Object);
    }

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_WithNoFilters_ReturnsAllStudents()
    {
        var students = new List<Student> { CreateStudent("Jane", "Smith"), CreateStudent("John", "Doe") };
        var mockRepo = MockHelpers.CreateMockStudentRepository(students);
        _mockUnitOfWork.Setup(u => u.Students).Returns(mockRepo.Object);

        var result = await _service.GetAllAsync(null, null);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetAllAsync_WithSearchFilter_ReturnsMatchingStudents()
    {
        var students = new List<Student> { CreateStudent("Jane", "Smith"), CreateStudent("John", "Doe") };
        var mockRepo = MockHelpers.CreateMockStudentRepository(students);
        _mockUnitOfWork.Setup(u => u.Students).Returns(mockRepo.Object);

        var result = await _service.GetAllAsync("Jane", null);

        Assert.Single(result);
        Assert.Contains("Jane", result[0].FullName);
    }

    [Fact]
    public async Task GetAllAsync_WithStatusFilter_ReturnsMatchingStudents()
    {
        var activeStudent = CreateStudent("Jane", "Smith");
        var inactiveStudent = CreateStudent("Bob", "Jones");
        inactiveStudent.Status = StudentStatus.Inactive;
        var students = new List<Student> { activeStudent, inactiveStudent };
        var mockRepo = MockHelpers.CreateMockStudentRepository(students);
        _mockUnitOfWork.Setup(u => u.Students).Returns(mockRepo.Object);

        var result = await _service.GetAllAsync(null, StudentStatus.Active);

        Assert.Single(result);
        Assert.Equal(StudentStatus.Active, result[0].Status);
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsStudentDto()
    {
        var student = CreateStudent();
        var mockRepo = MockHelpers.CreateMockStudentRepository(new List<Student> { student });
        _mockUnitOfWork.Setup(u => u.Students).Returns(mockRepo.Object);

        var result = await _service.GetByIdAsync(student.Id);

        Assert.NotNull(result);
        Assert.Equal(student.Id, result.Id);
        Assert.Equal(student.FullName, result.FullName);
        Assert.Equal(student.Email, result.Email);
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ReturnsNull()
    {
        var mockRepo = MockHelpers.CreateMockStudentRepository(new List<Student>());
        _mockUnitOfWork.Setup(u => u.Students).Returns(mockRepo.Object);

        var result = await _service.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    #endregion

    #region GetWithEnrollmentsAsync Tests

    [Fact]
    public async Task GetWithEnrollmentsAsync_WithValidId_ReturnsStudentWithEnrollments()
    {
        var student = CreateStudent();
        var teacher = CreateTeacher();
        var instrument = CreateInstrument();
        var courseType = CreateCourseType(instrument);
        var course = CreateCourse(teacher, courseType);
        var enrollment = CreateEnrollment(student, course);
        student.Enrollments = new List<Enrollment> { enrollment };

        var mockRepo = MockHelpers.CreateMockStudentRepository(new List<Student> { student });
        _mockUnitOfWork.Setup(u => u.Students).Returns(mockRepo.Object);

        var (dto, enrollments, notFound) = await _service.GetWithEnrollmentsAsync(student.Id);

        Assert.False(notFound);
        Assert.NotNull(dto);
        Assert.Equal(student.Id, dto.Id);
        Assert.Single(enrollments);
        Assert.Equal(enrollment.Id, enrollments[0].Id);
    }

    [Fact]
    public async Task GetWithEnrollmentsAsync_WithInvalidId_ReturnsNotFound()
    {
        var mockRepo = MockHelpers.CreateMockStudentRepository(new List<Student>());
        _mockUnitOfWork.Setup(u => u.Students).Returns(mockRepo.Object);

        var (dto, enrollments, notFound) = await _service.GetWithEnrollmentsAsync(Guid.NewGuid());

        Assert.True(notFound);
        Assert.Null(dto);
        Assert.Empty(enrollments);
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithValidData_ReturnsCreatedStudent()
    {
        var mockRepo = MockHelpers.CreateMockStudentRepository(new List<Student>());
        _mockUnitOfWork.Setup(u => u.Students).Returns(mockRepo.Object);

        var dto = new CreateStudentDto
        {
            FirstName = "Jane",
            LastName = "Smith",
            Email = "jane.smith@example.com",
            Status = StudentStatus.Active
        };

        var (student, error) = await _service.CreateAsync(dto);

        Assert.Null(error);
        Assert.NotNull(student);
        Assert.Equal("Jane", student.FirstName);
        Assert.Equal("jane.smith@example.com", student.Email);
        Assert.Equal(StudentStatus.Active, student.Status);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateEmail_ReturnsError()
    {
        var existing = CreateStudent();
        existing.Email = "jane.smith@example.com";
        var mockRepo = MockHelpers.CreateMockStudentRepository(new List<Student> { existing });
        _mockUnitOfWork.Setup(u => u.Students).Returns(mockRepo.Object);

        var dto = new CreateStudentDto
        {
            FirstName = "Jane",
            LastName = "Other",
            Email = "jane.smith@example.com",
            Status = StudentStatus.Active
        };

        var (student, error) = await _service.CreateAsync(dto);

        Assert.Null(student);
        Assert.NotNull(error);
        Assert.Contains("already exists", error);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithValidData_ReturnsUpdatedStudent()
    {
        var student = CreateStudent();
        var mockRepo = MockHelpers.CreateMockStudentRepository(new List<Student> { student });
        mockRepo.Setup(r => r.UpdateAsync(It.IsAny<Student>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.Students).Returns(mockRepo.Object);

        var dto = new UpdateStudentDto
        {
            FirstName = "Updated",
            LastName = "Name",
            Email = student.Email,
            Status = StudentStatus.Active
        };

        var (result, error, notFound) = await _service.UpdateAsync(student.Id, dto);

        Assert.False(notFound);
        Assert.Null(error);
        Assert.NotNull(result);
        Assert.Equal("Updated", result.FirstName);
    }

    [Fact]
    public async Task UpdateAsync_WithInvalidId_ReturnsNotFound()
    {
        var mockRepo = MockHelpers.CreateMockStudentRepository(new List<Student>());
        _mockUnitOfWork.Setup(u => u.Students).Returns(mockRepo.Object);

        var dto = new UpdateStudentDto
        {
            FirstName = "X",
            LastName = "Y",
            Email = "x@y.com",
            Status = StudentStatus.Active
        };

        var (result, error, notFound) = await _service.UpdateAsync(Guid.NewGuid(), dto);

        Assert.True(notFound);
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_WithDuplicateEmailOfAnotherStudent_ReturnsError()
    {
        var student1 = CreateStudent("Jane", "Smith");
        student1.Email = "jane@example.com";
        var student2 = CreateStudent("Bob", "Jones");
        student2.Email = "bob@example.com";
        var mockRepo = MockHelpers.CreateMockStudentRepository(new List<Student> { student1, student2 });
        _mockUnitOfWork.Setup(u => u.Students).Returns(mockRepo.Object);

        var dto = new UpdateStudentDto
        {
            FirstName = "Jane",
            LastName = "Smith",
            Email = "bob@example.com",
            Status = StudentStatus.Active
        };

        var (result, error, notFound) = await _service.UpdateAsync(student1.Id, dto);

        Assert.False(notFound);
        Assert.Null(result);
        Assert.Contains("already exists", error);
    }

    [Fact]
    public async Task UpdateAsync_WithSameEmail_UpdatesSuccessfully()
    {
        var student = CreateStudent();
        student.Email = "same@example.com";
        var mockRepo = MockHelpers.CreateMockStudentRepository(new List<Student> { student });
        mockRepo.Setup(r => r.UpdateAsync(It.IsAny<Student>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.Students).Returns(mockRepo.Object);

        var dto = new UpdateStudentDto
        {
            FirstName = "Updated",
            LastName = student.LastName,
            Email = "same@example.com",
            Status = StudentStatus.Active
        };

        var (result, error, notFound) = await _service.UpdateAsync(student.Id, dto);

        Assert.False(notFound);
        Assert.Null(error);
        Assert.NotNull(result);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithValidId_ReturnsSuccess()
    {
        var student = CreateStudent();
        var mockRepo = MockHelpers.CreateMockStudentRepository(new List<Student> { student });
        mockRepo.Setup(r => r.DeleteAsync(It.IsAny<Student>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.Students).Returns(mockRepo.Object);

        var (success, notFound) = await _service.DeleteAsync(student.Id);

        Assert.True(success);
        Assert.False(notFound);
    }

    [Fact]
    public async Task DeleteAsync_WithInvalidId_ReturnsNotFound()
    {
        var mockRepo = MockHelpers.CreateMockStudentRepository(new List<Student>());
        _mockUnitOfWork.Setup(u => u.Students).Returns(mockRepo.Object);

        var (success, notFound) = await _service.DeleteAsync(Guid.NewGuid());

        Assert.False(success);
        Assert.True(notFound);
    }

    #endregion

    #region GetRegistrationFeeStatusAsync Tests

    [Fact]
    public async Task GetRegistrationFeeStatusAsync_WithValidId_ReturnsFeeStatus()
    {
        var student = CreateStudent();
        var mockRepo = MockHelpers.CreateMockStudentRepository(new List<Student> { student });
        _mockUnitOfWork.Setup(u => u.Students).Returns(mockRepo.Object);

        var feeStatus = new RegistrationFeeStatusDto { HasPaid = true };
        _mockRegistrationFeeService
            .Setup(s => s.GetFeeStatusAsync(student.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(feeStatus);

        var (result, notFound) = await _service.GetRegistrationFeeStatusAsync(student.Id);

        Assert.False(notFound);
        Assert.NotNull(result);
        Assert.True(result.HasPaid);
    }

    [Fact]
    public async Task GetRegistrationFeeStatusAsync_WithInvalidId_ReturnsNotFound()
    {
        var mockRepo = MockHelpers.CreateMockStudentRepository(new List<Student>());
        _mockUnitOfWork.Setup(u => u.Students).Returns(mockRepo.Object);

        var (result, notFound) = await _service.GetRegistrationFeeStatusAsync(Guid.NewGuid());

        Assert.True(notFound);
        Assert.Null(result);
    }

    #endregion

    #region HasActiveEnrollmentsAsync Tests

    [Fact]
    public async Task HasActiveEnrollmentsAsync_WithActiveEnrollments_ReturnsTrue()
    {
        var student = CreateStudent();
        var mockRepo = MockHelpers.CreateMockStudentRepository(new List<Student> { student });
        mockRepo
            .Setup(r => r.HasActiveEnrollmentsAsync(student.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockUnitOfWork.Setup(u => u.Students).Returns(mockRepo.Object);

        var (hasActive, notFound) = await _service.HasActiveEnrollmentsAsync(student.Id);

        Assert.False(notFound);
        Assert.True(hasActive);
    }

    [Fact]
    public async Task HasActiveEnrollmentsAsync_WithInvalidId_ReturnsNotFound()
    {
        var mockRepo = MockHelpers.CreateMockStudentRepository(new List<Student>());
        _mockUnitOfWork.Setup(u => u.Students).Returns(mockRepo.Object);

        var (hasActive, notFound) = await _service.HasActiveEnrollmentsAsync(Guid.NewGuid());

        Assert.True(notFound);
        Assert.Null(hasActive);
    }

    #endregion
}
