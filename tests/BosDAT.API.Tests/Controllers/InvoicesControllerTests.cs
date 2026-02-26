using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using BosDAT.API.Controllers;
using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Enums;
using BosDAT.Core.Interfaces;

namespace BosDAT.API.Tests.Controllers;

public class InvoicesControllerTests
{
    private readonly Mock<IInvoiceService> _mockInvoiceService;
    private readonly Mock<ICreditInvoiceService> _mockCreditInvoiceService;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly Mock<IInvoicePdfService> _mockInvoicePdfService;
    private readonly Mock<IStudentTransactionService> _mockStudentTransactionService;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly InvoicesController _controller;
    private readonly Guid _testUserId = Guid.NewGuid();
    private readonly Guid _testInvoiceId = Guid.NewGuid();
    private readonly Guid _testStudentId = Guid.NewGuid();
    private readonly Guid _testEnrollmentId = Guid.NewGuid();

    public InvoicesControllerTests()
    {
        _mockInvoiceService = new Mock<IInvoiceService>();
        _mockCreditInvoiceService = new Mock<ICreditInvoiceService>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockInvoicePdfService = new Mock<IInvoicePdfService>();
        _mockStudentTransactionService = new Mock<IStudentTransactionService>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockCurrentUserService.Setup(s => s.UserId).Returns(_testUserId);

        _controller = new InvoicesController(
            _mockInvoiceService.Object,
            _mockCreditInvoiceService.Object,
            _mockCurrentUserService.Object,
            _mockInvoicePdfService.Object);
    }

    #region GetById Tests

