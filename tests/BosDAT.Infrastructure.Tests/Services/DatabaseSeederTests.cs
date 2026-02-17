using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using BosDAT.Core.Entities;
using BosDAT.Core.Enums;
using BosDAT.Infrastructure.Data;
using BosDAT.Infrastructure.Seeding;
using BosDAT.Infrastructure.Seeding.DataGenerators;

namespace BosDAT.Infrastructure.Tests.Services;

/// <summary>
/// Integration tests for the DatabaseSeeder service.
/// Tests seeding, resetting, and data integrity.
/// </summary>
public class DatabaseSeederTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
    private readonly Mock<ILogger<DatabaseSeeder>> _mockLogger;
    private readonly DatabaseSeeder _seeder;
    private readonly ApplicationUser _adminUser;

    public DatabaseSeederTests()
    {
        // Create in-memory database with unique name
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"SeederTest_{Guid.NewGuid()}")
            .Options;

        _context = new ApplicationDbContext(options);

        // Seed the reference data that would normally come from migrations
        SeedReferenceData();

        // Setup mock UserManager
        var store = new Mock<IUserStore<ApplicationUser>>();
        _mockUserManager = new Mock<UserManager<ApplicationUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        // Create admin user
        _adminUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = SeederConstants.AdminEmail,
            Email = SeederConstants.AdminEmail,
            FirstName = "Admin",
            LastName = "User",
            IsActive = true
        };

        _mockUserManager
            .Setup(x => x.FindByEmailAsync(SeederConstants.AdminEmail))
            .ReturnsAsync(_adminUser);

        _mockLogger = new Mock<ILogger<DatabaseSeeder>>();

        _seeder = new DatabaseSeeder(_context, _mockUserManager.Object, _mockLogger.Object);
    }

    private void SeedReferenceData()
    {
        // Seed instruments (same as ApplicationDbContext.SeedData)
        _context.Instruments.AddRange(
            new Instrument { Id = 1, Name = "Piano", Category = InstrumentCategory.Keyboard },
            new Instrument { Id = 2, Name = "Guitar", Category = InstrumentCategory.String },
            new Instrument { Id = 3, Name = "Bass Guitar", Category = InstrumentCategory.String },
            new Instrument { Id = 4, Name = "Drums", Category = InstrumentCategory.Percussion },
            new Instrument { Id = 5, Name = "Violin", Category = InstrumentCategory.String },
            new Instrument { Id = 6, Name = "Vocals", Category = InstrumentCategory.Vocal },
            new Instrument { Id = 7, Name = "Saxophone", Category = InstrumentCategory.Wind },
            new Instrument { Id = 8, Name = "Flute", Category = InstrumentCategory.Wind },
            new Instrument { Id = 9, Name = "Trumpet", Category = InstrumentCategory.Brass },
            new Instrument { Id = 10, Name = "Keyboard", Category = InstrumentCategory.Keyboard }
        );

        // Seed rooms
        _context.Rooms.AddRange(
            new Room { Id = 1, Name = "Room 1", Capacity = 2, HasPiano = true },
            new Room { Id = 2, Name = "Room 2", Capacity = 2, HasPiano = true },
            new Room { Id = 3, Name = "Room 3", Capacity = 4, HasDrums = true },
            new Room { Id = 4, Name = "Room 4", Capacity = 6, HasAmplifier = true, HasMicrophone = true },
            new Room { Id = 5, Name = "Group Room", Capacity = 10, HasPiano = true, HasWhiteboard = true }
        );

        // Seed settings
        _context.Settings.AddRange(
            new Setting { Key = "vat_rate", Value = "21", Type = "decimal" },
            new Setting { Key = "child_age_limit", Value = "18", Type = "int" },
            new Setting { Key = "registration_fee", Value = "25", Type = "decimal" },
            new Setting { Key = "invoice_prefix", Value = "NMI", Type = "string" }
        );

        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    #region IsSeededAsync Tests

    [Fact]
    public async Task IsSeededAsync_WhenNoTeachers_ReturnsFalse()
    {
        // Arrange - no teachers in database

        // Act
        var result = await _seeder.IsSeededAsync();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task IsSeededAsync_WhenOneTeacher_ReturnsFalse()
    {
        // Arrange
        _context.Teachers.Add(new Teacher
        {
            Id = Guid.NewGuid(),
            FirstName = "Test",
            LastName = "Teacher",
            Email = "test@test.com",
            HourlyRate = 50m
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _seeder.IsSeededAsync();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task IsSeededAsync_WhenMultipleTeachers_ReturnsTrue()
    {
        // Arrange
        _context.Teachers.AddRange(
            new Teacher { Id = Guid.NewGuid(), FirstName = "Test1", LastName = "Teacher", Email = "test1@test.com", HourlyRate = 50m },
            new Teacher { Id = Guid.NewGuid(), FirstName = "Test2", LastName = "Teacher", Email = "test2@test.com", HourlyRate = 50m }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _seeder.IsSeededAsync();

        // Assert
        Assert.True(result);
    }

    #endregion

    #region SeedAsync Tests

    [Fact]
    public async Task SeedAsync_CreatesTeachers()
    {
        // Arrange - empty database with reference data

        // Act
        await _seeder.SeedAsync();

        // Assert
        var teachers = await _context.Teachers.ToListAsync();
        Assert.Equal(8, teachers.Count);
        Assert.All(teachers, t => Assert.False(string.IsNullOrEmpty(t.Email)));
        Assert.All(teachers, t => Assert.True(t.HourlyRate > 0));
    }

    [Fact]
    public async Task SeedAsync_CreatesCourseTypesForAllInstruments()
    {
        // Arrange

        // Act
        await _seeder.SeedAsync();

        // Assert
        var courseTypes = await _context.CourseTypes.ToListAsync();
        var instrumentIds = courseTypes.Select(ct => ct.InstrumentId).Distinct().ToList();

        // Should have course types for all 10 instruments
        Assert.Equal(10, instrumentIds.Count);

        // Should have Individual types for all instruments (30min and 45min)
        var individualTypes = courseTypes.Where(ct => ct.Type == CourseTypeCategory.Individual).ToList();
        Assert.InRange(individualTypes.Count, 20, int.MaxValue); // 2 per instrument

        // Should have Group types for some instruments
        var groupTypes = courseTypes.Where(ct => ct.Type == CourseTypeCategory.Group).ToList();
        Assert.InRange(groupTypes.Count, 4, int.MaxValue);

        // Should have Workshop types for some instruments
        var workshopTypes = courseTypes.Where(ct => ct.Type == CourseTypeCategory.Workshop).ToList();
        Assert.InRange(workshopTypes.Count, 3, int.MaxValue);
    }

    [Fact]
    public async Task SeedAsync_CreatesPricingVersionsForAllCourseTypes()
    {
        // Arrange

        // Act
        await _seeder.SeedAsync();

        // Assert
        var courseTypes = await _context.CourseTypes.ToListAsync();
        var pricingVersions = await _context.CourseTypePricingVersions.ToListAsync();

        // Each course type should have at least one current pricing version
        foreach (var courseType in courseTypes)
        {
            var currentPricing = pricingVersions
                .Where(pv => pv.CourseTypeId == courseType.Id && pv.IsCurrent)
                .ToList();

            Assert.Single(currentPricing);
            Assert.True(currentPricing[0].PriceAdult > 0);
            Assert.True(currentPricing[0].PriceChild > 0);
            Assert.True(currentPricing[0].PriceChild < currentPricing[0].PriceAdult); // Child discount
        }
    }

    [Fact]
    public async Task SeedAsync_CreatesStudentsWithVariousStatuses()
    {
        // Arrange

        // Act
        await _seeder.SeedAsync();

        // Assert
        var students = await _context.Students.ToListAsync();
        Assert.Equal(25, students.Count);

        // Should have various statuses
        var activeCount = students.Count(s => s.Status == StudentStatus.Active);
        var trialCount = students.Count(s => s.Status == StudentStatus.Trial);
        var inactiveCount = students.Count(s => s.Status == StudentStatus.Inactive);

        Assert.InRange(activeCount, 16, int.MaxValue);
        Assert.InRange(trialCount, 3, int.MaxValue);
        Assert.InRange(inactiveCount, 3, int.MaxValue);

        // Some should have billing contacts (children)
        var withBillingContact = students.Count(s => !string.IsNullOrEmpty(s.BillingContactEmail));
        Assert.InRange(withBillingContact, 8, int.MaxValue);
    }

    [Fact]
    public async Task SeedAsync_CreatesCoursesWithVariousFrequencies()
    {
        // Arrange

        // Act
        await _seeder.SeedAsync();

        // Assert
        var courses = await _context.Courses.ToListAsync();
        Assert.InRange(courses.Count, 31, int.MaxValue);

        // Should have various frequencies
        var weeklyCount = courses.Count(c => c.Frequency == CourseFrequency.Weekly);
        var biweeklyCount = courses.Count(c => c.Frequency == CourseFrequency.Biweekly);

        Assert.True(weeklyCount > biweeklyCount, "Majority should be weekly"); // Comparison assertion

        // Should have various statuses
        var activeCount = courses.Count(c => c.Status == CourseStatus.Active);
        var completedCount = courses.Count(c => c.Status == CourseStatus.Completed);

        Assert.True(activeCount > completedCount, "Active courses should outnumber completed"); // Comparison assertion

        // Some should be trials
        var trialCount = courses.Count(c => c.IsTrial);
        Assert.NotEqual(0, trialCount);
    }

    [Fact]
    public async Task SeedAsync_CreatesEnrollmentsWithDiscounts()
    {
        // Arrange

        // Act
        await _seeder.SeedAsync();

        // Assert
        var enrollments = await _context.Enrollments.ToListAsync();
        Assert.InRange(enrollments.Count, 20, int.MaxValue);

        // Some should have discounts
        var withDiscount = enrollments.Count(e => e.DiscountPercent > 0);
        Assert.NotEqual(0, withDiscount);

        // Should have family and course discounts
        var familyDiscount = enrollments.Count(e => e.DiscountType == DiscountType.Family);
        var courseDiscount = enrollments.Count(e => e.DiscountType == DiscountType.Course);
        var totalDiscounts = familyDiscount + courseDiscount;
        Assert.NotEqual(0, totalDiscounts);
    }

    [Fact]
    public async Task SeedAsync_CreatesLessonsWithProperScheduling()
    {
        // Arrange

        // Act
        await _seeder.SeedAsync();

        // Assert
        var lessons = await _context.Lessons.ToListAsync();
        Assert.InRange(lessons.Count, 101, int.MaxValue); // Should have many lessons

        // Should have past and future lessons
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var pastLessons = lessons.Count(l => l.ScheduledDate < today);
        var futureLessons = lessons.Count(l => l.ScheduledDate >= today);

        Assert.NotEqual(0, pastLessons);
        Assert.NotEqual(0, futureLessons);

        // Past lessons should mostly be completed
        var completedPastLessons = lessons
            .Where(l => l.ScheduledDate < today && l.Status == LessonStatus.Completed)
            .Count();
        var minExpectedCompleted = (int)(pastLessons * 0.7);
        Assert.InRange(completedPastLessons, minExpectedCompleted, int.MaxValue); // At least 70% completed
    }

    [Fact]
    public async Task SeedAsync_CreatesInvoicesWithRegistrationFees()
    {
        // Arrange

        // Act
        await _seeder.SeedAsync();

        // Assert
        var invoices = await _context.Invoices.ToListAsync();
        var invoiceLines = await _context.InvoiceLines.ToListAsync();

        Assert.NotEmpty(invoices);
        Assert.NotEmpty(invoiceLines);

        // Should have registration fee lines
        var registrationFeeLines = invoiceLines.Count(il =>
            il.Description.Contains("inschrijfgeld", StringComparison.OrdinalIgnoreCase));
        Assert.NotEqual(0, registrationFeeLines);

        // Invoices should have proper totals
        Assert.All(invoices, inv =>
        {
            Assert.True(inv.Total >= inv.Subtotal);
            Assert.True(inv.VatAmount >= 0);
        });
    }

    [Fact]
    public async Task SeedAsync_CreatesHolidays()
    {
        // Arrange

        // Act
        await _seeder.SeedAsync();

        // Assert
        var holidays = await _context.Holidays.ToListAsync();
        Assert.Equal(11, holidays.Count);
        Assert.Contains(holidays, h => h.Name.Contains("Kerst"));
        Assert.Contains(holidays, h => h.Name.Contains("Zomer"));
    }

    [Fact]
    public async Task SeedAsync_IsIdempotent_DoesNotDuplicateData()
    {
        // Arrange
        await _seeder.SeedAsync();
        var initialTeacherCount = await _context.Teachers.CountAsync();
        var initialStudentCount = await _context.Students.CountAsync();

        // Act - seed again
        await _seeder.SeedAsync();

        // Assert - counts should be the same
        var finalTeacherCount = await _context.Teachers.CountAsync();
        var finalStudentCount = await _context.Students.CountAsync();

        Assert.Equal(initialTeacherCount, finalTeacherCount);
        Assert.Equal(initialStudentCount, finalStudentCount);
    }

    #endregion

    #region ResetAsync Tests

    [Fact]
    public async Task ResetAsync_RemovesSeededData()
    {
        // Arrange
        await _seeder.SeedAsync();
        Assert.NotEqual(0, await _context.Teachers.CountAsync());
        Assert.NotEqual(0, await _context.Students.CountAsync());
        Assert.NotEqual(0, await _context.Courses.CountAsync());

        // Act
        await _seeder.ResetAsync();

        // Assert
        Assert.Equal(0, await _context.Teachers.CountAsync());
        Assert.Equal(0, await _context.Students.CountAsync());
        Assert.Equal(0, await _context.Courses.CountAsync());
        Assert.Equal(0, await _context.Lessons.CountAsync());
        Assert.Equal(0, await _context.Invoices.CountAsync());
    }

    [Fact]
    public async Task ResetAsync_PreservesReferenceData()
    {
        // Arrange
        await _seeder.SeedAsync();
        var initialInstrumentCount = await _context.Instruments.CountAsync();
        var initialRoomCount = await _context.Rooms.CountAsync();
        var initialSettingCount = await _context.Settings.CountAsync();

        // Act
        await _seeder.ResetAsync();

        // Assert - reference data should be preserved
        Assert.Equal(initialInstrumentCount, await _context.Instruments.CountAsync());
        Assert.Equal(initialRoomCount, await _context.Rooms.CountAsync());
        Assert.Equal(initialSettingCount, await _context.Settings.CountAsync());
    }

    [Fact]
    public async Task ResetAsync_ThenSeedAsync_WorksCorrectly()
    {
        // Arrange
        await _seeder.SeedAsync();
        await _seeder.ResetAsync();

        // Act
        await _seeder.SeedAsync();

        // Assert
        Assert.NotEqual(0, await _context.Teachers.CountAsync());
        Assert.NotEqual(0, await _context.Students.CountAsync());
        Assert.NotEqual(0, await _context.Courses.CountAsync());
    }

    #endregion

    #region Data Integrity Tests

    [Fact]
    public async Task SeedAsync_AllTeachersHaveInstruments()
    {
        // Arrange

        // Act
        await _seeder.SeedAsync();

        // Assert
        var teachers = await _context.Teachers.ToListAsync();
        var teacherInstruments = await _context.TeacherInstruments.ToListAsync();

        foreach (var teacher in teachers)
        {
            var instruments = teacherInstruments.Count(ti => ti.TeacherId == teacher.Id);
            Assert.NotEqual(0, instruments);
        }
    }

    [Fact]
    public async Task SeedAsync_AllTeachersHaveCourseTypes()
    {
        // Arrange

        // Act
        await _seeder.SeedAsync();

        // Assert
        var teachers = await _context.Teachers.ToListAsync();
        var teacherCourseTypes = await _context.TeacherCourseTypes.ToListAsync();

        foreach (var teacher in teachers)
        {
            var courseTypes = teacherCourseTypes.Count(tct => tct.TeacherId == teacher.Id);
            Assert.NotEqual(0, courseTypes);
        }
    }

    [Fact]
    public async Task SeedAsync_AllCoursesHaveValidTeacher()
    {
        // Arrange

        // Act
        await _seeder.SeedAsync();

        // Assert
        var courses = await _context.Courses.ToListAsync();
        var teacherIds = await _context.Teachers.Select(t => t.Id).ToListAsync();

        foreach (var course in courses)
        {
            Assert.Contains(course.TeacherId, teacherIds);
        }
    }

    [Fact]
    public async Task SeedAsync_AllEnrollmentsHaveValidStudentAndCourse()
    {
        // Arrange

        // Act
        await _seeder.SeedAsync();

        // Assert
        var enrollments = await _context.Enrollments.ToListAsync();
        var studentIds = await _context.Students.Select(s => s.Id).ToListAsync();
        var courseIds = await _context.Courses.Select(c => c.Id).ToListAsync();

        foreach (var enrollment in enrollments)
        {
            Assert.Contains(enrollment.StudentId, studentIds);
            Assert.Contains(enrollment.CourseId, courseIds);
        }
    }

    [Fact]
    public async Task SeedAsync_AllInvoiceLinesHaveValidInvoice()
    {
        // Arrange

        // Act
        await _seeder.SeedAsync();

        // Assert
        var invoiceLines = await _context.InvoiceLines.ToListAsync();
        var invoiceIds = await _context.Invoices.Select(i => i.Id).ToListAsync();

        foreach (var line in invoiceLines)
        {
            Assert.Contains(line.InvoiceId, invoiceIds);
        }
    }

    #endregion
}
