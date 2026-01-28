using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using BosDAT.API.Controllers;
using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Interfaces;
using BosDAT.API.Tests.Helpers;

namespace BosDAT.API.Tests.Controllers;

public class StudentsControllerTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IDuplicateDetectionService> _mockDuplicateDetectionService;
    private readonly Mock<IRegistrationFeeService> _mockRegistrationFeeService;
    private readonly StudentsController _controller;

    public StudentsControllerTests()
    {
        _mockUnitOfWork = MockHelpers.CreateMockUnitOfWork();
        _mockDuplicateDetectionService = MockHelpers.CreateMockDuplicateDetectionService();
        _mockRegistrationFeeService = MockHelpers.CreateMockRegistrationFeeService();
        _controller = new StudentsController(
            _mockUnitOfWork.Object,
            _mockDuplicateDetectionService.Object,
            _mockRegistrationFeeService.Object);
    }

    private static Student CreateStudent(string firstName = "John", string lastName = "Doe", string email = "john.doe@example.com")
    {
        return new Student
        {
            Id = Guid.NewGuid(),
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            Phone = "123-456-7890",
            Status = StudentStatus.Active,
            EnrolledAt = DateTime.UtcNow,
            Enrollments = new List<Enrollment>()
        };
    }

    #region GetAll Tests

    [Fact]
    public async Task GetAll_WithNoFilters_ReturnsAllStudents()
    {
        // Arrange
        var students = new List<Student>
        {
            CreateStudent("John", "Doe", "john@example.com"),
            CreateStudent("Jane", "Smith", "jane@example.com")
        };
        var mockStudentRepo = MockHelpers.CreateMockStudentRepository(students);
        _mockUnitOfWork.Setup(u => u.Students).Returns(mockStudentRepo.Object);

        // Act
        var result = await _controller.GetAll(null, null, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedStudents = Assert.IsAssignableFrom<IEnumerable<StudentListDto>>(okResult.Value);
        Assert.Equal(2, returnedStudents.Count());
    }

    [Fact]
    public async Task GetAll_WithSearchFilter_ReturnsMatchingStudents()
    {
        // Arrange
        var students = new List<Student>
        {
            CreateStudent("John", "Doe", "john@example.com"),
            CreateStudent("Jane", "Smith", "jane@example.com"),
            CreateStudent("Johnny", "Walker", "johnny@example.com")
        };
        var mockStudentRepo = MockHelpers.CreateMockStudentRepository(students);
        _mockUnitOfWork.Setup(u => u.Students).Returns(mockStudentRepo.Object);

        // Act
        var result = await _controller.GetAll("john", null, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedStudents = Assert.IsAssignableFrom<IEnumerable<StudentListDto>>(okResult.Value);
        Assert.Equal(2, returnedStudents.Count());
    }

    [Fact]
    public async Task GetAll_WithStatusFilter_ReturnsMatchingStudents()
    {
        // Arrange
        var activeStudent = CreateStudent("John", "Doe", "john@example.com");
        activeStudent.Status = StudentStatus.Active;
        var inactiveStudent = CreateStudent("Jane", "Smith", "jane@example.com");
        inactiveStudent.Status = StudentStatus.Inactive;
        var students = new List<Student> { activeStudent, inactiveStudent };

        var mockStudentRepo = MockHelpers.CreateMockStudentRepository(students);
        _mockUnitOfWork.Setup(u => u.Students).Returns(mockStudentRepo.Object);

        // Act
        var result = await _controller.GetAll(null, StudentStatus.Active, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedStudents = Assert.IsAssignableFrom<IEnumerable<StudentListDto>>(okResult.Value);
        Assert.Single(returnedStudents);
        Assert.Equal(StudentStatus.Active, returnedStudents.First().Status);
    }

    [Fact]
    public async Task GetAll_WithSearchAndStatusFilters_ReturnsMatchingStudents()
    {
        // Arrange
        var students = new List<Student>
        {
            CreateStudent("John", "Doe", "john@example.com"),
            CreateStudent("Jane", "Smith", "jane@example.com"),
            CreateStudent("Johnny", "Walker", "johnny@example.com")
        };
        students[0].Status = StudentStatus.Active;
        students[1].Status = StudentStatus.Active;
        students[2].Status = StudentStatus.Inactive;

        var mockStudentRepo = MockHelpers.CreateMockStudentRepository(students);
        _mockUnitOfWork.Setup(u => u.Students).Returns(mockStudentRepo.Object);

        // Act
        var result = await _controller.GetAll("john", StudentStatus.Active, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedStudents = Assert.IsAssignableFrom<IEnumerable<StudentListDto>>(okResult.Value);
        Assert.Single(returnedStudents);
    }

    [Fact]
    public async Task GetAll_WithNoMatchingSearch_ReturnsEmptyList()
    {
        // Arrange
        var students = new List<Student>
        {
            CreateStudent("John", "Doe", "john@example.com")
        };
        var mockStudentRepo = MockHelpers.CreateMockStudentRepository(students);
        _mockUnitOfWork.Setup(u => u.Students).Returns(mockStudentRepo.Object);

        // Act
        var result = await _controller.GetAll("xyz", null, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedStudents = Assert.IsAssignableFrom<IEnumerable<StudentListDto>>(okResult.Value);
        Assert.Empty(returnedStudents);
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetById_WithValidId_ReturnsStudent()
    {
        // Arrange
        var student = CreateStudent();
        var students = new List<Student> { student };
        var mockStudentRepo = MockHelpers.CreateMockStudentRepository(students);
        _mockUnitOfWork.Setup(u => u.Students).Returns(mockStudentRepo.Object);

        // Act
        var result = await _controller.GetById(student.Id, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedStudent = Assert.IsType<StudentDto>(okResult.Value);
        Assert.Equal(student.Id, returnedStudent.Id);
        Assert.Equal(student.Email, returnedStudent.Email);
    }

    [Fact]
    public async Task GetById_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var students = new List<Student>();
        var mockStudentRepo = MockHelpers.CreateMockStudentRepository(students);
        _mockUnitOfWork.Setup(u => u.Students).Returns(mockStudentRepo.Object);

        // Act
        var result = await _controller.GetById(Guid.NewGuid(), CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    #endregion

    #region GetWithEnrollments Tests

    [Fact]
    public async Task GetWithEnrollments_WithValidId_ReturnsStudentWithEnrollments()
    {
        // Arrange
        var student = CreateStudent();
        student.Enrollments = new List<Enrollment>
        {
            new()
            {
                Id = Guid.NewGuid(),
                StudentId = student.Id,
                CourseId = Guid.NewGuid(),
                Status = EnrollmentStatus.Active,
                EnrolledAt = DateTime.UtcNow
            }
        };
        var students = new List<Student> { student };
        var mockStudentRepo = MockHelpers.CreateMockStudentRepository(students);
        _mockUnitOfWork.Setup(u => u.Students).Returns(mockStudentRepo.Object);

        // Act
        var result = await _controller.GetWithEnrollments(student.Id, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetWithEnrollments_WithNoEnrollments_ReturnsStudentWithEmptyEnrollments()
    {
        // Arrange
        var student = CreateStudent();
        student.Enrollments = new List<Enrollment>();
        var students = new List<Student> { student };
        var mockStudentRepo = MockHelpers.CreateMockStudentRepository(students);
        _mockUnitOfWork.Setup(u => u.Students).Returns(mockStudentRepo.Object);

        // Act
        var result = await _controller.GetWithEnrollments(student.Id, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetWithEnrollments_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var students = new List<Student>();
        var mockStudentRepo = MockHelpers.CreateMockStudentRepository(students);
        _mockUnitOfWork.Setup(u => u.Students).Returns(mockStudentRepo.Object);

        // Act
        var result = await _controller.GetWithEnrollments(Guid.NewGuid(), CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    #endregion

    #region CheckDuplicates Tests

    [Fact]
    public async Task CheckDuplicates_WithDuplicatesFound_ReturnsResultWithDuplicates()
    {
        // Arrange
        var dto = new CheckDuplicatesDto
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com"
        };
        var duplicateResult = new DuplicateCheckResultDto
        {
            HasDuplicates = true,
            Duplicates = new List<DuplicateMatchDto>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    FullName = "John Doe",
                    Email = "john@example.com",
                    Status = StudentStatus.Active,
                    ConfidenceScore = 100,
                    MatchReason = "Exact email match"
                }
            }
        };
        _mockDuplicateDetectionService.Setup(s => s.CheckForDuplicatesAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(duplicateResult);

        // Act
        var result = await _controller.CheckDuplicates(dto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<DuplicateCheckResultDto>(okResult.Value);
        Assert.True(response.HasDuplicates);
        Assert.Single(response.Duplicates);
    }

    [Fact]
    public async Task CheckDuplicates_WithNoDuplicates_ReturnsEmptyResult()
    {
        // Arrange
        var dto = new CheckDuplicatesDto
        {
            FirstName = "Unique",
            LastName = "Person",
            Email = "unique@example.com"
        };
        var duplicateResult = new DuplicateCheckResultDto
        {
            HasDuplicates = false,
            Duplicates = new List<DuplicateMatchDto>()
        };
        _mockDuplicateDetectionService.Setup(s => s.CheckForDuplicatesAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(duplicateResult);

        // Act
        var result = await _controller.CheckDuplicates(dto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<DuplicateCheckResultDto>(okResult.Value);
        Assert.False(response.HasDuplicates);
        Assert.Empty(response.Duplicates);
    }

    #endregion

    #region Create Tests

    [Fact]
    public async Task Create_WithValidData_ReturnsCreatedStudent()
    {
        // Arrange
        var dto = new CreateStudentDto
        {
            FirstName = "New",
            LastName = "Student",
            Email = "new@example.com",
            Status = StudentStatus.Active
        };
        var students = new List<Student>();
        var mockStudentRepo = MockHelpers.CreateMockStudentRepository(students);
        _mockUnitOfWork.Setup(u => u.Students).Returns(mockStudentRepo.Object);

        // Act
        var result = await _controller.Create(dto, CancellationToken.None);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(StudentsController.GetById), createdResult.ActionName);
        var returnedStudent = Assert.IsType<StudentDto>(createdResult.Value);
        Assert.Equal(dto.Email, returnedStudent.Email);
        Assert.Equal(dto.FirstName, returnedStudent.FirstName);
    }

    [Fact]
    public async Task Create_WithDuplicateEmail_ReturnsBadRequest()
    {
        // Arrange
        var existingStudent = CreateStudent("Existing", "Student", "existing@example.com");
        var students = new List<Student> { existingStudent };
        var mockStudentRepo = MockHelpers.CreateMockStudentRepository(students);
        _mockUnitOfWork.Setup(u => u.Students).Returns(mockStudentRepo.Object);

        var dto = new CreateStudentDto
        {
            FirstName = "New",
            LastName = "Student",
            Email = "existing@example.com",
            Status = StudentStatus.Active
        };

        // Act
        var result = await _controller.Create(dto, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequestResult.Value);
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_WithValidData_ReturnsUpdatedStudent()
    {
        // Arrange
        var student = CreateStudent();
        var students = new List<Student> { student };
        var mockStudentRepo = MockHelpers.CreateMockStudentRepository(students);
        _mockUnitOfWork.Setup(u => u.Students).Returns(mockStudentRepo.Object);

        var dto = new UpdateStudentDto
        {
            FirstName = "Updated",
            LastName = "Name",
            Email = student.Email,
            Status = StudentStatus.Active
        };

        // Act
        var result = await _controller.Update(student.Id, dto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedStudent = Assert.IsType<StudentDto>(okResult.Value);
        Assert.Equal("Updated", returnedStudent.FirstName);
        Assert.Equal("Name", returnedStudent.LastName);
    }

    [Fact]
    public async Task Update_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var students = new List<Student>();
        var mockStudentRepo = MockHelpers.CreateMockStudentRepository(students);
        _mockUnitOfWork.Setup(u => u.Students).Returns(mockStudentRepo.Object);

        var dto = new UpdateStudentDto
        {
            FirstName = "Updated",
            LastName = "Name",
            Email = "updated@example.com",
            Status = StudentStatus.Active
        };

        // Act
        var result = await _controller.Update(Guid.NewGuid(), dto, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Update_WithDuplicateEmail_ReturnsBadRequest()
    {
        // Arrange
        var student1 = CreateStudent("John", "Doe", "john@example.com");
        var student2 = CreateStudent("Jane", "Smith", "jane@example.com");
        var students = new List<Student> { student1, student2 };
        var mockStudentRepo = MockHelpers.CreateMockStudentRepository(students);
        _mockUnitOfWork.Setup(u => u.Students).Returns(mockStudentRepo.Object);

        var dto = new UpdateStudentDto
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "jane@example.com", // Trying to use student2's email
            Status = StudentStatus.Active
        };

        // Act
        var result = await _controller.Update(student1.Id, dto, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task Update_WithSameEmail_ReturnsSuccess()
    {
        // Arrange
        var student = CreateStudent();
        var students = new List<Student> { student };
        var mockStudentRepo = MockHelpers.CreateMockStudentRepository(students);
        _mockUnitOfWork.Setup(u => u.Students).Returns(mockStudentRepo.Object);

        var dto = new UpdateStudentDto
        {
            FirstName = "Updated",
            LastName = "Name",
            Email = student.Email, // Same email
            Status = StudentStatus.Active
        };

        // Act
        var result = await _controller.Update(student.Id, dto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.NotNull(okResult.Value);
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_WithValidId_ReturnsNoContent()
    {
        // Arrange
        var student = CreateStudent();
        var students = new List<Student> { student };
        var mockStudentRepo = MockHelpers.CreateMockStudentRepository(students);
        _mockUnitOfWork.Setup(u => u.Students).Returns(mockStudentRepo.Object);

        // Act
        var result = await _controller.Delete(student.Id, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var students = new List<Student>();
        var mockStudentRepo = MockHelpers.CreateMockStudentRepository(students);
        _mockUnitOfWork.Setup(u => u.Students).Returns(mockStudentRepo.Object);

        // Act
        var result = await _controller.Delete(Guid.NewGuid(), CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    #endregion

    #region GetRegistrationFeeStatus Tests

    [Fact]
    public async Task GetRegistrationFeeStatus_WithValidId_ReturnsFeeStatus()
    {
        // Arrange
        var student = CreateStudent();
        var students = new List<Student> { student };
        var mockStudentRepo = MockHelpers.CreateMockStudentRepository(students);
        _mockUnitOfWork.Setup(u => u.Students).Returns(mockStudentRepo.Object);

        var feeStatus = new RegistrationFeeStatusDto
        {
            HasPaid = true,
            PaidAt = DateTime.UtcNow,
            Amount = 50m
        };
        _mockRegistrationFeeService.Setup(s => s.GetFeeStatusAsync(student.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(feeStatus);

        // Act
        var result = await _controller.GetRegistrationFeeStatus(student.Id, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedStatus = Assert.IsType<RegistrationFeeStatusDto>(okResult.Value);
        Assert.True(returnedStatus.HasPaid);
        Assert.Equal(50m, returnedStatus.Amount);
    }

    [Fact]
    public async Task GetRegistrationFeeStatus_WithUnpaidFee_ReturnsUnpaidStatus()
    {
        // Arrange
        var student = CreateStudent();
        var students = new List<Student> { student };
        var mockStudentRepo = MockHelpers.CreateMockStudentRepository(students);
        _mockUnitOfWork.Setup(u => u.Students).Returns(mockStudentRepo.Object);

        var feeStatus = new RegistrationFeeStatusDto
        {
            HasPaid = false,
            PaidAt = null,
            Amount = null
        };
        _mockRegistrationFeeService.Setup(s => s.GetFeeStatusAsync(student.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(feeStatus);

        // Act
        var result = await _controller.GetRegistrationFeeStatus(student.Id, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedStatus = Assert.IsType<RegistrationFeeStatusDto>(okResult.Value);
        Assert.False(returnedStatus.HasPaid);
    }

    [Fact]
    public async Task GetRegistrationFeeStatus_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var students = new List<Student>();
        var mockStudentRepo = MockHelpers.CreateMockStudentRepository(students);
        _mockUnitOfWork.Setup(u => u.Students).Returns(mockStudentRepo.Object);

        // Act
        var result = await _controller.GetRegistrationFeeStatus(Guid.NewGuid(), CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    #endregion

    #region HasActiveEnrollments Tests

    [Fact]
    public async Task HasActiveEnrollments_WithValidIdAndActiveEnrollments_ReturnsTrue()
    {
        // Arrange
        var student = CreateStudent();
        var students = new List<Student> { student };
        var mockStudentRepo = MockHelpers.CreateMockStudentRepository(students);
        mockStudentRepo.Setup(r => r.HasActiveEnrollmentsAsync(student.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockUnitOfWork.Setup(u => u.Students).Returns(mockStudentRepo.Object);

        // Act
        var result = await _controller.HasActiveEnrollments(student.Id, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var hasEnrollments = Assert.IsType<bool>(okResult.Value);
        Assert.True(hasEnrollments);
    }

    [Fact]
    public async Task HasActiveEnrollments_WithValidIdAndNoActiveEnrollments_ReturnsFalse()
    {
        // Arrange
        var student = CreateStudent();
        var students = new List<Student> { student };
        var mockStudentRepo = MockHelpers.CreateMockStudentRepository(students);
        mockStudentRepo.Setup(r => r.HasActiveEnrollmentsAsync(student.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockUnitOfWork.Setup(u => u.Students).Returns(mockStudentRepo.Object);

        // Act
        var result = await _controller.HasActiveEnrollments(student.Id, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var hasEnrollments = Assert.IsType<bool>(okResult.Value);
        Assert.False(hasEnrollments);
    }

    [Fact]
    public async Task HasActiveEnrollments_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var students = new List<Student>();
        var mockStudentRepo = MockHelpers.CreateMockStudentRepository(students);
        _mockUnitOfWork.Setup(u => u.Students).Returns(mockStudentRepo.Object);

        // Act
        var result = await _controller.HasActiveEnrollments(Guid.NewGuid(), CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    #endregion
}
