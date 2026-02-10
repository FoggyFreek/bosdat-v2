using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using BosDAT.API.Controllers;
using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Interfaces;

namespace BosDAT.API.Tests.Controllers;

public class RoomsControllerTests
{
    private readonly Mock<IRoomService> _mockRoomService;
    private readonly RoomsController _controller;

    public RoomsControllerTests()
    {
        _mockRoomService = new Mock<IRoomService>();
        _controller = new RoomsController(_mockRoomService.Object);
    }

    [Fact]
    public async Task GetAll_ReturnsAllRooms()
    {
        // Arrange
        var roomDtos = new List<RoomDto>
        {
            new RoomDto
            {
                Id = 1,
                Name = "Room A",
                FloorLevel = 1,
                Capacity = 2,
                HasPiano = true,
                HasStereo = true,
                IsActive = true
            },
            new RoomDto
            {
                Id = 2,
                Name = "Room B",
                FloorLevel = 2,
                Capacity = 4,
                HasDrums = true,
                HasGuitar = true,
                IsActive = true
            }
        };

        _mockRoomService.Setup(s => s.GetAllAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(roomDtos);

        // Act
        var result = await _controller.GetAll(null, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedRooms = Assert.IsAssignableFrom<IEnumerable<RoomDto>>(okResult.Value);
        Assert.Equal(2, returnedRooms.Count());
    }

    [Fact]
    public async Task GetAll_ActiveOnly_ReturnsOnlyActiveRooms()
    {
        // Arrange
        var roomDtos = new List<RoomDto>
        {
            new RoomDto
            {
                Id = 1,
                Name = "Active Room",
                IsActive = true
            }
        };

        _mockRoomService.Setup(s => s.GetAllAsync(true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(roomDtos);

        // Act
        var result = await _controller.GetAll(true, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedRooms = Assert.IsAssignableFrom<IEnumerable<RoomDto>>(okResult.Value);
        Assert.Single(returnedRooms);
        Assert.Equal("Active Room", returnedRooms.First().Name);
    }

    [Fact]
    public async Task Create_WithValidData_ReturnsCreatedRoom()
    {
        // Arrange
        var dto = new CreateRoomDto
        {
            Name = "New Room",
            FloorLevel = 1,
            Capacity = 3,
            HasPiano = true,
            HasStereo = true,
            HasGuitar = false
        };

        var createdRoom = new RoomDto
        {
            Id = 1,
            Name = "New Room",
            FloorLevel = 1,
            Capacity = 3,
            HasPiano = true,
            HasStereo = true,
            HasGuitar = false,
            IsActive = true
        };

        _mockRoomService.Setup(s => s.CreateAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdRoom);

        // Act
        var result = await _controller.Create(dto, CancellationToken.None);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var returnedRoom = Assert.IsType<RoomDto>(createdResult.Value);
        Assert.Equal("New Room", returnedRoom.Name);
        Assert.Equal(1, returnedRoom.FloorLevel);
        Assert.Equal(3, returnedRoom.Capacity);
        Assert.True(returnedRoom.HasPiano);
        Assert.True(returnedRoom.HasStereo);
        Assert.False(returnedRoom.HasGuitar);
    }

    [Fact]
    public async Task Archive_WithNoLinkedData_ReturnsArchivedRoom()
    {
        // Arrange
        var archivedRoom = new RoomDto
        {
            Id = 1,
            Name = "Room A",
            IsActive = false
        };

        _mockRoomService.Setup(s => s.ArchiveAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, null, archivedRoom));

        // Act
        var result = await _controller.Archive(1, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedRoom = Assert.IsType<RoomDto>(okResult.Value);
        Assert.False(returnedRoom.IsActive);
    }

    [Fact]
    public async Task Archive_WithActiveCourses_ReturnsBadRequest()
    {
        // Arrange
        _mockRoomService.Setup(s => s.ArchiveAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, "Cannot archive room: 1 active courses and 0 scheduled lessons are linked to this room.", null));

        // Act
        var result = await _controller.Archive(1, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task Archive_WithScheduledLessons_ReturnsBadRequest()
    {
        // Arrange
        _mockRoomService.Setup(s => s.ArchiveAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, "Cannot archive room: 0 active courses and 1 scheduled lessons are linked to this room.", null));

        // Act
        var result = await _controller.Archive(1, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task Delete_WithNoLinkedData_ReturnsNoContent()
    {
        // Arrange
        _mockRoomService.Setup(s => s.DeleteAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, null));

        // Act
        var result = await _controller.Delete(1, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_WithLinkedCourses_ReturnsBadRequest()
    {
        // Arrange
        _mockRoomService.Setup(s => s.DeleteAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, "Cannot delete room: 1 courses and 0 lessons are linked to this room. Use archive instead."));

        // Act
        var result = await _controller.Delete(1, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task Reactivate_WithValidId_ReturnsReactivatedRoom()
    {
        // Arrange
        var reactivatedRoom = new RoomDto
        {
            Id = 1,
            Name = "Archived Room",
            IsActive = true
        };

        _mockRoomService.Setup(s => s.ReactivateAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reactivatedRoom);

        // Act
        var result = await _controller.Reactivate(1, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedRoom = Assert.IsType<RoomDto>(okResult.Value);
        Assert.True(returnedRoom.IsActive);
    }

    [Fact]
    public async Task Reactivate_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        _mockRoomService.Setup(s => s.ReactivateAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RoomDto?)null);

        // Act
        var result = await _controller.Reactivate(999, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Archive_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        _mockRoomService.Setup(s => s.ArchiveAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, null, null));

        // Act
        var result = await _controller.Archive(999, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Delete_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        _mockRoomService.Setup(s => s.DeleteAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, null));

        // Act
        var result = await _controller.Delete(999, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }
}
