using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using BosDAT.API.Controllers;
using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Enums;
using BosDAT.Core.Interfaces;

namespace BosDAT.API.Tests.Controllers;

public class EnrollmentsControllerTests
{
    private readonly Mock<IEnrollmentService> _mockEnrollmentService;
    private readonly Mock<IEnrollmentPricingService> _mockEnrollmentPricingService;
    private readonly EnrollmentsController _controller;

    public EnrollmentsControllerTests()
    {
        _mockEnrollmentService = new Mock<IEnrollmentService>();
        _mockEnrollmentPricingService = new Mock<IEnrollmentPricingService>();
        _controller = new EnrollmentsController(
            _mockEnrollmentService.Object,
            _mockEnrollmentPricingService.Object);
    }

    #region GetAll Tests

    [Fact]
    public async Task GetAll_ReturnsOkWithEnrollments()
    {
        // Arrange
        var enrollments = new List<EnrollmentDto>
        {
            new() { Id = Guid.NewGuid(), StudentName = "Jane Smith", Status = EnrollmentStatus.Active },
            new() { Id = Guid.NewGuid(), StudentName = "John Doe", Status = EnrollmentStatus.Trail }
        };

        _mockEnrollmentService
            .Setup(s => s.GetAllAsync(null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(enrollments);

        // Act
        var result = await _controller.GetAll(null, null, null, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedEnrollments = Assert.IsAssignableFrom<IEnumerable<EnrollmentDto>>(okResult.Value);
        Assert.Equal(2, returnedEnrollments.Count());
    }

    [Fact]
    public async Task GetAll_WithStudentFilter_PassesFilterToService()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        _mockEnrollmentService
            .Setup(s => s.GetAllAsync(studentId, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EnrollmentDto>());

        // Act
        await _controller.GetAll(studentId, null, null, CancellationToken.None);

        // Assert
        _mockEnrollmentService.Verify(
            s => s.GetAllAsync(studentId, null, null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetAll_WithCourseFilter_PassesFilterToService()
    {
        // Arrange
        var courseId = Guid.NewGuid();
        _mockEnrollmentService
            .Setup(s => s.GetAllAsync(null, courseId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EnrollmentDto>());

        // Act
        await _controller.GetAll(null, courseId, null, CancellationToken.None);

        // Assert
        _mockEnrollmentService.Verify(
            s => s.GetAllAsync(null, courseId, null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetAll_WithStatusFilter_PassesFilterToService()
    {
        // Arrange
        var status = EnrollmentStatus.Active;
        _mockEnrollmentService
            .Setup(s => s.GetAllAsync(null, null, status, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EnrollmentDto>());

        // Act
        await _controller.GetAll(null, null, status, CancellationToken.None);

        // Assert
        _mockEnrollmentService.Verify(
            s => s.GetAllAsync(null, null, status, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetAll_WithMultipleFilters_PassesAllFiltersToService()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var status = EnrollmentStatus.Active;
        _mockEnrollmentService
            .Setup(s => s.GetAllAsync(studentId, courseId, status, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EnrollmentDto>());

        // Act
        await _controller.GetAll(studentId, courseId, status, CancellationToken.None);

        // Assert
        _mockEnrollmentService.Verify(
            s => s.GetAllAsync(studentId, courseId, status, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetById_WithValidId_ReturnsOkWithEnrollment()
    {
        // Arrange
        var enrollmentId = Guid.NewGuid();
        var enrollment = new EnrollmentDetailDto
        {
            Id = enrollmentId,
            StudentName = "Jane Smith",
            Status = EnrollmentStatus.Active
        };

        _mockEnrollmentService
            .Setup(s => s.GetByIdAsync(enrollmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(enrollment);

        // Act
        var result = await _controller.GetById(enrollmentId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedEnrollment = Assert.IsType<EnrollmentDetailDto>(okResult.Value);
        Assert.Equal(enrollmentId, returnedEnrollment.Id);
    }

    [Fact]
    public async Task GetById_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        _mockEnrollmentService
            .Setup(s => s.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EnrollmentDetailDto?)null);

        // Act
        var result = await _controller.GetById(Guid.NewGuid(), CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    #endregion

    #region GetByStudent Tests

    [Fact]
    public async Task GetByStudent_WithValidStudent_ReturnsOkWithEnrollments()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var enrollments = new List<StudentEnrollmentDto>
        {
            new() { Id = Guid.NewGuid(), CourseTypeName = "Piano", Status = EnrollmentStatus.Active }
        };

        _mockEnrollmentService
            .Setup(s => s.GetByStudentAsync(studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(enrollments);

        // Act
        var result = await _controller.GetByStudent(studentId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedEnrollments = Assert.IsAssignableFrom<IEnumerable<StudentEnrollmentDto>>(okResult.Value);
        Assert.Single(returnedEnrollments);
    }

    [Fact]
    public async Task GetByStudent_WithInvalidStudent_ReturnsNotFound()
    {
        // Arrange
        _mockEnrollmentService
            .Setup(s => s.GetByStudentAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StudentEnrollmentDto>());

        // Act
        var result = await _controller.GetByStudent(Guid.NewGuid(), CancellationToken.None);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.NotNull(notFoundResult.Value);
    }

    #endregion

    #region GetEnrollmentPricing Tests

    [Fact]
    public async Task GetEnrollmentPricing_WithValidData_ReturnsOkWithPricing()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var pricing = new EnrollmentPricingDto
        {
            ApplicableBasePrice = 50m,
            PricePerLesson = 45m
        };

        _mockEnrollmentPricingService
            .Setup(s => s.GetEnrollmentPricingAsync(studentId, courseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pricing);

        // Act
        var result = await _controller.GetEnrollmentPricing(studentId, courseId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedPricing = Assert.IsType<EnrollmentPricingDto>(okResult.Value);
        Assert.Equal(50m, returnedPricing.ApplicableBasePrice);
    }

    [Fact]
    public async Task GetEnrollmentPricing_WithInvalidData_ReturnsNotFound()
    {
        // Arrange
        _mockEnrollmentPricingService
            .Setup(s => s.GetEnrollmentPricingAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EnrollmentPricingDto?)null);

        // Act
        var result = await _controller.GetEnrollmentPricing(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.NotNull(notFoundResult.Value);
    }

    #endregion

    #region Create Tests

    [Fact]
    public async Task Create_WithValidData_ReturnsOkWithEnrollment()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var enrollment = new EnrollmentDto
        {
            Id = Guid.NewGuid(),
            StudentId = studentId,
            CourseId = courseId,
            Status = EnrollmentStatus.Active
        };

        _mockEnrollmentService
            .Setup(s => s.CreateAsync(courseId, It.IsAny<CreateEnrollmentDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((enrollment, false, (string?)null));

        var dto = new CreateEnrollmentDto
        {
            StudentId = studentId,
            CourseId = courseId,
            InvoicingPreference = InvoicingPreference.Monthly
        };

        // Act
        var result = await _controller.Create(dto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedEnrollment = Assert.IsType<EnrollmentDto>(okResult.Value);
        Assert.Equal(enrollment.Id, returnedEnrollment.Id);
    }

    [Fact]
    public async Task Create_WithNotFoundError_ReturnsNotFound()
    {
        // Arrange
        _mockEnrollmentService
            .Setup(s => s.CreateAsync(It.IsAny<Guid>(), It.IsAny<CreateEnrollmentDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((EnrollmentDto?)null, true, (string?)null));

        var dto = new CreateEnrollmentDto
        {
            StudentId = Guid.NewGuid(),
            CourseId = Guid.NewGuid(),
            InvoicingPreference = InvoicingPreference.Monthly
        };

        // Act
        var result = await _controller.Create(dto, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Create_WithValidationError_ReturnsBadRequest()
    {
        // Arrange
        var errorMessage = "Student is already enrolled in this course";
        _mockEnrollmentService
            .Setup(s => s.CreateAsync(It.IsAny<Guid>(), It.IsAny<CreateEnrollmentDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((EnrollmentDto?)null, false, errorMessage));

        var dto = new CreateEnrollmentDto
        {
            StudentId = Guid.NewGuid(),
            CourseId = Guid.NewGuid(),
            InvoicingPreference = InvoicingPreference.Monthly
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
    public async Task Update_WithValidData_ReturnsOkWithUpdatedEnrollment()
    {
        // Arrange
        var enrollmentId = Guid.NewGuid();
        var enrollment = new EnrollmentDto
        {
            Id = enrollmentId,
            Status = EnrollmentStatus.Active,
            DiscountPercent = 15
        };

        _mockEnrollmentService
            .Setup(s => s.UpdateAsync(enrollmentId, It.IsAny<UpdateEnrollmentDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(enrollment);

        var dto = new UpdateEnrollmentDto
        {
            DiscountPercent = 15,
            Status = EnrollmentStatus.Active,
            InvoicingPreference = InvoicingPreference.Monthly
        };

        // Act
        var result = await _controller.Update(enrollmentId, dto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedEnrollment = Assert.IsType<EnrollmentDto>(okResult.Value);
        Assert.Equal(15, returnedEnrollment.DiscountPercent);
    }

    [Fact]
    public async Task Update_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        _mockEnrollmentService
            .Setup(s => s.UpdateAsync(It.IsAny<Guid>(), It.IsAny<UpdateEnrollmentDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EnrollmentDto?)null);

        var dto = new UpdateEnrollmentDto
        {
            DiscountPercent = 10,
            Status = EnrollmentStatus.Active,
            InvoicingPreference = InvoicingPreference.Monthly
        };

        // Act
        var result = await _controller.Update(Guid.NewGuid(), dto, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    #endregion

    #region PromoteFromTrail Tests

    [Fact]
    public async Task PromoteFromTrail_WithValidTrailEnrollment_ReturnsOkWithPromotedEnrollment()
    {
        // Arrange
        var enrollmentId = Guid.NewGuid();
        var enrollment = new EnrollmentDto
        {
            Id = enrollmentId,
            Status = EnrollmentStatus.Active
        };

        _mockEnrollmentService
            .Setup(s => s.PromoteFromTrailAsync(enrollmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(enrollment);

        // Act
        var result = await _controller.PromoteFromTrail(enrollmentId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedEnrollment = Assert.IsType<EnrollmentDto>(okResult.Value);
        Assert.Equal(EnrollmentStatus.Active, returnedEnrollment.Status);
    }

    [Fact]
    public async Task PromoteFromTrail_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        _mockEnrollmentService
            .Setup(s => s.PromoteFromTrailAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EnrollmentDto?)null);

        // Act
        var result = await _controller.PromoteFromTrail(Guid.NewGuid(), CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task PromoteFromTrail_WithNonTrailEnrollment_ReturnsBadRequest()
    {
        // Arrange
        var enrollmentId = Guid.NewGuid();
        var errorMessage = "Only trial enrollments can be promoted";
        _mockEnrollmentService
            .Setup(s => s.PromoteFromTrailAsync(enrollmentId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException(errorMessage));

        // Act
        var result = await _controller.PromoteFromTrail(enrollmentId, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequestResult.Value);
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_WithValidId_ReturnsNoContent()
    {
        // Arrange
        var enrollmentId = Guid.NewGuid();
        _mockEnrollmentService
            .Setup(s => s.DeleteAsync(enrollmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Delete(enrollmentId, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        _mockEnrollmentService
            .Setup(s => s.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.Delete(Guid.NewGuid(), CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    #endregion
}
