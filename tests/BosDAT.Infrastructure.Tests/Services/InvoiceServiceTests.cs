using Microsoft.EntityFrameworkCore;
using Moq;
using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Enums;
using BosDAT.Core.Interfaces;
using BosDAT.Infrastructure.Data;
using BosDAT.Infrastructure.Services;

namespace BosDAT.Infrastructure.Tests.Services;

public class InvoiceServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ICourseTypePricingService> _mockPricingService;
    private readonly InvoiceService _service;
    private readonly Guid _userId = Guid.NewGuid();

    public InvoiceServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"InvoiceServiceTest_{Guid.NewGuid()}")
            .Options;

        _context = new ApplicationDbContext(options);
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockPricingService = new Mock<ICourseTypePricingService>();

        SetupMockInvoiceRepository();

        var queryService = new InvoiceQueryService(_context);
        var ledgerService = new InvoiceLedgerService(_context, _mockUnitOfWork.Object, queryService);
        var generationService = new InvoiceGenerationService(
            _context, _mockUnitOfWork.Object, _mockPricingService.Object, ledgerService, queryService);
        _service = new InvoiceService(generationService, ledgerService, queryService);

        SeedBaseData();
    }

    private void SetupMockInvoiceRepository()
    {
        var mockInvoiceRepository = new Mock<IInvoiceRepository>();
        var invoiceCounter = 0;

        mockInvoiceRepository
            .Setup(r => r.GenerateInvoiceNumberAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                invoiceCounter++;
                return $"{DateTime.UtcNow.Year}{invoiceCounter:D2}";
            });

        mockInvoiceRepository
            .Setup(r => r.GetByPeriodAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Invoice?)null);

        _mockUnitOfWork.Setup(u => u.Invoices).Returns(mockInvoiceRepository.Object);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _mockUnitOfWork.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
    }

    private void SeedBaseData()
    {
        _context.Settings.AddRange(
            new Setting { Key = "vat_rate", Value = "21" },
            new Setting { Key = "payment_due_days", Value = "14" },
            new Setting { Key = "school_name", Value = "Test Music School" },
            new Setting { Key = "school_address", Value = "Test Street 123" },
            new Setting { Key = "school_postal_code", Value = "1234AB" },
            new Setting { Key = "school_city", Value = "Test City" },
            new Setting { Key = "school_phone", Value = "0612345678" },
            new Setting { Key = "school_email", Value = "test@school.nl" },
            new Setting { Key = "school_kvk", Value = "12345678" },
            new Setting { Key = "school_iban", Value = "NL00TEST0000000001" }
        );

        _context.Instruments.Add(new Instrument
        {
            Id = 1,
            Name = "Piano",
            Category = InstrumentCategory.Keyboard
        });

        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    #region GeneratePeriodDescription Tests

    [Theory]
    [InlineData(2026, 1, 1, 2026, 1, 31, InvoicingPreference.Monthly, "jan26")]
    [InlineData(2026, 2, 1, 2026, 2, 28, InvoicingPreference.Monthly, "feb26")]
    [InlineData(2026, 12, 1, 2026, 12, 31, InvoicingPreference.Monthly, "dec26")]
    [InlineData(2025, 1, 1, 2025, 1, 31, InvoicingPreference.Monthly, "jan25")]
    public void GeneratePeriodDescription_Monthly_ReturnsCorrectFormat(
        int startYear, int startMonth, int startDay,
        int endYear, int endMonth, int endDay,
        InvoicingPreference periodType, string expected)
    {
        // Arrange
        var periodStart = new DateOnly(startYear, startMonth, startDay);
        var periodEnd = new DateOnly(endYear, endMonth, endDay);

        // Act
        var result = _service.GeneratePeriodDescription(periodStart, periodEnd, periodType);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(2026, 1, 1, 2026, 3, 31, InvoicingPreference.Quarterly, "jan-mar26")]
    [InlineData(2026, 4, 1, 2026, 6, 30, InvoicingPreference.Quarterly, "apr-jun26")]
    [InlineData(2026, 7, 1, 2026, 9, 30, InvoicingPreference.Quarterly, "jul-sep26")]
    [InlineData(2026, 10, 1, 2026, 12, 31, InvoicingPreference.Quarterly, "oct-dec26")]
    public void GeneratePeriodDescription_Quarterly_ReturnsCorrectFormat(
        int startYear, int startMonth, int startDay,
        int endYear, int endMonth, int endDay,
        InvoicingPreference periodType, string expected)
    {
        // Arrange
        var periodStart = new DateOnly(startYear, startMonth, startDay);
        var periodEnd = new DateOnly(endYear, endMonth, endDay);

        // Act
        var result = _service.GeneratePeriodDescription(periodStart, periodEnd, periodType);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GeneratePeriodDescription_YearTransition_UsesStartYearForSuffix()
    {
        // Arrange - quarterly that spans year transition
        var periodStart = new DateOnly(2025, 10, 1);
        var periodEnd = new DateOnly(2025, 12, 31);

        // Act
        var result = _service.GeneratePeriodDescription(periodStart, periodEnd, InvoicingPreference.Quarterly);

        // Assert
        Assert.Equal("oct-dec25", result);
    }

    #endregion

    #region GetSchoolBillingInfoAsync Tests

    [Fact]
    public async Task GetSchoolBillingInfoAsync_ReturnsAllSettings()
    {
        // Act
        var result = await _service.GetSchoolBillingInfoAsync();

        // Assert
        Assert.Equal("Test Music School", result.Name);
        Assert.Equal("Test Street 123", result.Address);
        Assert.Equal("1234AB", result.PostalCode);
        Assert.Equal("Test City", result.City);
        Assert.Equal("0612345678", result.Phone);
        Assert.Equal("test@school.nl", result.Email);
        Assert.Equal("12345678", result.KvkNumber);
        Assert.Equal("NL00TEST0000000001", result.Iban);
        Assert.Equal(21m, result.VatRate);
    }

    [Fact]
    public async Task GetSchoolBillingInfoAsync_WithMissingSetting_ReturnsEmptyString()
    {
        // Arrange - remove school name
        var setting = await _context.Settings.FirstAsync(s => s.Key == "school_name");
        _context.Settings.Remove(setting);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetSchoolBillingInfoAsync();

        // Assert
        Assert.Equal("", result.Name);
    }

    [Fact]
    public async Task GetSchoolBillingInfoAsync_WithMissingVatRate_Returns21AsDefault()
    {
        // Arrange - remove vat rate
        var setting = await _context.Settings.FirstAsync(s => s.Key == "vat_rate");
        _context.Settings.Remove(setting);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetSchoolBillingInfoAsync();

        // Assert
        Assert.Equal(21m, result.VatRate);
    }

    #endregion

    #region GenerateInvoiceAsync Tests

    [Fact]
    public async Task GenerateInvoiceAsync_WithNonexistentEnrollment_ThrowsInvalidOperationException()
    {
        // Arrange
        var dto = new GenerateInvoiceDto
        {
            EnrollmentId = Guid.NewGuid(),
            PeriodStart = new DateOnly(2026, 1, 1),
            PeriodEnd = new DateOnly(2026, 1, 31)
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.GenerateInvoiceAsync(dto, _userId));
        Assert.Contains("not found", exception.Message);
    }

    [Fact]
    public async Task GenerateInvoiceAsync_WithNoLessons_ThrowsInvalidOperationException()
    {
        // Arrange
        var (_, enrollment) = await SetupEnrollmentWithLessons(lessonCount: 0);

        var dto = new GenerateInvoiceDto
        {
            EnrollmentId = enrollment.Id,
            PeriodStart = new DateOnly(2026, 1, 1),
            PeriodEnd = new DateOnly(2026, 1, 31)
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.GenerateInvoiceAsync(dto, _userId));
        Assert.Contains("No invoiceable lessons", exception.Message);
    }

    [Fact]
    public async Task GenerateInvoiceAsync_WithValidData_CreatesInvoice()
    {
        // Arrange
        var (_, enrollment) = await SetupEnrollmentWithLessons(lessonCount: 4);

        var dto = new GenerateInvoiceDto
        {
            EnrollmentId = enrollment.Id,
            PeriodStart = new DateOnly(2026, 1, 1),
            PeriodEnd = new DateOnly(2026, 1, 31),
            ApplyLedgerCorrections = false
        };

        // Act
        var result = await _service.GenerateInvoiceAsync(dto, _userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(enrollment.StudentId, result.StudentId);
        Assert.Equal(enrollment.Id, result.EnrollmentId);
        Assert.Equal(4, result.Lines.Count);
        Assert.Equal(InvoiceStatus.Draft, result.Status);
    }

    [Fact]
    public async Task GenerateInvoiceAsync_CalculatesCorrectTotals()
    {
        // Arrange
        var (_, enrollment) = await SetupEnrollmentWithLessons(lessonCount: 4);

        var dto = new GenerateInvoiceDto
        {
            EnrollmentId = enrollment.Id,
            PeriodStart = new DateOnly(2026, 1, 1),
            PeriodEnd = new DateOnly(2026, 1, 31)
        };

        // Act
        var result = await _service.GenerateInvoiceAsync(dto, _userId);

        // Assert
        // 4 lessons * 25 (adult price) = 100 subtotal
        Assert.Equal(100m, result.Subtotal);
        // VAT 21% = 21
        Assert.Equal(21m, result.VatAmount);
        // Total = 121
        Assert.Equal(121m, result.Total);
    }

    [Fact]
    public async Task GenerateInvoiceAsync_MarksLessonsAsInvoiced()
    {
        // Arrange
        var (_, enrollment) = await SetupEnrollmentWithLessons(lessonCount: 4);

        var dto = new GenerateInvoiceDto
        {
            EnrollmentId = enrollment.Id,
            PeriodStart = new DateOnly(2026, 1, 1),
            PeriodEnd = new DateOnly(2026, 1, 31)
        };

        // Act
        await _service.GenerateInvoiceAsync(dto, _userId);

        // Assert
        var lessons = await _context.Lessons.Where(l => l.CourseId == enrollment.CourseId).ToListAsync();
        Assert.All(lessons, l => Assert.True(l.IsInvoiced));
    }

    [Fact]
    public async Task GenerateInvoiceAsync_WithChildStudent_UsesChildPrice()
    {
        // Arrange
        var (_, enrollment) = await SetupEnrollmentWithChildStudent(lessonCount: 4);

        var dto = new GenerateInvoiceDto
        {
            EnrollmentId = enrollment.Id,
            PeriodStart = new DateOnly(2026, 1, 1),
            PeriodEnd = new DateOnly(2026, 1, 31),
            ApplyLedgerCorrections = false
        };

        // Act
        var result = await _service.GenerateInvoiceAsync(dto, _userId);

        // Assert - 4 lessons * 20 (child price) = 80
        Assert.Equal(80m, result.Subtotal);
    }

    [Fact]
    public async Task GenerateInvoiceAsync_WithDiscount_AppliesDiscount()
    {
        // Arrange
        var (_, enrollment) = await SetupEnrollmentWithLessons(lessonCount: 4, discountPercent: 10);

        var dto = new GenerateInvoiceDto
        {
            EnrollmentId = enrollment.Id,
            PeriodStart = new DateOnly(2026, 1, 1),
            PeriodEnd = new DateOnly(2026, 1, 31),
            ApplyLedgerCorrections = false
        };

        // Act
        var result = await _service.GenerateInvoiceAsync(dto, _userId);

        // Assert - 4 lessons * (25 - 10% = 22.5) = 90
        Assert.Equal(90m, result.Subtotal);
    }

    [Fact]
    public async Task GenerateInvoiceAsync_WithChildAndDiscount_AppliesBoth()
    {
        // Arrange
        var (_, enrollment) = await SetupEnrollmentWithChildStudent(lessonCount: 4, discountPercent: 10);

        var dto = new GenerateInvoiceDto
        {
            EnrollmentId = enrollment.Id,
            PeriodStart = new DateOnly(2026, 1, 1),
            PeriodEnd = new DateOnly(2026, 1, 31),
            ApplyLedgerCorrections = false
        };

        // Act
        var result = await _service.GenerateInvoiceAsync(dto, _userId);

        // Assert - 4 lessons * (20 - 10% = 18) = 72
        Assert.Equal(72m, result.Subtotal);
    }

    [Fact]
    public async Task GenerateInvoiceAsync_DuplicatePeriod_Throws()
    {
        // Arrange
        var (student, enrollment) = await SetupEnrollmentWithLessons(lessonCount: 4);
        var existingInvoice = new Invoice
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = "202601",
            StudentId = student.Id,
            EnrollmentId = enrollment.Id,
            IssueDate = DateOnly.FromDateTime(DateTime.UtcNow),
            DueDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(14),
            Status = InvoiceStatus.Draft
        };

        // Override mock to return existing invoice
        _mockUnitOfWork.Setup(u => u.Invoices.GetByPeriodAsync(
            student.Id, enrollment.Id,
            new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingInvoice);

        var dto = new GenerateInvoiceDto
        {
            EnrollmentId = enrollment.Id,
            PeriodStart = new DateOnly(2026, 1, 1),
            PeriodEnd = new DateOnly(2026, 1, 31)
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.GenerateInvoiceAsync(dto, _userId));
        Assert.Contains("already exists", exception.Message);
    }

    [Fact]
    public async Task GenerateInvoiceAsync_NoPricingFound_Throws()
    {
        // Arrange
        var (_, enrollment) = await SetupEnrollmentWithLessons(lessonCount: 4);

        // Override pricing mocks to return null
        var courseTypeId = (await _context.Enrollments
            .Include(e => e.Course)
            .FirstAsync(e => e.Id == enrollment.Id)).Course.CourseTypeId;

        _mockPricingService
            .Setup(p => p.GetPricingForDateAsync(courseTypeId, It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CourseTypePricingVersion?)null);
        _mockPricingService
            .Setup(p => p.GetCurrentPricingAsync(courseTypeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CourseTypePricingVersion?)null);

        var dto = new GenerateInvoiceDto
        {
            EnrollmentId = enrollment.Id,
            PeriodStart = new DateOnly(2026, 1, 1),
            PeriodEnd = new DateOnly(2026, 1, 31)
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.GenerateInvoiceAsync(dto, _userId));
        Assert.Contains("No pricing found", exception.Message);
    }

    [Fact]
    public async Task GenerateInvoiceAsync_GroupCourse_IncludesAllLessons()
    {
        // Arrange
        var (_, enrollment) = await SetupEnrollmentWithGroupLessons(lessonCount: 4);

        var dto = new GenerateInvoiceDto
        {
            EnrollmentId = enrollment.Id,
            PeriodStart = new DateOnly(2026, 1, 1),
            PeriodEnd = new DateOnly(2026, 1, 31),
            ApplyLedgerCorrections = false
        };

        // Act
        var result = await _service.GenerateInvoiceAsync(dto, _userId);

        // Assert - group lessons have no StudentId filter, so all 4 are included
        Assert.Equal(4, result.Lines.Count);
    }

    [Fact]
    public async Task GenerateInvoiceAsync_IndividualCourse_OnlyIncludesStudentLessons()
    {
        // Arrange - creates individual lessons for this student + some for another
        var (student, enrollment, otherStudent) = await SetupEnrollmentWithMixedLessons();

        var dto = new GenerateInvoiceDto
        {
            EnrollmentId = enrollment.Id,
            PeriodStart = new DateOnly(2026, 1, 1),
            PeriodEnd = new DateOnly(2026, 1, 31),
            ApplyLedgerCorrections = false
        };

        // Act
        var result = await _service.GenerateInvoiceAsync(dto, _userId);

        // Assert - only 2 lessons belong to this student
        Assert.Equal(2, result.Lines.Count);
    }

    [Fact]
    public async Task GenerateInvoiceAsync_WithLedgerCredits_AppliesCredits()
    {
        // Arrange
        var (student, enrollment) = await SetupEnrollmentWithLessons(lessonCount: 4);
        await SeedLedgerEntry(student.Id, LedgerEntryType.Credit, 30m, LedgerEntryStatus.Open);

        var dto = new GenerateInvoiceDto
        {
            EnrollmentId = enrollment.Id,
            PeriodStart = new DateOnly(2026, 1, 1),
            PeriodEnd = new DateOnly(2026, 1, 31),
            ApplyLedgerCorrections = true
        };

        // Act
        var result = await _service.GenerateInvoiceAsync(dto, _userId);

        // Assert - Total is 121, credit of 30 applied
        Assert.Equal(30m, result.LedgerCreditsApplied);
        Assert.Equal(91m, result.Balance);
    }

    [Fact]
    public async Task GenerateInvoiceAsync_WithLedgerDebits_AppliesDebits()
    {
        // Arrange
        var (student, enrollment) = await SetupEnrollmentWithLessons(lessonCount: 4);
        await SeedLedgerEntry(student.Id, LedgerEntryType.Debit, 15m, LedgerEntryStatus.Open);

        var dto = new GenerateInvoiceDto
        {
            EnrollmentId = enrollment.Id,
            PeriodStart = new DateOnly(2026, 1, 1),
            PeriodEnd = new DateOnly(2026, 1, 31),
            ApplyLedgerCorrections = true
        };

        // Act
        var result = await _service.GenerateInvoiceAsync(dto, _userId);

        // Assert - Total is 121, debit of 15 adds to owed
        Assert.Equal(15m, result.LedgerDebitsApplied);
        Assert.Equal(136m, result.Balance);
    }

    [Fact]
    public async Task GenerateInvoiceAsync_WithCreditsExceedingTotal_SetsStatusToPaid()
    {
        // Arrange
        var (student, enrollment) = await SetupEnrollmentWithLessons(lessonCount: 4);
        // Total will be 121, credit of 150 exceeds it
        await SeedLedgerEntry(student.Id, LedgerEntryType.Credit, 150m, LedgerEntryStatus.Open);

        var dto = new GenerateInvoiceDto
        {
            EnrollmentId = enrollment.Id,
            PeriodStart = new DateOnly(2026, 1, 1),
            PeriodEnd = new DateOnly(2026, 1, 31),
            ApplyLedgerCorrections = true
        };

        // Act
        var result = await _service.GenerateInvoiceAsync(dto, _userId);

        // Assert - credits cover entire total, invoice should be paid
        Assert.Equal(InvoiceStatus.Paid, result.Status);
    }

    #endregion

    #region GetStudentInvoicesAsync Tests

    [Fact]
    public async Task GetStudentInvoicesAsync_ReturnsInvoicesForStudent()
    {
        // Arrange
        var (student, enrollment) = await SetupEnrollmentWithLessons(lessonCount: 4);
        var dto = new GenerateInvoiceDto
        {
            EnrollmentId = enrollment.Id,
            PeriodStart = new DateOnly(2026, 1, 1),
            PeriodEnd = new DateOnly(2026, 1, 31)
        };
        await _service.GenerateInvoiceAsync(dto, _userId);

        // Act
        var result = await _service.GetStudentInvoicesAsync(student.Id);

        // Assert
        Assert.Single(result);
        Assert.Equal(student.FullName, result[0].StudentName);
    }

    [Fact]
    public async Task GetStudentInvoicesAsync_WithNoInvoices_ReturnsEmptyList()
    {
        // Arrange
        var (student, _) = await SetupEnrollmentWithLessons(lessonCount: 0);

        // Act
        var result = await _service.GetStudentInvoicesAsync(student.Id);

        // Assert
        Assert.Empty(result);
    }

    #endregion

    #region GetInvoiceAsync Tests

    [Fact]
    public async Task GetInvoiceAsync_WithNonexistentId_ReturnsNull()
    {
        // Act
        var result = await _service.GetInvoiceAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetInvoiceAsync_WithBillingContact_ReturnsBillingContactInfo()
    {
        // Arrange
        var (student, enrollment) = await SetupEnrollmentWithLessons(lessonCount: 4);

        // Set billing contact on student
        student.BillingContactName = "Parent Name";
        student.BillingContactEmail = "parent@test.com";
        student.BillingContactPhone = "0699999999";
        student.BillingAddress = "Billing Street 1";
        student.BillingPostalCode = "5678CD";
        student.BillingCity = "Billing City";
        await _context.SaveChangesAsync();

        var dto = new GenerateInvoiceDto
        {
            EnrollmentId = enrollment.Id,
            PeriodStart = new DateOnly(2026, 1, 1),
            PeriodEnd = new DateOnly(2026, 1, 31),
            ApplyLedgerCorrections = false
        };
        var invoice = await _service.GenerateInvoiceAsync(dto, _userId);

        // Act
        var result = await _service.GetInvoiceAsync(invoice.Id);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.BillingContact);
        Assert.Equal("Parent Name", result.BillingContact.Name);
        Assert.Equal("parent@test.com", result.BillingContact.Email);
        Assert.Equal("0699999999", result.BillingContact.Phone);
        Assert.Equal("Billing Street 1", result.BillingContact.Address);
        Assert.Equal("5678CD", result.BillingContact.PostalCode);
        Assert.Equal("Billing City", result.BillingContact.City);
    }

    [Fact]
    public async Task GetInvoiceAsync_WithoutBillingContact_FallsBackToStudent()
    {
        // Arrange
        var (student, enrollment) = await SetupEnrollmentWithLessons(lessonCount: 4);

        var dto = new GenerateInvoiceDto
        {
            EnrollmentId = enrollment.Id,
            PeriodStart = new DateOnly(2026, 1, 1),
            PeriodEnd = new DateOnly(2026, 1, 31),
            ApplyLedgerCorrections = false
        };
        var invoice = await _service.GenerateInvoiceAsync(dto, _userId);

        // Act
        var result = await _service.GetInvoiceAsync(invoice.Id);

        // Assert - should fall back to student's own info
        Assert.NotNull(result);
        Assert.NotNull(result.BillingContact);
        Assert.Equal(student.FullName, result.BillingContact.Name);
        Assert.Equal(student.Email, result.BillingContact.Email);
        Assert.Equal(student.Phone, result.BillingContact.Phone);
    }

    #endregion

    #region GetByInvoiceNumberAsync Tests

    [Fact]
    public async Task GetByInvoiceNumberAsync_WithValidNumber_ReturnsInvoice()
    {
        // Arrange
        var (_, enrollment) = await SetupEnrollmentWithLessons(lessonCount: 4);
        var dto = new GenerateInvoiceDto
        {
            EnrollmentId = enrollment.Id,
            PeriodStart = new DateOnly(2026, 1, 1),
            PeriodEnd = new DateOnly(2026, 1, 31),
            ApplyLedgerCorrections = false
        };
        var invoice = await _service.GenerateInvoiceAsync(dto, _userId);

        // Act
        var result = await _service.GetByInvoiceNumberAsync(invoice.InvoiceNumber);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(invoice.Id, result.Id);
        Assert.Equal(invoice.InvoiceNumber, result.InvoiceNumber);
    }

    [Fact]
    public async Task GetByInvoiceNumberAsync_WithNonexistentNumber_ReturnsNull()
    {
        // Act
        var result = await _service.GetByInvoiceNumberAsync("NONEXISTENT");

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetByStatusAsync Tests

    [Fact]
    public async Task GetByStatusAsync_ReturnsMatchingInvoices()
    {
        // Arrange
        var (_, enrollment) = await SetupEnrollmentWithLessons(lessonCount: 4);
        var dto = new GenerateInvoiceDto
        {
            EnrollmentId = enrollment.Id,
            PeriodStart = new DateOnly(2026, 1, 1),
            PeriodEnd = new DateOnly(2026, 1, 31),
            ApplyLedgerCorrections = false
        };
        await _service.GenerateInvoiceAsync(dto, _userId);

        // Act
        var result = await _service.GetByStatusAsync(InvoiceStatus.Draft);

        // Assert
        Assert.Single(result);
    }

    [Fact]
    public async Task GetByStatusAsync_NoMatches_ReturnsEmptyList()
    {
        // Act
        var result = await _service.GetByStatusAsync(InvoiceStatus.Paid);

        // Assert
        Assert.Empty(result);
    }

    #endregion

    #region RecalculateInvoiceAsync Tests

    [Fact]
    public async Task RecalculateInvoiceAsync_WithNonexistentInvoice_ThrowsInvalidOperationException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.RecalculateInvoiceAsync(Guid.NewGuid(), _userId));
        Assert.Contains("not found", exception.Message);
    }

    [Fact]
    public async Task RecalculateInvoiceAsync_WithPaidInvoice_ThrowsInvalidOperationException()
    {
        // Arrange
        var (_, enrollment) = await SetupEnrollmentWithLessons(lessonCount: 4);
        var dto = new GenerateInvoiceDto
        {
            EnrollmentId = enrollment.Id,
            PeriodStart = new DateOnly(2026, 1, 1),
            PeriodEnd = new DateOnly(2026, 1, 31)
        };
        var invoice = await _service.GenerateInvoiceAsync(dto, _userId);

        // Mark as paid
        var dbInvoice = await _context.Invoices.FirstAsync(i => i.Id == invoice.Id);
        dbInvoice.Status = InvoiceStatus.Paid;
        await _context.SaveChangesAsync();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.RecalculateInvoiceAsync(invoice.Id, _userId));
        Assert.Contains("Paid", exception.Message);
    }

    [Fact]
    public async Task RecalculateInvoiceAsync_WithCancelledInvoice_Throws()
    {
        // Arrange
        var (_, enrollment) = await SetupEnrollmentWithLessons(lessonCount: 4);
        var dto = new GenerateInvoiceDto
        {
            EnrollmentId = enrollment.Id,
            PeriodStart = new DateOnly(2026, 1, 1),
            PeriodEnd = new DateOnly(2026, 1, 31),
            ApplyLedgerCorrections = false
        };
        var invoice = await _service.GenerateInvoiceAsync(dto, _userId);

        var dbInvoice = await _context.Invoices.FirstAsync(i => i.Id == invoice.Id);
        dbInvoice.Status = InvoiceStatus.Cancelled;
        await _context.SaveChangesAsync();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.RecalculateInvoiceAsync(invoice.Id, _userId));
        Assert.Contains("Cancelled", exception.Message);
    }

    [Fact]
    public async Task RecalculateInvoiceAsync_WithValidInvoice_RecalculatesTotals()
    {
        // Arrange
        var (_, enrollment) = await SetupEnrollmentWithLessons(lessonCount: 4);
        var dto = new GenerateInvoiceDto
        {
            EnrollmentId = enrollment.Id,
            PeriodStart = new DateOnly(2026, 1, 1),
            PeriodEnd = new DateOnly(2026, 1, 31),
            ApplyLedgerCorrections = false
        };
        var invoice = await _service.GenerateInvoiceAsync(dto, _userId);

        // Mark invoice as Sent so recalculate is allowed, and un-invoice all lessons
        var dbInvoice = await _context.Invoices.FirstAsync(i => i.Id == invoice.Id);
        dbInvoice.Status = InvoiceStatus.Sent;
        var lessons = await _context.Lessons
            .Where(l => l.CourseId == enrollment.CourseId)
            .ToListAsync();
        foreach (var l in lessons) l.IsInvoiced = false;
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.RecalculateInvoiceAsync(invoice.Id, _userId);

        // Assert - recalculate picks up all 4 un-invoiced lessons
        Assert.NotNull(result);
        Assert.Equal(4, result.Lines.Count);
        Assert.Equal(100m, result.Subtotal);
    }

    [Fact]
    public async Task RecalculateInvoiceAsync_WithNoRemainingLessons_CancelsInvoice()
    {
        // Arrange
        var (_, enrollment) = await SetupEnrollmentWithLessons(lessonCount: 4);
        var dto = new GenerateInvoiceDto
        {
            EnrollmentId = enrollment.Id,
            PeriodStart = new DateOnly(2026, 1, 1),
            PeriodEnd = new DateOnly(2026, 1, 31),
            ApplyLedgerCorrections = false
        };
        var invoice = await _service.GenerateInvoiceAsync(dto, _userId);

        // Delete all lessons so recalculate finds none
        var lessons = await _context.Lessons
            .Where(l => l.CourseId == enrollment.CourseId)
            .ToListAsync();
        _context.Lessons.RemoveRange(lessons);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.RecalculateInvoiceAsync(invoice.Id, _userId);

        // Assert
        Assert.Equal(InvoiceStatus.Cancelled, result.Status);
        Assert.Equal(0m, result.Total);
    }

    #endregion

    #region ApplyLedgerCorrectionAsync Tests

    [Fact]
    public async Task ApplyLedgerCorrectionAsync_WithValidCredit_ReducesInvoiceBalance()
    {
        // Arrange
        var (student, invoice) = await CreateInvoiceForStudent();
        var ledgerEntry = await SeedLedgerEntry(student.Id, LedgerEntryType.Credit, 30m, LedgerEntryStatus.Open);

        // Act
        var result = await _service.ApplyLedgerCorrectionAsync(invoice.Id, ledgerEntry.Id, 30m, _userId);

        // Assert
        Assert.Equal(30m, result.LedgerCreditsApplied);
        Assert.Equal(invoice.Total - 30m, result.Balance);
    }

    [Fact]
    public async Task ApplyLedgerCorrectionAsync_WithValidDebit_IncreasesInvoiceOwed()
    {
        // Arrange
        var (student, invoice) = await CreateInvoiceForStudent();
        var ledgerEntry = await SeedLedgerEntry(student.Id, LedgerEntryType.Debit, 20m, LedgerEntryStatus.Open);

        // Act
        var result = await _service.ApplyLedgerCorrectionAsync(invoice.Id, ledgerEntry.Id, 20m, _userId);

        // Assert
        Assert.Equal(20m, result.LedgerDebitsApplied);
        Assert.Equal(invoice.Total + 20m, result.Balance);
    }

    [Fact]
    public async Task ApplyLedgerCorrectionAsync_FullyPaysInvoice_SetsStatusToPaid()
    {
        // Arrange
        var (student, invoice) = await CreateInvoiceForStudent();
        // Create a credit that covers the entire invoice total
        var ledgerEntry = await SeedLedgerEntry(student.Id, LedgerEntryType.Credit, invoice.Total + 10m, LedgerEntryStatus.Open);

        // Act
        var result = await _service.ApplyLedgerCorrectionAsync(invoice.Id, ledgerEntry.Id, invoice.Total, _userId);

        // Assert
        Assert.Equal(InvoiceStatus.Paid, result.Status);
    }

    [Fact]
    public async Task ApplyLedgerCorrectionAsync_PartialCredit_DoesNotChangeToPaid()
    {
        // Arrange
        var (student, invoice) = await CreateInvoiceForStudent();
        var ledgerEntry = await SeedLedgerEntry(student.Id, LedgerEntryType.Credit, 10m, LedgerEntryStatus.Open);

        // Act
        var result = await _service.ApplyLedgerCorrectionAsync(invoice.Id, ledgerEntry.Id, 10m, _userId);

        // Assert
        Assert.Equal(InvoiceStatus.Draft, result.Status);
    }

    [Fact]
    public async Task ApplyLedgerCorrectionAsync_FullyAppliesLedgerEntry_SetsStatusFullyApplied()
    {
        // Arrange
        var (student, invoice) = await CreateInvoiceForStudent();
        var ledgerEntry = await SeedLedgerEntry(student.Id, LedgerEntryType.Credit, 30m, LedgerEntryStatus.Open);

        // Act
        await _service.ApplyLedgerCorrectionAsync(invoice.Id, ledgerEntry.Id, 30m, _userId);

        // Assert
        var updatedEntry = await _context.StudentLedgerEntries.FirstAsync(e => e.Id == ledgerEntry.Id);
        Assert.Equal(LedgerEntryStatus.FullyApplied, updatedEntry.Status);
    }

    [Fact]
    public async Task ApplyLedgerCorrectionAsync_PartiallyAppliesLedgerEntry_SetsStatusPartiallyApplied()
    {
        // Arrange
        var (student, invoice) = await CreateInvoiceForStudent();
        var ledgerEntry = await SeedLedgerEntry(student.Id, LedgerEntryType.Credit, 50m, LedgerEntryStatus.Open);

        // Act
        await _service.ApplyLedgerCorrectionAsync(invoice.Id, ledgerEntry.Id, 20m, _userId);

        // Assert
        var updatedEntry = await _context.StudentLedgerEntries.FirstAsync(e => e.Id == ledgerEntry.Id);
        Assert.Equal(LedgerEntryStatus.PartiallyApplied, updatedEntry.Status);
    }

    [Fact]
    public async Task ApplyLedgerCorrectionAsync_InvoiceNotFound_Throws()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ApplyLedgerCorrectionAsync(Guid.NewGuid(), Guid.NewGuid(), 10m, _userId));
        Assert.Contains("Invoice", exception.Message);
        Assert.Contains("not found", exception.Message);
    }

    [Fact]
    public async Task ApplyLedgerCorrectionAsync_LedgerEntryNotFound_Throws()
    {
        // Arrange
        var (_, invoice) = await CreateInvoiceForStudent();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ApplyLedgerCorrectionAsync(invoice.Id, Guid.NewGuid(), 10m, _userId));
        Assert.Contains("Ledger entry", exception.Message);
        Assert.Contains("not found", exception.Message);
    }

    [Fact]
    public async Task ApplyLedgerCorrectionAsync_PaidInvoice_Throws()
    {
        // Arrange
        var (student, invoice) = await CreateInvoiceForStudent();
        var dbInvoice = await _context.Invoices.FirstAsync(i => i.Id == invoice.Id);
        dbInvoice.Status = InvoiceStatus.Paid;
        await _context.SaveChangesAsync();

        var ledgerEntry = await SeedLedgerEntry(student.Id, LedgerEntryType.Credit, 10m, LedgerEntryStatus.Open);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ApplyLedgerCorrectionAsync(invoice.Id, ledgerEntry.Id, 10m, _userId));
        Assert.Contains("Paid", exception.Message);
    }

    [Fact]
    public async Task ApplyLedgerCorrectionAsync_CancelledInvoice_Throws()
    {
        // Arrange
        var (student, invoice) = await CreateInvoiceForStudent();
        var dbInvoice = await _context.Invoices.FirstAsync(i => i.Id == invoice.Id);
        dbInvoice.Status = InvoiceStatus.Cancelled;
        await _context.SaveChangesAsync();

        var ledgerEntry = await SeedLedgerEntry(student.Id, LedgerEntryType.Credit, 10m, LedgerEntryStatus.Open);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ApplyLedgerCorrectionAsync(invoice.Id, ledgerEntry.Id, 10m, _userId));
        Assert.Contains("Cancelled", exception.Message);
    }

    [Fact]
    public async Task ApplyLedgerCorrectionAsync_AmountExceedsAvailable_Throws()
    {
        // Arrange
        var (student, invoice) = await CreateInvoiceForStudent();
        var ledgerEntry = await SeedLedgerEntry(student.Id, LedgerEntryType.Credit, 30m, LedgerEntryStatus.Open);

        // Act & Assert - try to apply 50 when only 30 available
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ApplyLedgerCorrectionAsync(invoice.Id, ledgerEntry.Id, 50m, _userId));
        Assert.Contains("exceeds available", exception.Message);
    }

    [Fact]
    public async Task ApplyLedgerCorrectionAsync_LedgerEntryBelongsToDifferentStudent_Throws()
    {
        // Arrange
        var (_, invoice) = await CreateInvoiceForStudent();
        var otherStudentId = Guid.NewGuid();
        var otherStudent = new Student
        {
            Id = otherStudentId,
            FirstName = "Other",
            LastName = "Student",
            Email = "other@test.com",
            Phone = "0600000000",
            Status = StudentStatus.Active
        };
        _context.Students.Add(otherStudent);
        await _context.SaveChangesAsync();

        var ledgerEntry = await SeedLedgerEntry(otherStudentId, LedgerEntryType.Credit, 30m, LedgerEntryStatus.Open);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ApplyLedgerCorrectionAsync(invoice.Id, ledgerEntry.Id, 30m, _userId));
        Assert.Contains("does not belong", exception.Message);
    }

    #endregion

    #region GenerateBatchInvoicesAsync Tests

    [Fact]
    public async Task GenerateBatchInvoicesAsync_WithMultipleEnrollments_GeneratesMultipleInvoices()
    {
        // Arrange - create two enrollments with lessons
        var (_, enrollment1) = await SetupEnrollmentWithLessons(lessonCount: 4);
        var (_, enrollment2) = await SetupEnrollmentWithLessons(lessonCount: 3);

        var dto = new GenerateBatchInvoicesDto
        {
            PeriodStart = new DateOnly(2026, 1, 1),
            PeriodEnd = new DateOnly(2026, 1, 31),
            PeriodType = InvoicingPreference.Monthly,
            ApplyLedgerCorrections = false
        };

        // Act
        var result = await _service.GenerateBatchInvoicesAsync(dto, _userId);

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GenerateBatchInvoicesAsync_SkipsEnrollmentsWithNoLessons()
    {
        // Arrange - one with lessons, one without
        var (_, enrollment1) = await SetupEnrollmentWithLessons(lessonCount: 4);
        var (_, enrollment2) = await SetupEnrollmentWithLessons(lessonCount: 0);

        var dto = new GenerateBatchInvoicesDto
        {
            PeriodStart = new DateOnly(2026, 1, 1),
            PeriodEnd = new DateOnly(2026, 1, 31),
            PeriodType = InvoicingPreference.Monthly,
            ApplyLedgerCorrections = false
        };

        // Act
        var result = await _service.GenerateBatchInvoicesAsync(dto, _userId);

        // Assert - only the one with lessons should generate
        Assert.Single(result);
    }

    [Fact]
    public async Task GenerateBatchInvoicesAsync_FiltersByPeriodType()
    {
        // Arrange - monthly enrollment has lessons
        var (_, enrollment) = await SetupEnrollmentWithLessons(lessonCount: 4);

        // Request quarterly batch - monthly enrollment should not match
        var dto = new GenerateBatchInvoicesDto
        {
            PeriodStart = new DateOnly(2026, 1, 1),
            PeriodEnd = new DateOnly(2026, 3, 31),
            PeriodType = InvoicingPreference.Quarterly,
            ApplyLedgerCorrections = false
        };

        // Act
        var result = await _service.GenerateBatchInvoicesAsync(dto, _userId);

        // Assert - monthly enrollment shouldn't be included in quarterly batch
        Assert.Empty(result);
    }

    [Fact]
    public async Task GenerateBatchInvoicesAsync_WithNoMatchingEnrollments_ReturnsEmptyList()
    {
        // Arrange - no enrollments at all

        var dto = new GenerateBatchInvoicesDto
        {
            PeriodStart = new DateOnly(2026, 1, 1),
            PeriodEnd = new DateOnly(2026, 1, 31),
            PeriodType = InvoicingPreference.Monthly,
            ApplyLedgerCorrections = false
        };

        // Act
        var result = await _service.GenerateBatchInvoicesAsync(dto, _userId);

        // Assert
        Assert.Empty(result);
    }

    #endregion

    #region Helper Methods

    private async Task<(Student student, Enrollment enrollment)> SetupEnrollmentWithLessons(
        int lessonCount, decimal discountPercent = 0)
    {
        var student = new Student
        {
            Id = Guid.NewGuid(),
            FirstName = "Test",
            LastName = "Student",
            Email = $"test{Guid.NewGuid():N}@student.com",
            Phone = "0612345678",
            Status = StudentStatus.Active
        };
        _context.Students.Add(student);

        var teacher = new Teacher
        {
            Id = Guid.NewGuid(),
            FirstName = "Test",
            LastName = "Teacher",
            Email = $"teacher{Guid.NewGuid():N}@test.com",
            Phone = "0612345679",
            IsActive = true
        };
        _context.Teachers.Add(teacher);

        var courseType = new CourseType
        {
            Id = Guid.NewGuid(),
            Name = "Piano Individual 30min",
            InstrumentId = 1,
            Type = CourseTypeCategory.Individual,
            DurationMinutes = 30,
            MaxStudents = 1,
            IsActive = true
        };
        _context.CourseTypes.Add(courseType);

        var pricingVersion = new CourseTypePricingVersion
        {
            Id = Guid.NewGuid(),
            CourseTypeId = courseType.Id,
            PriceAdult = 25m,
            PriceChild = 20m,
            ValidFrom = new DateOnly(2025, 1, 1),
            IsCurrent = true
        };
        _context.CourseTypePricingVersions.Add(pricingVersion);

        var course = new Course
        {
            Id = Guid.NewGuid(),
            CourseTypeId = courseType.Id,
            TeacherId = teacher.Id,
            DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(10, 30),
            StartDate = new DateOnly(2026, 1, 1),
            Frequency = CourseFrequency.Weekly,
            Status = CourseStatus.Active,
            IsTrial = false
        };
        _context.Courses.Add(course);

        var enrollment = new Enrollment
        {
            Id = Guid.NewGuid(),
            StudentId = student.Id,
            CourseId = course.Id,
            EnrolledAt = DateTime.UtcNow,
            Status = EnrollmentStatus.Active,
            InvoicingPreference = InvoicingPreference.Monthly,
            DiscountPercent = discountPercent
        };
        _context.Enrollments.Add(enrollment);

        for (int i = 0; i < lessonCount; i++)
        {
            var lesson = new Lesson
            {
                Id = Guid.NewGuid(),
                CourseId = course.Id,
                StudentId = student.Id,
                ScheduledDate = new DateOnly(2026, 1, 1).AddDays(i * 7),
                StartTime = new TimeOnly(10, 0),
                EndTime = new TimeOnly(10, 30),
                Status = LessonStatus.Scheduled,
                IsInvoiced = false
            };
            _context.Lessons.Add(lesson);
        }

        await _context.SaveChangesAsync();

        _mockPricingService
            .Setup(p => p.GetPricingForDateAsync(courseType.Id, It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pricingVersion);
        _mockPricingService
            .Setup(p => p.GetCurrentPricingAsync(courseType.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pricingVersion);

        return (student, enrollment);
    }

    private async Task<(Student student, Enrollment enrollment)> SetupEnrollmentWithChildStudent(
        int lessonCount, decimal discountPercent = 0)
    {
        var student = new Student
        {
            Id = Guid.NewGuid(),
            FirstName = "Child",
            LastName = "Student",
            Email = $"child{Guid.NewGuid():N}@student.com",
            Phone = "0612345678",
            DateOfBirth = DateOnly.FromDateTime(DateTime.Today.AddYears(-10)),
            Status = StudentStatus.Active
        };
        _context.Students.Add(student);

        var teacher = new Teacher
        {
            Id = Guid.NewGuid(),
            FirstName = "Test",
            LastName = "Teacher",
            Email = $"teacher{Guid.NewGuid():N}@test.com",
            Phone = "0612345679",
            IsActive = true
        };
        _context.Teachers.Add(teacher);

        var courseType = new CourseType
        {
            Id = Guid.NewGuid(),
            Name = "Piano Individual 30min",
            InstrumentId = 1,
            Type = CourseTypeCategory.Individual,
            DurationMinutes = 30,
            MaxStudents = 1,
            IsActive = true
        };
        _context.CourseTypes.Add(courseType);

        var pricingVersion = new CourseTypePricingVersion
        {
            Id = Guid.NewGuid(),
            CourseTypeId = courseType.Id,
            PriceAdult = 25m,
            PriceChild = 20m,
            ValidFrom = new DateOnly(2025, 1, 1),
            IsCurrent = true
        };
        _context.CourseTypePricingVersions.Add(pricingVersion);

        var course = new Course
        {
            Id = Guid.NewGuid(),
            CourseTypeId = courseType.Id,
            TeacherId = teacher.Id,
            DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(10, 30),
            StartDate = new DateOnly(2026, 1, 1),
            Frequency = CourseFrequency.Weekly,
            Status = CourseStatus.Active,
            IsTrial = false
        };
        _context.Courses.Add(course);

        var enrollment = new Enrollment
        {
            Id = Guid.NewGuid(),
            StudentId = student.Id,
            CourseId = course.Id,
            EnrolledAt = DateTime.UtcNow,
            Status = EnrollmentStatus.Active,
            InvoicingPreference = InvoicingPreference.Monthly,
            DiscountPercent = discountPercent
        };
        _context.Enrollments.Add(enrollment);

        for (int i = 0; i < lessonCount; i++)
        {
            var lesson = new Lesson
            {
                Id = Guid.NewGuid(),
                CourseId = course.Id,
                StudentId = student.Id,
                ScheduledDate = new DateOnly(2026, 1, 1).AddDays(i * 7),
                StartTime = new TimeOnly(10, 0),
                EndTime = new TimeOnly(10, 30),
                Status = LessonStatus.Scheduled,
                IsInvoiced = false
            };
            _context.Lessons.Add(lesson);
        }

        await _context.SaveChangesAsync();

        _mockPricingService
            .Setup(p => p.GetPricingForDateAsync(courseType.Id, It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pricingVersion);
        _mockPricingService
            .Setup(p => p.GetCurrentPricingAsync(courseType.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pricingVersion);

        return (student, enrollment);
    }

    private async Task<(Student student, Enrollment enrollment)> SetupEnrollmentWithGroupLessons(int lessonCount)
    {
        var student = new Student
        {
            Id = Guid.NewGuid(),
            FirstName = "Group",
            LastName = "Student",
            Email = $"group{Guid.NewGuid():N}@student.com",
            Phone = "0612345678",
            Status = StudentStatus.Active
        };
        _context.Students.Add(student);

        var teacher = new Teacher
        {
            Id = Guid.NewGuid(),
            FirstName = "Test",
            LastName = "Teacher",
            Email = $"teacher{Guid.NewGuid():N}@test.com",
            Phone = "0612345679",
            IsActive = true
        };
        _context.Teachers.Add(teacher);

        var courseType = new CourseType
        {
            Id = Guid.NewGuid(),
            Name = "Piano Group 60min",
            InstrumentId = 1,
            Type = CourseTypeCategory.Group,
            DurationMinutes = 60,
            MaxStudents = 6,
            IsActive = true
        };
        _context.CourseTypes.Add(courseType);

        var pricingVersion = new CourseTypePricingVersion
        {
            Id = Guid.NewGuid(),
            CourseTypeId = courseType.Id,
            PriceAdult = 25m,
            PriceChild = 20m,
            ValidFrom = new DateOnly(2025, 1, 1),
            IsCurrent = true
        };
        _context.CourseTypePricingVersions.Add(pricingVersion);

        var course = new Course
        {
            Id = Guid.NewGuid(),
            CourseTypeId = courseType.Id,
            TeacherId = teacher.Id,
            DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(11, 0),
            StartDate = new DateOnly(2026, 1, 1),
            Frequency = CourseFrequency.Weekly,
            Status = CourseStatus.Active,
            IsTrial = false
        };
        _context.Courses.Add(course);

        var enrollment = new Enrollment
        {
            Id = Guid.NewGuid(),
            StudentId = student.Id,
            CourseId = course.Id,
            EnrolledAt = DateTime.UtcNow,
            Status = EnrollmentStatus.Active,
            InvoicingPreference = InvoicingPreference.Monthly,
            DiscountPercent = 0
        };
        _context.Enrollments.Add(enrollment);

        // Group lessons have StudentId = null
        for (int i = 0; i < lessonCount; i++)
        {
            var lesson = new Lesson
            {
                Id = Guid.NewGuid(),
                CourseId = course.Id,
                StudentId = null,
                ScheduledDate = new DateOnly(2026, 1, 1).AddDays(i * 7),
                StartTime = new TimeOnly(10, 0),
                EndTime = new TimeOnly(11, 0),
                Status = LessonStatus.Scheduled,
                IsInvoiced = false
            };
            _context.Lessons.Add(lesson);
        }

        await _context.SaveChangesAsync();

        _mockPricingService
            .Setup(p => p.GetPricingForDateAsync(courseType.Id, It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pricingVersion);
        _mockPricingService
            .Setup(p => p.GetCurrentPricingAsync(courseType.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pricingVersion);

        return (student, enrollment);
    }

    private async Task<(Student student, Enrollment enrollment, Student otherStudent)> SetupEnrollmentWithMixedLessons()
    {
        var student = new Student
        {
            Id = Guid.NewGuid(),
            FirstName = "Main",
            LastName = "Student",
            Email = $"main{Guid.NewGuid():N}@student.com",
            Phone = "0612345678",
            Status = StudentStatus.Active
        };
        _context.Students.Add(student);

        var otherStudent = new Student
        {
            Id = Guid.NewGuid(),
            FirstName = "Other",
            LastName = "Student",
            Email = $"other{Guid.NewGuid():N}@student.com",
            Phone = "0600000000",
            Status = StudentStatus.Active
        };
        _context.Students.Add(otherStudent);

        var teacher = new Teacher
        {
            Id = Guid.NewGuid(),
            FirstName = "Test",
            LastName = "Teacher",
            Email = $"teacher{Guid.NewGuid():N}@test.com",
            Phone = "0612345679",
            IsActive = true
        };
        _context.Teachers.Add(teacher);

        var courseType = new CourseType
        {
            Id = Guid.NewGuid(),
            Name = "Piano Individual 30min",
            InstrumentId = 1,
            Type = CourseTypeCategory.Individual,
            DurationMinutes = 30,
            MaxStudents = 1,
            IsActive = true
        };
        _context.CourseTypes.Add(courseType);

        var pricingVersion = new CourseTypePricingVersion
        {
            Id = Guid.NewGuid(),
            CourseTypeId = courseType.Id,
            PriceAdult = 25m,
            PriceChild = 20m,
            ValidFrom = new DateOnly(2025, 1, 1),
            IsCurrent = true
        };
        _context.CourseTypePricingVersions.Add(pricingVersion);

        var course = new Course
        {
            Id = Guid.NewGuid(),
            CourseTypeId = courseType.Id,
            TeacherId = teacher.Id,
            DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(10, 30),
            StartDate = new DateOnly(2026, 1, 1),
            Frequency = CourseFrequency.Weekly,
            Status = CourseStatus.Active,
            IsTrial = false
        };
        _context.Courses.Add(course);

        var enrollment = new Enrollment
        {
            Id = Guid.NewGuid(),
            StudentId = student.Id,
            CourseId = course.Id,
            EnrolledAt = DateTime.UtcNow,
            Status = EnrollmentStatus.Active,
            InvoicingPreference = InvoicingPreference.Monthly,
            DiscountPercent = 0
        };
        _context.Enrollments.Add(enrollment);

        // 2 lessons for main student
        for (int i = 0; i < 2; i++)
        {
            _context.Lessons.Add(new Lesson
            {
                Id = Guid.NewGuid(),
                CourseId = course.Id,
                StudentId = student.Id,
                ScheduledDate = new DateOnly(2026, 1, 1).AddDays(i * 7),
                StartTime = new TimeOnly(10, 0),
                EndTime = new TimeOnly(10, 30),
                Status = LessonStatus.Scheduled,
                IsInvoiced = false
            });
        }

        // 2 lessons for other student (same course, different student)
        for (int i = 2; i < 4; i++)
        {
            _context.Lessons.Add(new Lesson
            {
                Id = Guid.NewGuid(),
                CourseId = course.Id,
                StudentId = otherStudent.Id,
                ScheduledDate = new DateOnly(2026, 1, 1).AddDays(i * 7),
                StartTime = new TimeOnly(10, 0),
                EndTime = new TimeOnly(10, 30),
                Status = LessonStatus.Scheduled,
                IsInvoiced = false
            });
        }

        await _context.SaveChangesAsync();

        _mockPricingService
            .Setup(p => p.GetPricingForDateAsync(courseType.Id, It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pricingVersion);
        _mockPricingService
            .Setup(p => p.GetCurrentPricingAsync(courseType.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pricingVersion);

        return (student, enrollment, otherStudent);
    }

    private async Task<(Student student, InvoiceDto invoice)> CreateInvoiceForStudent()
    {
        var (student, enrollment) = await SetupEnrollmentWithLessons(lessonCount: 4);

        var dto = new GenerateInvoiceDto
        {
            EnrollmentId = enrollment.Id,
            PeriodStart = new DateOnly(2026, 1, 1),
            PeriodEnd = new DateOnly(2026, 1, 31),
            ApplyLedgerCorrections = false
        };

        var invoice = await _service.GenerateInvoiceAsync(dto, _userId);
        return (student, invoice);
    }

    private async Task<StudentLedgerEntry> SeedLedgerEntry(
        Guid studentId, LedgerEntryType entryType, decimal amount, LedgerEntryStatus status)
    {
        var entry = new StudentLedgerEntry
        {
            Id = Guid.NewGuid(),
            StudentId = studentId,
            CorrectionRefName = $"TEST-{Guid.NewGuid():N}"[..16],
            Description = $"Test {entryType} entry",
            Amount = amount,
            EntryType = entryType,
            Status = status,
            CreatedById = _userId
        };

        _context.StudentLedgerEntries.Add(entry);
        await _context.SaveChangesAsync();

        return entry;
    }

    #endregion
}
