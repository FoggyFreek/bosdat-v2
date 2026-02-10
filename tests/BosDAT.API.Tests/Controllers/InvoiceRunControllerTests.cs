using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using BosDAT.API.Controllers;
using BosDAT.Core.DTOs;
using BosDAT.Core.Enums;
using BosDAT.Core.Interfaces;

namespace BosDAT.API.Tests.Controllers;

public class InvoiceRunControllerTests
{
    private readonly Mock<IInvoiceRunService> _mockInvoiceRunService;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly InvoiceRunController _controller;
    private readonly Guid _testUserId = Guid.NewGuid();
    private readonly Guid _testRunId = Guid.NewGuid();

    public InvoiceRunControllerTests()
    {
        _mockInvoiceRunService = new Mock<IInvoiceRunService>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockCurrentUserService.Setup(s => s.UserId).Returns(_testUserId);
        _mockCurrentUserService.Setup(s => s.UserEmail).Returns("admin@bosdat.nl");

        _controller = new InvoiceRunController(
            _mockInvoiceRunService.Object,
            _mockCurrentUserService.Object);
    }

    #region RunBulkGeneration Tests

    [Fact]
    public async Task RunBulkGeneration_WithValidData_ReturnsResult()
    {
        // Arrange
        var dto = new StartInvoiceRunDto
        {
            PeriodStart = new DateOnly(2026, 1, 1),
            PeriodEnd = new DateOnly(2026, 1, 31),
            PeriodType = InvoicingPreference.Monthly
        };

        var expectedResult = new InvoiceRunResultDto
        {
            InvoiceRunId = _testRunId,
            PeriodStart = dto.PeriodStart,
            PeriodEnd = dto.PeriodEnd,
            PeriodType = InvoicingPreference.Monthly,
            TotalEnrollmentsProcessed = 10,
            TotalInvoicesGenerated = 8,
            TotalSkipped = 2,
            TotalFailed = 0,
            TotalAmount = 968m,
            DurationMs = 1500,
            Status = InvoiceRunStatus.Success
        };

        _mockInvoiceRunService
            .Setup(s => s.RunBulkInvoiceGenerationAsync(
                dto, "admin@bosdat.nl", _testUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.RunBulkGeneration(dto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedResult = Assert.IsType<InvoiceRunResultDto>(okResult.Value);
        Assert.Equal(_testRunId, returnedResult.InvoiceRunId);
        Assert.Equal(8, returnedResult.TotalInvoicesGenerated);
        Assert.Equal(InvoiceRunStatus.Success, returnedResult.Status);
    }

    [Fact]
    public async Task RunBulkGeneration_WithQuarterlyPeriod_ReturnsResult()
    {
        // Arrange
        var dto = new StartInvoiceRunDto
        {
            PeriodStart = new DateOnly(2026, 1, 1),
            PeriodEnd = new DateOnly(2026, 3, 31),
            PeriodType = InvoicingPreference.Quarterly
        };

        var expectedResult = new InvoiceRunResultDto
        {
            InvoiceRunId = _testRunId,
            PeriodStart = dto.PeriodStart,
            PeriodEnd = dto.PeriodEnd,
            PeriodType = InvoicingPreference.Quarterly,
            TotalEnrollmentsProcessed = 5,
            TotalInvoicesGenerated = 5,
            TotalSkipped = 0,
            TotalFailed = 0,
            TotalAmount = 2420m,
            DurationMs = 3000,
            Status = InvoiceRunStatus.Success
        };

        _mockInvoiceRunService
            .Setup(s => s.RunBulkInvoiceGenerationAsync(
                dto, "admin@bosdat.nl", _testUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.RunBulkGeneration(dto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedResult = Assert.IsType<InvoiceRunResultDto>(okResult.Value);
        Assert.Equal(InvoicingPreference.Quarterly, returnedResult.PeriodType);
        Assert.Equal(5, returnedResult.TotalInvoicesGenerated);
    }

    [Fact]
    public async Task RunBulkGeneration_WithNoAuthenticatedUser_ReturnsUnauthorized()
    {
        // Arrange
        _mockCurrentUserService.Setup(s => s.UserId).Returns((Guid?)null);

        var controller = new InvoiceRunController(
            _mockInvoiceRunService.Object,
            _mockCurrentUserService.Object);

        var dto = new StartInvoiceRunDto
        {
            PeriodStart = new DateOnly(2026, 1, 1),
            PeriodEnd = new DateOnly(2026, 1, 31),
            PeriodType = InvoicingPreference.Monthly
        };

        // Act
        var result = await controller.RunBulkGeneration(dto, CancellationToken.None);

        // Assert
        Assert.IsType<UnauthorizedResult>(result.Result);
    }

    [Fact]
    public async Task RunBulkGeneration_WithFailedRun_ReturnsFailedResult()
    {
        // Arrange
        var dto = new StartInvoiceRunDto
        {
            PeriodStart = new DateOnly(2026, 1, 1),
            PeriodEnd = new DateOnly(2026, 1, 31),
            PeriodType = InvoicingPreference.Monthly
        };

        var expectedResult = new InvoiceRunResultDto
        {
            InvoiceRunId = _testRunId,
            PeriodStart = dto.PeriodStart,
            PeriodEnd = dto.PeriodEnd,
            PeriodType = InvoicingPreference.Monthly,
            TotalEnrollmentsProcessed = 10,
            TotalInvoicesGenerated = 0,
            TotalSkipped = 0,
            TotalFailed = 10,
            TotalAmount = 0,
            DurationMs = 500,
            Status = InvoiceRunStatus.Failed
        };

        _mockInvoiceRunService
            .Setup(s => s.RunBulkInvoiceGenerationAsync(
                dto, "admin@bosdat.nl", _testUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.RunBulkGeneration(dto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedResult = Assert.IsType<InvoiceRunResultDto>(okResult.Value);
        Assert.Equal(InvoiceRunStatus.Failed, returnedResult.Status);
        Assert.Equal(0, returnedResult.TotalInvoicesGenerated);
    }

    #endregion

    #region GetRuns Tests

    [Fact]
    public async Task GetRuns_ReturnsPagedRuns()
    {
        // Arrange
        var runs = new InvoiceRunsPageDto
        {
            Items = new List<InvoiceRunDto>
            {
                CreateTestInvoiceRunDto(),
                CreateTestInvoiceRunDto()
            },
            TotalCount = 2,
            Page = 1,
            PageSize = 5
        };

        _mockInvoiceRunService
            .Setup(s => s.GetRunsAsync(1, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(runs);

        // Act
        var result = await _controller.GetRuns(1, 5, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedRuns = Assert.IsType<InvoiceRunsPageDto>(okResult.Value);
        Assert.Equal(2, returnedRuns.Items.Count);
        Assert.Equal(2, returnedRuns.TotalCount);
    }

    [Fact]
    public async Task GetRuns_WithNoRuns_ReturnsEmptyPage()
    {
        // Arrange
        var runs = new InvoiceRunsPageDto
        {
            Items = new List<InvoiceRunDto>(),
            TotalCount = 0,
            Page = 1,
            PageSize = 5
        };

        _mockInvoiceRunService
            .Setup(s => s.GetRunsAsync(1, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(runs);

        // Act
        var result = await _controller.GetRuns(1, 5, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedRuns = Assert.IsType<InvoiceRunsPageDto>(okResult.Value);
        Assert.Empty(returnedRuns.Items);
        Assert.Equal(0, returnedRuns.TotalCount);
    }

    #endregion

    #region GetRunById Tests

    [Fact]
    public async Task GetRunById_WithValidId_ReturnsRun()
    {
        // Arrange
        var run = CreateTestInvoiceRunDto();

        _mockInvoiceRunService
            .Setup(s => s.GetRunByIdAsync(_testRunId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(run);

        // Act
        var result = await _controller.GetRunById(_testRunId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedRun = Assert.IsType<InvoiceRunDto>(okResult.Value);
        Assert.Equal(_testRunId, returnedRun.Id);
    }

    [Fact]
    public async Task GetRunById_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        _mockInvoiceRunService
            .Setup(s => s.GetRunByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((InvoiceRunDto?)null);

        // Act
        var result = await _controller.GetRunById(Guid.NewGuid(), CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    #endregion

    #region Helper Methods

    private InvoiceRunDto CreateTestInvoiceRunDto()
    {
        return new InvoiceRunDto
        {
            Id = _testRunId,
            PeriodStart = new DateOnly(2026, 1, 1),
            PeriodEnd = new DateOnly(2026, 1, 31),
            PeriodType = InvoicingPreference.Monthly,
            TotalEnrollmentsProcessed = 10,
            TotalInvoicesGenerated = 8,
            TotalSkipped = 2,
            TotalFailed = 0,
            TotalAmount = 968m,
            DurationMs = 1500,
            Status = InvoiceRunStatus.Success,
            InitiatedBy = "admin@bosdat.nl",
            CreatedAt = DateTime.UtcNow
        };
    }

    #endregion
}
