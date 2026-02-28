using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Enums;
using BosDAT.Infrastructure.Data;
using BosDAT.Infrastructure.Repositories;
using BosDAT.Infrastructure.Services;
using BosDAT.Infrastructure.Tests.Helpers;

namespace BosDAT.Infrastructure.Tests.Services;

public class CourseTaskServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly UnitOfWork _unitOfWork;
    private readonly CourseTaskService _service;

    private readonly Guid _courseId = Guid.NewGuid();

    public CourseTaskServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"CourseTaskServiceTest_{Guid.NewGuid()}")
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new ApplicationDbContext(options);
        _unitOfWork = TestHelpers.CreateUnitOfWork(_context);
        _service = new CourseTaskService(_unitOfWork);

        SeedCourse();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    private void SeedCourse()
    {
        _context.Instruments.Add(new Instrument { Id = 1, Name = "Piano", Category = InstrumentCategory.Keyboard });
        _context.Courses.Add(new Course
        {
            Id = _courseId,
            CourseTypeId = Guid.NewGuid(),
            TeacherId = Guid.NewGuid(),
            DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(10, 30),
            StartDate = new DateOnly(2026, 1, 1),
            Frequency = CourseFrequency.Weekly,
            Status = CourseStatus.Active
        });
        _context.SaveChanges();
    }

    #region GetByCourseAsync

    [Fact]
    public async Task GetByCourseAsync_WithNoTasks_ReturnsEmpty()
    {
        // Act
        var result = await _service.GetByCourseAsync(_courseId);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByCourseAsync_ReturnsOnlyTasksForCourse()
    {
        // Arrange
        var otherCourseId = Guid.NewGuid();
        _context.CourseTasks.AddRange(
            new CourseTask { Id = Guid.NewGuid(), CourseId = _courseId, Title = "Scale practice" },
            new CourseTask { Id = Guid.NewGuid(), CourseId = _courseId, Title = "Etude 3" },
            new CourseTask { Id = Guid.NewGuid(), CourseId = otherCourseId, Title = "Other course task" });
        await _context.SaveChangesAsync();

        // Act
        var result = (await _service.GetByCourseAsync(_courseId)).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, t => Assert.Equal(_courseId, t.CourseId));
    }

    [Fact]
    public async Task GetByCourseAsync_MapsDtoFieldsCorrectly()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        _context.CourseTasks.Add(new CourseTask { Id = taskId, CourseId = _courseId, Title = "Hanon exercises" });
        await _context.SaveChangesAsync();

        // Act
        var result = (await _service.GetByCourseAsync(_courseId)).ToList();

        // Assert
        var dto = Assert.Single(result);
        Assert.Equal(taskId, dto.Id);
        Assert.Equal(_courseId, dto.CourseId);
        Assert.Equal("Hanon exercises", dto.Title);
    }

    #endregion

    #region CreateAsync

    [Fact]
    public async Task CreateAsync_WithValidCourse_PersistsAndReturnsDto()
    {
        // Arrange
        var dto = new CreateCourseTaskDto { Title = "Play C major scale" };

        // Act
        var result = await _service.CreateAsync(_courseId, dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_courseId, result.CourseId);
        Assert.Equal("Play C major scale", result.Title);

        var saved = await _context.CourseTasks.FirstOrDefaultAsync(t => t.CourseId == _courseId);
        Assert.NotNull(saved);
        Assert.Equal("Play C major scale", saved.Title);
    }

    [Fact]
    public async Task CreateAsync_WithNonexistentCourse_ReturnsNull()
    {
        // Arrange
        var dto = new CreateCourseTaskDto { Title = "Task for ghost course" };

        // Act
        var result = await _service.CreateAsync(Guid.NewGuid(), dto);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region DeleteAsync

    [Fact]
    public async Task DeleteAsync_WhenTaskExists_DeletesAndReturnsTrue()
    {
        // Arrange
        var task = new CourseTask { Id = Guid.NewGuid(), CourseId = _courseId, Title = "To delete" };
        _context.CourseTasks.Add(task);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.DeleteAsync(task.Id);

        // Assert
        Assert.True(result);
        Assert.Null(await _context.CourseTasks.FindAsync(task.Id));
    }

    [Fact]
    public async Task DeleteAsync_WhenTaskNotFound_ReturnsFalse()
    {
        // Act
        var result = await _service.DeleteAsync(Guid.NewGuid());

        // Assert
        Assert.False(result);
    }

    #endregion
}