    [Fact]
    public async Task GetById_WithValidId_ReturnsInvoice()
    {
        // Arrange
        var invoice = CreateTestInvoiceDto();

        _mockInvoiceService
            .Setup(s => s.GetInvoiceAsync(_testInvoiceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invoice);

        // Act
        var result = await _controller.GetById(_testInvoiceId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedInvoice = Assert.IsType<InvoiceDto>(okResult.Value);
        Assert.Equal(_testInvoiceId, returnedInvoice.Id);
        Assert.Equal("NMI-2026-00001", returnedInvoice.InvoiceNumber);
    }

    [Fact]
    public async Task GetById_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        _mockInvoiceService
            .Setup(s => s.GetInvoiceAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((InvoiceDto?)null);

        // Act
        var result = await _controller.GetById(Guid.NewGuid(), CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    #endregion

    #region GetByNumber Tests

    [Fact]
    public async Task GetByNumber_WithValidNumber_ReturnsInvoice()
    {
        // Arrange
        var invoice = CreateTestInvoiceDto();

        _mockInvoiceService
            .Setup(s => s.GetByInvoiceNumberAsync("NMI-2026-00001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(invoice);

        // Act
        var result = await _controller.GetByNumber("NMI-2026-00001", CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedInvoice = Assert.IsType<InvoiceDto>(okResult.Value);
        Assert.Equal("NMI-2026-00001", returnedInvoice.InvoiceNumber);
    }

    [Fact]
    public async Task GetByNumber_WithInvalidNumber_ReturnsNotFound()
    {
        // Arrange
        _mockInvoiceService
            .Setup(s => s.GetByInvoiceNumberAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((InvoiceDto?)null);

        // Act
        var result = await _controller.GetByNumber("NONEXISTENT", CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    #endregion

    #region GetByStudent Tests

    [Fact]
    public async Task GetByStudent_ReturnsAllInvoicesForStudent()
    {
        // Arrange
        var invoices = new List<InvoiceListDto>
        {
            CreateTestInvoiceListDto("NMI-2026-00001"),
            CreateTestInvoiceListDto("NMI-2026-00002")
        };

        _mockInvoiceService
            .Setup(s => s.GetStudentInvoicesAsync(_testStudentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invoices);

        // Act
        var result = await _controller.GetByStudent(_testStudentId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedInvoices = Assert.IsAssignableFrom<IReadOnlyList<InvoiceListDto>>(okResult.Value);
        Assert.Equal(2, returnedInvoices.Count);
    }

    [Fact]
    public async Task GetByStudent_WithNoInvoices_ReturnsEmptyList()
    {
        // Arrange
        _mockInvoiceService
            .Setup(s => s.GetStudentInvoicesAsync(_testStudentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<InvoiceListDto>());

        // Act
        var result = await _controller.GetByStudent(_testStudentId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedInvoices = Assert.IsAssignableFrom<IReadOnlyList<InvoiceListDto>>(okResult.Value);
        Assert.Empty(returnedInvoices);
    }

    #endregion

    #region GetByStatus Tests

    [Fact]
    public async Task GetByStatus_ReturnsInvoicesWithMatchingStatus()
    {
        // Arrange
        var invoices = new List<InvoiceListDto>
        {
            CreateTestInvoiceListDto("NMI-2026-00001", InvoiceStatus.Sent),
            CreateTestInvoiceListDto("NMI-2026-00002", InvoiceStatus.Sent)
        };

        _mockInvoiceService
            .Setup(s => s.GetByStatusAsync(InvoiceStatus.Sent, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invoices);

        // Act
        var result = await _controller.GetByStatus(InvoiceStatus.Sent, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedInvoices = Assert.IsAssignableFrom<IReadOnlyList<InvoiceListDto>>(okResult.Value);
        Assert.Equal(2, returnedInvoices.Count);
    }

    [Fact]
    public async Task GetByStatus_WithNoMatchingInvoices_ReturnsEmptyList()
    {
        // Arrange
        _mockInvoiceService
            .Setup(s => s.GetByStatusAsync(InvoiceStatus.Paid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<InvoiceListDto>());

        // Act
        var result = await _controller.GetByStatus(InvoiceStatus.Paid, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedInvoices = Assert.IsAssignableFrom<IReadOnlyList<InvoiceListDto>>(okResult.Value);
        Assert.Empty(returnedInvoices);
    }

    #endregion

    #region Generate Tests

    [Fact]
    public async Task Generate_WithValidData_ReturnsCreatedInvoice()
    {
        // Arrange
        var dto = new GenerateInvoiceDto
        {
            EnrollmentId = _testEnrollmentId,
            PeriodStart = new DateOnly(2026, 1, 1),
            PeriodEnd = new DateOnly(2026, 1, 31)
        };
        var invoice = CreateTestInvoiceDto();

        _mockInvoiceService
            .Setup(s => s.GenerateInvoiceAsync(dto, _testUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invoice);

        // Act
        var result = await _controller.Generate(dto, CancellationToken.None);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(InvoicesController.GetById), createdResult.ActionName);
        var returnedInvoice = Assert.IsType<InvoiceDto>(createdResult.Value);
        Assert.Equal(_testInvoiceId, returnedInvoice.Id);
    }

    [Fact]
    public async Task Generate_WithNoAuthenticatedUser_ReturnsUnauthorized()
    {
        // Arrange
        _mockCurrentUserService.Setup(s => s.UserId).Returns((Guid?)null);

        var controller = new InvoicesController(
            _mockInvoiceService.Object,
            _mockCreditInvoiceService.Object,
            _mockCurrentUserService.Object,
            _mockInvoicePdfService.Object);

        var dto = new GenerateInvoiceDto
        {
            EnrollmentId = _testEnrollmentId,
            PeriodStart = new DateOnly(2026, 1, 1),
            PeriodEnd = new DateOnly(2026, 1, 31)
        };

        // Act
        var result = await controller.Generate(dto, CancellationToken.None);

        // Assert
        Assert.IsType<UnauthorizedResult>(result.Result);
    }

    [Fact]
    public async Task Generate_WithInvalidOperation_ReturnsBadRequest()
    {
        // Arrange
        var dto = new GenerateInvoiceDto
        {
            EnrollmentId = _testEnrollmentId,
            PeriodStart = new DateOnly(2026, 1, 1),
            PeriodEnd = new DateOnly(2026, 1, 31)
        };

        _mockInvoiceService
            .Setup(s => s.GenerateInvoiceAsync(dto, _testUserId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("No lessons found for this period."));

        // Act
        var result = await _controller.Generate(dto, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequestResult.Value);
    }

    #endregion

    #region GenerateBatch Tests

    [Fact]
    public async Task GenerateBatch_WithValidData_ReturnsInvoices()
    {
        // Arrange
        var dto = new GenerateBatchInvoicesDto
        {
            PeriodStart = new DateOnly(2026, 1, 1),
            PeriodEnd = new DateOnly(2026, 1, 31),
            PeriodType = InvoicingPreference.Monthly
        };
        var invoices = new List<InvoiceDto> { CreateTestInvoiceDto() };

        _mockInvoiceService
            .Setup(s => s.GenerateBatchInvoicesAsync(dto, _testUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invoices);

        // Act
        var result = await _controller.GenerateBatch(dto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedInvoices = Assert.IsAssignableFrom<IReadOnlyList<InvoiceDto>>(okResult.Value);
        Assert.Single(returnedInvoices);
    }

    [Fact]
    public async Task GenerateBatch_WithNoAuthenticatedUser_ReturnsUnauthorized()
    {
        // Arrange
        _mockCurrentUserService.Setup(s => s.UserId).Returns((Guid?)null);

        var controller = new InvoicesController(
            _mockInvoiceService.Object,
            _mockCreditInvoiceService.Object,
            _mockCurrentUserService.Object,
            _mockInvoicePdfService.Object);

        var dto = new GenerateBatchInvoicesDto
        {
            PeriodStart = new DateOnly(2026, 1, 1),
            PeriodEnd = new DateOnly(2026, 1, 31),
            PeriodType = InvoicingPreference.Monthly
        };

        // Act
        var result = await controller.GenerateBatch(dto, CancellationToken.None);

        // Assert
        Assert.IsType<UnauthorizedResult>(result.Result);
    }

    [Fact]
    public async Task GenerateBatch_WithInvalidOperation_ReturnsBadRequest()
    {
        // Arrange
        var dto = new GenerateBatchInvoicesDto
        {
            PeriodStart = new DateOnly(2026, 1, 1),
            PeriodEnd = new DateOnly(2026, 1, 31),
            PeriodType = InvoicingPreference.Monthly
        };

        _mockInvoiceService
            .Setup(s => s.GenerateBatchInvoicesAsync(dto, _testUserId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("No active enrollments found."));

        // Act
        var result = await _controller.GenerateBatch(dto, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequestResult.Value);
    }

    #endregion

    #region Recalculate Tests

    [Fact]
    public async Task Recalculate_WithValidId_ReturnsRecalculatedInvoice()
    {
        // Arrange
        var invoice = CreateTestInvoiceDto();

        _mockInvoiceService
            .Setup(s => s.RecalculateInvoiceAsync(_testInvoiceId, _testUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invoice);

        // Act
        var result = await _controller.Recalculate(_testInvoiceId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedInvoice = Assert.IsType<InvoiceDto>(okResult.Value);
        Assert.Equal(_testInvoiceId, returnedInvoice.Id);
    }

    [Fact]
    public async Task Recalculate_WithNoAuthenticatedUser_ReturnsUnauthorized()
    {
        // Arrange
        _mockCurrentUserService.Setup(s => s.UserId).Returns((Guid?)null);

        var controller = new InvoicesController(
            _mockInvoiceService.Object,
            _mockCreditInvoiceService.Object,
            _mockCurrentUserService.Object,
            _mockInvoicePdfService.Object);

        // Act
        var result = await controller.Recalculate(_testInvoiceId, CancellationToken.None);

        // Assert
        Assert.IsType<UnauthorizedResult>(result.Result);
    }

    [Fact]
    public async Task Recalculate_WithInvalidOperation_ReturnsBadRequest()
    {
        // Arrange
        _mockInvoiceService
            .Setup(s => s.RecalculateInvoiceAsync(_testInvoiceId, _testUserId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Cannot recalculate a paid invoice."));

        // Act
        var result = await _controller.Recalculate(_testInvoiceId, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequestResult.Value);
    }

    #endregion

    #region GetSchoolBillingInfo Tests

    [Fact]
    public async Task GetSchoolBillingInfo_ReturnsSchoolInfo()
    {
        // Arrange
        var info = CreateTestSchoolBillingInfo();

        _mockInvoiceService
            .Setup(s => s.GetSchoolBillingInfoAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(info);

        // Act
        var result = await _controller.GetSchoolBillingInfo(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedInfo = Assert.IsType<SchoolBillingInfoDto>(okResult.Value);
        Assert.Equal("Test Music School", returnedInfo.Name);
        Assert.Equal("NL00INGB0001234567", returnedInfo.Iban);
    }

    #endregion

    #region GetForPrint Tests

    [Fact]
    public async Task GetForPrint_WithValidId_ReturnsInvoicePrintData()
    {
        // Arrange
        var invoice = CreateTestInvoiceDto();
        var schoolInfo = CreateTestSchoolBillingInfo();

        _mockInvoiceService
            .Setup(s => s.GetInvoiceAsync(_testInvoiceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invoice);

        _mockInvoiceService
            .Setup(s => s.GetSchoolBillingInfoAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(schoolInfo);

        // Act
        var result = await _controller.GetForPrint(_testInvoiceId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var printDto = Assert.IsType<InvoicePrintDto>(okResult.Value);
        Assert.Equal(_testInvoiceId, printDto.Invoice.Id);
        Assert.Equal("Test Music School", printDto.SchoolInfo.Name);
    }

    [Fact]
    public async Task GetForPrint_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        _mockInvoiceService
            .Setup(s => s.GetInvoiceAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((InvoiceDto?)null);

        // Act
        var result = await _controller.GetForPrint(Guid.NewGuid(), CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    #endregion

    #region CreateCreditInvoice Tests

    [Fact]
    public async Task CreateCreditInvoice_WithValidData_ReturnsCreatedCreditInvoice()
    {
        // Arrange
        var dto = new CreateCreditInvoiceDto
        {
            SelectedLineIds = new List<int> { 1, 2 }
        };
        var creditInvoice = CreateTestCreditInvoiceDto();

        _mockCreditInvoiceService
            .Setup(s => s.CreateCreditInvoiceAsync(_testInvoiceId, dto, _testUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(creditInvoice);

        // Act
        var result = await _controller.CreateCreditInvoice(_testInvoiceId, dto, CancellationToken.None);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(InvoicesController.GetById), createdResult.ActionName);
        var returnedInvoice = Assert.IsType<InvoiceDto>(createdResult.Value);
        Assert.True(returnedInvoice.IsCreditInvoice);
        Assert.Equal(_testInvoiceId, returnedInvoice.OriginalInvoiceId);
        Assert.Equal("NMI-2026-00001", returnedInvoice.OriginalInvoiceNumber);
    }

    [Fact]
    public async Task CreateCreditInvoice_WithNoAuthenticatedUser_ReturnsUnauthorized()
    {
        // Arrange
        _mockCurrentUserService.Setup(s => s.UserId).Returns((Guid?)null);

        var controller = new InvoicesController(
            _mockInvoiceService.Object,
            _mockCreditInvoiceService.Object,
            _mockCurrentUserService.Object,
            _mockInvoicePdfService.Object);

        var dto = new CreateCreditInvoiceDto
        {
            SelectedLineIds = new List<int> { 1 }
        };

        // Act
        var result = await controller.CreateCreditInvoice(_testInvoiceId, dto, CancellationToken.None);

        // Assert
        Assert.IsType<UnauthorizedResult>(result.Result);
    }

    [Fact]
    public async Task CreateCreditInvoice_ForDraftInvoice_ReturnsBadRequest()
    {
        // Arrange
        var dto = new CreateCreditInvoiceDto
        {
            SelectedLineIds = new List<int> { 1 }
        };

        _mockCreditInvoiceService
            .Setup(s => s.CreateCreditInvoiceAsync(_testInvoiceId, dto, _testUserId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Cannot create a credit invoice for a draft invoice."));

        // Act
        var result = await _controller.CreateCreditInvoice(_testInvoiceId, dto, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task CreateCreditInvoice_WithEmptyLineIds_ReturnsBadRequest()
    {
        // Arrange
        var dto = new CreateCreditInvoiceDto
        {
            SelectedLineIds = new List<int>()
        };

        _mockCreditInvoiceService
            .Setup(s => s.CreateCreditInvoiceAsync(_testInvoiceId, dto, _testUserId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("At least one invoice line must be selected for crediting."));

        // Act
        var result = await _controller.CreateCreditInvoice(_testInvoiceId, dto, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequestResult.Value);
    }

    #endregion

    #region ConfirmCreditInvoice Tests

    [Fact]
    public async Task ConfirmCreditInvoice_WithValidId_ReturnsConfirmedCreditInvoice()
    {
        // Arrange
        var creditInvoiceId = Guid.NewGuid();
        var confirmedInvoice = CreateTestCreditInvoiceDto(InvoiceStatus.Sent);

        _mockCreditInvoiceService
            .Setup(s => s.ConfirmCreditInvoiceAsync(creditInvoiceId, _testUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(confirmedInvoice);

        // Act
        var result = await _controller.ConfirmCreditInvoice(creditInvoiceId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedInvoice = Assert.IsType<InvoiceDto>(okResult.Value);
        Assert.True(returnedInvoice.IsCreditInvoice);
        Assert.Equal(InvoiceStatus.Sent, returnedInvoice.Status);
    }

    [Fact]
    public async Task ConfirmCreditInvoice_WithNoAuthenticatedUser_ReturnsUnauthorized()
    {
        // Arrange
        _mockCurrentUserService.Setup(s => s.UserId).Returns((Guid?)null);

        var controller = new InvoicesController(
            _mockInvoiceService.Object,
            _mockCreditInvoiceService.Object,
            _mockCurrentUserService.Object,
            _mockInvoicePdfService.Object);

        // Act
        var result = await controller.ConfirmCreditInvoice(Guid.NewGuid(), CancellationToken.None);

        // Assert
        Assert.IsType<UnauthorizedResult>(result.Result);
    }

    [Fact]
    public async Task ConfirmCreditInvoice_ForNonCreditInvoice_ReturnsBadRequest()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();

        _mockCreditInvoiceService
            .Setup(s => s.ConfirmCreditInvoiceAsync(invoiceId, _testUserId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("This invoice is not a credit invoice."));

        // Act
        var result = await _controller.ConfirmCreditInvoice(invoiceId, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequestResult.Value);
    }

    #endregion

    #region GetAvailableCredit Tests

    [Fact]
    public async Task GetAvailableCredit_ReturnsAvailableCreditAmount()
    {
        // Arrange
        _mockCreditInvoiceService
            .Setup(s => s.GetAvailableCreditAsync(_testStudentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(75.50m);

        // Act
        var result = await _controller.GetAvailableCredit(_testStudentId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(75.50m, okResult.Value);
    }

    [Fact]
    public async Task GetAvailableCredit_WhenNoCredit_ReturnsZero()
    {
        // Arrange
        _mockCreditInvoiceService
            .Setup(s => s.GetAvailableCreditAsync(_testStudentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0m);

        // Act
        var result = await _controller.GetAvailableCredit(_testStudentId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(0m, okResult.Value);
    }

    #endregion

    #region Helper Methods

    private InvoiceDto CreateTestInvoiceDto()
    {
        return new InvoiceDto
        {
            Id = _testInvoiceId,
            InvoiceNumber = "NMI-2026-00001",
            StudentId = _testStudentId,
            EnrollmentId = _testEnrollmentId,
            StudentName = "Test Student",
            StudentEmail = "test@example.com",
            IssueDate = new DateOnly(2026, 1, 15),
            DueDate = new DateOnly(2026, 2, 15),
            PeriodStart = new DateOnly(2026, 1, 1),
            PeriodEnd = new DateOnly(2026, 1, 31),
            PeriodType = InvoicingPreference.Monthly,
            Description = "jan26",
            Subtotal = 100m,
            VatAmount = 21m,
            Total = 121m,
            Status = InvoiceStatus.Draft,
            Lines = new List<InvoiceLineDto>(),
            Payments = new List<PaymentDto>(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    private static InvoiceListDto CreateTestInvoiceListDto(
        string invoiceNumber = "NMI-2026-00001",
        InvoiceStatus status = InvoiceStatus.Draft)
    {
        return new InvoiceListDto
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = invoiceNumber,
            StudentName = "Test Student",
            Description = "jan26",
            IssueDate = new DateOnly(2026, 1, 15),
            DueDate = new DateOnly(2026, 2, 15),
            PeriodStart = new DateOnly(2026, 1, 1),
            PeriodEnd = new DateOnly(2026, 1, 31),
            Total = 121m,
            Status = status,
            Balance = 121m
        };
    }

    private InvoiceDto CreateTestCreditInvoiceDto(InvoiceStatus status = InvoiceStatus.Draft)
    {
        return new InvoiceDto
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = "C-202601",
            StudentId = _testStudentId,
            EnrollmentId = _testEnrollmentId,
            StudentName = "Test Student",
            StudentEmail = "test@example.com",
            IssueDate = new DateOnly(2026, 1, 20),
            DueDate = new DateOnly(2026, 1, 20),
            Description = "Credit NMI-2026-00001",
            PeriodStart = new DateOnly(2026, 1, 1),
            PeriodEnd = new DateOnly(2026, 1, 31),
            PeriodType = InvoicingPreference.Monthly,
            Subtotal = 100m,
            VatAmount = 21m,
            Total = 121m,
            Status = status,
            IsCreditInvoice = true,
            OriginalInvoiceId = _testInvoiceId,
            OriginalInvoiceNumber = "NMI-2026-00001",
            Notes = "Creditfactuur voor factuur NMI-2026-00001",
            Lines = new List<InvoiceLineDto>(),
            Payments = new List<PaymentDto>(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    private static SchoolBillingInfoDto CreateTestSchoolBillingInfo()
    {
        return new SchoolBillingInfoDto
        {
            Name = "Test Music School",
            Address = "Teststraat 1",
            PostalCode = "1234 AB",
            City = "Amsterdam",
            Phone = "020-1234567",
            Email = "info@testmusicschool.nl",
            KvkNumber = "12345678",
            BtwNumber = "NL123456789B01",
            Iban = "NL00INGB0001234567",
            VatRate = 21m
        };
    }

    #endregion
}
