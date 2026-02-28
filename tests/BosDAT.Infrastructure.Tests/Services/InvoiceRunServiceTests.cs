using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Enums;
using BosDAT.Core.Interfaces;
using BosDAT.Core.Interfaces.Services;
using BosDAT.Core.Interfaces.Repositories;
using BosDAT.Infrastructure.Data;
using BosDAT.Infrastructure.Services;

namespace BosDAT.Infrastructure.Tests.Services;

public class InvoiceRunServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IInvoiceGenerationService> _mockGenerationService;
    private readonly InvoiceRunService _service;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly DateOnly _periodStart = new(2025, 1, 1);
    private readonly DateOnly _periodEnd = new(2025, 1, 31);

    public InvoiceRunServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"InvoiceRunServiceTest_{Guid.NewGuid()}")
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new ApplicationDbContext(options);
        _mockGenerationService = new Mock<IInvoiceGenerationService>();
        _service = new InvoiceRunService(_context, _mockGenerationService.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    #region RunBulkInvoiceGenerationAsync

    [Fact]
    public async Task RunBulkInvoiceGenerationAsync_OnSuccess_PersistsSuccessRunAndReturnsDto()
    {
        SeedEnrollments(2, InvoicingPreference.Monthly);
        var generatedInvoices = new List<InvoiceDto>
        {
            CreateInvoiceDto(100m),
            CreateInvoiceDto(75m)
        };

        _mockGenerationService
            .Setup(s => s.GenerateBatchInvoicesAsync(
                It.IsAny<GenerateBatchInvoicesDto>(), _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(generatedInvoices);

        var dto = new StartInvoiceRunDto
        {
            PeriodStart = _periodStart,
            PeriodEnd = _periodEnd,
            PeriodType = InvoicingPreference.Monthly
        };

        var result = await _service.RunBulkInvoiceGenerationAsync(dto, "admin", _userId);

        Assert.Equal(InvoiceRunStatus.Success, result.Status);
        Assert.Equal(2, result.TotalEnrollmentsProcessed);
        Assert.Equal(2, result.TotalInvoicesGenerated);
        Assert.Equal(0, result.TotalSkipped);
        Assert.Equal(175m, result.TotalAmount);
        Assert.Equal(_periodStart, result.PeriodStart);
        Assert.Equal(_periodEnd, result.PeriodEnd);

        var saved = await _context.InvoiceRuns.FindAsync(result.InvoiceRunId);
        Assert.NotNull(saved);
        Assert.Equal(InvoiceRunStatus.Success, saved.Status);
        Assert.Equal("admin", saved.InitiatedBy);
    }

    [Fact]
    public async Task RunBulkInvoiceGenerationAsync_WhenNoInvoicesGeneratedButEnrollmentsExist_ReturnsPartialSuccess()
    {
        SeedEnrollments(3, InvoicingPreference.Monthly);
        _mockGenerationService
            .Setup(s => s.GenerateBatchInvoicesAsync(
                It.IsAny<GenerateBatchInvoicesDto>(), _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<InvoiceDto>());

        var dto = new StartInvoiceRunDto
        {
            PeriodStart = _periodStart,
            PeriodEnd = _periodEnd,
            PeriodType = InvoicingPreference.Monthly
        };

        var result = await _service.RunBulkInvoiceGenerationAsync(dto, "admin", _userId);

        Assert.Equal(InvoiceRunStatus.PartialSuccess, result.Status);
        Assert.Equal(3, result.TotalEnrollmentsProcessed);
        Assert.Equal(0, result.TotalInvoicesGenerated);
    }

    [Fact]
    public async Task RunBulkInvoiceGenerationAsync_WhenNoEnrollmentsAndNoInvoices_ReturnsSuccess()
    {
        // No enrollments seeded
        _mockGenerationService
            .Setup(s => s.GenerateBatchInvoicesAsync(
                It.IsAny<GenerateBatchInvoicesDto>(), _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<InvoiceDto>());

        var dto = new StartInvoiceRunDto
        {
            PeriodStart = _periodStart,
            PeriodEnd = _periodEnd,
            PeriodType = InvoicingPreference.Monthly
        };

        var result = await _service.RunBulkInvoiceGenerationAsync(dto, "system", _userId);

        // 0 invoices and 0 enrollments → not partial, → success
        Assert.Equal(InvoiceRunStatus.Success, result.Status);
    }

    [Fact]
    public async Task RunBulkInvoiceGenerationAsync_OnGenerationException_PersistsFailedRunAndReturnsDto()
    {
        SeedEnrollments(2, InvoicingPreference.Monthly);
        _mockGenerationService
            .Setup(s => s.GenerateBatchInvoicesAsync(
                It.IsAny<GenerateBatchInvoicesDto>(), _userId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Invoice generation failed"));

        var dto = new StartInvoiceRunDto
        {
            PeriodStart = _periodStart,
            PeriodEnd = _periodEnd,
            PeriodType = InvoicingPreference.Monthly
        };

        var result = await _service.RunBulkInvoiceGenerationAsync(dto, "admin", _userId);

        Assert.Equal(InvoiceRunStatus.Failed, result.Status);
        Assert.Equal(0, result.TotalInvoicesGenerated);
        Assert.Equal(2, result.TotalFailed);

        var saved = await _context.InvoiceRuns.FindAsync(result.InvoiceRunId);
        Assert.NotNull(saved);
        Assert.Equal(InvoiceRunStatus.Failed, saved.Status);
        Assert.Equal("Invoice generation failed", saved.ErrorMessage);
    }

    [Fact]
    public async Task RunBulkInvoiceGenerationAsync_OnlyCountsMatchingPeriodTypeEnrollments()
    {
        SeedEnrollments(2, InvoicingPreference.Monthly);
        SeedEnrollments(3, InvoicingPreference.Quarterly);

        _mockGenerationService
            .Setup(s => s.GenerateBatchInvoicesAsync(
                It.IsAny<GenerateBatchInvoicesDto>(), _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<InvoiceDto>());

        var dto = new StartInvoiceRunDto
        {
            PeriodStart = _periodStart,
            PeriodEnd = _periodEnd,
            PeriodType = InvoicingPreference.Monthly
        };

        var result = await _service.RunBulkInvoiceGenerationAsync(dto, "admin", _userId);

        Assert.Equal(2, result.TotalEnrollmentsProcessed);
    }

    #endregion

    #region GetRunsAsync

    [Fact]
    public async Task GetRunsAsync_WithNoRuns_ReturnsEmptyPage()
    {
        var result = await _service.GetRunsAsync(1, 5);

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(1, result.Page);
        Assert.Equal(5, result.PageSize);
    }

    [Fact]
    public async Task GetRunsAsync_ReturnsPagedResults()
    {
        for (int i = 0; i < 7; i++)
        {
            _context.InvoiceRuns.Add(CreateInvoiceRun());
        }
        await _context.SaveChangesAsync();

        var result = await _service.GetRunsAsync(1, 5);

        Assert.Equal(5, result.Items.Count);
        Assert.Equal(7, result.TotalCount);
    }

    [Theory]
    [InlineData(0, 5, 5)]   // page < 1 → clamp to 1
    [InlineData(1, 0, 5)]   // pageSize < 1 → clamp to 5
    [InlineData(1, 100, 5)] // pageSize > 50 → clamp to 5
    public async Task GetRunsAsync_ClampsInvalidPagingParameters(int page, int pageSize, int expectedPageSize)
    {
        var result = await _service.GetRunsAsync(page, pageSize);

        Assert.Equal(expectedPageSize, result.PageSize);
    }

    #endregion

    #region GetRunByIdAsync

    [Fact]
    public async Task GetRunByIdAsync_WhenRunExists_ReturnsMappedDto()
    {
        var run = CreateInvoiceRun();
        _context.InvoiceRuns.Add(run);
        await _context.SaveChangesAsync();

        var result = await _service.GetRunByIdAsync(run.Id);

        Assert.NotNull(result);
        Assert.Equal(run.Id, result.Id);
        Assert.Equal(InvoiceRunStatus.Success, result.Status);
        Assert.Equal("admin", result.InitiatedBy);
    }

    [Fact]
    public async Task GetRunByIdAsync_WhenRunNotFound_ReturnsNull()
    {
        var result = await _service.GetRunByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    #endregion

    #region Helpers

    private void SeedEnrollments(int count, InvoicingPreference preference)
    {
        for (int i = 0; i < count; i++)
        {
            _context.Enrollments.Add(new Enrollment
            {
                Id = Guid.NewGuid(),
                StudentId = Guid.NewGuid(),
                CourseId = Guid.NewGuid(),
                Status = EnrollmentStatus.Active,
                InvoicingPreference = preference,
                EnrolledAt = DateTime.UtcNow
            });
        }
        _context.SaveChanges();
    }

    private static InvoiceDto CreateInvoiceDto(decimal total) => new()
    {
        Id = Guid.NewGuid(),
        InvoiceNumber = "INV-001",
        StudentId = Guid.NewGuid(),
        PeriodStart = new DateOnly(2025, 1, 1),
        PeriodEnd = new DateOnly(2025, 1, 31),
        Total = total,
        Status = InvoiceStatus.Draft,
        Lines = []
    };

    private static InvoiceRun CreateInvoiceRun() => new()
    {
        Id = Guid.NewGuid(),
        PeriodStart = new DateOnly(2025, 1, 1),
        PeriodEnd = new DateOnly(2025, 1, 31),
        PeriodType = InvoicingPreference.Monthly,
        TotalEnrollmentsProcessed = 5,
        TotalInvoicesGenerated = 4,
        TotalSkipped = 1,
        TotalFailed = 0,
        TotalAmount = 400m,
        DurationMs = 250,
        Status = InvoiceRunStatus.Success,
        InitiatedBy = "admin"
    };

    #endregion
}
