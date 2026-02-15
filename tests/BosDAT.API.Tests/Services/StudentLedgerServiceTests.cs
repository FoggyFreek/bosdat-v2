using Microsoft.EntityFrameworkCore;
using Xunit;
using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Interfaces;
using BosDAT.Infrastructure.Data;
using BosDAT.Infrastructure.Repositories;
using BosDAT.Infrastructure.Services;
using Moq;

namespace BosDAT.API.Tests.Services;

/// <summary>
/// Stub implementation of IUnitOfWork for testing with in-memory database.
/// Since EF Core's in-memory provider doesn't support transactions, these are no-ops.
/// </summary>
internal class TestUnitOfWork(ApplicationDbContext context, IStudentLedgerRepository ledgerRepository) : IUnitOfWork
{
    public IStudentRepository Students => throw new NotImplementedException();
    public ITeacherRepository Teachers => throw new NotImplementedException();
    public ICourseRepository Courses => throw new NotImplementedException();
    public IEnrollmentRepository Enrollments => throw new NotImplementedException();
    public ILessonRepository Lessons => throw new NotImplementedException();
    public IInvoiceRepository Invoices => throw new NotImplementedException();
    public IStudentLedgerRepository StudentLedgerEntries => ledgerRepository;

    public IStudentTransactionRepository StudentTransactions => throw new NotImplementedException();

    public IRepository<T> Repository<T>() where T : class => throw new NotImplementedException();

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => context.SaveChangesAsync(cancellationToken);

    public Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        => Task.CompletedTask; // No-op for in-memory

    public Task CommitTransactionAsync(CancellationToken cancellationToken = default)
        => Task.CompletedTask; // No-op for in-memory

    public Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
        => Task.CompletedTask; // No-op for in-memory

    public void Dispose() { }
}

