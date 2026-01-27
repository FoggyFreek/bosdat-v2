using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using BosDAT.Core.Entities;
using BosDAT.Core.Interfaces;
using BosDAT.Infrastructure.Data;
using BosDAT.Infrastructure.Services;

namespace BosDAT.API.Tests.Services;

public class EnrollmentPricingServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<ICourseTypePricingService> _pricingServiceMock;
    private readonly EnrollmentPricingService _service;
    private readonly Guid _testStudentId = Guid.NewGuid();
    private readonly Guid _testCourseId = Guid.NewGuid();
    private readonly Guid _testCourseTypeId = Guid.NewGuid();
    private readonly Guid _testTeacherId = Guid.NewGuid();
    private readonly Guid _testEnrollmentId = Guid.NewGuid();

    public EnrollmentPricingServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options, null!);
        _pricingServiceMock = new Mock<ICourseTypePricingService>();
        _service = new EnrollmentPricingService(_context, _pricingServiceMock.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    private void SeedTestData(DateOnly? studentDateOfBirth = null, decimal discountPercent = 0)
    {
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

        var teacher = new Teacher
        {
            Id = _testTeacherId,
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            IsActive = true
        };
        _context.Teachers.Add(teacher);

        var student = new Student
        {
            Id = _testStudentId,
            FirstName = "Jane",
            LastName = "Student",
            Email = "jane.student@example.com",
            Status = StudentStatus.Active,
            DateOfBirth = studentDateOfBirth
        };
        _context.Students.Add(student);

        var course = new Course
        {
            Id = _testCourseId,
            TeacherId = _testTeacherId,
            CourseTypeId = _testCourseTypeId,
            DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(10, 30),
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            Status = CourseStatus.Active
        };
        _context.Courses.Add(course);

        var enrollment = new Enrollment
        {
            Id = _testEnrollmentId,
            StudentId = _testStudentId,
            CourseId = _testCourseId,
            DiscountPercent = discountPercent,
            Status = EnrollmentStatus.Active
        };
        _context.Enrollments.Add(enrollment);

        _context.SaveChanges();
    }

    [Fact]
    public async Task GetPricing_ChildStudent_ReturnsChildPrice()
    {
        // Arrange - student is 10 years old
        var childDateOfBirth = DateOnly.FromDateTime(DateTime.Today.AddYears(-10));
        SeedTestData(studentDateOfBirth: childDateOfBirth);

        var pricing = new CourseTypePricingVersion
        {
            Id = Guid.NewGuid(),
            CourseTypeId = _testCourseTypeId,
            PriceAdult = 50.00m,
            PriceChild = 35.00m,
            ValidFrom = DateOnly.FromDateTime(DateTime.Today),
            IsCurrent = true
        };

        _pricingServiceMock
            .Setup(x => x.GetCurrentPricingAsync(_testCourseTypeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pricing);

        // Act
        var result = await _service.GetEnrollmentPricingAsync(_testStudentId, _testCourseId);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsChildPricing);
        Assert.Equal(35.00m, result.ApplicableBasePrice);
        Assert.Equal(35.00m, result.PricePerLesson);
    }

    [Fact]
    public async Task GetPricing_AdultStudent_ReturnsAdultPrice()
    {
        // Arrange - student is 25 years old
        var adultDateOfBirth = DateOnly.FromDateTime(DateTime.Today.AddYears(-25));
        SeedTestData(studentDateOfBirth: adultDateOfBirth);

        var pricing = new CourseTypePricingVersion
        {
            Id = Guid.NewGuid(),
            CourseTypeId = _testCourseTypeId,
            PriceAdult = 50.00m,
            PriceChild = 35.00m,
            ValidFrom = DateOnly.FromDateTime(DateTime.Today),
            IsCurrent = true
        };

        _pricingServiceMock
            .Setup(x => x.GetCurrentPricingAsync(_testCourseTypeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pricing);

        // Act
        var result = await _service.GetEnrollmentPricingAsync(_testStudentId, _testCourseId);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsChildPricing);
        Assert.Equal(50.00m, result.ApplicableBasePrice);
        Assert.Equal(50.00m, result.PricePerLesson);
    }

    [Fact]
    public async Task GetPricing_StudentWithNoDateOfBirth_ReturnsAdultPrice()
    {
        // Arrange - no date of birth defaults to adult pricing
        SeedTestData(studentDateOfBirth: null);

        var pricing = new CourseTypePricingVersion
        {
            Id = Guid.NewGuid(),
            CourseTypeId = _testCourseTypeId,
            PriceAdult = 50.00m,
            PriceChild = 35.00m,
            ValidFrom = DateOnly.FromDateTime(DateTime.Today),
            IsCurrent = true
        };

        _pricingServiceMock
            .Setup(x => x.GetCurrentPricingAsync(_testCourseTypeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pricing);

        // Act
        var result = await _service.GetEnrollmentPricingAsync(_testStudentId, _testCourseId);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsChildPricing);
        Assert.Equal(50.00m, result.ApplicableBasePrice);
    }

    [Fact]
    public async Task GetPricing_WithDiscount_AppliesCorrectly()
    {
        // Arrange - 20% discount
        var adultDateOfBirth = DateOnly.FromDateTime(DateTime.Today.AddYears(-25));
        SeedTestData(studentDateOfBirth: adultDateOfBirth, discountPercent: 20m);

        var pricing = new CourseTypePricingVersion
        {
            Id = Guid.NewGuid(),
            CourseTypeId = _testCourseTypeId,
            PriceAdult = 50.00m,
            PriceChild = 35.00m,
            ValidFrom = DateOnly.FromDateTime(DateTime.Today),
            IsCurrent = true
        };

        _pricingServiceMock
            .Setup(x => x.GetCurrentPricingAsync(_testCourseTypeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pricing);

        // Act
        var result = await _service.GetEnrollmentPricingAsync(_testStudentId, _testCourseId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(50.00m, result.ApplicableBasePrice);
        Assert.Equal(20m, result.DiscountPercent);
        Assert.Equal(10.00m, result.DiscountAmount); // 20% of 50
        Assert.Equal(40.00m, result.PricePerLesson); // 50 - 10
    }

    [Fact]
    public async Task GetPricing_EnrollmentNotFound_ReturnsNull()
    {
        // Arrange - no data seeded
        var nonExistentStudentId = Guid.NewGuid();
        var nonExistentCourseId = Guid.NewGuid();

        // Act
        var result = await _service.GetEnrollmentPricingAsync(nonExistentStudentId, nonExistentCourseId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetPricing_NoPricing_ReturnsNull()
    {
        // Arrange
        var adultDateOfBirth = DateOnly.FromDateTime(DateTime.Today.AddYears(-25));
        SeedTestData(studentDateOfBirth: adultDateOfBirth);

        _pricingServiceMock
            .Setup(x => x.GetCurrentPricingAsync(_testCourseTypeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CourseTypePricingVersion?)null);

        // Act
        var result = await _service.GetEnrollmentPricingAsync(_testStudentId, _testCourseId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetPricing_StudentExactly18_ReturnsAdultPrice()
    {
        // Arrange - student turns 18 today
        var exactlyEighteenDateOfBirth = DateOnly.FromDateTime(DateTime.Today.AddYears(-18));
        SeedTestData(studentDateOfBirth: exactlyEighteenDateOfBirth);

        var pricing = new CourseTypePricingVersion
        {
            Id = Guid.NewGuid(),
            CourseTypeId = _testCourseTypeId,
            PriceAdult = 50.00m,
            PriceChild = 35.00m,
            ValidFrom = DateOnly.FromDateTime(DateTime.Today),
            IsCurrent = true
        };

        _pricingServiceMock
            .Setup(x => x.GetCurrentPricingAsync(_testCourseTypeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pricing);

        // Act
        var result = await _service.GetEnrollmentPricingAsync(_testStudentId, _testCourseId);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsChildPricing); // 18 is considered adult
    }

    [Fact]
    public async Task GetPricing_StudentAlmost18_ReturnsChildPrice()
    {
        // Arrange - student turns 18 tomorrow (still 17)
        var almostEighteenDateOfBirth = DateOnly.FromDateTime(DateTime.Today.AddYears(-18).AddDays(1));
        SeedTestData(studentDateOfBirth: almostEighteenDateOfBirth);

        var pricing = new CourseTypePricingVersion
        {
            Id = Guid.NewGuid(),
            CourseTypeId = _testCourseTypeId,
            PriceAdult = 50.00m,
            PriceChild = 35.00m,
            ValidFrom = DateOnly.FromDateTime(DateTime.Today),
            IsCurrent = true
        };

        _pricingServiceMock
            .Setup(x => x.GetCurrentPricingAsync(_testCourseTypeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pricing);

        // Act
        var result = await _service.GetEnrollmentPricingAsync(_testStudentId, _testCourseId);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsChildPricing); // Still 17
    }

    [Fact]
    public async Task GetPricing_ReturnsCorrectEnrollmentAndCourseInfo()
    {
        // Arrange
        var adultDateOfBirth = DateOnly.FromDateTime(DateTime.Today.AddYears(-25));
        SeedTestData(studentDateOfBirth: adultDateOfBirth);

        var pricing = new CourseTypePricingVersion
        {
            Id = Guid.NewGuid(),
            CourseTypeId = _testCourseTypeId,
            PriceAdult = 50.00m,
            PriceChild = 35.00m,
            ValidFrom = DateOnly.FromDateTime(DateTime.Today),
            IsCurrent = true
        };

        _pricingServiceMock
            .Setup(x => x.GetCurrentPricingAsync(_testCourseTypeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pricing);

        // Act
        var result = await _service.GetEnrollmentPricingAsync(_testStudentId, _testCourseId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_testEnrollmentId, result.EnrollmentId);
        Assert.Equal(_testCourseId, result.CourseId);
        Assert.Contains("Piano Lesson", result.CourseName);
        Assert.Contains("John Doe", result.CourseName);
    }
}
