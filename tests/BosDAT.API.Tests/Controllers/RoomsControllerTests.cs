using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using BosDAT.API.Controllers;
using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Interfaces;
using BosDAT.API.Tests.Helpers;

namespace BosDAT.API.Tests.Controllers;

public class RoomsControllerTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly RoomsController _controller;

    public RoomsControllerTests()
    {
        _mockUnitOfWork = MockHelpers.CreateMockUnitOfWork();
        _controller = new RoomsController(_mockUnitOfWork.Object);
    }

    [Fact]
    public async Task GetAll_ReturnsAllRooms()
    {
        // Arrange
        var rooms = new List<Room>
        {
            new Room
            {
                Id = 1,
                Name = "Room A",
                FloorLevel = 1,
                Capacity = 2,
                HasPiano = true,
                HasStereo = true,
                IsActive = true,
                Courses = new List<Course>(),
                Lessons = new List<Lesson>()
            },
            new Room
            {
                Id = 2,
                Name = "Room B",
                FloorLevel = 2,
                Capacity = 4,
                HasDrums = true,
                HasGuitar = true,
                IsActive = true,
                Courses = new List<Course>(),
                Lessons = new List<Lesson>()
            }
        };

        var mockRoomRepo = MockHelpers.CreateMockRepository(rooms);
        _mockUnitOfWork.Setup(u => u.Repository<Room>()).Returns(mockRoomRepo.Object);

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
        var rooms = new List<Room>
        {
            new Room
            {
                Id = 1,
                Name = "Active Room",
                IsActive = true,
                Courses = new List<Course>(),
                Lessons = new List<Lesson>()
            },
            new Room
            {
                Id = 2,
                Name = "Archived Room",
                IsActive = false,
                Courses = new List<Course>(),
                Lessons = new List<Lesson>()
            }
        };

        var mockRoomRepo = MockHelpers.CreateMockRepository(rooms);
        _mockUnitOfWork.Setup(u => u.Repository<Room>()).Returns(mockRoomRepo.Object);

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
        var mockRoomRepo = new Mock<IRepository<Room>>();
        mockRoomRepo.Setup(r => r.AddAsync(It.IsAny<Room>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Room r, CancellationToken _) =>
            {
                r.Id = 1;
                r.Courses = new List<Course>();
                r.Lessons = new List<Lesson>();
                return r;
            });

        _mockUnitOfWork.Setup(u => u.Repository<Room>()).Returns(mockRoomRepo.Object);

        var dto = new CreateRoomDto
        {
            Name = "New Room",
            FloorLevel = 1,
            Capacity = 3,
            HasPiano = true,
            HasStereo = true,
            HasGuitar = false
        };

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
        var room = new Room
        {
            Id = 1,
            Name = "Room A",
            IsActive = true,
            Courses = new List<Course>(),
            Lessons = new List<Lesson>()
        };

        var rooms = new List<Room> { room };
        var mockRoomRepo = MockHelpers.CreateMockRepository(rooms);
        _mockUnitOfWork.Setup(u => u.Repository<Room>()).Returns(mockRoomRepo.Object);

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
        var room = new Room
        {
            Id = 1,
            Name = "Room A",
            IsActive = true,
            Courses = new List<Course>
            {
                new Course
                {
                    Id = Guid.NewGuid(),
                    RoomId = 1,
                    Status = CourseStatus.Active,
                    TeacherId = Guid.NewGuid(),
                    LessonTypeId = 1,
                    DayOfWeek = DayOfWeek.Monday,
                    StartTime = new TimeOnly(10, 0),
                    EndTime = new TimeOnly(11, 0),
                    StartDate = DateOnly.FromDateTime(DateTime.Today)
                }
            },
            Lessons = new List<Lesson>()
        };

        var rooms = new List<Room> { room };
        var mockRoomRepo = MockHelpers.CreateMockRepository(rooms);
        _mockUnitOfWork.Setup(u => u.Repository<Room>()).Returns(mockRoomRepo.Object);

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
        var room = new Room
        {
            Id = 1,
            Name = "Room A",
            IsActive = true,
            Courses = new List<Course>(),
            Lessons = new List<Lesson>
            {
                new Lesson
                {
                    Id = Guid.NewGuid(),
                    RoomId = 1,
                    Status = LessonStatus.Scheduled,
                    CourseId = Guid.NewGuid(),
                    TeacherId = Guid.NewGuid(),
                    ScheduledDate = DateOnly.FromDateTime(DateTime.Today.AddDays(7)),
                    StartTime = new TimeOnly(10, 0),
                    EndTime = new TimeOnly(11, 0)
                }
            }
        };

        var rooms = new List<Room> { room };
        var mockRoomRepo = MockHelpers.CreateMockRepository(rooms);
        _mockUnitOfWork.Setup(u => u.Repository<Room>()).Returns(mockRoomRepo.Object);

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
        var room = new Room
        {
            Id = 1,
            Name = "Room A",
            IsActive = true,
            Courses = new List<Course>(),
            Lessons = new List<Lesson>()
        };

        var rooms = new List<Room> { room };
        var mockRoomRepo = MockHelpers.CreateMockRepository(rooms);
        mockRoomRepo.Setup(r => r.DeleteAsync(It.IsAny<Room>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork.Setup(u => u.Repository<Room>()).Returns(mockRoomRepo.Object);

        // Act
        var result = await _controller.Delete(1, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_WithLinkedCourses_ReturnsBadRequest()
    {
        // Arrange
        var room = new Room
        {
            Id = 1,
            Name = "Room A",
            IsActive = true,
            Courses = new List<Course>
            {
                new Course
                {
                    Id = Guid.NewGuid(),
                    RoomId = 1,
                    Status = CourseStatus.Completed, // Even completed courses block deletion
                    TeacherId = Guid.NewGuid(),
                    LessonTypeId = 1,
                    DayOfWeek = DayOfWeek.Monday,
                    StartTime = new TimeOnly(10, 0),
                    EndTime = new TimeOnly(11, 0),
                    StartDate = DateOnly.FromDateTime(DateTime.Today)
                }
            },
            Lessons = new List<Lesson>()
        };

        var rooms = new List<Room> { room };
        var mockRoomRepo = MockHelpers.CreateMockRepository(rooms);
        _mockUnitOfWork.Setup(u => u.Repository<Room>()).Returns(mockRoomRepo.Object);

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
        var room = new Room
        {
            Id = 1,
            Name = "Archived Room",
            IsActive = false,
            Courses = new List<Course>(),
            Lessons = new List<Lesson>()
        };

        var mockRoomRepo = new Mock<IRepository<Room>>();
        mockRoomRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(room);

        _mockUnitOfWork.Setup(u => u.Repository<Room>()).Returns(mockRoomRepo.Object);

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
        var mockRoomRepo = new Mock<IRepository<Room>>();
        mockRoomRepo.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Room?)null);

        _mockUnitOfWork.Setup(u => u.Repository<Room>()).Returns(mockRoomRepo.Object);

        // Act
        var result = await _controller.Reactivate(999, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Archive_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var rooms = new List<Room>();
        var mockRoomRepo = MockHelpers.CreateMockRepository(rooms);
        _mockUnitOfWork.Setup(u => u.Repository<Room>()).Returns(mockRoomRepo.Object);

        // Act
        var result = await _controller.Archive(999, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Delete_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var rooms = new List<Room>();
        var mockRoomRepo = MockHelpers.CreateMockRepository(rooms);
        _mockUnitOfWork.Setup(u => u.Repository<Room>()).Returns(mockRoomRepo.Object);

        // Act
        var result = await _controller.Delete(999, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }
}
