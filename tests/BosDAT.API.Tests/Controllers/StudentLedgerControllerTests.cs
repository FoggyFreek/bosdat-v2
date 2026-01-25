using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using BosDAT.API.Controllers;
using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Interfaces;

namespace BosDAT.API.Tests.Controllers;

public class StudentLedgerControllerTests
{
    private readonly Mock<IStudentLedgerService> _mockLedgerService;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly StudentLedgerController _controller;
    private readonly Guid _testUserId = Guid.NewGuid();
    private readonly Guid _testStudentId = Guid.NewGuid();
    private readonly Guid _testEntryId = Guid.NewGuid();
    private readonly Guid _testInvoiceId = Guid.NewGuid();

    public StudentLedgerControllerTests()
    {
        _mockLedgerService = new Mock<IStudentLedgerService>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockCurrentUserService.Setup(s => s.UserId).Returns(_testUserId);

        _controller = new StudentLedgerController(
            _mockLedgerService.Object,
            _mockCurrentUserService.Object);
    }

    #region GetByStudent Tests

    [Fact]
    public async Task GetByStudent_ReturnsAllEntriesForStudent()
    {
        // Arrange
        var entries = new List<StudentLedgerEntryDto>
        {
            CreateTestEntryDto(LedgerEntryType.Credit, 100m),
            CreateTestEntryDto(LedgerEntryType.Debit, 50m)
        };

        _mockLedgerService
            .Setup(s => s.GetStudentLedgerAsync(_testStudentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entries);

        // Act
        var result = await _controller.GetByStudent(_testStudentId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedEntries = Assert.IsAssignableFrom<IReadOnlyList<StudentLedgerEntryDto>>(okResult.Value);
        Assert.Equal(2, returnedEntries.Count);
    }

    [Fact]
    public async Task GetByStudent_WithNoEntries_ReturnsEmptyList()
    {
        // Arrange
        _mockLedgerService
            .Setup(s => s.GetStudentLedgerAsync(_testStudentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StudentLedgerEntryDto>());

        // Act
        var result = await _controller.GetByStudent(_testStudentId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedEntries = Assert.IsAssignableFrom<IReadOnlyList<StudentLedgerEntryDto>>(okResult.Value);
        Assert.Empty(returnedEntries);
    }

    #endregion

    #region GetStudentSummary Tests

    [Fact]
    public async Task GetStudentSummary_ReturnsCorrectSummary()
    {
        // Arrange
        var summary = new StudentLedgerSummaryDto
        {
            StudentId = _testStudentId,
            StudentName = "Test Student",
            TotalCredits = 200m,
            TotalDebits = 50m,
            AvailableCredit = 150m,
            OpenEntryCount = 2
        };

        _mockLedgerService
            .Setup(s => s.GetStudentLedgerSummaryAsync(_testStudentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(summary);

        // Act
        var result = await _controller.GetStudentSummary(_testStudentId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedSummary = Assert.IsType<StudentLedgerSummaryDto>(okResult.Value);
        Assert.Equal(200m, returnedSummary.TotalCredits);
        Assert.Equal(50m, returnedSummary.TotalDebits);
        Assert.Equal(150m, returnedSummary.AvailableCredit);
    }

    [Fact]
    public async Task GetStudentSummary_WithInvalidStudentId_ReturnsNotFound()
    {
        // Arrange
        _mockLedgerService
            .Setup(s => s.GetStudentLedgerSummaryAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Student not found"));

        // Act
        var result = await _controller.GetStudentSummary(Guid.NewGuid(), CancellationToken.None);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.NotNull(notFoundResult.Value);
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetById_WithValidId_ReturnsEntry()
    {
        // Arrange
        var entry = CreateTestEntryDto(LedgerEntryType.Credit, 100m);

        _mockLedgerService
            .Setup(s => s.GetEntryAsync(_testEntryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entry);

        // Act
        var result = await _controller.GetById(_testEntryId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedEntry = Assert.IsType<StudentLedgerEntryDto>(okResult.Value);
        Assert.Equal(_testEntryId, returnedEntry.Id);
    }

    [Fact]
    public async Task GetById_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        _mockLedgerService
            .Setup(s => s.GetEntryAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((StudentLedgerEntryDto?)null);

        // Act
        var result = await _controller.GetById(Guid.NewGuid(), CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    #endregion

    #region Create Tests

    [Fact]
    public async Task Create_WithValidData_ReturnsCreatedEntry()
    {
        // Arrange
        var createDto = new CreateStudentLedgerEntryDto
        {
            StudentId = _testStudentId,
            Description = "Test credit",
            Amount = 100m,
            EntryType = LedgerEntryType.Credit
        };

        var createdEntry = CreateTestEntryDto(LedgerEntryType.Credit, 100m);

        _mockLedgerService
            .Setup(s => s.CreateEntryAsync(createDto, _testUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdEntry);

        // Act
        var result = await _controller.Create(createDto, CancellationToken.None);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var returnedEntry = Assert.IsType<StudentLedgerEntryDto>(createdResult.Value);
        Assert.Equal(100m, returnedEntry.Amount);
        Assert.Equal(LedgerEntryType.Credit, returnedEntry.EntryType);
    }

    [Fact]
    public async Task Create_WithInvalidAmount_ReturnsBadRequest()
    {
        // Arrange
        var createDto = new CreateStudentLedgerEntryDto
        {
            StudentId = _testStudentId,
            Description = "Test credit",
            Amount = 0m, // Invalid amount
            EntryType = LedgerEntryType.Credit
        };

        _mockLedgerService
            .Setup(s => s.CreateEntryAsync(createDto, _testUserId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Amount must be greater than zero."));

        // Act
        var result = await _controller.Create(createDto, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task Create_WithInvalidStudentId_ReturnsBadRequest()
    {
        // Arrange
        var createDto = new CreateStudentLedgerEntryDto
        {
            StudentId = Guid.NewGuid(),
            Description = "Test credit",
            Amount = 100m,
            EntryType = LedgerEntryType.Credit
        };

        _mockLedgerService
            .Setup(s => s.CreateEntryAsync(createDto, _testUserId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Student not found"));

        // Act
        var result = await _controller.Create(createDto, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task Create_WithNoAuthenticatedUser_ReturnsUnauthorized()
    {
        // Arrange
        _mockCurrentUserService.Setup(s => s.UserId).Returns((Guid?)null);

        var controller = new StudentLedgerController(
            _mockLedgerService.Object,
            _mockCurrentUserService.Object);

        var createDto = new CreateStudentLedgerEntryDto
        {
            StudentId = _testStudentId,
            Description = "Test credit",
            Amount = 100m,
            EntryType = LedgerEntryType.Credit
        };

        // Act
        var result = await controller.Create(createDto, CancellationToken.None);

        // Assert
        Assert.IsType<UnauthorizedResult>(result.Result);
    }

    #endregion

    #region Reverse Tests

    [Fact]
    public async Task Reverse_WithValidEntry_ReturnsReversalEntry()
    {
        // Arrange
        var reverseDto = new ReverseLedgerEntryDto { Reason = "Customer refund" };
        var reversalEntry = CreateTestEntryDto(LedgerEntryType.Debit, 100m);
        reversalEntry = reversalEntry with
        {
            Description = $"Reversal of CR-2026-0001: Customer refund"
        };

        _mockLedgerService
            .Setup(s => s.ReverseEntryAsync(_testEntryId, reverseDto.Reason, _testUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reversalEntry);

        // Act
        var result = await _controller.Reverse(_testEntryId, reverseDto, CancellationToken.None);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var returnedEntry = Assert.IsType<StudentLedgerEntryDto>(createdResult.Value);
        Assert.Contains("Reversal", returnedEntry.Description);
    }

    [Fact]
    public async Task Reverse_WithFullyAppliedEntry_ReturnsBadRequest()
    {
        // Arrange
        var reverseDto = new ReverseLedgerEntryDto { Reason = "Customer refund" };

        _mockLedgerService
            .Setup(s => s.ReverseEntryAsync(_testEntryId, reverseDto.Reason, _testUserId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Cannot reverse a fully applied entry."));

        // Act
        var result = await _controller.Reverse(_testEntryId, reverseDto, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task Reverse_WithNoAuthenticatedUser_ReturnsUnauthorized()
    {
        // Arrange
        _mockCurrentUserService.Setup(s => s.UserId).Returns((Guid?)null);

        var controller = new StudentLedgerController(
            _mockLedgerService.Object,
            _mockCurrentUserService.Object);

        var reverseDto = new ReverseLedgerEntryDto { Reason = "Customer refund" };

        // Act
        var result = await controller.Reverse(_testEntryId, reverseDto, CancellationToken.None);

        // Assert
        Assert.IsType<UnauthorizedResult>(result.Result);
    }

    [Fact]
    public async Task Reverse_WithMissingReason_ReturnsBadRequest()
    {
        // Arrange
        var reverseDto = new ReverseLedgerEntryDto { Reason = "" };

        // Act
        var result = await _controller.Reverse(_testEntryId, reverseDto, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task Reverse_WithWhitespaceReason_ReturnsBadRequest()
    {
        // Arrange
        var reverseDto = new ReverseLedgerEntryDto { Reason = "   " };

        // Act
        var result = await _controller.Reverse(_testEntryId, reverseDto, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequestResult.Value);
    }

    #endregion

    #region ApplyCreditsToInvoice Tests

    [Fact]
    public async Task ApplyCreditsToInvoice_WithAvailableCredits_ReturnsResult()
    {
        // Arrange
        var applyResult = new ApplyCreditResultDto
        {
            InvoiceId = _testInvoiceId,
            InvoiceNumber = "NMI-2026-00001",
            AmountApplied = 50m,
            RemainingBalance = 25m,
            Applications = new List<LedgerApplicationDto>
            {
                new LedgerApplicationDto
                {
                    Id = Guid.NewGuid(),
                    InvoiceId = _testInvoiceId,
                    InvoiceNumber = "NMI-2026-00001",
                    AppliedAmount = 50m,
                    AppliedAt = DateTime.UtcNow,
                    AppliedByName = "Test User"
                }
            }
        };

        _mockLedgerService
            .Setup(s => s.ApplyCreditsToInvoiceAsync(_testInvoiceId, _testUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(applyResult);

        // Act
        var result = await _controller.ApplyCreditsToInvoice(_testInvoiceId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedResult = Assert.IsType<ApplyCreditResultDto>(okResult.Value);
        Assert.Equal(50m, returnedResult.AmountApplied);
        Assert.Equal(25m, returnedResult.RemainingBalance);
        Assert.Single(returnedResult.Applications);
    }

    [Fact]
    public async Task ApplyCreditsToInvoice_WithNoAvailableCredits_ReturnsZeroApplied()
    {
        // Arrange
        var applyResult = new ApplyCreditResultDto
        {
            InvoiceId = _testInvoiceId,
            InvoiceNumber = "NMI-2026-00001",
            AmountApplied = 0m,
            RemainingBalance = 100m,
            Applications = new List<LedgerApplicationDto>()
        };

        _mockLedgerService
            .Setup(s => s.ApplyCreditsToInvoiceAsync(_testInvoiceId, _testUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(applyResult);

        // Act
        var result = await _controller.ApplyCreditsToInvoice(_testInvoiceId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedResult = Assert.IsType<ApplyCreditResultDto>(okResult.Value);
        Assert.Equal(0m, returnedResult.AmountApplied);
        Assert.Empty(returnedResult.Applications);
    }

    [Fact]
    public async Task ApplyCreditsToInvoice_WithInvalidInvoiceId_ReturnsBadRequest()
    {
        // Arrange
        _mockLedgerService
            .Setup(s => s.ApplyCreditsToInvoiceAsync(It.IsAny<Guid>(), _testUserId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Invoice not found"));

        // Act
        var result = await _controller.ApplyCreditsToInvoice(Guid.NewGuid(), CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task ApplyCreditsToInvoice_WithNoAuthenticatedUser_ReturnsUnauthorized()
    {
        // Arrange
        _mockCurrentUserService.Setup(s => s.UserId).Returns((Guid?)null);

        var controller = new StudentLedgerController(
            _mockLedgerService.Object,
            _mockCurrentUserService.Object);

        // Act
        var result = await controller.ApplyCreditsToInvoice(_testInvoiceId, CancellationToken.None);

        // Assert
        Assert.IsType<UnauthorizedResult>(result.Result);
    }

    #endregion

    #region GetAvailableCredit Tests

    [Fact]
    public async Task GetAvailableCredit_ReturnsCorrectAmount()
    {
        // Arrange
        _mockLedgerService
            .Setup(s => s.GetAvailableCreditForStudentAsync(_testStudentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(150m);

        // Act
        var result = await _controller.GetAvailableCredit(_testStudentId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var availableCreditDto = Assert.IsType<AvailableCreditDto>(okResult.Value);
        Assert.Equal(150m, availableCreditDto.AvailableCredit);
    }

    [Fact]
    public async Task GetAvailableCredit_WithNoCredits_ReturnsZero()
    {
        // Arrange
        _mockLedgerService
            .Setup(s => s.GetAvailableCreditForStudentAsync(_testStudentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0m);

        // Act
        var result = await _controller.GetAvailableCredit(_testStudentId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var availableCreditDto = Assert.IsType<AvailableCreditDto>(okResult.Value);
        Assert.Equal(0m, availableCreditDto.AvailableCredit);
    }

    #endregion

    #region Helper Methods

    private StudentLedgerEntryDto CreateTestEntryDto(LedgerEntryType entryType, decimal amount)
    {
        return new StudentLedgerEntryDto
        {
            Id = _testEntryId,
            CorrectionRefName = "CR-2026-0001",
            Description = $"Test {entryType}",
            StudentId = _testStudentId,
            StudentName = "Test Student",
            CourseId = null,
            CourseName = null,
            Amount = amount,
            EntryType = entryType,
            Status = LedgerEntryStatus.Open,
            AppliedAmount = 0m,
            RemainingAmount = amount,
            CreatedAt = DateTime.UtcNow,
            CreatedByName = "Test User",
            Applications = new List<LedgerApplicationDto>()
        };
    }

    #endregion
}
