using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using BosDAT.Core.Entities;
using BosDAT.Core.Interfaces;
using BosDAT.Infrastructure.Data;
using BosDAT.Infrastructure.Services;

namespace BosDAT.API.Tests.Services;

public class RegistrationFeeServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IInvoiceRepository> _mockInvoiceRepository;
    private readonly RegistrationFeeService _service;
    private readonly Guid _testStudentId = Guid.NewGuid();
    private readonly Guid _testCourseId = Guid.NewGuid();
    private readonly Guid _testTrialCourseId = Guid.NewGuid();

    public RegistrationFeeServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options, null!);
        _mockInvoiceRepository = new Mock<IInvoiceRepository>();
        _service = new RegistrationFeeService(_context, _mockInvoiceRepository.Object);

        SeedTestData();
    }

    private void SeedTestData()
    {
        // Seed settings
        _context.Settings.AddRange(
            new Setting { Key = "registration_fee", Value = "25", Type = "decimal", Description = "Registration fee" },
            new Setting { Key = "registration_fee_description", Value = "Eenmalig inschrijfgeld", Type = "string", Description = "Fee description" },
            new Setting { Key = "vat_rate", Value = "21", Type = "decimal", Description = "VAT rate" },
            new Setting { Key = "payment_due_days", Value = "14", Type = "int", Description = "Payment due days" },
            new Setting { Key = "invoice_prefix", Value = "NMI", Type = "string", Description = "Invoice prefix" }
        );

        // Add test student
        var student = new Student
        {
            Id = _testStudentId,
            FirstName = "Test",
            LastName = "Student",
            Email = "test@example.com",
            Status = StudentStatus.Active
        };
        _context.Students.Add(student);

        // Add instrument and course type
        var instrument = new Instrument { Id = 1, Name = "Piano", IsActive = true };
        _context.Instruments.Add(instrument);

        var courseType = new CourseType
        {
            Id = Guid.NewGuid(),
            Name = "Piano Lesson",
            DurationMinutes = 30,
            InstrumentId = 1,
            IsActive = true
        };
        _context.CourseTypes.Add(courseType);

        // Add teacher
        var teacher = new Teacher
        {
            Id = Guid.NewGuid(),
            FirstName = "Test",
            LastName = "Teacher",
            Email = "teacher@example.com",
            IsActive = true
        };
        _context.Teachers.Add(teacher);

        // Add non-trial course
        var course = new Course
        {
            Id = _testCourseId,
            TeacherId = teacher.Id,
            CourseTypeId = courseType.Id,
            IsTrial = false,
            Status = CourseStatus.Active,
            DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(10, 30),
            StartDate = DateOnly.FromDateTime(DateTime.Today)
        };
        _context.Courses.Add(course);

        // Add trial course
        var trialCourse = new Course
        {
            Id = _testTrialCourseId,
            TeacherId = teacher.Id,
            CourseTypeId = courseType.Id,
            IsTrial = true,
            Status = CourseStatus.Active,
            DayOfWeek = DayOfWeek.Tuesday,
            StartTime = new TimeOnly(11, 0),
            EndTime = new TimeOnly(11, 30),
            StartDate = DateOnly.FromDateTime(DateTime.Today)
        };
        _context.Courses.Add(trialCourse);

        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task IsStudentEligibleForFeeAsync_WhenNotPaid_ReturnsTrue()
    {
        // Act
        var result = await _service.IsStudentEligibleForFeeAsync(_testStudentId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsStudentEligibleForFeeAsync_WhenAlreadyPaid_ReturnsFalse()
    {
        // Arrange
        var student = await _context.Students.FindAsync(_testStudentId);
        student!.RegistrationFeePaidAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.IsStudentEligibleForFeeAsync(_testStudentId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task IsStudentEligibleForFeeAsync_WithInvalidStudentId_ThrowsException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.IsStudentEligibleForFeeAsync(Guid.NewGuid()));

        Assert.Contains("not found", exception.Message.ToLower());
    }

    [Fact]
    public async Task ShouldApplyFeeForCourseAsync_ForNonTrialCourse_ReturnsTrue()
    {
        // Act
        var result = await _service.ShouldApplyFeeForCourseAsync(_testCourseId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ShouldApplyFeeForCourseAsync_ForTrialCourse_ReturnsFalse()
    {
        // Act
        var result = await _service.ShouldApplyFeeForCourseAsync(_testTrialCourseId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ShouldApplyFeeForCourseAsync_WithInvalidCourseId_ThrowsException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ShouldApplyFeeForCourseAsync(Guid.NewGuid()));

        Assert.Contains("not found", exception.Message.ToLower());
    }

    [Fact]
    public async Task ApplyRegistrationFeeAsync_CreatesInvoiceAndInvoiceLine()
    {
        // Arrange
        _mockInvoiceRepository
            .Setup(r => r.GenerateInvoiceNumberAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("NMI-2024-00001");

        // Act
        var invoiceId = await _service.ApplyRegistrationFeeAsync(_testStudentId);

        // Assert
        Assert.NotEqual(Guid.Empty, invoiceId);

        var invoice = await _context.Invoices
            .Include(i => i.Lines)
            .FirstOrDefaultAsync(i => i.Id == invoiceId);

        Assert.NotNull(invoice);
        Assert.Equal(_testStudentId, invoice.StudentId);
        Assert.Equal(InvoiceStatus.Draft, invoice.Status);
        Assert.Single(invoice.Lines);

        var line = invoice.Lines.First();
        Assert.Equal("Eenmalig inschrijfgeld", line.Description);
        Assert.Equal(25m, line.UnitPrice);
        Assert.Null(line.LessonId);
    }

    [Fact]
    public async Task ApplyRegistrationFeeAsync_SetsRegistrationFeePaidAt()
    {
        // Arrange
        _mockInvoiceRepository
            .Setup(r => r.GenerateInvoiceNumberAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("NMI-2024-00001");

        // Act
        await _service.ApplyRegistrationFeeAsync(_testStudentId);

        // Assert
        var student = await _context.Students.FindAsync(_testStudentId);
        Assert.NotNull(student!.RegistrationFeePaidAt);
    }

    [Fact]
    public async Task ApplyRegistrationFeeAsync_AddsToExistingDraftInvoice()
    {
        // Arrange
        var existingInvoice = new Invoice
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = "NMI-2024-00001",
            StudentId = _testStudentId,
            Status = InvoiceStatus.Draft,
            IssueDate = DateOnly.FromDateTime(DateTime.Today),
            DueDate = DateOnly.FromDateTime(DateTime.Today.AddDays(14)),
            Subtotal = 50m,
            VatAmount = 10.5m,
            Total = 60.5m
        };
        _context.Invoices.Add(existingInvoice);
        await _context.SaveChangesAsync();

        // Act
        var invoiceId = await _service.ApplyRegistrationFeeAsync(_testStudentId);

        // Assert
        Assert.Equal(existingInvoice.Id, invoiceId);

        var invoice = await _context.Invoices
            .Include(i => i.Lines)
            .FirstOrDefaultAsync(i => i.Id == invoiceId);

        Assert.Equal(75m, invoice!.Subtotal); // 50 + 25
        Assert.Single(invoice.Lines);
    }

    [Fact]
    public async Task ApplyRegistrationFeeAsync_WhenAlreadyPaid_ThrowsException()
    {
        // Arrange
        var student = await _context.Students.FindAsync(_testStudentId);
        student!.RegistrationFeePaidAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ApplyRegistrationFeeAsync(_testStudentId));

        Assert.Contains("already been applied", exception.Message.ToLower());
    }

    [Fact]
    public async Task GetFeeStatusAsync_WhenNotPaid_ReturnsCorrectStatus()
    {
        // Act
        var result = await _service.GetFeeStatusAsync(_testStudentId);

        // Assert
        Assert.False(result.HasPaid);
        Assert.Null(result.PaidAt);
        Assert.Equal(25m, result.Amount);
        Assert.Null(result.InvoiceId);
    }

    [Fact]
    public async Task GetFeeStatusAsync_WhenPaid_ReturnsCorrectStatus()
    {
        // Arrange
        var paidAt = DateTime.UtcNow.AddDays(-5);
        var student = await _context.Students.FindAsync(_testStudentId);
        student!.RegistrationFeePaidAt = paidAt;

        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = "NMI-2024-00001",
            StudentId = _testStudentId,
            Status = InvoiceStatus.Paid,
            IssueDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-5)),
            DueDate = DateOnly.FromDateTime(DateTime.Today.AddDays(9)),
            Subtotal = 25m,
            VatAmount = 5.25m,
            Total = 30.25m
        };
        _context.Invoices.Add(invoice);

        var line = new InvoiceLine
        {
            InvoiceId = invoice.Id,
            Description = "Eenmalig inschrijfgeld",
            Quantity = 1,
            UnitPrice = 25m,
            VatRate = 21m,
            LineTotal = 25m,
            LessonId = null
        };
        _context.InvoiceLines.Add(line);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetFeeStatusAsync(_testStudentId);

        // Assert
        Assert.True(result.HasPaid);
        Assert.NotNull(result.PaidAt);
        Assert.Equal(25m, result.Amount);
        Assert.Equal(invoice.Id, result.InvoiceId);
    }

    [Fact]
    public async Task GetFeeStatusAsync_WithInvalidStudentId_ThrowsException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.GetFeeStatusAsync(Guid.NewGuid()));

        Assert.Contains("not found", exception.Message.ToLower());
    }

    [Fact]
    public async Task ApplyRegistrationFeeAsync_UsesDefaultValuesWhenSettingsMissing()
    {
        // Arrange - Remove settings
        var settings = _context.Settings.ToList();
        _context.Settings.RemoveRange(settings);
        await _context.SaveChangesAsync();

        _mockInvoiceRepository
            .Setup(r => r.GenerateInvoiceNumberAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("NMI-2024-00001");

        // Add a new student for this test
        var newStudentId = Guid.NewGuid();
        _context.Students.Add(new Student
        {
            Id = newStudentId,
            FirstName = "New",
            LastName = "Student",
            Email = "new@example.com",
            Status = StudentStatus.Active
        });
        await _context.SaveChangesAsync();

        // Act
        var invoiceId = await _service.ApplyRegistrationFeeAsync(newStudentId);

        // Assert
        var invoice = await _context.Invoices
            .Include(i => i.Lines)
            .FirstOrDefaultAsync(i => i.Id == invoiceId);

        var line = invoice!.Lines.First();
        Assert.Equal("Eenmalig inschrijfgeld", line.Description);
        Assert.Equal(25m, line.UnitPrice); // Default value
        Assert.Equal(21m, line.VatRate); // Default value
    }
}
