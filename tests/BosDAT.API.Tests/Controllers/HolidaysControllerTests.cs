using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using BosDAT.API.Controllers;
using BosDAT.Core.DTOs;
using BosDAT.Core.Interfaces;
using BosDAT.Core.Interfaces.Services;

namespace BosDAT.API.Tests.Controllers;

public class HolidaysControllerTests
{
    private readonly Mock<IHolidayService> _mockHolidayService;
    private readonly HolidaysController _controller;

    public HolidaysControllerTests()
    {
        _mockHolidayService = new Mock<IHolidayService>();
        _controller = new HolidaysController(_mockHolidayService.Object);
    }

    [Fact]
    public async Task GetAll_ReturnsAllHolidays()
    {
        // Arrange
        var holidays = new List<HolidayDto>
        {
            new HolidayDto { Id = 1, Name = "Summer Break", StartDate = new DateOnly(2024, 7, 1), EndDate = new DateOnly(2024, 8, 31) },
            new HolidayDto { Id = 2, Name = "Christmas", StartDate = new DateOnly(2024, 12, 23), EndDate = new DateOnly(2025, 1, 5) }
        };

        _mockHolidayService
            .Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(holidays);

        // Act
        var result = await _controller.GetAll(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedHolidays = Assert.IsAssignableFrom<IEnumerable<HolidayDto>>(okResult.Value);
        Assert.Equal(2, returnedHolidays.Count());
    }

    [Fact]
    public async Task GetById_WithValidId_ReturnsHoliday()
    {
        // Arrange
        var holiday = new HolidayDto
        {
            Id = 1,
            Name = "Summer Break",
            StartDate = new DateOnly(2024, 7, 1),
            EndDate = new DateOnly(2024, 8, 31)
        };

        _mockHolidayService
            .Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(holiday);

        // Act
        var result = await _controller.GetById(1, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedHoliday = Assert.IsType<HolidayDto>(okResult.Value);
        Assert.Equal("Summer Break", returnedHoliday.Name);
    }

    [Fact]
    public async Task GetById_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        _mockHolidayService
            .Setup(s => s.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((HolidayDto?)null);

        // Act
        var result = await _controller.GetById(999, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Create_WithValidData_ReturnsCreatedHoliday()
    {
        // Arrange
        var dto = new CreateHolidayDto
        {
            Name = "Spring Break",
            StartDate = new DateOnly(2024, 4, 1),
            EndDate = new DateOnly(2024, 4, 14)
        };

        var createdHoliday = new HolidayDto
        {
            Id = 1,
            Name = "Spring Break",
            StartDate = new DateOnly(2024, 4, 1),
            EndDate = new DateOnly(2024, 4, 14)
        };

        _mockHolidayService
            .Setup(s => s.CreateAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdHoliday);

        // Act
        var result = await _controller.Create(dto, CancellationToken.None);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var returnedHoliday = Assert.IsType<HolidayDto>(createdResult.Value);
        Assert.Equal("Spring Break", returnedHoliday.Name);
        Assert.Equal(new DateOnly(2024, 4, 1), returnedHoliday.StartDate);
        Assert.Equal(new DateOnly(2024, 4, 14), returnedHoliday.EndDate);
    }

    [Fact]
    public async Task Update_WithValidData_ReturnsUpdatedHoliday()
    {
        // Arrange
        var dto = new UpdateHolidayDto
        {
            Name = "Extended Summer Break",
            StartDate = new DateOnly(2024, 6, 15),
            EndDate = new DateOnly(2024, 9, 1)
        };

        var updatedHoliday = new HolidayDto
        {
            Id = 1,
            Name = "Extended Summer Break",
            StartDate = new DateOnly(2024, 6, 15),
            EndDate = new DateOnly(2024, 9, 1)
        };

        _mockHolidayService
            .Setup(s => s.UpdateAsync(1, dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedHoliday);

        // Act
        var result = await _controller.Update(1, dto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedHoliday = Assert.IsType<HolidayDto>(okResult.Value);
        Assert.Equal("Extended Summer Break", returnedHoliday.Name);
        Assert.Equal(new DateOnly(2024, 6, 15), returnedHoliday.StartDate);
        Assert.Equal(new DateOnly(2024, 9, 1), returnedHoliday.EndDate);
    }

    [Fact]
    public async Task Update_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var dto = new UpdateHolidayDto
        {
            Name = "Test",
            StartDate = new DateOnly(2024, 1, 1),
            EndDate = new DateOnly(2024, 1, 7)
        };

        _mockHolidayService
            .Setup(s => s.UpdateAsync(999, dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync((HolidayDto?)null);

        // Act
        var result = await _controller.Update(999, dto, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Delete_WithValidId_ReturnsNoContent()
    {
        // Arrange
        _mockHolidayService
            .Setup(s => s.DeleteAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Delete(1, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
        _mockHolidayService.Verify(s => s.DeleteAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Delete_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        _mockHolidayService
            .Setup(s => s.DeleteAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.Delete(999, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }
}
