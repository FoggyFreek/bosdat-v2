using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using BosDAT.API.Controllers;
using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Interfaces;

namespace BosDAT.API.Tests.Controllers;

public class CoursesControllerTests
{
    private readonly Mock<ICourseService> _mockCourseService;
    private readonly CoursesController _controller;

    public CoursesControllerTests()
    {
        _mockCourseService = new Mock<ICourseService>();
        _controller = new CoursesController(_mockCourseService.Object);
    }

    #region GetSummary Tests

    [Fact]
    public async Task GetSummary_ReturnsOkWithCourses()
    {
        // Arrange
        var courses = new List<CourseListDto>
        {
            new() { Id = Guid.NewGuid(), TeacherName = "John Doe", CourseTypeName = "Piano", Status = CourseStatus.Active },
            new() { Id = Guid.NewGuid(), TeacherName = "John Doe", CourseTypeName = "Guitar", Status = CourseStatus.Active }
        };

        _mockCourseService
            .Setup(s => s.GetSummaryAsync(null, null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(courses);

        // Act
        var result = await _controller.GetSummary(null, null, null, null, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedCourses = Assert.IsAssignableFrom<List<CourseListDto>>(okResult.Value);
        Assert.Equal(2, returnedCourses.Count);
    }

    [Fact]
    public async Task GetSummary_PassesFiltersToService()
    {
        // Arrange
        var teacherId = Guid.NewGuid();
        _mockCourseService
            .Setup(s => s.GetSummaryAsync(CourseStatus.Active, teacherId, DayOfWeek.Monday, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CourseListDto>());

        // Act
        await _controller.GetSummary(CourseStatus.Active, teacherId, DayOfWeek.Monday, 1, CancellationToken.None);

        // Assert
        _mockCourseService.Verify(s => s.GetSummaryAsync(
            CourseStatus.Active, teacherId, DayOfWeek.Monday, 1, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region GetCount Tests

    [Fact]
    public async Task GetCount_ReturnsOkWithCount()
    {
        // Arrange
        _mockCourseService
            .Setup(s => s.GetCountAsync(null, null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        // Act
        var result = await _controller.GetCount(null, null, null, null, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(5, okResult.Value);
    }

    [Fact]
    public async Task GetCount_PassesFiltersToService()
    {
        // Arrange
        var teacherId = Guid.NewGuid();
        _mockCourseService
            .Setup(s => s.GetCountAsync(CourseStatus.Active, teacherId, DayOfWeek.Monday, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        // Act
        await _controller.GetCount(CourseStatus.Active, teacherId, DayOfWeek.Monday, 1, CancellationToken.None);

        // Assert
        _mockCourseService.Verify(s => s.GetCountAsync(
            CourseStatus.Active, teacherId, DayOfWeek.Monday, 1, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region GetAll Tests

    [Fact]
    public async Task GetAll_ReturnsOkWithCourses()
    {
        // Arrange
        var courses = new List<CourseDto>
        {
            new() { Id = Guid.NewGuid(), TeacherName = "John Doe", Status = CourseStatus.Active },
            new() { Id = Guid.NewGuid(), TeacherName = "Jane Smith", Status = CourseStatus.Active }
        };

        _mockCourseService
            .Setup(s => s.GetAllAsync(null, null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(courses);

        // Act
        var result = await _controller.GetAll(null, null, null, null, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedCourses = Assert.IsAssignableFrom<List<CourseDto>>(okResult.Value);
        Assert.Equal(2, returnedCourses.Count);
    }

    [Fact]
    public async Task GetAll_PassesFiltersToService()
    {
        // Arrange
        var teacherId = Guid.NewGuid();
        _mockCourseService
            .Setup(s => s.GetAllAsync(CourseStatus.Active, teacherId, DayOfWeek.Monday, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CourseDto>());

        // Act
        await _controller.GetAll(CourseStatus.Active, teacherId, DayOfWeek.Monday, 1, CancellationToken.None);

        // Assert
        _mockCourseService.Verify(s => s.GetAllAsync(
            CourseStatus.Active, teacherId, DayOfWeek.Monday, 1, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetById_WithValidId_ReturnsOkWithCourse()
    {
        // Arrange
        var courseId = Guid.NewGuid();
        var courseDto = new CourseDto { Id = courseId, TeacherName = "John Doe", Status = CourseStatus.Active };

        _mockCourseService
            .Setup(s => s.GetByIdAsync(courseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(courseDto);

        // Act
        var result = await _controller.GetById(courseId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedCourse = Assert.IsType<CourseDto>(okResult.Value);
        Assert.Equal(courseId, returnedCourse.Id);
    }

    [Fact]
    public async Task GetById_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        _mockCourseService
            .Setup(s => s.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CourseDto?)null);

        // Act
        var result = await _controller.GetById(Guid.NewGuid(), CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    #endregion

    #region Create Tests

    [Fact]
    public async Task Create_WithValidData_ReturnsCreatedAtAction()
    {
        // Arrange
        var courseId = Guid.NewGuid();
        var teacherId = Guid.NewGuid();
        var courseDto = new CourseDto { Id = courseId, TeacherId = teacherId, TeacherName = "John Doe", Status = CourseStatus.Active };

        _mockCourseService
            .Setup(s => s.CreateAsync(It.IsAny<CreateCourseDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((courseDto, (string?)null));

        var dto = new CreateCourseDto
        {
            TeacherId = teacherId,
            CourseTypeId = Guid.NewGuid(),
            DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(10, 0),
            StartDate = DateOnly.FromDateTime(DateTime.Today)
        };

        // Act
        var result = await _controller.Create(dto, CancellationToken.None);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(CoursesController.GetById), createdResult.ActionName);
        var returnedCourse = Assert.IsType<CourseDto>(createdResult.Value);
        Assert.Equal(teacherId, returnedCourse.TeacherId);
    }

    [Fact]
    public async Task Create_WithInvalidTeacher_ReturnsBadRequest()
    {
        // Arrange
        _mockCourseService
            .Setup(s => s.CreateAsync(It.IsAny<CreateCourseDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((CourseDto?)null, "Teacher not found"));

        var dto = new CreateCourseDto
        {
            TeacherId = Guid.NewGuid(),
            CourseTypeId = Guid.NewGuid(),
            DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(10, 0),
            StartDate = DateOnly.FromDateTime(DateTime.Today)
        };

        // Act
        var result = await _controller.Create(dto, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task Create_WithInvalidCourseType_ReturnsBadRequest()
    {
        // Arrange
        _mockCourseService
            .Setup(s => s.CreateAsync(It.IsAny<CreateCourseDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((CourseDto?)null, "Course type not found"));

        var dto = new CreateCourseDto
        {
            TeacherId = Guid.NewGuid(),
            CourseTypeId = Guid.NewGuid(),
            DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(10, 0),
            StartDate = DateOnly.FromDateTime(DateTime.Today)
        };

        // Act
        var result = await _controller.Create(dto, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequestResult.Value);
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_WithValidData_ReturnsOkWithUpdatedCourse()
    {
        // Arrange
        var courseId = Guid.NewGuid();
        var courseDto = new CourseDto { Id = courseId, DayOfWeek = DayOfWeek.Wednesday, Status = CourseStatus.Active };

        _mockCourseService
            .Setup(s => s.UpdateAsync(courseId, It.IsAny<UpdateCourseDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((courseDto, false));

        var dto = new UpdateCourseDto
        {
            TeacherId = Guid.NewGuid(),
            CourseTypeId = Guid.NewGuid(),
            DayOfWeek = DayOfWeek.Wednesday,
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(11, 0),
            Frequency = CourseFrequency.Weekly,
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            Status = CourseStatus.Active
        };

        // Act
        var result = await _controller.Update(courseId, dto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedCourse = Assert.IsType<CourseDto>(okResult.Value);
        Assert.Equal(DayOfWeek.Wednesday, returnedCourse.DayOfWeek);
    }

    [Fact]
    public async Task Update_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        _mockCourseService
            .Setup(s => s.UpdateAsync(It.IsAny<Guid>(), It.IsAny<UpdateCourseDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((CourseDto?)null, true));

        var dto = new UpdateCourseDto
        {
            TeacherId = Guid.NewGuid(),
            CourseTypeId = Guid.NewGuid(),
            DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(10, 0),
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            Status = CourseStatus.Active
        };

        // Act
        var result = await _controller.Update(Guid.NewGuid(), dto, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Update_StatusChange_ReturnsUpdatedStatus()
    {
        // Arrange
        var courseId = Guid.NewGuid();
        var courseDto = new CourseDto { Id = courseId, Status = CourseStatus.Completed };

        _mockCourseService
            .Setup(s => s.UpdateAsync(courseId, It.IsAny<UpdateCourseDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((courseDto, false));

        var dto = new UpdateCourseDto
        {
            TeacherId = Guid.NewGuid(),
            CourseTypeId = Guid.NewGuid(),
            DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(10, 0),
            Frequency = CourseFrequency.Weekly,
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            Status = CourseStatus.Completed
        };

        // Act
        var result = await _controller.Update(courseId, dto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedCourse = Assert.IsType<CourseDto>(okResult.Value);
        Assert.Equal(CourseStatus.Completed, returnedCourse.Status);
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_WithValidId_ReturnsNoContent()
    {
        // Arrange
        var courseId = Guid.NewGuid();
        _mockCourseService
            .Setup(s => s.DeleteAsync(courseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Delete(courseId, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        _mockCourseService
            .Setup(s => s.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.Delete(Guid.NewGuid(), CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    #endregion
}
