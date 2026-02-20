using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Enums;
using BosDAT.Infrastructure.Data;
using BosDAT.Infrastructure.Repositories;
using BosDAT.Infrastructure.Services;

namespace BosDAT.Infrastructure.Tests.Services;

public class RoomServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly UnitOfWork _unitOfWork;
    private readonly RoomService _service;

    public RoomServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"RoomServiceTest_{Guid.NewGuid()}")
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new ApplicationDbContext(options);
        _unitOfWork = new UnitOfWork(_context);
        _service = new RoomService(_unitOfWork);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    #region GetAllAsync

    [Fact]
    public async Task GetAllAsync_WithNoFilter_ReturnsAllRooms()
    {
        _context.Rooms.AddRange(
            CreateRoom("Room A", isActive: true),
            CreateRoom("Room B", isActive: false));
        await _context.SaveChangesAsync();

        var result = await _service.GetAllAsync(activeOnly: null);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetAllAsync_WithActiveOnlyTrue_ReturnsOnlyActiveRooms()
    {
        _context.Rooms.AddRange(
            CreateRoom("Room A", isActive: true),
            CreateRoom("Room B", isActive: false),
            CreateRoom("Room C", isActive: true));
        await _context.SaveChangesAsync();

        var result = await _service.GetAllAsync(activeOnly: true);

        Assert.Equal(2, result.Count);
        Assert.All(result, r => Assert.True(r.IsActive));
    }

    [Fact]
    public async Task GetAllAsync_ReturnsMappedDtoCorrectly()
    {
        var room = new Room
        {
            Id = 1,
            Name = "Piano Room",
            FloorLevel = 2,
            Capacity = 5,
            HasPiano = true,
            HasDrums = false,
            IsActive = true,
            Notes = "Ground floor"
        };
        _context.Rooms.Add(room);
        await _context.SaveChangesAsync();

        var result = await _service.GetAllAsync(null);

        var dto = Assert.Single(result);
        Assert.Equal("Piano Room", dto.Name);
        Assert.Equal(2, dto.FloorLevel);
        Assert.Equal(5, dto.Capacity);
        Assert.True(dto.HasPiano);
        Assert.False(dto.HasDrums);
        Assert.Equal("Ground floor", dto.Notes);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsRoomsOrderedByName()
    {
        _context.Rooms.AddRange(
            CreateRoom("Zaal C"),
            CreateRoom("Aula A"),
            CreateRoom("Muziek B"));
        await _context.SaveChangesAsync();

        var result = await _service.GetAllAsync(null);

        Assert.Equal(new[] { "Aula A", "Muziek B", "Zaal C" }, result.Select(r => r.Name));
    }

    #endregion

    #region GetByIdAsync

    [Fact]
    public async Task GetByIdAsync_WhenRoomExists_ReturnsMappedDto()
    {
        var room = CreateRoom("Studio 1");
        _context.Rooms.Add(room);
        await _context.SaveChangesAsync();

        var result = await _service.GetByIdAsync(room.Id);

        Assert.NotNull(result);
        Assert.Equal(room.Id, result.Id);
        Assert.Equal("Studio 1", result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_WhenRoomNotFound_ReturnsNull()
    {
        var result = await _service.GetByIdAsync(999);

        Assert.Null(result);
    }

    #endregion

    #region CreateAsync

    [Fact]
    public async Task CreateAsync_WithValidDto_PersistsAndReturnsMappedRoom()
    {
        var dto = new CreateRoomDto
        {
            Name = "New Studio",
            FloorLevel = 1,
            Capacity = 3,
            HasPiano = true,
            HasGuitar = false,
            Notes = "Nice room"
        };

        var result = await _service.CreateAsync(dto);

        Assert.NotEqual(0, result.Id);
        Assert.Equal("New Studio", result.Name);
        Assert.Equal(1, result.FloorLevel);
        Assert.Equal(3, result.Capacity);
        Assert.True(result.HasPiano);
        Assert.True(result.IsActive);

        var saved = await _context.Rooms.FindAsync(result.Id);
        Assert.NotNull(saved);
        Assert.True(saved.IsActive);
    }

    [Fact]
    public async Task CreateAsync_AlwaysSetsIsActiveToTrue()
    {
        var dto = new CreateRoomDto { Name = "Room X", Capacity = 2 };

        var result = await _service.CreateAsync(dto);

        Assert.True(result.IsActive);
    }

    #endregion

    #region UpdateAsync

    [Fact]
    public async Task UpdateAsync_WhenRoomExists_UpdatesAndReturnsDto()
    {
        var room = CreateRoom("Old Name");
        _context.Rooms.Add(room);
        await _context.SaveChangesAsync();

        var dto = new UpdateRoomDto
        {
            Name = "New Name",
            Capacity = 10,
            HasDrums = true,
            FloorLevel = 3
        };

        var result = await _service.UpdateAsync(room.Id, dto);

        Assert.NotNull(result);
        Assert.Equal("New Name", result.Name);
        Assert.Equal(10, result.Capacity);
        Assert.True(result.HasDrums);
        Assert.Equal(3, result.FloorLevel);
    }

    [Fact]
    public async Task UpdateAsync_WhenRoomNotFound_ReturnsNull()
    {
        var dto = new UpdateRoomDto { Name = "Does Not Matter", Capacity = 1 };

        var result = await _service.UpdateAsync(999, dto);

        Assert.Null(result);
    }

    #endregion

    #region DeleteAsync

    [Fact]
    public async Task DeleteAsync_WhenRoomExistsWithNoLinks_DeletesAndReturnsSuccess()
    {
        var room = CreateRoom("Empty Room");
        _context.Rooms.Add(room);
        await _context.SaveChangesAsync();

        var (success, error) = await _service.DeleteAsync(room.Id);

        Assert.True(success);
        Assert.Null(error);
        Assert.Null(await _context.Rooms.FindAsync(room.Id));
    }

    [Fact]
    public async Task DeleteAsync_WhenRoomNotFound_ReturnsFalseWithNoError()
    {
        var (success, error) = await _service.DeleteAsync(999);

        Assert.False(success);
        Assert.Null(error);
    }

    [Fact]
    public async Task DeleteAsync_WhenRoomHasLinkedCourses_ReturnsFalseWithMessage()
    {
        var room = CreateRoom("Busy Room");
        _context.Rooms.Add(room);
        var course = CreateCourse(room.Id, CourseStatus.Active);
        _context.Courses.Add(course);
        await _context.SaveChangesAsync();

        var (success, error) = await _service.DeleteAsync(room.Id);

        Assert.False(success);
        Assert.NotNull(error);
        Assert.Contains("courses", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DeleteAsync_WhenRoomHasLinkedLessons_ReturnsFalseWithMessage()
    {
        var room = CreateRoom("Lesson Room");
        _context.Rooms.Add(room);
        var lesson = CreateLesson(room.Id);
        _context.Lessons.Add(lesson);
        await _context.SaveChangesAsync();

        var (success, error) = await _service.DeleteAsync(room.Id);

        Assert.False(success);
        Assert.NotNull(error);
        Assert.Contains("lessons", error, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region ArchiveAsync

    [Fact]
    public async Task ArchiveAsync_WhenRoomHasNoActiveCourses_SetsIsActiveFalse()
    {
        var room = CreateRoom("Archive Me");
        _context.Rooms.Add(room);
        var completedCourse = CreateCourse(room.Id, CourseStatus.Completed);
        _context.Courses.Add(completedCourse);
        await _context.SaveChangesAsync();

        var (success, error, dto) = await _service.ArchiveAsync(room.Id);

        Assert.True(success);
        Assert.Null(error);
        Assert.NotNull(dto);
        Assert.False(dto.IsActive);

        var saved = await _context.Rooms.FindAsync(room.Id);
        Assert.False(saved!.IsActive);
    }

    [Fact]
    public async Task ArchiveAsync_WhenRoomNotFound_ReturnsFalse()
    {
        var (success, error, dto) = await _service.ArchiveAsync(999);

        Assert.False(success);
        Assert.Null(error);
        Assert.Null(dto);
    }

    [Fact]
    public async Task ArchiveAsync_WhenRoomHasActiveCourses_ReturnsFalseWithMessage()
    {
        var room = CreateRoom("Active Room");
        _context.Rooms.Add(room);
        _context.Courses.Add(CreateCourse(room.Id, CourseStatus.Active));
        await _context.SaveChangesAsync();

        var (success, error, dto) = await _service.ArchiveAsync(room.Id);

        Assert.False(success);
        Assert.NotNull(error);
        Assert.Contains("active courses", error, StringComparison.OrdinalIgnoreCase);
        Assert.Null(dto);
    }

    [Fact]
    public async Task ArchiveAsync_WhenRoomHasScheduledLessons_ReturnsFalseWithMessage()
    {
        var room = CreateRoom("Scheduled Room");
        _context.Rooms.Add(room);
        _context.Lessons.Add(CreateLesson(room.Id, LessonStatus.Scheduled));
        await _context.SaveChangesAsync();

        var (success, error, dto) = await _service.ArchiveAsync(room.Id);

        Assert.False(success);
        Assert.NotNull(error);
        Assert.Contains("scheduled lessons", error, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region ReactivateAsync

    [Fact]
    public async Task ReactivateAsync_WhenRoomExists_SetsIsActiveTrue()
    {
        var room = CreateRoom("Inactive Room", isActive: false);
        _context.Rooms.Add(room);
        await _context.SaveChangesAsync();

        var result = await _service.ReactivateAsync(room.Id);

        Assert.NotNull(result);
        Assert.True(result.IsActive);

        var saved = await _context.Rooms.FindAsync(room.Id);
        Assert.True(saved!.IsActive);
    }

    [Fact]
    public async Task ReactivateAsync_WhenRoomNotFound_ReturnsNull()
    {
        var result = await _service.ReactivateAsync(999);

        Assert.Null(result);
    }

    #endregion

    #region Helpers

    private static int _nextRoomId = 1;

    private static Room CreateRoom(string name, bool isActive = true) => new()
    {
        Id = _nextRoomId++,
        Name = name,
        Capacity = 2,
        IsActive = isActive
    };

    private static Course CreateCourse(int roomId, CourseStatus status) => new()
    {
        Id = Guid.NewGuid(),
        CourseTypeId = Guid.NewGuid(),
        TeacherId = Guid.NewGuid(),
        RoomId = roomId,
        DayOfWeek = DayOfWeek.Monday,
        StartTime = new TimeOnly(10, 0),
        EndTime = new TimeOnly(10, 30),
        StartDate = new DateOnly(2025, 1, 1),
        Status = status,
        Frequency = CourseFrequency.Weekly
    };

    private static Lesson CreateLesson(int roomId, LessonStatus status = LessonStatus.Scheduled) => new()
    {
        Id = Guid.NewGuid(),
        CourseId = Guid.NewGuid(),
        TeacherId = Guid.NewGuid(),
        RoomId = roomId,
        ScheduledDate = new DateOnly(2025, 6, 1),
        StartTime = new TimeOnly(10, 0),
        EndTime = new TimeOnly(10, 30),
        Status = status
    };

    #endregion
}
