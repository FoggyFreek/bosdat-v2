using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using BosDAT.API.Controllers;
using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Interfaces;

namespace BosDAT.API.Tests.Controllers;

public class TeachersControllerTests
{
    private readonly Mock<ITeacherService> _mockTeacherService;
    private readonly TeachersController _controller;

    public TeachersControllerTests()
    {
        _mockTeacherService = new Mock<ITeacherService>();
        _controller = new TeachersController(_mockTeacherService.Object);
    }

    #region GetAll Tests

    [Fact]
    public async Task GetAll_WithNoFilters_ReturnsAllTeachers()
    {
        // Arrange
        var teachers = new List<TeacherListDto>
        {
            new() { Id = Guid.NewGuid(), FullName = "John Doe", Email = "john@example.com", IsActive = true, Role = TeacherRole.Teacher, Instruments = new List<string>(), CourseTypes = new List<string>() },
            new() { Id = Guid.NewGuid(), FullName = "Jane Smith", Email = "jane@example.com", IsActive = true, Role = TeacherRole.Teacher, Instruments = new List<string>(), CourseTypes = new List<string>() }
        };

        _mockTeacherService.Setup(s => s.GetAllAsync(null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(teachers);

        // Act
        var result = await _controller.GetAll(null, null, null, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedTeachers = Assert.IsAssignableFrom<IEnumerable<TeacherListDto>>(okResult.Value);
        Assert.Equal(2, returnedTeachers.Count());
    }

    [Fact]
    public async Task GetAll_WithActiveOnlyFilter_ReturnsOnlyActiveTeachers()
    {
        // Arrange
        var teachers = new List<TeacherListDto>
        {
            new() { Id = Guid.NewGuid(), FullName = "John Doe", Email = "john@example.com", IsActive = true, Role = TeacherRole.Teacher, Instruments = new List<string>(), CourseTypes = new List<string>() }
        };

        _mockTeacherService.Setup(s => s.GetAllAsync(true, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(teachers);

        // Act
        var result = await _controller.GetAll(activeOnly: true, null, null, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedTeachers = Assert.IsAssignableFrom<IEnumerable<TeacherListDto>>(okResult.Value);
        Assert.Single(returnedTeachers);
        Assert.True(returnedTeachers.First().IsActive);
    }

    [Fact]
    public async Task GetAll_WithInstrumentFilter_ReturnsTeachersWithInstrument()
    {
        // Arrange
        var teachers = new List<TeacherListDto>
        {
            new() { Id = Guid.NewGuid(), FullName = "John Doe", Email = "john@example.com", IsActive = true, Role = TeacherRole.Teacher, Instruments = new List<string> { "Piano" }, CourseTypes = new List<string>() }
        };

        _mockTeacherService.Setup(s => s.GetAllAsync(null, 1, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(teachers);

        // Act
        var result = await _controller.GetAll(null, instrumentId: 1, null, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedTeachers = Assert.IsAssignableFrom<IEnumerable<TeacherListDto>>(okResult.Value);
        Assert.Single(returnedTeachers);
    }

    [Fact]
    public async Task GetAll_WithCourseTypeFilter_ReturnsTeachersWithCourseType()
    {
        // Arrange
        var courseTypeId = Guid.NewGuid();
        var teachers = new List<TeacherListDto>
        {
            new() { Id = Guid.NewGuid(), FullName = "John Doe", Email = "john@example.com", IsActive = true, Role = TeacherRole.Teacher, Instruments = new List<string> { "Piano" }, CourseTypes = new List<string> { "Beginner Piano" } }
        };

        _mockTeacherService.Setup(s => s.GetAllAsync(null, null, courseTypeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(teachers);

        // Act
        var result = await _controller.GetAll(null, null, CourseTypeId: courseTypeId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedTeachers = Assert.IsAssignableFrom<IEnumerable<TeacherListDto>>(okResult.Value);
        Assert.Single(returnedTeachers);
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetById_WithValidId_ReturnsTeacher()
    {
        // Arrange
        var teacherId = Guid.NewGuid();
        var teacherDto = new TeacherDto
        {
            Id = teacherId,
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            IsActive = true,
            Role = TeacherRole.Teacher,
            Instruments = new List<InstrumentDto>(),
            CourseTypes = new List<CourseTypeSimpleDto>()
        };

        _mockTeacherService.Setup(s => s.GetByIdAsync(teacherId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(teacherDto);

        // Act
        var result = await _controller.GetById(teacherId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedTeacher = Assert.IsType<TeacherDto>(okResult.Value);
        Assert.Equal(teacherId, returnedTeacher.Id);
        Assert.Equal("john.doe@example.com", returnedTeacher.Email);
    }

    [Fact]
    public async Task GetById_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var invalidId = Guid.NewGuid();
        _mockTeacherService.Setup(s => s.GetByIdAsync(invalidId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TeacherDto?)null);

        // Act
        var result = await _controller.GetById(invalidId, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    #endregion

    #region GetWithCourses Tests

    [Fact]
    public async Task GetWithCourses_WithValidId_ReturnsTeacherWithCourses()
    {
        // Arrange
        var teacherId = Guid.NewGuid();
        var teacherDto = new TeacherDto
        {
            Id = teacherId,
            FirstName = "John",
            LastName = "Doe",
            FullName = "John Doe",
            Email = "john@example.com",
            Instruments = new List<InstrumentDto>(),
            CourseTypes = new List<CourseTypeSimpleDto>()
        };
        var courses = new List<CourseListDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                TeacherName = "John Doe",
                CourseTypeName = "Beginner Piano",
                InstrumentName = "Piano",
                DayOfWeek = DayOfWeek.Monday,
                StartTime = new TimeOnly(9, 0),
                EndTime = new TimeOnly(10, 0),
                Status = CourseStatus.Active,
                EnrollmentCount = 0
            }
        };

        _mockTeacherService.Setup(s => s.GetWithCoursesAsync(teacherId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((teacherDto, courses));

        // Act
        var result = await _controller.GetWithCourses(teacherId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetWithCourses_WithNoCourses_ReturnsTeacherWithEmptyCourses()
    {
        // Arrange
        var teacherId = Guid.NewGuid();
        var teacherDto = new TeacherDto
        {
            Id = teacherId,
            FirstName = "John",
            LastName = "Doe",
            FullName = "John Doe",
            Email = "john@example.com",
            Instruments = new List<InstrumentDto>(),
            CourseTypes = new List<CourseTypeSimpleDto>()
        };
        var courses = new List<CourseListDto>();

        _mockTeacherService.Setup(s => s.GetWithCoursesAsync(teacherId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((teacherDto, courses));

        // Act
        var result = await _controller.GetWithCourses(teacherId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetWithCourses_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var invalidId = Guid.NewGuid();
        _mockTeacherService.Setup(s => s.GetWithCoursesAsync(invalidId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((TeacherDto, List<CourseListDto>)?)null);

        // Act
        var result = await _controller.GetWithCourses(invalidId, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    #endregion

    #region Create Tests

    [Fact]
    public async Task Create_WithValidData_ReturnsCreatedTeacher()
    {
        // Arrange
        var dto = new CreateTeacherDto
        {
            FirstName = "New",
            LastName = "Teacher",
            Email = "new@example.com",
            HourlyRate = 50m,
            Role = TeacherRole.Teacher,
            InstrumentIds = new List<int> { 1 },
            CourseTypeIds = new List<Guid>()
        };

        var createdTeacher = new TeacherDto
        {
            Id = Guid.NewGuid(),
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = dto.Email,
            HourlyRate = dto.HourlyRate,
            Role = dto.Role,
            IsActive = true,
            Instruments = new List<InstrumentDto>(),
            CourseTypes = new List<CourseTypeSimpleDto>()
        };

        _mockTeacherService.Setup(s => s.CreateAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdTeacher);

        // Act
        var result = await _controller.Create(dto, CancellationToken.None);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(TeachersController.GetById), createdResult.ActionName);
        var returnedTeacher = Assert.IsType<TeacherDto>(createdResult.Value);
        Assert.Equal(dto.Email, returnedTeacher.Email);
    }

    [Fact]
    public async Task Create_WithDuplicateEmail_ReturnsBadRequest()
    {
        // Arrange
        var dto = new CreateTeacherDto
        {
            FirstName = "New",
            LastName = "Teacher",
            Email = "existing@example.com",
            HourlyRate = 50m,
            InstrumentIds = new List<int>(),
            CourseTypeIds = new List<Guid>()
        };

        _mockTeacherService.Setup(s => s.CreateAsync(dto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("A teacher with this email already exists"));

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
        var dto = new CreateTeacherDto
        {
            FirstName = "New",
            LastName = "Teacher",
            Email = "new@example.com",
            HourlyRate = 50m,
            InstrumentIds = new List<int> { 1 },
            CourseTypeIds = new List<Guid> { Guid.NewGuid() }
        };

        _mockTeacherService.Setup(s => s.CreateAsync(dto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("One or more lesson types not found"));

        // Act
        var result = await _controller.Create(dto, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task Create_WithInactiveCourseType_ReturnsBadRequest()
    {
        // Arrange
        var dto = new CreateTeacherDto
        {
            FirstName = "New",
            LastName = "Teacher",
            Email = "new@example.com",
            HourlyRate = 50m,
            InstrumentIds = new List<int> { 1 },
            CourseTypeIds = new List<Guid> { Guid.NewGuid() }
        };

        _mockTeacherService.Setup(s => s.CreateAsync(dto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Cannot assign inactive lesson types: Inactive Piano Lesson"));

        // Act
        var result = await _controller.Create(dto, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task Create_WithMismatchedInstrumentAndCourseType_ReturnsBadRequest()
    {
        // Arrange
        var dto = new CreateTeacherDto
        {
            FirstName = "New",
            LastName = "Teacher",
            Email = "new@example.com",
            HourlyRate = 50m,
            InstrumentIds = new List<int> { 1 },
            CourseTypeIds = new List<Guid> { Guid.NewGuid() }
        };

        _mockTeacherService.Setup(s => s.CreateAsync(dto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Lesson types must match teacher's instruments: Guitar Lesson"));

        // Act
        var result = await _controller.Create(dto, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequestResult.Value);
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_WithValidData_ReturnsUpdatedTeacher()
    {
        // Arrange
        var teacherId = Guid.NewGuid();
        var dto = new UpdateTeacherDto
        {
            FirstName = "Updated",
            LastName = "Name",
            Email = "updated@example.com",
            HourlyRate = 55m,
            IsActive = true,
            Role = TeacherRole.Teacher,
            InstrumentIds = new List<int> { 1 },
            CourseTypeIds = new List<Guid>()
        };

        var updatedTeacher = new TeacherDto
        {
            Id = teacherId,
            FirstName = "Updated",
            LastName = "Name",
            Email = "updated@example.com",
            HourlyRate = 55m,
            IsActive = true,
            Role = TeacherRole.Teacher,
            Instruments = new List<InstrumentDto>(),
            CourseTypes = new List<CourseTypeSimpleDto>()
        };

        _mockTeacherService.Setup(s => s.UpdateAsync(teacherId, dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedTeacher);

        // Act
        var result = await _controller.Update(teacherId, dto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedTeacher = Assert.IsType<TeacherDto>(okResult.Value);
        Assert.Equal("Updated", returnedTeacher.FirstName);
    }

    [Fact]
    public async Task Update_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var invalidId = Guid.NewGuid();
        var dto = new UpdateTeacherDto
        {
            FirstName = "Updated",
            LastName = "Name",
            Email = "updated@example.com",
            HourlyRate = 55m,
            IsActive = true,
            Role = TeacherRole.Teacher,
            InstrumentIds = new List<int>(),
            CourseTypeIds = new List<Guid>()
        };

        _mockTeacherService.Setup(s => s.UpdateAsync(invalidId, dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TeacherDto?)null);

        // Act
        var result = await _controller.Update(invalidId, dto, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Update_WithDuplicateEmail_ReturnsBadRequest()
    {
        // Arrange
        var teacherId = Guid.NewGuid();
        var dto = new UpdateTeacherDto
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "jane@example.com",
            HourlyRate = 55m,
            IsActive = true,
            Role = TeacherRole.Teacher,
            InstrumentIds = new List<int>(),
            CourseTypeIds = new List<Guid>()
        };

        _mockTeacherService.Setup(s => s.UpdateAsync(teacherId, dto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("A teacher with this email already exists"));

        // Act
        var result = await _controller.Update(teacherId, dto, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task Update_WithInactiveCourseType_ReturnsBadRequest()
    {
        // Arrange
        var teacherId = Guid.NewGuid();
        var dto = new UpdateTeacherDto
        {
            FirstName = "Updated",
            LastName = "Teacher",
            Email = "teacher@example.com",
            HourlyRate = 50m,
            IsActive = true,
            Role = TeacherRole.Teacher,
            InstrumentIds = new List<int> { 1 },
            CourseTypeIds = new List<Guid> { Guid.NewGuid() }
        };

        _mockTeacherService.Setup(s => s.UpdateAsync(teacherId, dto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Cannot assign inactive lesson types: Inactive Piano Lesson"));

        // Act
        var result = await _controller.Update(teacherId, dto, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task Update_WithMismatchedInstrumentAndCourseType_ReturnsBadRequest()
    {
        // Arrange
        var teacherId = Guid.NewGuid();
        var dto = new UpdateTeacherDto
        {
            FirstName = "Updated",
            LastName = "Teacher",
            Email = "teacher@example.com",
            HourlyRate = 50m,
            IsActive = true,
            Role = TeacherRole.Teacher,
            InstrumentIds = new List<int> { 1 },
            CourseTypeIds = new List<Guid> { Guid.NewGuid() }
        };

        _mockTeacherService.Setup(s => s.UpdateAsync(teacherId, dto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Lesson types must match teacher's instruments: Guitar Lesson"));

        // Act
        var result = await _controller.Update(teacherId, dto, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequestResult.Value);
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_WithValidId_ReturnsNoContent()
    {
        // Arrange
        var teacherId = Guid.NewGuid();
        _mockTeacherService.Setup(s => s.DeleteAsync(teacherId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Delete(teacherId, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var invalidId = Guid.NewGuid();
        _mockTeacherService.Setup(s => s.DeleteAsync(invalidId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.Delete(invalidId, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    #endregion

    #region GetAvailableCourseTypes Tests

    [Fact]
    public async Task GetAvailableCourseTypes_WithValidTeacherAndInstruments_ReturnsCourseTypes()
    {
        // Arrange
        var teacherId = Guid.NewGuid();
        var courseTypes = new List<CourseTypeSimpleDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Beginner Piano",
                InstrumentId = 1,
                InstrumentName = "Piano",
                DurationMinutes = 30,
                Type = CourseTypeCategory.Individual
            }
        };

        _mockTeacherService.Setup(s => s.GetAvailableCourseTypesAsync(teacherId, "1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(courseTypes);

        // Act
        var result = await _controller.GetAvailableCourseTypes(teacherId, "1", CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedCourseTypes = Assert.IsAssignableFrom<IEnumerable<CourseTypeSimpleDto>>(okResult.Value);
        Assert.Single(returnedCourseTypes);
    }

    [Fact]
    public async Task GetAvailableCourseTypes_WithNoInstrumentIds_ReturnsEmptyList()
    {
        // Arrange
        var teacherId = Guid.NewGuid();
        _mockTeacherService.Setup(s => s.GetAvailableCourseTypesAsync(teacherId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CourseTypeSimpleDto>());

        // Act
        var result = await _controller.GetAvailableCourseTypes(teacherId, null, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var courseTypes = Assert.IsAssignableFrom<IEnumerable<CourseTypeSimpleDto>>(okResult.Value);
        Assert.Empty(courseTypes);
    }

    [Fact]
    public async Task GetAvailableCourseTypes_WithInvalidTeacherId_ReturnsNotFound()
    {
        // Arrange
        var invalidId = Guid.NewGuid();
        _mockTeacherService.Setup(s => s.GetAvailableCourseTypesAsync(invalidId, "1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<CourseTypeSimpleDto>?)null);

        // Act
        var result = await _controller.GetAvailableCourseTypes(invalidId, "1", CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    #endregion

    #region GetAvailability Tests

    [Fact]
    public async Task GetAvailability_WithValidId_ReturnsAvailability()
    {
        // Arrange
        var teacherId = Guid.NewGuid();
        var availability = new List<TeacherAvailabilityDto>
        {
            new() { Id = Guid.NewGuid(), DayOfWeek = DayOfWeek.Monday, FromTime = new TimeOnly(9, 0), UntilTime = new TimeOnly(17, 0) }
        };

        _mockTeacherService.Setup(s => s.GetAvailabilityAsync(teacherId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(availability);

        // Act
        var result = await _controller.GetAvailability(teacherId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedAvailability = Assert.IsAssignableFrom<IEnumerable<TeacherAvailabilityDto>>(okResult.Value);
        Assert.Single(returnedAvailability);
    }

    [Fact]
    public async Task GetAvailability_WithInvalidId_ReturnsNotFound()
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

    #endregion

    #region UpdateAvailability Tests

    [Fact]
    public async Task UpdateAvailability_WithValidData_ReturnsUpdatedAvailability()
    {
        // Arrange
        var teacherId = Guid.NewGuid();
        var dtos = new List<UpdateTeacherAvailabilityDto>
        {
            new() { DayOfWeek = DayOfWeek.Monday, FromTime = new TimeOnly(9, 0), UntilTime = new TimeOnly(17, 0) }
        };

        var updatedAvailability = new List<TeacherAvailabilityDto>
        {
            new() { Id = Guid.NewGuid(), DayOfWeek = DayOfWeek.Monday, FromTime = new TimeOnly(9, 0), UntilTime = new TimeOnly(17, 0) }
        };

        _mockTeacherService.Setup(s => s.UpdateAvailabilityAsync(teacherId, dtos, It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedAvailability);

        // Act
        var result = await _controller.UpdateAvailability(teacherId, dtos, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedAvailability = Assert.IsAssignableFrom<IEnumerable<TeacherAvailabilityDto>>(okResult.Value);
        Assert.Single(returnedAvailability);
    }

    [Fact]
    public async Task UpdateAvailability_WithInvalidId_ReturnsNotFound()
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
    public async Task UpdateAvailability_WithTooManyEntries_ReturnsBadRequest()
    {
        // Arrange
        var teacherId = Guid.NewGuid();
        var dtos = new List<UpdateTeacherAvailabilityDto>();
        for (int i = 0; i < 8; i++)
        {
            dtos.Add(new UpdateTeacherAvailabilityDto { DayOfWeek = (DayOfWeek)i, FromTime = new TimeOnly(9, 0), UntilTime = new TimeOnly(17, 0) });
        }

        _mockTeacherService.Setup(s => s.UpdateAvailabilityAsync(teacherId, dtos, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Maximum of 7 availability entries allowed (one per day)"));

        // Act
        var result = await _controller.UpdateAvailability(teacherId, dtos, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequestResult.Value);
    }

    #endregion
}
