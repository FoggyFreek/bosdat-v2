using Microsoft.EntityFrameworkCore;
using Xunit;
using BosDAT.Core.Entities;
using BosDAT.Infrastructure.Data;
using BosDAT.Infrastructure.Services;

namespace BosDAT.API.Tests.Services;

public class CourseTypePricingServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly CourseTypePricingService _service;
    private readonly Guid _testCourseTypeId = Guid.NewGuid();

    public CourseTypePricingServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options, null!);
        _service = new CourseTypePricingService(_context);

        SeedTestData();
    }

    private void SeedTestData()
    {
        // Set up a test course type
        var instrument = new Instrument
        {
            Id = 1,
            Name = "Piano",
            IsActive = true
        };
        _context.Instruments.Add(instrument);

        var courseType = new CourseType
        {
            Id = _testCourseTypeId,
            Name = "Piano Lesson",
            DurationMinutes = 30,
            InstrumentId = 1,
            IsActive = true
        };
        _context.CourseTypes.Add(courseType);
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task GetCurrentPricingAsync_WithNoPricing_ReturnsNull()
    {
        // Act
        var result = await _service.GetCurrentPricingAsync(_testCourseTypeId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetCurrentPricingAsync_WithCurrentPricing_ReturnsPricing()
    {
        // Arrange
        var pricingVersion = new CourseTypePricingVersion
        {
            Id = Guid.NewGuid(),
            CourseTypeId = _testCourseTypeId,
            PriceAdult = 50.00m,
            PriceChild = 40.00m,
            ValidFrom = DateOnly.FromDateTime(DateTime.Today),
            IsCurrent = true
        };
        _context.CourseTypePricingVersions.Add(pricingVersion);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetCurrentPricingAsync(_testCourseTypeId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(50.00m, result.PriceAdult);
        Assert.Equal(40.00m, result.PriceChild);
        Assert.True(result.IsCurrent);
    }

    [Fact]
    public async Task GetPricingHistoryAsync_ReturnsAllVersionsOrderedByDate()
    {
        // Arrange
        var version1 = new CourseTypePricingVersion
        {
            Id = Guid.NewGuid(),
            CourseTypeId = _testCourseTypeId,
            PriceAdult = 45.00m,
            PriceChild = 35.00m,
            ValidFrom = DateOnly.FromDateTime(DateTime.Today.AddMonths(-6)),
            ValidUntil = DateOnly.FromDateTime(DateTime.Today.AddDays(-1)),
            IsCurrent = false
        };
        var version2 = new CourseTypePricingVersion
        {
            Id = Guid.NewGuid(),
            CourseTypeId = _testCourseTypeId,
            PriceAdult = 50.00m,
            PriceChild = 40.00m,
            ValidFrom = DateOnly.FromDateTime(DateTime.Today),
            IsCurrent = true
        };
        _context.CourseTypePricingVersions.AddRange(version1, version2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetPricingHistoryAsync(_testCourseTypeId);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(50.00m, result[0].PriceAdult); // Most recent first
        Assert.Equal(45.00m, result[1].PriceAdult);
    }

    [Fact]
    public async Task IsCurrentPricingInvoicedAsync_WithNoInvoices_ReturnsFalse()
    {
        // Arrange
        var pricingVersion = new CourseTypePricingVersion
        {
            Id = Guid.NewGuid(),
            CourseTypeId = _testCourseTypeId,
            PriceAdult = 50.00m,
            PriceChild = 40.00m,
            ValidFrom = DateOnly.FromDateTime(DateTime.Today),
            IsCurrent = true
        };
        _context.CourseTypePricingVersions.Add(pricingVersion);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.IsCurrentPricingInvoicedAsync(_testCourseTypeId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task IsCurrentPricingInvoicedAsync_WithInvoices_ReturnsTrue()
    {
        // Arrange
        var pricingVersion = new CourseTypePricingVersion
        {
            Id = Guid.NewGuid(),
            CourseTypeId = _testCourseTypeId,
            PriceAdult = 50.00m,
            PriceChild = 40.00m,
            ValidFrom = DateOnly.FromDateTime(DateTime.Today),
            IsCurrent = true
        };
        _context.CourseTypePricingVersions.Add(pricingVersion);

        // Add a student for the invoice
        var student = new Student
        {
            Id = Guid.NewGuid(),
            FirstName = "Test",
            LastName = "Student",
            Email = "test@example.com",
            Status = StudentStatus.Active
        };
        _context.Students.Add(student);

        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = "INV-001",
            StudentId = student.Id,
            Status = InvoiceStatus.Draft,
            IssueDate = DateOnly.FromDateTime(DateTime.Today),
            DueDate = DateOnly.FromDateTime(DateTime.Today.AddDays(30)),
            Total = 50.00m
        };
        _context.Invoices.Add(invoice);

        var invoiceLine = new InvoiceLine
        {
            InvoiceId = invoice.Id,
            Description = "Piano Lesson",
            Quantity = 1,
            UnitPrice = 50.00m,
            LineTotal = 50.00m,
            PricingVersionId = pricingVersion.Id
        };
        _context.InvoiceLines.Add(invoiceLine);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.IsCurrentPricingInvoicedAsync(_testCourseTypeId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task UpdateCurrentPricingAsync_WhenNotInvoiced_UpdatesPricing()
    {
        // Arrange
        var pricingVersion = new CourseTypePricingVersion
        {
            Id = Guid.NewGuid(),
            CourseTypeId = _testCourseTypeId,
            PriceAdult = 50.00m,
            PriceChild = 40.00m,
            ValidFrom = DateOnly.FromDateTime(DateTime.Today),
            IsCurrent = true
        };
        _context.CourseTypePricingVersions.Add(pricingVersion);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.UpdateCurrentPricingAsync(_testCourseTypeId, 55.00m, 45.00m);

        // Assert
        Assert.Equal(55.00m, result.PriceAdult);
        Assert.Equal(45.00m, result.PriceChild);

        // Verify it was saved
        var saved = await _context.CourseTypePricingVersions.FindAsync(pricingVersion.Id);
        Assert.Equal(55.00m, saved!.PriceAdult);
    }

    [Fact]
    public async Task UpdateCurrentPricingAsync_WhenInvoiced_ThrowsException()
    {
        // Arrange
        var pricingVersion = new CourseTypePricingVersion
        {
            Id = Guid.NewGuid(),
            CourseTypeId = _testCourseTypeId,
            PriceAdult = 50.00m,
            PriceChild = 40.00m,
            ValidFrom = DateOnly.FromDateTime(DateTime.Today),
            IsCurrent = true
        };
        _context.CourseTypePricingVersions.Add(pricingVersion);

        var student = new Student
        {
            Id = Guid.NewGuid(),
            FirstName = "Test",
            LastName = "Student",
            Email = "test@example.com",
            Status = StudentStatus.Active
        };
        _context.Students.Add(student);

        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = "INV-001",
            StudentId = student.Id,
            Status = InvoiceStatus.Draft,
            IssueDate = DateOnly.FromDateTime(DateTime.Today),
            DueDate = DateOnly.FromDateTime(DateTime.Today.AddDays(30)),
            Total = 50.00m
        };
        _context.Invoices.Add(invoice);

        var invoiceLine = new InvoiceLine
        {
            InvoiceId = invoice.Id,
            Description = "Piano Lesson",
            Quantity = 1,
            UnitPrice = 50.00m,
            LineTotal = 50.00m,
            PricingVersionId = pricingVersion.Id
        };
        _context.InvoiceLines.Add(invoiceLine);
        await _context.SaveChangesAsync();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateCurrentPricingAsync(_testCourseTypeId, 55.00m, 45.00m));

        Assert.Contains("invoice", exception.Message.ToLower());
    }

    [Fact]
    public async Task UpdateCurrentPricingAsync_WithNoPricing_ThrowsException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateCurrentPricingAsync(_testCourseTypeId, 55.00m, 45.00m));

        Assert.Contains("no current pricing", exception.Message.ToLower());
    }

    [Fact]
    public async Task CreateNewPricingVersionAsync_ClosesCurrentVersion()
    {
        // Arrange
        var currentVersion = new CourseTypePricingVersion
        {
            Id = Guid.NewGuid(),
            CourseTypeId = _testCourseTypeId,
            PriceAdult = 50.00m,
            PriceChild = 40.00m,
            ValidFrom = DateOnly.FromDateTime(DateTime.Today.AddMonths(-1)),
            IsCurrent = true
        };
        _context.CourseTypePricingVersions.Add(currentVersion);
        await _context.SaveChangesAsync();

        var newValidFrom = DateOnly.FromDateTime(DateTime.Today.AddDays(7));

        // Act
        var newVersion = await _service.CreateNewPricingVersionAsync(
            _testCourseTypeId, 55.00m, 45.00m, newValidFrom);

        // Assert
        // New version is current
        Assert.True(newVersion.IsCurrent);
        Assert.Equal(55.00m, newVersion.PriceAdult);
        Assert.Null(newVersion.ValidUntil);

        // Old version is closed
        var oldVersion = await _context.CourseTypePricingVersions.FindAsync(currentVersion.Id);
        Assert.False(oldVersion!.IsCurrent);
        Assert.Equal(newValidFrom.AddDays(-1), oldVersion.ValidUntil);
    }

    [Fact]
    public async Task CreateNewPricingVersionAsync_WithPastDate_ThrowsException()
    {
        // Arrange
        var pastDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-1));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CreateNewPricingVersionAsync(_testCourseTypeId, 55.00m, 45.00m, pastDate));

        Assert.Contains("past", exception.Message.ToLower());
    }

    [Fact]
    public async Task CreateNewPricingVersionAsync_WithTodayDate_Succeeds()
    {
        // Arrange
        var today = DateOnly.FromDateTime(DateTime.Today);

        // Act
        var result = await _service.CreateNewPricingVersionAsync(
            _testCourseTypeId, 55.00m, 45.00m, today);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(today, result.ValidFrom);
        Assert.True(result.IsCurrent);
    }

    [Fact]
    public async Task GetPricingForDateAsync_ReturnsCorrectVersion()
    {
        // Arrange
        var oldVersion = new CourseTypePricingVersion
        {
            Id = Guid.NewGuid(),
            CourseTypeId = _testCourseTypeId,
            PriceAdult = 45.00m,
            PriceChild = 35.00m,
            ValidFrom = DateOnly.FromDateTime(DateTime.Today.AddMonths(-6)),
            ValidUntil = DateOnly.FromDateTime(DateTime.Today.AddDays(-1)),
            IsCurrent = false
        };
        var currentVersion = new CourseTypePricingVersion
        {
            Id = Guid.NewGuid(),
            CourseTypeId = _testCourseTypeId,
            PriceAdult = 50.00m,
            PriceChild = 40.00m,
            ValidFrom = DateOnly.FromDateTime(DateTime.Today),
            IsCurrent = true
        };
        _context.CourseTypePricingVersions.AddRange(oldVersion, currentVersion);
        await _context.SaveChangesAsync();

        // Act - Get pricing for a date in the past
        var pastResult = await _service.GetPricingForDateAsync(
            _testCourseTypeId, DateOnly.FromDateTime(DateTime.Today.AddMonths(-3)));

        // Act - Get pricing for today
        var currentResult = await _service.GetPricingForDateAsync(
            _testCourseTypeId, DateOnly.FromDateTime(DateTime.Today));

        // Assert
        Assert.NotNull(pastResult);
        Assert.Equal(45.00m, pastResult.PriceAdult);

        Assert.NotNull(currentResult);
        Assert.Equal(50.00m, currentResult.PriceAdult);
    }

    [Fact]
    public async Task CreateInitialPricingVersionAsync_CreatesFirstVersion()
    {
        // Act
        var result = await _service.CreateInitialPricingVersionAsync(
            _testCourseTypeId, 50.00m, 40.00m);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(50.00m, result.PriceAdult);
        Assert.Equal(40.00m, result.PriceChild);
        Assert.True(result.IsCurrent);
        Assert.Equal(DateOnly.FromDateTime(DateTime.Today), result.ValidFrom);
        Assert.Null(result.ValidUntil);
    }

    [Fact]
    public async Task CreateInitialPricingVersionAsync_WhenVersionExists_ThrowsException()
    {
        // Arrange
        var existingVersion = new CourseTypePricingVersion
        {
            Id = Guid.NewGuid(),
            CourseTypeId = _testCourseTypeId,
            PriceAdult = 50.00m,
            PriceChild = 40.00m,
            ValidFrom = DateOnly.FromDateTime(DateTime.Today),
            IsCurrent = true
        };
        _context.CourseTypePricingVersions.Add(existingVersion);
        await _context.SaveChangesAsync();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateInitialPricingVersionAsync(_testCourseTypeId, 55.00m, 45.00m));

        Assert.Contains("already exists", exception.Message.ToLower());
    }
}
