using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using BosDAT.API.Controllers;
using BosDAT.Core.DTOs;
using BosDAT.Core.Interfaces;
using BosDAT.Core.Interfaces.Services;
using BosDAT.Core.Interfaces.Repositories;

namespace BosDAT.API.Tests.Controllers;

public class CourseTasksControllerTests
{
    private readonly Mock<ICourseTaskService> _serviceMock = new();
    private readonly CourseTasksController _controller;

    private readonly Guid _courseId = Guid.NewGuid();

    public CourseTasksControllerTests()
    {
        _controller = new CourseTasksController(_serviceMock.Object);
    }

    #region GetByCourse

    [Fact]
    public async Task GetByCourse_ReturnsOkWithTasks()
    {
        // Arrange
        var tasks = new List<CourseTaskDto>
        {
            new() { Id = Guid.NewGuid(), CourseId = _courseId, Title = "Scale practice" },
            new() { Id = Guid.NewGuid(), CourseId = _courseId, Title = "Etude 3" }
        };
        _serviceMock
            .Setup(s => s.GetByCourseAsync(_courseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tasks);

        // Act
        var result = await _controller.GetByCourse(_courseId, CancellationToken.None);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var returned = Assert.IsAssignableFrom<IEnumerable<CourseTaskDto>>(ok.Value);
        Assert.Equal(2, returned.Count());
    }

    [Fact]
    public async Task GetByCourse_WithNoTasks_ReturnsOkWithEmptyList()
    {
        // Arrange
        _serviceMock
            .Setup(s => s.GetByCourseAsync(_courseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Act
        var result = await _controller.GetByCourse(_courseId, CancellationToken.None);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Empty((IEnumerable<CourseTaskDto>)ok.Value!);
    }

    #endregion

    #region Create

    [Fact]
    public async Task Create_WithValidCourse_ReturnsOkWithDto()
    {
        // Arrange
        var dto = new CreateCourseTaskDto { Title = "New task" };
        var created = new CourseTaskDto { Id = Guid.NewGuid(), CourseId = _courseId, Title = "New task" };
        _serviceMock
            .Setup(s => s.CreateAsync(_courseId, dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(created);

        // Act
        var result = await _controller.Create(_courseId, dto, CancellationToken.None);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var returned = Assert.IsType<CourseTaskDto>(ok.Value);
        Assert.Equal("New task", returned.Title);
    }

    [Fact]
    public async Task Create_WhenCourseNotFound_ReturnsNotFound()
    {
        // Arrange
        var dto = new CreateCourseTaskDto { Title = "Task" };
        _serviceMock
            .Setup(s => s.CreateAsync(_courseId, dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CourseTaskDto?)null);

        // Act
        var result = await _controller.Create(_courseId, dto, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    #endregion

    #region Delete

    [Fact]
    public async Task Delete_WhenTaskExists_ReturnsNoContent()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        _serviceMock
            .Setup(s => s.DeleteAsync(taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Delete(_courseId, taskId, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_WhenTaskNotFound_ReturnsNotFound()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        _serviceMock
            .Setup(s => s.DeleteAsync(taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.Delete(_courseId, taskId, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    #endregion
}
