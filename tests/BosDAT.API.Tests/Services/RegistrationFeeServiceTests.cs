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
    private readonly Mock<IStudentLedgerRepository> _mockLedgerRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly RegistrationFeeService _service;
    private readonly Guid _testStudentId = Guid.NewGuid();
    private readonly Guid _testCourseId = Guid.NewGuid();
    private readonly Guid _testTrialCourseId = Guid.NewGuid();
    private readonly Guid _testUserId = Guid.NewGuid();

    public RegistrationFeeServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options, null!);
        _mockLedgerRepository = new Mock<IStudentLedgerRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();

        // Setup default mocks
        _mockCurrentUserService.Setup(s => s.UserId).Returns(_testUserId);
        _mockLedgerRepository
            .Setup(r => r.GenerateCorrectionRefNameAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("CORR-2024-00001");
        _mockUnitOfWork
            .Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockUnitOfWork
            .Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockUnitOfWork
            .Setup(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _service = new RegistrationFeeService(
            _context,
            _mockLedgerRepository.Object,
            _mockUnitOfWork.Object,
            _mockCurrentUserService.Object);

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

        // Add test user
        _context.Users.Add(new ApplicationUser
        {
            Id = _testUserId,
            UserName = "testuser@example.com",
            Email = "testuser@example.com",
            FirstName = "Test",
            LastName = "User"
        });

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
    public async Task ApplyRegistrationFeeAsync_CreatesLedgerEntry()
    {
        // Act
        var ledgerEntryId = await _service.ApplyRegistrationFeeAsync(_testStudentId);

        // Assert
        Assert.NotEqual(Guid.Empty, ledgerEntryId);

        var ledgerEntry = await _context.StudentLedgerEntries
            .FirstOrDefaultAsync(e => e.Id == ledgerEntryId);

        Assert.NotNull(ledgerEntry);
        Assert.Equal(_testStudentId, ledgerEntry.StudentId);
        Assert.Equal("Eenmalig inschrijfgeld", ledgerEntry.Description);
        Assert.Equal(25m, ledgerEntry.Amount);
        Assert.Equal(LedgerEntryType.Debit, ledgerEntry.EntryType);
        Assert.Equal(LedgerEntryStatus.Open, ledgerEntry.Status);
        Assert.Equal(_testUserId, ledgerEntry.CreatedById);
    }

    [Fact]
    public async Task ApplyRegistrationFeeAsync_SetsCorrectLedgerEntryType()
    {
        // Act
        var ledgerEntryId = await _service.ApplyRegistrationFeeAsync(_testStudentId);

        // Assert
        var ledgerEntry = await _context.StudentLedgerEntries
            .FirstOrDefaultAsync(e => e.Id == ledgerEntryId);

        Assert.NotNull(ledgerEntry);
        Assert.Equal(LedgerEntryType.Debit, ledgerEntry.EntryType);
    }

    [Fact]
    public async Task ApplyRegistrationFeeAsync_SetsRegistrationFeePaidAt()
    {
        // Act
        await _service.ApplyRegistrationFeeAsync(_testStudentId);

        // Assert
        var student = await _context.Students.FindAsync(_testStudentId);
        Assert.NotNull(student!.RegistrationFeePaidAt);
    }

    [Fact]
    public async Task ApplyRegistrationFeeAsync_GeneratesCorrectionRefName()
    {
        // Act
        var ledgerEntryId = await _service.ApplyRegistrationFeeAsync(_testStudentId);

        // Assert
        var ledgerEntry = await _context.StudentLedgerEntries
            .FirstOrDefaultAsync(e => e.Id == ledgerEntryId);

        Assert.NotNull(ledgerEntry);
        Assert.Equal("CORR-2024-00001", ledgerEntry.CorrectionRefName);
        _mockLedgerRepository.Verify(r => r.GenerateCorrectionRefNameAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ApplyRegistrationFeeAsync_UsesTransaction()
    {
        // Act
        await _service.ApplyRegistrationFeeAsync(_testStudentId);

        // Assert
        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
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
    public async Task ApplyRegistrationFeeAsync_WhenNoCurrentUser_ThrowsException()
    {
        // Arrange
        _mockCurrentUserService.Setup(s => s.UserId).Returns((Guid?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ApplyRegistrationFeeAsync(_testStudentId));

        Assert.Contains("user", exception.Message.ToLower());
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
        Assert.Null(result.LedgerEntryId);
    }

    [Fact]
    public async Task GetFeeStatusAsync_WhenPaid_ReturnsCorrectStatus()
    {
        // Arrange
        var paidAt = DateTime.UtcNow.AddDays(-5);
        var student = await _context.Students.FindAsync(_testStudentId);
        student!.RegistrationFeePaidAt = paidAt;

        var ledgerEntry = new StudentLedgerEntry
        {
            Id = Guid.NewGuid(),
            CorrectionRefName = "CORR-2024-00001",
            Description = "Eenmalig inschrijfgeld",
            StudentId = _testStudentId,
            Amount = 25m,
            EntryType = LedgerEntryType.Debit,
            Status = LedgerEntryStatus.Open,
            CreatedById = _testUserId,
            CreatedAt = paidAt
        };
        _context.StudentLedgerEntries.Add(ledgerEntry);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetFeeStatusAsync(_testStudentId);

        // Assert
        Assert.True(result.HasPaid);
        Assert.NotNull(result.PaidAt);
        Assert.Equal(25m, result.Amount);
        Assert.Equal(ledgerEntry.Id, result.LedgerEntryId);
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
        var ledgerEntryId = await _service.ApplyRegistrationFeeAsync(newStudentId);

        // Assert
        var ledgerEntry = await _context.StudentLedgerEntries
            .FirstOrDefaultAsync(e => e.Id == ledgerEntryId);

        Assert.NotNull(ledgerEntry);
        Assert.Equal("Eenmalig inschrijfgeld", ledgerEntry.Description);
        Assert.Equal(25m, ledgerEntry.Amount); // Default value
    }

    [Fact]
    public async Task ApplyRegistrationFeeAsync_DoesNotCreateInvoice()
    {
        // Act
        await _service.ApplyRegistrationFeeAsync(_testStudentId);

        // Assert - No invoice should be created
        var invoices = await _context.Invoices
            .Where(i => i.StudentId == _testStudentId)
            .ToListAsync();

        Assert.Empty(invoices);
    }
}
