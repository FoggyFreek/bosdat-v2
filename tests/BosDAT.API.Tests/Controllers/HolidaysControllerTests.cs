using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using BosDAT.API.Controllers;
using BosDAT.Core.Entities;
using BosDAT.Core.Interfaces;
using BosDAT.API.Tests.Helpers;

namespace BosDAT.API.Tests.Controllers;

public class HolidaysControllerTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly HolidaysController _controller;

    public HolidaysControllerTests()
    {
        _mockUnitOfWork = MockHelpers.CreateMockUnitOfWork();
        _controller = new HolidaysController(_mockUnitOfWork.Object);
    }

    [Fact]
    public async Task GetAll_ReturnsAllHolidays()
    {
        // Arrange
        var holidays = new List<Holiday>
        {
            new Holiday { Id = 1, Name = "Summer Break", StartDate = new DateOnly(2024, 7, 1), EndDate = new DateOnly(2024, 8, 31) },
            new Holiday { Id = 2, Name = "Christmas", StartDate = new DateOnly(2024, 12, 23), EndDate = new DateOnly(2025, 1, 5) }
        };

        var mockHolidayRepo = MockHelpers.CreateMockRepository(holidays);

        _mockUnitOfWork.Setup(u => u.Repository<Holiday>()).Returns(mockHolidayRepo.Object);

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
        var holiday = new Holiday
        {
            Id = 1,
            Name = "Summer Break",
            StartDate = new DateOnly(2024, 7, 1),
            EndDate = new DateOnly(2024, 8, 31)
        };

        var mockHolidayRepo = new Mock<IRepository<Holiday>>();
        mockHolidayRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(holiday);

        _mockUnitOfWork.Setup(u => u.Repository<Holiday>()).Returns(mockHolidayRepo.Object);

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
        var mockHolidayRepo = new Mock<IRepository<Holiday>>();
        mockHolidayRepo.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Holiday?)null);

        _mockUnitOfWork.Setup(u => u.Repository<Holiday>()).Returns(mockHolidayRepo.Object);

        // Act
        var result = await _controller.GetById(999, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Create_WithValidData_ReturnsCreatedHoliday()
    {
        // Arrange
        var mockHolidayRepo = new Mock<IRepository<Holiday>>();
        mockHolidayRepo.Setup(r => r.AddAsync(It.IsAny<Holiday>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Holiday h, CancellationToken _) =>
            {
                h.Id = 1;
                return h;
            });

        _mockUnitOfWork.Setup(u => u.Repository<Holiday>()).Returns(mockHolidayRepo.Object);

        var dto = new CreateHolidayDto
        {
            Name = "Spring Break",
            StartDate = new DateOnly(2024, 4, 1),
            EndDate = new DateOnly(2024, 4, 14)
        };

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
        var holiday = new Holiday
        {
            Id = 1,
            Name = "Summer Break",
            StartDate = new DateOnly(2024, 7, 1),
            EndDate = new DateOnly(2024, 8, 31)
        };

        var mockHolidayRepo = new Mock<IRepository<Holiday>>();
        mockHolidayRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(holiday);

        _mockUnitOfWork.Setup(u => u.Repository<Holiday>()).Returns(mockHolidayRepo.Object);

        var dto = new UpdateHolidayDto
        {
            Name = "Extended Summer Break",
            StartDate = new DateOnly(2024, 6, 15),
            EndDate = new DateOnly(2024, 9, 1)
        };

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
        var mockHolidayRepo = new Mock<IRepository<Holiday>>();
        mockHolidayRepo.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Holiday?)null);

        _mockUnitOfWork.Setup(u => u.Repository<Holiday>()).Returns(mockHolidayRepo.Object);

        var dto = new UpdateHolidayDto
        {
            Name = "Test",
            StartDate = new DateOnly(2024, 1, 1),
            EndDate = new DateOnly(2024, 1, 7)
        };

        // Act
        var result = await _controller.Update(999, dto, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Delete_WithValidId_ReturnsNoContent()
    {
        // Arrange
        var holiday = new Holiday
        {
            Id = 1,
            Name = "Summer Break",
            StartDate = new DateOnly(2024, 7, 1),
            EndDate = new DateOnly(2024, 8, 31)
        };

        var mockHolidayRepo = new Mock<IRepository<Holiday>>();
        mockHolidayRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(holiday);

        _mockUnitOfWork.Setup(u => u.Repository<Holiday>()).Returns(mockHolidayRepo.Object);

        // Act
        var result = await _controller.Delete(1, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
        mockHolidayRepo.Verify(r => r.DeleteAsync(holiday, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Delete_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var mockHolidayRepo = new Mock<IRepository<Holiday>>();
        mockHolidayRepo.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Holiday?)null);

        _mockUnitOfWork.Setup(u => u.Repository<Holiday>()).Returns(mockHolidayRepo.Object);

        // Act
        var result = await _controller.Delete(999, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }
}