public class StudentLedgerServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly StudentLedgerRepository _ledgerRepository;
    private readonly TestUnitOfWork _unitOfWork;
    private readonly Mock<IUnitOfWork> _unitOfWork2;
    private readonly StudentLedgerService _service;
    private readonly StudentTransactionRepository _studentTransactionRepository;
    private readonly StudentTransactionService _studentTransactionService;
        private readonly Guid _testStudentId = Guid.NewGuid();
    private readonly Guid _testUserId = Guid.NewGuid();
    private readonly Guid _testCourseId = Guid.NewGuid();
    private readonly Guid _testInvoiceId = Guid.NewGuid();

    public StudentLedgerServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options, null!);
        _ledgerRepository = new StudentLedgerRepository(_context);
        _studentTransactionRepository = new StudentTransactionRepository(_context);
        _unitOfWork = new TestUnitOfWork(_context, _ledgerRepository);
        _unitOfWork2 = new Mock<IUnitOfWork>();
        _studentTransactionService = new StudentTransactionService(_context, _studentTransactionRepository, _unitOfWork2.Object);
        _service = new StudentLedgerService(_context, _ledgerRepository, _unitOfWork, _studentTransactionService);

        SeedTestData();
    }

    private void SeedTestData()
    {
        // Add test user
        var user = new ApplicationUser
        {
            Id = _testUserId,
            UserName = "testuser@example.com",
            Email = "testuser@example.com",
            FirstName = "Test",
            LastName = "User",
            NormalizedUserName = "TESTUSER@EXAMPLE.COM",
            NormalizedEmail = "TESTUSER@EXAMPLE.COM"
        };
        _context.Users.Add(user);

        // Add test student
        var student = new Student
        {
            Id = _testStudentId,
            FirstName = "Test",
            LastName = "Student",
            Email = "student@example.com",
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

        // Add course
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

        // Add test invoice
        var invoice = new Invoice
        {
            Id = _testInvoiceId,
            InvoiceNumber = "NMI-2026-00001",
            StudentId = _testStudentId,
            Status = InvoiceStatus.Sent,
            IssueDate = DateOnly.FromDateTime(DateTime.Today),
            DueDate = DateOnly.FromDateTime(DateTime.Today.AddDays(14)),
            Subtotal = 100m,
            VatAmount = 21m,
            Total = 121m
        };
        _context.Invoices.Add(invoice);

        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _unitOfWork.Dispose();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    #region CreateEntryAsync Tests

    [Fact]
    public async Task CreateEntryAsync_WithValidCreditData_CreatesEntry()
    {
        // Arrange
        var dto = new CreateStudentLedgerEntryDto
        {
            StudentId = _testStudentId,
            Description = "Overpayment refund",
            Amount = 50m,
            EntryType = LedgerEntryType.Credit
        };

        // Act
        var result = await _service.CreateEntryAsync(dto, _testUserId);

        // Assert
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.StartsWith("CR-", result.CorrectionRefName);
        Assert.Equal("Overpayment refund", result.Description);
        Assert.Equal(50m, result.Amount);
        Assert.Equal(LedgerEntryType.Credit, result.EntryType);
        Assert.Equal(LedgerEntryStatus.Open, result.Status);
        Assert.Equal(0m, result.AppliedAmount);
        Assert.Equal(50m, result.RemainingAmount);
    }

    [Fact]
    public async Task CreateEntryAsync_WithValidDebitData_CreatesEntry()
    {
        // Arrange
        var dto = new CreateStudentLedgerEntryDto
        {
            StudentId = _testStudentId,
            Description = "Additional charge",
            Amount = 25m,
            EntryType = LedgerEntryType.Debit
        };

        // Act
        var result = await _service.CreateEntryAsync(dto, _testUserId);

        // Assert
        Assert.Equal(LedgerEntryType.Debit, result.EntryType);
        Assert.Equal(25m, result.Amount);
    }

    [Fact]
    public async Task CreateEntryAsync_WithCourseId_LinksToCourse()
    {
        // Arrange
        var dto = new CreateStudentLedgerEntryDto
        {
            StudentId = _testStudentId,
            Description = "Course cancellation refund",
            Amount = 75m,
            EntryType = LedgerEntryType.Credit,
            CourseId = _testCourseId
        };

        // Act
        var result = await _service.CreateEntryAsync(dto, _testUserId);

        // Assert
        Assert.Equal(_testCourseId, result.CourseId);
    }

    [Fact]
    public async Task CreateEntryAsync_WithZeroAmount_ThrowsArgumentException()
    {
        // Arrange
        var dto = new CreateStudentLedgerEntryDto
        {
            StudentId = _testStudentId,
            Description = "Invalid entry",
            Amount = 0m,
            EntryType = LedgerEntryType.Credit
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CreateEntryAsync(dto, _testUserId));

        Assert.Contains("greater than zero", exception.Message.ToLower());
    }

    [Fact]
    public async Task CreateEntryAsync_WithNegativeAmount_ThrowsArgumentException()
    {
        // Arrange
        var dto = new CreateStudentLedgerEntryDto
        {
            StudentId = _testStudentId,
            Description = "Invalid entry",
            Amount = -50m,
            EntryType = LedgerEntryType.Credit
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CreateEntryAsync(dto, _testUserId));

        Assert.Contains("greater than zero", exception.Message.ToLower());
    }

    [Fact]
    public async Task CreateEntryAsync_WithInvalidStudentId_ThrowsInvalidOperationException()
    {
        // Arrange
        var dto = new CreateStudentLedgerEntryDto
        {
            StudentId = Guid.NewGuid(),
            Description = "Test entry",
            Amount = 50m,
            EntryType = LedgerEntryType.Credit
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateEntryAsync(dto, _testUserId));

        Assert.Contains("student", exception.Message.ToLower());
        Assert.Contains("not found", exception.Message.ToLower());
    }

    [Fact]
    public async Task CreateEntryAsync_WithInvalidCourseId_ThrowsInvalidOperationException()
    {
        // Arrange
        var dto = new CreateStudentLedgerEntryDto
        {
            StudentId = _testStudentId,
            Description = "Test entry",
            Amount = 50m,
            EntryType = LedgerEntryType.Credit,
            CourseId = Guid.NewGuid()
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateEntryAsync(dto, _testUserId));

        Assert.Contains("course", exception.Message.ToLower());
        Assert.Contains("not found", exception.Message.ToLower());
    }

    [Fact]
    public async Task CreateEntryAsync_WithEmptyDescription_ThrowsArgumentException()
    {
        // Arrange
        var dto = new CreateStudentLedgerEntryDto
        {
            StudentId = _testStudentId,
            Description = "",
            Amount = 50m,
            EntryType = LedgerEntryType.Credit
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CreateEntryAsync(dto, _testUserId));

        Assert.Contains("description", exception.Message.ToLower());
    }

    [Fact]
    public async Task CreateEntryAsync_WithWhitespaceDescription_ThrowsArgumentException()
    {
        // Arrange
        var dto = new CreateStudentLedgerEntryDto
        {
            StudentId = _testStudentId,
            Description = "   ",
            Amount = 50m,
            EntryType = LedgerEntryType.Credit
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CreateEntryAsync(dto, _testUserId));

        Assert.Contains("description", exception.Message.ToLower());
    }

    [Fact]
    public async Task CreateEntryAsync_WithTooLongDescription_ThrowsArgumentException()
    {
        // Arrange
        var dto = new CreateStudentLedgerEntryDto
        {
            StudentId = _testStudentId,
            Description = new string('a', 501), // 501 chars, max is 500
            Amount = 50m,
            EntryType = LedgerEntryType.Credit
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CreateEntryAsync(dto, _testUserId));

        Assert.Contains("description", exception.Message.ToLower());
    }

    [Fact]
    public async Task CreateEntryAsync_GeneratesUniqueRefNames()
    {
        // Arrange
        var dto1 = new CreateStudentLedgerEntryDto
        {
            StudentId = _testStudentId,
            Description = "First entry",
            Amount = 50m,
            EntryType = LedgerEntryType.Credit
        };

        var dto2 = new CreateStudentLedgerEntryDto
        {
            StudentId = _testStudentId,
            Description = "Second entry",
            Amount = 75m,
            EntryType = LedgerEntryType.Credit
        };

        // Act
        var result1 = await _service.CreateEntryAsync(dto1, _testUserId);
        var result2 = await _service.CreateEntryAsync(dto2, _testUserId);

        // Assert
        Assert.NotEqual(result1.CorrectionRefName, result2.CorrectionRefName);
    }

    #endregion

    #region ReverseEntryAsync Tests

    [Fact]
    public async Task ReverseEntryAsync_WithOpenCreditEntry_CreatesDebitReversal()
    {
        // Arrange
        var createDto = new CreateStudentLedgerEntryDto
        {
            StudentId = _testStudentId,
            Description = "Original credit",
            Amount = 100m,
            EntryType = LedgerEntryType.Credit
        };
        var original = await _service.CreateEntryAsync(createDto, _testUserId);

        // Act
        var reversal = await _service.ReverseEntryAsync(original.Id, "Error correction", _testUserId);

        // Assert
        Assert.Equal(LedgerEntryType.Debit, reversal.EntryType);
        Assert.Equal(100m, reversal.Amount);
        Assert.Contains("Reversal", reversal.Description);
        Assert.Contains(original.CorrectionRefName, reversal.Description);
        Assert.Contains("Error correction", reversal.Description);
    }

    [Fact]
    public async Task ReverseEntryAsync_WithOpenDebitEntry_CreatesCreditReversal()
    {
        // Arrange
        var createDto = new CreateStudentLedgerEntryDto
        {
            StudentId = _testStudentId,
            Description = "Original debit",
            Amount = 50m,
            EntryType = LedgerEntryType.Debit
        };
        var original = await _service.CreateEntryAsync(createDto, _testUserId);

        // Act
        var reversal = await _service.ReverseEntryAsync(original.Id, "Charge cancelled", _testUserId);

        // Assert
        Assert.Equal(LedgerEntryType.Credit, reversal.EntryType);
        Assert.Equal(50m, reversal.Amount);
    }

    [Fact]
    public async Task ReverseEntryAsync_MarksOriginalAsFullyApplied()
    {
        // Arrange
        var createDto = new CreateStudentLedgerEntryDto
        {
            StudentId = _testStudentId,
            Description = "Original credit",
            Amount = 100m,
            EntryType = LedgerEntryType.Credit
        };
        var original = await _service.CreateEntryAsync(createDto, _testUserId);

        // Act
        await _service.ReverseEntryAsync(original.Id, "Error correction", _testUserId);

        // Assert
        var updatedOriginal = await _service.GetEntryAsync(original.Id);
        Assert.Equal(LedgerEntryStatus.FullyApplied, updatedOriginal!.Status);
    }

    [Fact]
    public async Task ReverseEntryAsync_WithInvalidEntryId_ThrowsInvalidOperationException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ReverseEntryAsync(Guid.NewGuid(), "Test reason", _testUserId));

        Assert.Contains("not found", exception.Message.ToLower());
    }

    [Fact]
    public async Task ReverseEntryAsync_WithEmptyReason_ThrowsArgumentException()
    {
        // Arrange
        var createDto = new CreateStudentLedgerEntryDto
        {
            StudentId = _testStudentId,
            Description = "Original credit",
            Amount = 100m,
            EntryType = LedgerEntryType.Credit
        };
        var original = await _service.CreateEntryAsync(createDto, _testUserId);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.ReverseEntryAsync(original.Id, "", _testUserId));

        Assert.Contains("reason", exception.Message.ToLower());
    }

    [Fact]
    public async Task ReverseEntryAsync_WithWhitespaceReason_ThrowsArgumentException()
    {
        // Arrange
        var createDto = new CreateStudentLedgerEntryDto
        {
            StudentId = _testStudentId,
            Description = "Original credit",
            Amount = 100m,
            EntryType = LedgerEntryType.Credit
        };
        var original = await _service.CreateEntryAsync(createDto, _testUserId);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.ReverseEntryAsync(original.Id, "   ", _testUserId));

        Assert.Contains("reason", exception.Message.ToLower());
    }

    #endregion

    #region GetStudentLedgerAsync Tests

    [Fact]
    public async Task GetStudentLedgerAsync_ReturnsAllEntriesForStudent()
    {
        // Arrange
        await _service.CreateEntryAsync(new CreateStudentLedgerEntryDto
        {
            StudentId = _testStudentId,
            Description = "Credit 1",
            Amount = 50m,
            EntryType = LedgerEntryType.Credit
        }, _testUserId);

        await _service.CreateEntryAsync(new CreateStudentLedgerEntryDto
        {
            StudentId = _testStudentId,
            Description = "Debit 1",
            Amount = 25m,
            EntryType = LedgerEntryType.Debit
        }, _testUserId);

        // Act
        var entries = await _service.GetStudentLedgerAsync(_testStudentId);

        // Assert
        Assert.Equal(2, entries.Count);
    }

    [Fact]
    public async Task GetStudentLedgerAsync_ReturnsEmptyForStudentWithNoEntries()
    {
        // Act
        var entries = await _service.GetStudentLedgerAsync(_testStudentId);

        // Assert
        Assert.Empty(entries);
    }

    #endregion

    #region GetStudentLedgerSummaryAsync Tests

    [Fact]
    public async Task GetStudentLedgerSummaryAsync_ReturnsCorrectTotals()
    {
        // Arrange
        await _service.CreateEntryAsync(new CreateStudentLedgerEntryDto
        {
            StudentId = _testStudentId,
            Description = "Credit 1",
            Amount = 100m,
            EntryType = LedgerEntryType.Credit
        }, _testUserId);

        await _service.CreateEntryAsync(new CreateStudentLedgerEntryDto
        {
            StudentId = _testStudentId,
            Description = "Credit 2",
            Amount = 50m,
            EntryType = LedgerEntryType.Credit
        }, _testUserId);

        await _service.CreateEntryAsync(new CreateStudentLedgerEntryDto
        {
            StudentId = _testStudentId,
            Description = "Debit 1",
            Amount = 30m,
            EntryType = LedgerEntryType.Debit
        }, _testUserId);

        // Act
        var summary = await _service.GetStudentLedgerSummaryAsync(_testStudentId);

        // Assert
        Assert.Equal(_testStudentId, summary.StudentId);
        Assert.Equal("Test Student", summary.StudentName);
        Assert.Equal(150m, summary.TotalCredits);
        Assert.Equal(30m, summary.TotalDebits);
        Assert.Equal(150m, summary.AvailableCredit);
        Assert.Equal(3, summary.OpenEntryCount);
    }

    [Fact]
    public async Task GetStudentLedgerSummaryAsync_WithInvalidStudentId_ThrowsInvalidOperationException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.GetStudentLedgerSummaryAsync(Guid.NewGuid()));

        Assert.Contains("not found", exception.Message.ToLower());
    }

    #endregion

    #region ApplyCreditsToInvoiceAsync Tests

    [Fact]
    public async Task ApplyCreditsToInvoiceAsync_WithSufficientCredits_FullyPaysInvoice()
    {
        // Arrange
        await _service.CreateEntryAsync(new CreateStudentLedgerEntryDto
        {
            StudentId = _testStudentId,
            Description = "Large credit",
            Amount = 200m,
            EntryType = LedgerEntryType.Credit
        }, _testUserId);

        // Act
        var result = await _service.ApplyCreditsToInvoiceAsync(_testInvoiceId, _testUserId);

        // Assert
        Assert.Equal(_testInvoiceId, result.InvoiceId);
        Assert.Equal(121m, result.AmountApplied); // Invoice total
        Assert.Equal(0m, result.RemainingBalance);
        Assert.Single(result.Applications);

        // Verify invoice is marked as paid
        var invoice = await _context.Invoices.FindAsync(_testInvoiceId);
        Assert.Equal(InvoiceStatus.Paid, invoice!.Status);
    }

    [Fact]
    public async Task ApplyCreditsToInvoiceAsync_WithInsufficientCredits_PartiallyPaysInvoice()
    {
        // Arrange
        await _service.CreateEntryAsync(new CreateStudentLedgerEntryDto
        {
            StudentId = _testStudentId,
            Description = "Small credit",
            Amount = 50m,
            EntryType = LedgerEntryType.Credit
        }, _testUserId);

        // Act
        var result = await _service.ApplyCreditsToInvoiceAsync(_testInvoiceId, _testUserId);

        // Assert
        Assert.Equal(50m, result.AmountApplied);
        Assert.Equal(71m, result.RemainingBalance);

        // Verify invoice status unchanged
        var invoice = await _context.Invoices.FindAsync(_testInvoiceId);
        Assert.Equal(InvoiceStatus.Sent, invoice!.Status);
    }

    [Fact]
    public async Task ApplyCreditsToInvoiceAsync_WithMultipleCredits_AppliesInFIFOOrder()
    {
        // Arrange
        var credit1 = await _service.CreateEntryAsync(new CreateStudentLedgerEntryDto
        {
            StudentId = _testStudentId,
            Description = "First credit",
            Amount = 40m,
            EntryType = LedgerEntryType.Credit
        }, _testUserId);

        // Add a small delay to ensure different CreatedAt timestamps
        await Task.Delay(10);

        var credit2 = await _service.CreateEntryAsync(new CreateStudentLedgerEntryDto
        {
            StudentId = _testStudentId,
            Description = "Second credit",
            Amount = 60m,
            EntryType = LedgerEntryType.Credit
        }, _testUserId);

        // Act
        var result = await _service.ApplyCreditsToInvoiceAsync(_testInvoiceId, _testUserId);

        // Assert
        Assert.Equal(100m, result.AmountApplied);
        Assert.Equal(2, result.Applications.Count);

        // Verify first credit is fully applied
        var entry1 = await _service.GetEntryAsync(credit1.Id);
        Assert.Equal(LedgerEntryStatus.FullyApplied, entry1!.Status);

        // Verify second credit is fully applied
        var entry2 = await _service.GetEntryAsync(credit2.Id);
        Assert.Equal(LedgerEntryStatus.FullyApplied, entry2!.Status);
    }

    [Fact]
    public async Task ApplyCreditsToInvoiceAsync_WithNoCredits_ReturnsZeroApplied()
    {
        // Act
        var result = await _service.ApplyCreditsToInvoiceAsync(_testInvoiceId, _testUserId);

        // Assert
        Assert.Equal(0m, result.AmountApplied);
        Assert.Equal(121m, result.RemainingBalance);
        Assert.Empty(result.Applications);
    }

    [Fact]
    public async Task ApplyCreditsToInvoiceAsync_UpdatesEntryStatus()
    {
        // Arrange
        var credit = await _service.CreateEntryAsync(new CreateStudentLedgerEntryDto
        {
            StudentId = _testStudentId,
            Description = "Credit to be applied",
            Amount = 50m,
            EntryType = LedgerEntryType.Credit
        }, _testUserId);

        // Act
        await _service.ApplyCreditsToInvoiceAsync(_testInvoiceId, _testUserId);

        // Assert
        var updatedEntry = await _service.GetEntryAsync(credit.Id);
        Assert.Equal(LedgerEntryStatus.FullyApplied, updatedEntry!.Status);
        Assert.Equal(50m, updatedEntry.AppliedAmount);
        Assert.Equal(0m, updatedEntry.RemainingAmount);
    }

    [Fact]
    public async Task ApplyCreditsToInvoiceAsync_WithPaidInvoice_ReturnsZeroApplied()
    {
        // Arrange
        var invoice = await _context.Invoices.FindAsync(_testInvoiceId);
        invoice!.Status = InvoiceStatus.Paid;
        invoice.PaidAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await _service.CreateEntryAsync(new CreateStudentLedgerEntryDto
        {
            StudentId = _testStudentId,
            Description = "Credit",
            Amount = 100m,
            EntryType = LedgerEntryType.Credit
        }, _testUserId);

        // Add payment to cover the total
        _context.Payments.Add(new Payment
        {
            Id = Guid.NewGuid(),
            InvoiceId = _testInvoiceId,
            Amount = 121m,
            PaymentDate = DateOnly.FromDateTime(DateTime.Today),
            Method = PaymentMethod.Bank
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ApplyCreditsToInvoiceAsync(_testInvoiceId, _testUserId);

        // Assert
        Assert.Equal(0m, result.AmountApplied);
        Assert.Equal(0m, result.RemainingBalance);
    }

    [Fact]
    public async Task ApplyCreditsToInvoiceAsync_WithInvalidInvoiceId_ThrowsInvalidOperationException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ApplyCreditsToInvoiceAsync(Guid.NewGuid(), _testUserId));

        Assert.Contains("invoice", exception.Message.ToLower());
        Assert.Contains("not found", exception.Message.ToLower());
    }

    [Fact]
    public async Task ApplyCreditsToInvoiceAsync_DoesNotApplyDebits()
    {
        // Arrange - Only add a debit, no credits
        await _service.CreateEntryAsync(new CreateStudentLedgerEntryDto
        {
            StudentId = _testStudentId,
            Description = "Debit entry",
            Amount = 50m,
            EntryType = LedgerEntryType.Debit
        }, _testUserId);

        // Act
        var result = await _service.ApplyCreditsToInvoiceAsync(_testInvoiceId, _testUserId);

        // Assert
        Assert.Equal(0m, result.AmountApplied);
        Assert.Empty(result.Applications);
    }

    #endregion

    #region GetAvailableCreditForStudentAsync Tests

    [Fact]
    public async Task GetAvailableCreditForStudentAsync_ReturnsCorrectAmount()
    {
        // Arrange
        await _service.CreateEntryAsync(new CreateStudentLedgerEntryDto
        {
            StudentId = _testStudentId,
            Description = "Credit 1",
            Amount = 100m,
            EntryType = LedgerEntryType.Credit
        }, _testUserId);

        await _service.CreateEntryAsync(new CreateStudentLedgerEntryDto
        {
            StudentId = _testStudentId,
            Description = "Credit 2",
            Amount = 50m,
            EntryType = LedgerEntryType.Credit
        }, _testUserId);

        // Act
        var availableCredit = await _service.GetAvailableCreditForStudentAsync(_testStudentId);

        // Assert
        Assert.Equal(150m, availableCredit);
    }

    [Fact]
    public async Task GetAvailableCreditForStudentAsync_ExcludesDebits()
    {
        // Arrange
        await _service.CreateEntryAsync(new CreateStudentLedgerEntryDto
        {
            StudentId = _testStudentId,
            Description = "Credit",
            Amount = 100m,
            EntryType = LedgerEntryType.Credit
        }, _testUserId);

        await _service.CreateEntryAsync(new CreateStudentLedgerEntryDto
        {
            StudentId = _testStudentId,
            Description = "Debit",
            Amount = 50m,
            EntryType = LedgerEntryType.Debit
        }, _testUserId);

        // Act
        var availableCredit = await _service.GetAvailableCreditForStudentAsync(_testStudentId);

        // Assert
        Assert.Equal(100m, availableCredit);
    }

    [Fact]
    public async Task GetAvailableCreditForStudentAsync_SubtractsAppliedAmounts()
    {
        // Arrange
        await _service.CreateEntryAsync(new CreateStudentLedgerEntryDto
        {
            StudentId = _testStudentId,
            Description = "Credit",
            Amount = 100m,
            EntryType = LedgerEntryType.Credit
        }, _testUserId);

        // Apply some credit to invoice
        await _service.ApplyCreditsToInvoiceAsync(_testInvoiceId, _testUserId);

        // Act
        var availableCredit = await _service.GetAvailableCreditForStudentAsync(_testStudentId);

        // Assert - Invoice total was 121m, but credit was only 100m
        Assert.Equal(0m, availableCredit); // All credit was applied
    }

    [Fact]
    public async Task GetAvailableCreditForStudentAsync_WithNoCredits_ReturnsZero()
    {
        // Act
        var availableCredit = await _service.GetAvailableCreditForStudentAsync(_testStudentId);

        // Assert
        Assert.Equal(0m, availableCredit);
    }

    #endregion

    #region GenerateCorrectionRefNameAsync Tests

    [Fact]
    public async Task GenerateCorrectionRefNameAsync_ReturnsCorrectFormat()
    {
        // Act
        var refName = await _service.GenerateCorrectionRefNameAsync();

        // Assert
        Assert.Matches(@"^CR-\d{4}-\d{4}$", refName);
        Assert.StartsWith($"CR-{DateTime.UtcNow.Year}-", refName);
    }

    [Fact]
    public async Task GenerateCorrectionRefNameAsync_IncrementsSequentially()
    {
        // Arrange - Create first entry
        await _service.CreateEntryAsync(new CreateStudentLedgerEntryDto
        {
            StudentId = _testStudentId,
            Description = "First entry",
            Amount = 50m,
            EntryType = LedgerEntryType.Credit
        }, _testUserId);

        // Act
        var refName = await _service.GenerateCorrectionRefNameAsync();

        // Assert
        Assert.EndsWith("-0002", refName);
    }

    #endregion

    #region DecoupleApplicationAsync Tests

    [Fact]
    public async Task DecoupleApplicationAsync_WithValidApplication_DecouplesSuccessfully()
    {
        // Arrange - 50m credit fully applied to 121m invoice (entry becomes FullyApplied, invoice stays Sent)
        var credit = await _service.CreateEntryAsync(new CreateStudentLedgerEntryDto
        {
            StudentId = _testStudentId,
            Description = "Credit to decouple",
            Amount = 50m,
            EntryType = LedgerEntryType.Credit
        }, _testUserId);

        await _service.ApplyCreditsToInvoiceAsync(_testInvoiceId, _testUserId);

        var entry = await _service.GetEntryAsync(credit.Id);
        Assert.Equal(LedgerEntryStatus.FullyApplied, entry!.Status);
        var applicationId = entry.Applications[0].Id;

        // Act
        var result = await _service.DecoupleApplicationAsync(applicationId, "Applied to wrong invoice", _testUserId);

        // Assert
        Assert.Equal(credit.Id, result.LedgerEntryId);
        Assert.Equal(_testInvoiceId, result.InvoiceId);
        Assert.Equal(50m, result.DecoupledAmount);
        Assert.Equal(LedgerEntryStatus.Open, result.NewEntryStatus);
        Assert.Equal(InvoiceStatus.Sent, result.NewInvoiceStatus);

        var updatedEntry = await _service.GetEntryAsync(credit.Id);
        Assert.Equal(LedgerEntryStatus.Open, updatedEntry!.Status);
        Assert.Empty(updatedEntry.Applications);
    }

    [Fact]
    public async Task DecoupleApplicationAsync_WithPaidInvoice_RevertsInvoiceStatus()
    {
        // Arrange - 200m credit covers 121m invoice fully → invoice becomes Paid
        await _service.CreateEntryAsync(new CreateStudentLedgerEntryDto
        {
            StudentId = _testStudentId,
            Description = "Large credit",
            Amount = 200m,
            EntryType = LedgerEntryType.Credit
        }, _testUserId);

        var applyResult = await _service.ApplyCreditsToInvoiceAsync(_testInvoiceId, _testUserId);
        Assert.Equal(0m, applyResult.RemainingBalance);

        var invoice = await _context.Invoices.FindAsync(_testInvoiceId);
        Assert.Equal(InvoiceStatus.Paid, invoice!.Status);

        var applicationId = applyResult.Applications[0].Id;

        // Act
        var result = await _service.DecoupleApplicationAsync(applicationId, "Incorrect application", _testUserId);

        // Assert - invoice reverts to Sent (due date is +14 days in SeedTestData)
        Assert.Equal(InvoiceStatus.Sent, result.NewInvoiceStatus);

        var updatedInvoice = await _context.Invoices.FindAsync(_testInvoiceId);
        Assert.Equal(InvoiceStatus.Sent, updatedInvoice!.Status);
        Assert.Null(updatedInvoice.PaidAt);
    }

    [Fact]
    public async Task DecoupleApplicationAsync_WithOverdueInvoice_RevertsToOverdue()
    {
        // Arrange - set due date in the past so revert lands on Overdue
        var invoice = await _context.Invoices.FindAsync(_testInvoiceId);
        invoice!.DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5));
        await _context.SaveChangesAsync();

        await _service.CreateEntryAsync(new CreateStudentLedgerEntryDto
        {
            StudentId = _testStudentId,
            Description = "Credit",
            Amount = 200m,
            EntryType = LedgerEntryType.Credit
        }, _testUserId);

        var applyResult = await _service.ApplyCreditsToInvoiceAsync(_testInvoiceId, _testUserId);
        var applicationId = applyResult.Applications[0].Id;

        // Act
        var result = await _service.DecoupleApplicationAsync(applicationId, "Wrong invoice", _testUserId);

        // Assert
        Assert.Equal(InvoiceStatus.Overdue, result.NewInvoiceStatus);
    }

    [Fact]
    public async Task DecoupleApplicationAsync_WithMultipleApplications_KeepsPartiallyApplied()
    {
        // Arrange - 200m credit applied across two invoices
        await _service.CreateEntryAsync(new CreateStudentLedgerEntryDto
        {
            StudentId = _testStudentId,
            Description = "Credit for two invoices",
            Amount = 200m,
            EntryType = LedgerEntryType.Credit
        }, _testUserId);

        // Apply to invoice 1 (121m)
        await _service.ApplyCreditsToInvoiceAsync(_testInvoiceId, _testUserId);

        // Create invoice 2 and apply remaining credit
        var invoice2 = new Invoice
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = "NMI-2026-00002",
            StudentId = _testStudentId,
            Status = InvoiceStatus.Sent,
            IssueDate = DateOnly.FromDateTime(DateTime.Today),
            DueDate = DateOnly.FromDateTime(DateTime.Today.AddDays(14)),
            Subtotal = 50m,
            VatAmount = 0m,
            Total = 50m
        };
        _context.Invoices.Add(invoice2);
        await _context.SaveChangesAsync();

        await _service.ApplyCreditsToInvoiceAsync(invoice2.Id, _testUserId);

        // Entry now has two applications (121 + 50 = 171 of 200)
        var entries = await _service.GetStudentLedgerAsync(_testStudentId);
        var creditEntry = entries.First();
        Assert.Equal(2, creditEntry.Applications.Count);

        // Act - decouple the application on invoice 1 (121m)
        var firstAppId = creditEntry.Applications
            .First(a => a.InvoiceId == _testInvoiceId).Id;
        var result = await _service.DecoupleApplicationAsync(firstAppId, "Wrong invoice", _testUserId);

        // Assert - 50m still applied out of 200m → PartiallyApplied
        Assert.Equal(LedgerEntryStatus.PartiallyApplied, result.NewEntryStatus);
        Assert.Equal(121m, result.DecoupledAmount);
    }

    [Fact]
    public async Task DecoupleApplicationAsync_WithInvalidApplicationId_ThrowsInvalidOperationException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.DecoupleApplicationAsync(Guid.NewGuid(), "Test reason", _testUserId));

        Assert.Contains("not found", exception.Message.ToLower());
    }

    [Fact]
    public async Task DecoupleApplicationAsync_WithEmptyReason_ThrowsArgumentException()
    {
        // Arrange
        await _service.CreateEntryAsync(new CreateStudentLedgerEntryDto
        {
            StudentId = _testStudentId,
            Description = "Credit",
            Amount = 50m,
            EntryType = LedgerEntryType.Credit
        }, _testUserId);

        var applyResult = await _service.ApplyCreditsToInvoiceAsync(_testInvoiceId, _testUserId);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.DecoupleApplicationAsync(applyResult.Applications[0].Id, "", _testUserId));

        Assert.Contains("reason", exception.Message.ToLower());
    }

    [Fact]
    public async Task DecoupleApplicationAsync_WithWhitespaceReason_ThrowsArgumentException()
    {
        // Arrange
        await _service.CreateEntryAsync(new CreateStudentLedgerEntryDto
        {
            StudentId = _testStudentId,
            Description = "Credit",
            Amount = 50m,
            EntryType = LedgerEntryType.Credit
        }, _testUserId);

        var applyResult = await _service.ApplyCreditsToInvoiceAsync(_testInvoiceId, _testUserId);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.DecoupleApplicationAsync(applyResult.Applications[0].Id, "   ", _testUserId));

        Assert.Contains("reason", exception.Message.ToLower());
    }

    #endregion

    #region GetEntryAsync Tests

    [Fact]
    public async Task GetEntryAsync_WithValidId_ReturnsEntry()
    {
        // Arrange
        var created = await _service.CreateEntryAsync(new CreateStudentLedgerEntryDto
        {
            StudentId = _testStudentId,
            Description = "Test entry",
            Amount = 50m,
            EntryType = LedgerEntryType.Credit
        }, _testUserId);

        // Act
        var entry = await _service.GetEntryAsync(created.Id);

        // Assert
        Assert.NotNull(entry);
        Assert.Equal(created.Id, entry.Id);
        Assert.Equal("Test entry", entry.Description);
    }

    [Fact]
    public async Task GetEntryAsync_WithInvalidId_ReturnsNull()
    {
        // Act
        var entry = await _service.GetEntryAsync(Guid.NewGuid());

        // Assert
        Assert.Null(entry);
    }

    [Fact]
    public async Task GetEntryAsync_IncludesApplications()
    {
        // Arrange
        var credit = await _service.CreateEntryAsync(new CreateStudentLedgerEntryDto
        {
            StudentId = _testStudentId,
            Description = "Credit with application",
            Amount = 200m,
            EntryType = LedgerEntryType.Credit
        }, _testUserId);

        await _service.ApplyCreditsToInvoiceAsync(_testInvoiceId, _testUserId);

        // Act
        var entry = await _service.GetEntryAsync(credit.Id);

        // Assert
        Assert.NotNull(entry);
        Assert.NotEmpty(entry.Applications);
        Assert.Equal(_testInvoiceId, entry.Applications[0].InvoiceId);
    }

    #endregion
}
