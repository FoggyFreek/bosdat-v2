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
    private readonly Mock<IStudentLedgerRepository> _mockLedgerRepository;
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
        _mockLedgerRepository = new Mock<IStudentLedgerRepository>();

        SetupMockInvoiceRepository();

        _service = new InvoiceService(
            _context,
            _mockUnitOfWork.Object,
            _mockPricingService.Object,
            _mockLedgerRepository.Object);

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
        // Seed settings
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

        // Seed instrument
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

    #endregion

    #region Helper Methods

    private async Task<(Student student, Enrollment enrollment)> SetupEnrollmentWithLessons(int lessonCount)
    {
        var student = new Student
        {
            Id = Guid.NewGuid(),
            FirstName = "Test",
            LastName = "Student",
            Email = "test@student.com",
            Phone = "0612345678",
            Status = StudentStatus.Active
        };
        _context.Students.Add(student);

        var teacher = new Teacher
        {
            Id = Guid.NewGuid(),
            FirstName = "Test",
            LastName = "Teacher",
            Email = "teacher@test.com",
            Phone = "0612345679",
            Status = TeacherStatus.Active
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

        // Add lessons
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

        // Setup pricing service mock
        _mockPricingService
            .Setup(p => p.GetPricingForDateAsync(courseType.Id, It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pricingVersion);
        _mockPricingService
            .Setup(p => p.GetCurrentPricingAsync(courseType.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pricingVersion);

        return (student, enrollment);
    }

    #endregion
}
