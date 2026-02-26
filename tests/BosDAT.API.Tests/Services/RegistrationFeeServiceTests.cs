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
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();

        // Setup default mocks
        _mockCurrentUserService.Setup(s => s.UserId).Returns(_testUserId);
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
            _context);

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
        GC.SuppressFinalize(this);
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
    public async Task GetFeeStatusAsync_WhenNotPaid_ReturnsCorrectStatus()
    {
        // Act
        var result = await _service.GetFeeStatusAsync(_testStudentId);

        // Assert
        Assert.False(result.HasPaid);
        Assert.Null(result.PaidAt);
        Assert.Equal(25m, result.Amount);
    }

    [Fact]
    public async Task GetFeeStatusAsync_WithInvalidStudentId_ThrowsException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.GetFeeStatusAsync(Guid.NewGuid()));

        Assert.Contains("not found", exception.Message.ToLower());
    }

}
