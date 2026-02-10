using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using BosDAT.API.Controllers;
using BosDAT.Core.DTOs;
using BosDAT.Core.Interfaces;

namespace BosDAT.API.Tests.Controllers;

public class TeachersControllerAvailabilityTests
{
    private readonly Mock<ITeacherService> _mockTeacherService;
    private readonly TeachersController _controller;

    public TeachersControllerAvailabilityTests()
    {
        _mockTeacherService = new Mock<ITeacherService>();
        _controller = new TeachersController(_mockTeacherService.Object);
    }

    #region GetAvailability Tests

    [Fact]
    public async Task GetAvailability_WithValidTeacher_ReturnsAvailability()
    {
        // Arrange
        var teacherId = Guid.NewGuid();
        var availability = new List<TeacherAvailabilityDto>
        {
            new() { Id = Guid.NewGuid(), DayOfWeek = DayOfWeek.Monday, FromTime = new TimeOnly(9, 0), UntilTime = new TimeOnly(17, 0) },
            new() { Id = Guid.NewGuid(), DayOfWeek = DayOfWeek.Tuesday, FromTime = new TimeOnly(10, 0), UntilTime = new TimeOnly(18, 0) }
        };

        _mockTeacherService.Setup(s => s.GetAvailabilityAsync(teacherId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(availability);

        // Act
        var result = await _controller.GetAvailability(teacherId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedAvailability = Assert.IsAssignableFrom<IEnumerable<TeacherAvailabilityDto>>(okResult.Value);
        Assert.Equal(2, returnedAvailability.Count());
    }

    [Fact]
    public async Task GetAvailability_WithNoAvailability_ReturnsEmptyList()
    {
        // Arrange
        var teacherId = Guid.NewGuid();
        _mockTeacherService.Setup(s => s.GetAvailabilityAsync(teacherId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TeacherAvailabilityDto>());

        // Act
        var result = await _controller.GetAvailability(teacherId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var availability = Assert.IsAssignableFrom<IEnumerable<TeacherAvailabilityDto>>(okResult.Value);
        Assert.Empty(availability);
    }

    [Fact]
    public async Task GetAvailability_WithInvalidTeacherId_ReturnsNotFound()
    {
        // Arrange
        var invalidId = Guid.NewGuid();
        _mockTeacherService.Setup(s => s.GetAvailabilityAsync(invalidId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<TeacherAvailabilityDto>?)null);

        // Act
        var result = await _controller.GetAvailability(invalidId, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetAvailability_ReturnsCorrectTimeValues()
    {
        // Arrange
        var teacherId = Guid.NewGuid();
        var fromTime = new TimeOnly(9, 30);
        var untilTime = new TimeOnly(17, 45);
        var availability = new List<TeacherAvailabilityDto>
        {
            new() { Id = Guid.NewGuid(), DayOfWeek = DayOfWeek.Wednesday, FromTime = fromTime, UntilTime = untilTime }
        };

        _mockTeacherService.Setup(s => s.GetAvailabilityAsync(teacherId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(availability);

        // Act
        var result = await _controller.GetAvailability(teacherId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedAvailability = Assert.IsAssignableFrom<IEnumerable<TeacherAvailabilityDto>>(okResult.Value);
        var first = returnedAvailability.First();
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
        var teacherId = Guid.NewGuid();
        var dtos = new List<UpdateTeacherAvailabilityDto>
        {
            new() { DayOfWeek = DayOfWeek.Monday, FromTime = new TimeOnly(9, 0), UntilTime = new TimeOnly(17, 0) },
            new() { DayOfWeek = DayOfWeek.Tuesday, FromTime = new TimeOnly(10, 0), UntilTime = new TimeOnly(18, 0) }
        };

        var updatedAvailability = new List<TeacherAvailabilityDto>
        {
            new() { Id = Guid.NewGuid(), DayOfWeek = DayOfWeek.Monday, FromTime = new TimeOnly(9, 0), UntilTime = new TimeOnly(17, 0) },
            new() { Id = Guid.NewGuid(), DayOfWeek = DayOfWeek.Tuesday, FromTime = new TimeOnly(10, 0), UntilTime = new TimeOnly(18, 0) }
        };

        _mockTeacherService.Setup(s => s.UpdateAvailabilityAsync(teacherId, dtos, It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedAvailability);

        // Act
        var result = await _controller.UpdateAvailability(teacherId, dtos, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var availability = Assert.IsAssignableFrom<IEnumerable<TeacherAvailabilityDto>>(okResult.Value);
        Assert.Equal(2, availability.Count());
    }

    [Fact]
    public async Task UpdateAvailability_WithInvalidTeacherId_ReturnsNotFound()
    {
        // Arrange
        var invalidId = Guid.NewGuid();
        var dtos = new List<UpdateTeacherAvailabilityDto>
        {
            new() { DayOfWeek = DayOfWeek.Monday, FromTime = new TimeOnly(9, 0), UntilTime = new TimeOnly(17, 0) }
        };

        _mockTeacherService.Setup(s => s.UpdateAvailabilityAsync(invalidId, dtos, It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<TeacherAvailabilityDto>?)null);

        // Act
        var result = await _controller.UpdateAvailability(invalidId, dtos, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task UpdateAvailability_WithMoreThan7Entries_ReturnsBadRequest()
    {
        // Arrange
        var teacherId = Guid.NewGuid();
        var dtos = new List<UpdateTeacherAvailabilityDto>
        {
            new() { DayOfWeek = DayOfWeek.Sunday, FromTime = new TimeOnly(9, 0), UntilTime = new TimeOnly(17, 0) },
            new() { DayOfWeek = DayOfWeek.Monday, FromTime = new TimeOnly(9, 0), UntilTime = new TimeOnly(17, 0) },
            new() { DayOfWeek = DayOfWeek.Tuesday, FromTime = new TimeOnly(9, 0), UntilTime = new TimeOnly(17, 0) },
            new() { DayOfWeek = DayOfWeek.Wednesday, FromTime = new TimeOnly(9, 0), UntilTime = new TimeOnly(17, 0) },
            new() { DayOfWeek = DayOfWeek.Thursday, FromTime = new TimeOnly(9, 0), UntilTime = new TimeOnly(17, 0) },
            new() { DayOfWeek = DayOfWeek.Friday, FromTime = new TimeOnly(9, 0), UntilTime = new TimeOnly(17, 0) },
            new() { DayOfWeek = DayOfWeek.Saturday, FromTime = new TimeOnly(9, 0), UntilTime = new TimeOnly(17, 0) },
            new() { DayOfWeek = DayOfWeek.Sunday, FromTime = new TimeOnly(10, 0), UntilTime = new TimeOnly(18, 0) }
        };

        _mockTeacherService.Setup(s => s.UpdateAvailabilityAsync(teacherId, dtos, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Maximum of 7 availability entries allowed (one per day)"));

        // Act
        var result = await _controller.UpdateAvailability(teacherId, dtos, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task UpdateAvailability_WithDuplicateDays_ReturnsBadRequest()
    {
        // Arrange
        var teacherId = Guid.NewGuid();
        var dtos = new List<UpdateTeacherAvailabilityDto>
        {
            new() { DayOfWeek = DayOfWeek.Monday, FromTime = new TimeOnly(9, 0), UntilTime = new TimeOnly(17, 0) },
            new() { DayOfWeek = DayOfWeek.Monday, FromTime = new TimeOnly(10, 0), UntilTime = new TimeOnly(18, 0) }
        };

        _mockTeacherService.Setup(s => s.UpdateAvailabilityAsync(teacherId, dtos, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Duplicate days are not allowed: Monday"));

        // Act
        var result = await _controller.UpdateAvailability(teacherId, dtos, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task UpdateAvailability_WithTimeRangeLessThan1Hour_ReturnsBadRequest()
    {
        // Arrange
        var teacherId = Guid.NewGuid();
        var dtos = new List<UpdateTeacherAvailabilityDto>
        {
            new() { DayOfWeek = DayOfWeek.Monday, FromTime = new TimeOnly(9, 0), UntilTime = new TimeOnly(9, 30) }
        };

        _mockTeacherService.Setup(s => s.UpdateAvailabilityAsync(teacherId, dtos, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("End time must be at least 1 hour after start time for Monday. Use 00:00-00:00 to mark as unavailable."));

        // Act
        var result = await _controller.UpdateAvailability(teacherId, dtos, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task UpdateAvailability_WithZeroZeroForUnavailable_ReturnsSuccess()
    {
        // Arrange
        var teacherId = Guid.NewGuid();
        var dtos = new List<UpdateTeacherAvailabilityDto>
        {
            new() { DayOfWeek = DayOfWeek.Monday, FromTime = TimeOnly.MinValue, UntilTime = TimeOnly.MinValue }
        };

        var updatedAvailability = new List<TeacherAvailabilityDto>
        {
            new() { Id = Guid.NewGuid(), DayOfWeek = DayOfWeek.Monday, FromTime = TimeOnly.MinValue, UntilTime = TimeOnly.MinValue }
        };

        _mockTeacherService.Setup(s => s.UpdateAvailabilityAsync(teacherId, dtos, It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedAvailability);

        // Act
        var result = await _controller.UpdateAvailability(teacherId, dtos, CancellationToken.None);

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
        var teacherId = Guid.NewGuid();
        var dtos = new List<UpdateTeacherAvailabilityDto>
        {
            new() { DayOfWeek = DayOfWeek.Monday, FromTime = new TimeOnly(9, 0), UntilTime = new TimeOnly(10, 0) }
        };

        var updatedAvailability = new List<TeacherAvailabilityDto>
        {
            new() { Id = Guid.NewGuid(), DayOfWeek = DayOfWeek.Monday, FromTime = new TimeOnly(9, 0), UntilTime = new TimeOnly(10, 0) }
        };

        _mockTeacherService.Setup(s => s.UpdateAvailabilityAsync(teacherId, dtos, It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedAvailability);

        // Act
        var result = await _controller.UpdateAvailability(teacherId, dtos, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var availability = Assert.IsAssignableFrom<IEnumerable<TeacherAvailabilityDto>>(okResult.Value);
        Assert.Single(availability);
    }

    [Fact]
    public async Task UpdateAvailability_ReplacesExistingAvailability()
    {
        // Arrange
        var teacherId = Guid.NewGuid();
        var dtos = new List<UpdateTeacherAvailabilityDto>
        {
            new() { DayOfWeek = DayOfWeek.Tuesday, FromTime = new TimeOnly(10, 0), UntilTime = new TimeOnly(18, 0) }
        };

        var updatedAvailability = new List<TeacherAvailabilityDto>
        {
            new() { Id = Guid.NewGuid(), DayOfWeek = DayOfWeek.Tuesday, FromTime = new TimeOnly(10, 0), UntilTime = new TimeOnly(18, 0) }
        };

        _mockTeacherService.Setup(s => s.UpdateAvailabilityAsync(teacherId, dtos, It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedAvailability);

        // Act
        var result = await _controller.UpdateAvailability(teacherId, dtos, CancellationToken.None);

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
        var teacherId = Guid.NewGuid();
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

        var updatedAvailability = dtos.Select(d => new TeacherAvailabilityDto
        {
            Id = Guid.NewGuid(),
            DayOfWeek = d.DayOfWeek,
            FromTime = d.FromTime,
            UntilTime = d.UntilTime
        }).ToList();

        _mockTeacherService.Setup(s => s.UpdateAvailabilityAsync(teacherId, dtos, It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedAvailability);

        // Act
        var result = await _controller.UpdateAvailability(teacherId, dtos, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var availability = Assert.IsAssignableFrom<IEnumerable<TeacherAvailabilityDto>>(okResult.Value);
        Assert.Equal(7, availability.Count());
    }

    #endregion
}
