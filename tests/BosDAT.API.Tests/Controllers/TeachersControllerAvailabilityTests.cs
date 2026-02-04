using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using BosDAT.API.Controllers;
using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Interfaces;
using BosDAT.API.Tests.Helpers;
using BosDAT.Infrastructure.Data;

namespace BosDAT.API.Tests.Controllers;

public class TeachersControllerAvailabilityTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly ApplicationDbContext _context;
    private readonly TeachersController _controller;

    public TeachersControllerAvailabilityTests()
    {
        _mockUnitOfWork = MockHelpers.CreateMockUnitOfWork();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);

        _controller = new TeachersController(_mockUnitOfWork.Object, _context);
    }

    private static Teacher CreateTeacher(
        string firstName = "John",
        string lastName = "Doe",
        string email = "john.doe@example.com",
        bool isActive = true)
    {
        return new Teacher
        {
            Id = Guid.NewGuid(),
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            Phone = "123-456-7890",
            HourlyRate = 50m,
            IsActive = isActive,
            Role = TeacherRole.Teacher,
            TeacherInstruments = new List<TeacherInstrument>(),
            TeacherCourseTypes = new List<TeacherCourseType>(),
            Courses = new List<Course>(),
            Availability = new List<TeacherAvailability>()
        };
    }

    private static TeacherAvailability CreateAvailability(
        Guid teacherId,
        DayOfWeek dayOfWeek,
        TimeOnly fromTime,
        TimeOnly untilTime)
    {
        return new TeacherAvailability
        {
            Id = Guid.NewGuid(),
            TeacherId = teacherId,
            DayOfWeek = dayOfWeek,
            FromTime = fromTime,
            UntilTime = untilTime
        };
    }

    #region GetAvailability Tests

    [Fact]
    public async Task GetAvailability_WithValidTeacher_ReturnsAvailability()
    {
        // Arrange
        var teacher = CreateTeacher();
        teacher.Availability = new List<TeacherAvailability>
        {
            CreateAvailability(teacher.Id, DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(17, 0)),
            CreateAvailability(teacher.Id, DayOfWeek.Tuesday, new TimeOnly(10, 0), new TimeOnly(18, 0))
        };

        var teachers = new List<Teacher> { teacher };
        var mockTeacherRepo = MockHelpers.CreateMockTeacherRepository(teachers);
        _mockUnitOfWork.Setup(u => u.Teachers).Returns(mockTeacherRepo.Object);

        // Act
        var result = await _controller.GetAvailability(teacher.Id, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var availability = Assert.IsAssignableFrom<IEnumerable<TeacherAvailabilityDto>>(okResult.Value);
        Assert.Equal(2, availability.Count());
    }

    [Fact]
    public async Task GetAvailability_WithNoAvailability_ReturnsEmptyList()
    {
        // Arrange
        var teacher = CreateTeacher();
        teacher.Availability = new List<TeacherAvailability>();

        var teachers = new List<Teacher> { teacher };
        var mockTeacherRepo = MockHelpers.CreateMockTeacherRepository(teachers);
        _mockUnitOfWork.Setup(u => u.Teachers).Returns(mockTeacherRepo.Object);

        // Act
        var result = await _controller.GetAvailability(teacher.Id, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var availability = Assert.IsAssignableFrom<IEnumerable<TeacherAvailabilityDto>>(okResult.Value);
        Assert.Empty(availability);
    }

    [Fact]
    public async Task GetAvailability_WithInvalidTeacherId_ReturnsNotFound()
    {
        // Arrange
        var teachers = new List<Teacher>();
        var mockTeacherRepo = MockHelpers.CreateMockTeacherRepository(teachers);
        _mockUnitOfWork.Setup(u => u.Teachers).Returns(mockTeacherRepo.Object);

        // Act
        var result = await _controller.GetAvailability(Guid.NewGuid(), CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetAvailability_ReturnsCorrectTimeValues()
    {
        // Arrange
        var teacher = CreateTeacher();
        var fromTime = new TimeOnly(9, 30);
        var untilTime = new TimeOnly(17, 45);
        teacher.Availability = new List<TeacherAvailability>
        {
            CreateAvailability(teacher.Id, DayOfWeek.Wednesday, fromTime, untilTime)
        };

        var teachers = new List<Teacher> { teacher };
        var mockTeacherRepo = MockHelpers.CreateMockTeacherRepository(teachers);
        _mockUnitOfWork.Setup(u => u.Teachers).Returns(mockTeacherRepo.Object);

        // Act
        var result = await _controller.GetAvailability(teacher.Id, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var availability = Assert.IsAssignableFrom<IEnumerable<TeacherAvailabilityDto>>(okResult.Value);
        var first = availability.First();
        Assert.Equal(DayOfWeek.Wednesday, first.DayOfWeek);
        Assert.Equal(fromTime, first.FromTime);
        Assert.Equal(untilTime, first.UntilTime);
    }

    #endregion

    #region UpdateAvailability Tests

    [Fact]
    public async Task UpdateAvailability_WithValidData_ReturnsUpdatedAvailability()
    {
        // Arrange
        var teacher = CreateTeacher();
        teacher.Availability = new List<TeacherAvailability>();

        var teachers = new List<Teacher> { teacher };
        var mockTeacherRepo = MockHelpers.CreateMockTeacherRepository(teachers);
        _mockUnitOfWork.Setup(u => u.Teachers).Returns(mockTeacherRepo.Object);

        var dtos = new List<UpdateTeacherAvailabilityDto>
        {
            new() { DayOfWeek = DayOfWeek.Monday, FromTime = new TimeOnly(9, 0), UntilTime = new TimeOnly(17, 0) },
            new() { DayOfWeek = DayOfWeek.Tuesday, FromTime = new TimeOnly(10, 0), UntilTime = new TimeOnly(18, 0) }
        };

        // Act
        var result = await _controller.UpdateAvailability(teacher.Id, dtos, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var availability = Assert.IsAssignableFrom<IEnumerable<TeacherAvailabilityDto>>(okResult.Value);
        Assert.Equal(2, availability.Count());
    }

    [Fact]
    public async Task UpdateAvailability_WithInvalidTeacherId_ReturnsNotFound()
    {
        // Arrange
        var teachers = new List<Teacher>();
        var mockTeacherRepo = MockHelpers.CreateMockTeacherRepository(teachers);
        _mockUnitOfWork.Setup(u => u.Teachers).Returns(mockTeacherRepo.Object);

        var dtos = new List<UpdateTeacherAvailabilityDto>
        {
            new() { DayOfWeek = DayOfWeek.Monday, FromTime = new TimeOnly(9, 0), UntilTime = new TimeOnly(17, 0) }
        };

        // Act
        var result = await _controller.UpdateAvailability(Guid.NewGuid(), dtos, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task UpdateAvailability_WithMoreThan7Entries_ReturnsBadRequest()
    {
        // Arrange
        var teacher = CreateTeacher();
        teacher.Availability = new List<TeacherAvailability>();

        var teachers = new List<Teacher> { teacher };
        var mockTeacherRepo = MockHelpers.CreateMockTeacherRepository(teachers);
        _mockUnitOfWork.Setup(u => u.Teachers).Returns(mockTeacherRepo.Object);

        var dtos = new List<UpdateTeacherAvailabilityDto>
        {
            new() { DayOfWeek = DayOfWeek.Sunday, FromTime = new TimeOnly(9, 0), UntilTime = new TimeOnly(17, 0) },
            new() { DayOfWeek = DayOfWeek.Monday, FromTime = new TimeOnly(9, 0), UntilTime = new TimeOnly(17, 0) },
            new() { DayOfWeek = DayOfWeek.Tuesday, FromTime = new TimeOnly(9, 0), UntilTime = new TimeOnly(17, 0) },
            new() { DayOfWeek = DayOfWeek.Wednesday, FromTime = new TimeOnly(9, 0), UntilTime = new TimeOnly(17, 0) },
            new() { DayOfWeek = DayOfWeek.Thursday, FromTime = new TimeOnly(9, 0), UntilTime = new TimeOnly(17, 0) },
            new() { DayOfWeek = DayOfWeek.Friday, FromTime = new TimeOnly(9, 0), UntilTime = new TimeOnly(17, 0) },
            new() { DayOfWeek = DayOfWeek.Saturday, FromTime = new TimeOnly(9, 0), UntilTime = new TimeOnly(17, 0) },
            new() { DayOfWeek = DayOfWeek.Sunday, FromTime = new TimeOnly(10, 0), UntilTime = new TimeOnly(18, 0) } // 8th entry
        };

        // Act
        var result = await _controller.UpdateAvailability(teacher.Id, dtos, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task UpdateAvailability_WithDuplicateDays_ReturnsBadRequest()
    {
        // Arrange
        var teacher = CreateTeacher();
        teacher.Availability = new List<TeacherAvailability>();

        var teachers = new List<Teacher> { teacher };
        var mockTeacherRepo = MockHelpers.CreateMockTeacherRepository(teachers);
        _mockUnitOfWork.Setup(u => u.Teachers).Returns(mockTeacherRepo.Object);

        var dtos = new List<UpdateTeacherAvailabilityDto>
        {
            new() { DayOfWeek = DayOfWeek.Monday, FromTime = new TimeOnly(9, 0), UntilTime = new TimeOnly(17, 0) },
            new() { DayOfWeek = DayOfWeek.Monday, FromTime = new TimeOnly(10, 0), UntilTime = new TimeOnly(18, 0) } // Duplicate
        };

        // Act
        var result = await _controller.UpdateAvailability(teacher.Id, dtos, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task UpdateAvailability_WithTimeRangeLessThan1Hour_ReturnsBadRequest()
    {
        // Arrange
        var teacher = CreateTeacher();
        teacher.Availability = new List<TeacherAvailability>();

        var teachers = new List<Teacher> { teacher };
        var mockTeacherRepo = MockHelpers.CreateMockTeacherRepository(teachers);
        _mockUnitOfWork.Setup(u => u.Teachers).Returns(mockTeacherRepo.Object);

        var dtos = new List<UpdateTeacherAvailabilityDto>
        {
            new() { DayOfWeek = DayOfWeek.Monday, FromTime = new TimeOnly(9, 0), UntilTime = new TimeOnly(9, 30) } // Only 30 minutes
        };

        // Act
        var result = await _controller.UpdateAvailability(teacher.Id, dtos, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task UpdateAvailability_WithZeroZeroForUnavailable_ReturnsSuccess()
    {
        // Arrange
        var teacher = CreateTeacher();
        teacher.Availability = new List<TeacherAvailability>();

        var teachers = new List<Teacher> { teacher };
        var mockTeacherRepo = MockHelpers.CreateMockTeacherRepository(teachers);
        _mockUnitOfWork.Setup(u => u.Teachers).Returns(mockTeacherRepo.Object);

        var dtos = new List<UpdateTeacherAvailabilityDto>
        {
            new() { DayOfWeek = DayOfWeek.Monday, FromTime = TimeOnly.MinValue, UntilTime = TimeOnly.MinValue } // 00:00-00:00 for unavailable
        };

        // Act
        var result = await _controller.UpdateAvailability(teacher.Id, dtos, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var availability = Assert.IsAssignableFrom<IEnumerable<TeacherAvailabilityDto>>(okResult.Value);
        var first = availability.First();
        Assert.Equal(TimeOnly.MinValue, first.FromTime);
        Assert.Equal(TimeOnly.MinValue, first.UntilTime);
    }

    [Fact]
    public async Task UpdateAvailability_WithExactly1HourRange_ReturnsSuccess()
    {
        // Arrange
        var teacher = CreateTeacher();
        teacher.Availability = new List<TeacherAvailability>();

        var teachers = new List<Teacher> { teacher };
        var mockTeacherRepo = MockHelpers.CreateMockTeacherRepository(teachers);
        _mockUnitOfWork.Setup(u => u.Teachers).Returns(mockTeacherRepo.Object);

        var dtos = new List<UpdateTeacherAvailabilityDto>
        {
            new() { DayOfWeek = DayOfWeek.Monday, FromTime = new TimeOnly(9, 0), UntilTime = new TimeOnly(10, 0) } // Exactly 1 hour
        };

        // Act
        var result = await _controller.UpdateAvailability(teacher.Id, dtos, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var availability = Assert.IsAssignableFrom<IEnumerable<TeacherAvailabilityDto>>(okResult.Value);
        Assert.Single(availability);
    }

    [Fact]
    public async Task UpdateAvailability_ReplacesExistingAvailability()
    {
        // Arrange
        var teacher = CreateTeacher();
        var existingAvailability = CreateAvailability(teacher.Id, DayOfWeek.Monday, new TimeOnly(8, 0), new TimeOnly(16, 0));
        teacher.Availability = new List<TeacherAvailability> { existingAvailability };
        _context.TeacherAvailabilities.Add(existingAvailability);
        await _context.SaveChangesAsync();

        var teachers = new List<Teacher> { teacher };
        var mockTeacherRepo = MockHelpers.CreateMockTeacherRepository(teachers);
        _mockUnitOfWork.Setup(u => u.Teachers).Returns(mockTeacherRepo.Object);

        var dtos = new List<UpdateTeacherAvailabilityDto>
        {
            new() { DayOfWeek = DayOfWeek.Tuesday, FromTime = new TimeOnly(10, 0), UntilTime = new TimeOnly(18, 0) }
        };

        // Act
        var result = await _controller.UpdateAvailability(teacher.Id, dtos, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var availability = Assert.IsAssignableFrom<IEnumerable<TeacherAvailabilityDto>>(okResult.Value);
        Assert.Single(availability);
        Assert.Equal(DayOfWeek.Tuesday, availability.First().DayOfWeek);
    }

    [Fact]
    public async Task UpdateAvailability_With7ValidDays_ReturnsSuccess()
    {
        // Arrange
        var teacher = CreateTeacher();
        teacher.Availability = new List<TeacherAvailability>();

        var teachers = new List<Teacher> { teacher };
        var mockTeacherRepo = MockHelpers.CreateMockTeacherRepository(teachers);
        _mockUnitOfWork.Setup(u => u.Teachers).Returns(mockTeacherRepo.Object);

        var dtos = new List<UpdateTeacherAvailabilityDto>
        {
            new() { DayOfWeek = DayOfWeek.Sunday, FromTime = new TimeOnly(9, 0), UntilTime = new TimeOnly(17, 0) },
            new() { DayOfWeek = DayOfWeek.Monday, FromTime = new TimeOnly(9, 0), UntilTime = new TimeOnly(17, 0) },
            new() { DayOfWeek = DayOfWeek.Tuesday, FromTime = new TimeOnly(9, 0), UntilTime = new TimeOnly(17, 0) },
            new() { DayOfWeek = DayOfWeek.Wednesday, FromTime = new TimeOnly(9, 0), UntilTime = new TimeOnly(17, 0) },
            new() { DayOfWeek = DayOfWeek.Thursday, FromTime = new TimeOnly(9, 0), UntilTime = new TimeOnly(17, 0) },
            new() { DayOfWeek = DayOfWeek.Friday, FromTime = new TimeOnly(9, 0), UntilTime = new TimeOnly(17, 0) },
            new() { DayOfWeek = DayOfWeek.Saturday, FromTime = new TimeOnly(9, 0), UntilTime = new TimeOnly(17, 0) }
        };

        // Act
        var result = await _controller.UpdateAvailability(teacher.Id, dtos, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var availability = Assert.IsAssignableFrom<IEnumerable<TeacherAvailabilityDto>>(okResult.Value);
        Assert.Equal(7, availability.Count());
    }

    #endregion
}
