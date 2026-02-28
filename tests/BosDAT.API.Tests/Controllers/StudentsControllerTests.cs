using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using BosDAT.API.Controllers;
using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Interfaces;
using BosDAT.Core.Interfaces.Services;
using BosDAT.Core.Interfaces.Repositories;
using BosDAT.API.Tests.Helpers;

namespace BosDAT.API.Tests.Controllers;

public class StudentsControllerTests
{
    private readonly Mock<IStudentService> _mockStudentService;
    private readonly Mock<IDuplicateDetectionService> _mockDuplicateDetectionService;
    private readonly StudentsController _controller;

    public StudentsControllerTests()
    {
        _mockStudentService = new Mock<IStudentService>();
        _mockDuplicateDetectionService = MockHelpers.CreateMockDuplicateDetectionService();
        _controller = new StudentsController(
            _mockStudentService.Object,
            _mockDuplicateDetectionService.Object);
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
        var studentDtos = new List<StudentListDto>
        {
            new() { Id = Guid.NewGuid(), FullName = "John Doe", Email = "john@example.com", Status = StudentStatus.Active },
            new() { Id = Guid.NewGuid(), FullName = "Jane Smith", Email = "jane@example.com", Status = StudentStatus.Active }
        };
        _mockStudentService.Setup(s => s.GetAllAsync(null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(studentDtos);

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
        var studentDtos = new List<StudentListDto>
        {
            new() { Id = Guid.NewGuid(), FullName = "John Doe", Email = "john@example.com", Status = StudentStatus.Active },
            new() { Id = Guid.NewGuid(), FullName = "Johnny Walker", Email = "johnny@example.com", Status = StudentStatus.Active }
        };
        _mockStudentService.Setup(s => s.GetAllAsync("john", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(studentDtos);

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
        var studentDtos = new List<StudentListDto>
        {
            new() { Id = Guid.NewGuid(), FullName = "John Doe", Email = "john@example.com", Status = StudentStatus.Active }
        };
        _mockStudentService.Setup(s => s.GetAllAsync(null, StudentStatus.Active, It.IsAny<CancellationToken>()))
            .ReturnsAsync(studentDtos);

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
        var studentDtos = new List<StudentListDto>
        {
            new() { Id = Guid.NewGuid(), FullName = "John Doe", Email = "john@example.com", Status = StudentStatus.Active }
        };
        _mockStudentService.Setup(s => s.GetAllAsync("john", StudentStatus.Active, It.IsAny<CancellationToken>()))
            .ReturnsAsync(studentDtos);

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
        _mockStudentService.Setup(s => s.GetAllAsync("xyz", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StudentListDto>());

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
        var studentId = Guid.NewGuid();
        var studentDto = new StudentDto
        {
            Id = studentId,
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com",
            Status = StudentStatus.Active
        };
        _mockStudentService.Setup(s => s.GetByIdAsync(studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(studentDto);

        // Act
        var result = await _controller.GetById(studentId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedStudent = Assert.IsType<StudentDto>(okResult.Value);
        Assert.Equal(studentId, returnedStudent.Id);
        Assert.Equal("john@example.com", returnedStudent.Email);
    }

    [Fact]
    public async Task GetById_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var invalidId = Guid.NewGuid();
        _mockStudentService.Setup(s => s.GetByIdAsync(invalidId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((StudentDto?)null);

        // Act
        var result = await _controller.GetById(invalidId, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    #endregion

    #region GetWithEnrollments Tests

    [Fact]
    public async Task GetWithEnrollments_WithValidId_ReturnsStudentWithEnrollments()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var studentDto = new StudentDto { Id = studentId, FirstName = "John", LastName = "Doe", Email = "john@example.com" };
        var enrollments = new List<EnrollmentDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                StudentId = studentId,
                CourseId = Guid.NewGuid(),
                Status = EnrollmentStatus.Active,
                EnrolledAt = DateTime.UtcNow
            }
        };
        _mockStudentService.Setup(s => s.GetWithEnrollmentsAsync(studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((studentDto, enrollments, false));

        // Act
        var result = await _controller.GetWithEnrollments(studentId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetWithEnrollments_WithNoEnrollments_ReturnsStudentWithEmptyEnrollments()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var studentDto = new StudentDto { Id = studentId, FirstName = "John", LastName = "Doe", Email = "john@example.com" };
        var enrollments = new List<EnrollmentDto>();
        _mockStudentService.Setup(s => s.GetWithEnrollmentsAsync(studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((studentDto, enrollments, false));

        // Act
        var result = await _controller.GetWithEnrollments(studentId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetWithEnrollments_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var invalidId = Guid.NewGuid();
        _mockStudentService.Setup(s => s.GetWithEnrollmentsAsync(invalidId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, new List<EnrollmentDto>(), true));

        // Act
        var result = await _controller.GetWithEnrollments(invalidId, CancellationToken.None);

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
        var createdStudent = new StudentDto
        {
            Id = Guid.NewGuid(),
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = dto.Email,
            Status = dto.Status
        };
        _mockStudentService.Setup(s => s.CreateAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync((createdStudent, null));

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
        var dto = new CreateStudentDto
        {
            FirstName = "New",
            LastName = "Student",
            Email = "existing@example.com",
            Status = StudentStatus.Active
        };
        _mockStudentService.Setup(s => s.CreateAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, "A student with this email already exists"));

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
        var studentId = Guid.NewGuid();
        var dto = new UpdateStudentDto
        {
            FirstName = "Updated",
            LastName = "Name",
            Email = "john@example.com",
            Status = StudentStatus.Active
        };
        var updatedStudent = new StudentDto
        {
            Id = studentId,
            FirstName = "Updated",
            LastName = "Name",
            Email = "john@example.com",
            Status = StudentStatus.Active
        };
        _mockStudentService.Setup(s => s.UpdateAsync(studentId, dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync((updatedStudent, null, false));

        // Act
        var result = await _controller.Update(studentId, dto, CancellationToken.None);

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
        var invalidId = Guid.NewGuid();
        var dto = new UpdateStudentDto
        {
            FirstName = "Updated",
            LastName = "Name",
            Email = "updated@example.com",
            Status = StudentStatus.Active
        };
        _mockStudentService.Setup(s => s.UpdateAsync(invalidId, dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, null, true));

        // Act
        var result = await _controller.Update(invalidId, dto, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Update_WithDuplicateEmail_ReturnsBadRequest()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var dto = new UpdateStudentDto
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "jane@example.com",
            Status = StudentStatus.Active
        };
        _mockStudentService.Setup(s => s.UpdateAsync(studentId, dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, "A student with this email already exists", false));

        // Act
        var result = await _controller.Update(studentId, dto, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task Update_WithSameEmail_ReturnsSuccess()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var dto = new UpdateStudentDto
        {
            FirstName = "Updated",
            LastName = "Name",
            Email = "john@example.com",
            Status = StudentStatus.Active
        };
        var updatedStudent = new StudentDto
        {
            Id = studentId,
            FirstName = "Updated",
            LastName = "Name",
            Email = "john@example.com",
            Status = StudentStatus.Active
        };
        _mockStudentService.Setup(s => s.UpdateAsync(studentId, dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync((updatedStudent, null, false));

        // Act
        var result = await _controller.Update(studentId, dto, CancellationToken.None);

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
        var studentId = Guid.NewGuid();
        _mockStudentService.Setup(s => s.DeleteAsync(studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, false));

        // Act
        var result = await _controller.Delete(studentId, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var invalidId = Guid.NewGuid();
        _mockStudentService.Setup(s => s.DeleteAsync(invalidId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, true));

        // Act
        var result = await _controller.Delete(invalidId, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    #endregion

    #region GetRegistrationFeeStatus Tests

    [Fact]
    public async Task GetRegistrationFeeStatus_WithValidId_ReturnsFeeStatus()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var feeStatus = new RegistrationFeeStatusDto
        {
            HasPaid = true,
            PaidAt = DateTime.UtcNow,
            Amount = 50m
        };
        _mockStudentService.Setup(s => s.GetRegistrationFeeStatusAsync(studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((feeStatus, false));

        // Act
        var result = await _controller.GetRegistrationFeeStatus(studentId, CancellationToken.None);

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
        var studentId = Guid.NewGuid();
        var feeStatus = new RegistrationFeeStatusDto
        {
            HasPaid = false,
            PaidAt = null,
            Amount = null
        };
        _mockStudentService.Setup(s => s.GetRegistrationFeeStatusAsync(studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((feeStatus, false));

        // Act
        var result = await _controller.GetRegistrationFeeStatus(studentId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedStatus = Assert.IsType<RegistrationFeeStatusDto>(okResult.Value);
        Assert.False(returnedStatus.HasPaid);
    }

    [Fact]
    public async Task GetRegistrationFeeStatus_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var invalidId = Guid.NewGuid();
        _mockStudentService.Setup(s => s.GetRegistrationFeeStatusAsync(invalidId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, true));

        // Act
        var result = await _controller.GetRegistrationFeeStatus(invalidId, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    #endregion

    #region HasActiveEnrollments Tests

    [Fact]
    public async Task HasActiveEnrollments_WithValidIdAndActiveEnrollments_ReturnsTrue()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        _mockStudentService.Setup(s => s.HasActiveEnrollmentsAsync(studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, false));

        // Act
        var result = await _controller.HasActiveEnrollments(studentId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var hasEnrollments = Assert.IsType<bool>(okResult.Value);
        Assert.True(hasEnrollments);
    }

    [Fact]
    public async Task HasActiveEnrollments_WithValidIdAndNoActiveEnrollments_ReturnsFalse()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        _mockStudentService.Setup(s => s.HasActiveEnrollmentsAsync(studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, false));

        // Act
        var result = await _controller.HasActiveEnrollments(studentId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var hasEnrollments = Assert.IsType<bool>(okResult.Value);
        Assert.False(hasEnrollments);
    }

    [Fact]
    public async Task HasActiveEnrollments_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var invalidId = Guid.NewGuid();
        _mockStudentService.Setup(s => s.HasActiveEnrollmentsAsync(invalidId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, true));

        // Act
        var result = await _controller.HasActiveEnrollments(invalidId, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    #endregion
}
