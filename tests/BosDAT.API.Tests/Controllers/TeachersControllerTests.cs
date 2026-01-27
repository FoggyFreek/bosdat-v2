using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using BosDAT.API.Controllers;
using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Interfaces;
using BosDAT.API.Tests.Helpers;
using BosDAT.Infrastructure.Data;

namespace BosDAT.API.Tests.Controllers;

public class TeachersControllerTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly ApplicationDbContext _context;
    private readonly TeachersController _controller;

    public TeachersControllerTests()
    {
        _mockUnitOfWork = MockHelpers.CreateMockUnitOfWork();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);

        _controller = new TeachersController(_mockUnitOfWork.Object, _context);
    }

    private static Teacher CreateTeacher(
        string firstName = "John",
        string lastName = "Doe",
        string email = "john.doe@example.com",
        bool isActive = true)
    {
        return new Teacher
        {
            Id = Guid.NewGuid(),
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            Phone = "123-456-7890",
            HourlyRate = 50m,
            IsActive = isActive,
            Role = TeacherRole.Teacher,
            TeacherInstruments = new List<TeacherInstrument>(),
            TeacherCourseTypes = new List<TeacherCourseType>(),
            Courses = new List<Course>()
        };
    }

    private static Instrument CreateInstrument(int id = 1, string name = "Piano")
    {
        return new Instrument
        {
            Id = id,
            Name = name,
            Category = InstrumentCategory.Keyboard,
            IsActive = true
        };
    }

    #region GetAll Tests

    [Fact]
    public async Task GetAll_WithNoFilters_ReturnsAllTeachers()
    {
        // Arrange
        var teachers = new List<Teacher>
        {
            CreateTeacher("John", "Doe", "john@example.com"),
            CreateTeacher("Jane", "Smith", "jane@example.com")
        };
        foreach (var teacher in teachers)
        {
            teacher.TeacherInstruments = new List<TeacherInstrument>();
            teacher.TeacherCourseTypes = new List<TeacherCourseType>();
        }

        var mockTeacherRepo = MockHelpers.CreateMockTeacherRepository(teachers);
        _mockUnitOfWork.Setup(u => u.Teachers).Returns(mockTeacherRepo.Object);

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
        var activeTeacher = CreateTeacher("John", "Doe", "john@example.com", isActive: true);
        var inactiveTeacher = CreateTeacher("Jane", "Smith", "jane@example.com", isActive: false);
        var teachers = new List<Teacher> { activeTeacher, inactiveTeacher };

        var mockTeacherRepo = MockHelpers.CreateMockTeacherRepository(teachers);
        _mockUnitOfWork.Setup(u => u.Teachers).Returns(mockTeacherRepo.Object);

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
        var instrument = CreateInstrument(1, "Piano");
        var teacherWithPiano = CreateTeacher("John", "Doe", "john@example.com");
        teacherWithPiano.TeacherInstruments = new List<TeacherInstrument>
        {
            new() { TeacherId = teacherWithPiano.Id, InstrumentId = 1, Instrument = instrument }
        };
        var teacherWithoutPiano = CreateTeacher("Jane", "Smith", "jane@example.com");
        teacherWithoutPiano.TeacherInstruments = new List<TeacherInstrument>();

        var teachers = new List<Teacher> { teacherWithPiano, teacherWithoutPiano };

        var mockTeacherRepo = MockHelpers.CreateMockTeacherRepository(teachers);
        _mockUnitOfWork.Setup(u => u.Teachers).Returns(mockTeacherRepo.Object);

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
        var instrument = CreateInstrument(1, "Piano");
        var courseType = new CourseType
        {
            Id = courseTypeId,
            Name = "Beginner Piano",
            InstrumentId = 1,
            Instrument = instrument,
            IsActive = true,
            Type = CourseTypeCategory.Individual,
            MaxStudents = 1,
            DurationMinutes = 30
        };

        var teacherWithCourseType = CreateTeacher("John", "Doe", "john@example.com");
        teacherWithCourseType.TeacherCourseTypes = new List<TeacherCourseType>
        {
            new() { TeacherId = teacherWithCourseType.Id, CourseTypeId = courseTypeId, CourseType = courseType }
        };
        var teacherWithoutCourseType = CreateTeacher("Jane", "Smith", "jane@example.com");
        teacherWithoutCourseType.TeacherCourseTypes = new List<TeacherCourseType>();

        var teachers = new List<Teacher> { teacherWithCourseType, teacherWithoutCourseType };

        var mockTeacherRepo = MockHelpers.CreateMockTeacherRepository(teachers);
        _mockUnitOfWork.Setup(u => u.Teachers).Returns(mockTeacherRepo.Object);

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
        var teacher = CreateTeacher();
        var teachers = new List<Teacher> { teacher };
        var mockTeacherRepo = MockHelpers.CreateMockTeacherRepository(teachers);
        _mockUnitOfWork.Setup(u => u.Teachers).Returns(mockTeacherRepo.Object);

        // Act
        var result = await _controller.GetById(teacher.Id, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedTeacher = Assert.IsType<TeacherDto>(okResult.Value);
        Assert.Equal(teacher.Id, returnedTeacher.Id);
        Assert.Equal(teacher.Email, returnedTeacher.Email);
    }

    [Fact]
    public async Task GetById_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var teachers = new List<Teacher>();
        var mockTeacherRepo = MockHelpers.CreateMockTeacherRepository(teachers);
        _mockUnitOfWork.Setup(u => u.Teachers).Returns(mockTeacherRepo.Object);

        // Act
        var result = await _controller.GetById(Guid.NewGuid(), CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    #endregion

    #region GetWithCourses Tests

    [Fact]
    public async Task GetWithCourses_WithValidId_ReturnsTeacherWithCourses()
    {
        // Arrange
        var instrument = CreateInstrument(1, "Piano");
        var courseType = new CourseType
        {
            Id = Guid.NewGuid(),
            Name = "Beginner Piano",
            InstrumentId = 1,
            Instrument = instrument,
            IsActive = true,
            Type = CourseTypeCategory.Individual,
            MaxStudents = 1,
            DurationMinutes = 30
        };

        var teacher = CreateTeacher();
        teacher.Courses = new List<Course>
        {
            new()
            {
                Id = Guid.NewGuid(),
                TeacherId = teacher.Id,
                Teacher = teacher,
                CourseTypeId = courseType.Id,
                CourseType = courseType,
                DayOfWeek = DayOfWeek.Monday,
                StartTime = new TimeOnly(9, 0),
                EndTime = new TimeOnly(10, 0),
                Status = CourseStatus.Active,
                Enrollments = new List<Enrollment>()
            }
        };

        var teachers = new List<Teacher> { teacher };
        var mockTeacherRepo = MockHelpers.CreateMockTeacherRepository(teachers);
        _mockUnitOfWork.Setup(u => u.Teachers).Returns(mockTeacherRepo.Object);

        // Act
        var result = await _controller.GetWithCourses(teacher.Id, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetWithCourses_WithNoCourses_ReturnsTeacherWithEmptyCourses()
    {
        // Arrange
        var teacher = CreateTeacher();
        teacher.Courses = new List<Course>();

        var teachers = new List<Teacher> { teacher };
        var mockTeacherRepo = MockHelpers.CreateMockTeacherRepository(teachers);
        _mockUnitOfWork.Setup(u => u.Teachers).Returns(mockTeacherRepo.Object);

        // Act
        var result = await _controller.GetWithCourses(teacher.Id, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetWithCourses_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var teachers = new List<Teacher>();
        var mockTeacherRepo = MockHelpers.CreateMockTeacherRepository(teachers);
        _mockUnitOfWork.Setup(u => u.Teachers).Returns(mockTeacherRepo.Object);

        // Act
        var result = await _controller.GetWithCourses(Guid.NewGuid(), CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    #endregion

    #region Create Tests

    [Fact]
    public async Task Create_WithValidData_ReturnsCreatedTeacher()
    {
        // Arrange
        var instrument = CreateInstrument(1, "Piano");
        await _context.Instruments.AddAsync(instrument);
        await _context.SaveChangesAsync();

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

        var teachers = new List<Teacher>();
        var mockTeacherRepo = MockHelpers.CreateMockTeacherRepository(teachers);
        _mockUnitOfWork.Setup(u => u.Teachers).Returns(mockTeacherRepo.Object);

        // Setup to return created teacher with populated instruments
        mockTeacherRepo.Setup(r => r.GetWithInstrumentsAndCourseTypesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) =>
            {
                var created = new Teacher
                {
                    Id = id,
                    FirstName = dto.FirstName,
                    LastName = dto.LastName,
                    Email = dto.Email,
                    HourlyRate = dto.HourlyRate,
                    Role = dto.Role,
                    IsActive = true,
                    TeacherInstruments = new List<TeacherInstrument>
                    {
                        new() { TeacherId = id, InstrumentId = 1, Instrument = instrument }
                    },
                    TeacherCourseTypes = new List<TeacherCourseType>()
                };
                return created;
            });

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
        var existingTeacher = CreateTeacher("Existing", "Teacher", "existing@example.com");
        var teachers = new List<Teacher> { existingTeacher };
        var mockTeacherRepo = MockHelpers.CreateMockTeacherRepository(teachers);
        _mockUnitOfWork.Setup(u => u.Teachers).Returns(mockTeacherRepo.Object);

        var dto = new CreateTeacherDto
        {
            FirstName = "New",
            LastName = "Teacher",
            Email = "existing@example.com",
            HourlyRate = 50m,
            InstrumentIds = new List<int>(),
            CourseTypeIds = new List<Guid>()
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
        var instrument = CreateInstrument(1, "Piano");
        await _context.Instruments.AddAsync(instrument);
        await _context.SaveChangesAsync();

        var teachers = new List<Teacher>();
        var mockTeacherRepo = MockHelpers.CreateMockTeacherRepository(teachers);
        _mockUnitOfWork.Setup(u => u.Teachers).Returns(mockTeacherRepo.Object);

        var dto = new CreateTeacherDto
        {
            FirstName = "New",
            LastName = "Teacher",
            Email = "new@example.com",
            HourlyRate = 50m,
            InstrumentIds = new List<int> { 1 },
            CourseTypeIds = new List<Guid> { Guid.NewGuid() } // Non-existent course type
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
    public async Task Update_WithValidData_ReturnsUpdatedTeacher()
    {
        // Arrange
        var instrument = CreateInstrument(1, "Piano");
        await _context.Instruments.AddAsync(instrument);
        await _context.SaveChangesAsync();

        var teacher = CreateTeacher();
        teacher.TeacherInstruments = new List<TeacherInstrument>();
        teacher.TeacherCourseTypes = new List<TeacherCourseType>();
        var teachers = new List<Teacher> { teacher };
        var mockTeacherRepo = MockHelpers.CreateMockTeacherRepository(teachers);
        _mockUnitOfWork.Setup(u => u.Teachers).Returns(mockTeacherRepo.Object);

        mockTeacherRepo.Setup(r => r.GetWithInstrumentsAndCourseTypesAsync(teacher.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) =>
            {
                teacher.FirstName = "Updated";
                teacher.LastName = "Name";
                teacher.TeacherInstruments = new List<TeacherInstrument>
                {
                    new() { TeacherId = id, InstrumentId = 1, Instrument = instrument }
                };
                return teacher;
            });

        var dto = new UpdateTeacherDto
        {
            FirstName = "Updated",
            LastName = "Name",
            Email = teacher.Email,
            HourlyRate = 55m,
            IsActive = true,
            Role = TeacherRole.Teacher,
            InstrumentIds = new List<int> { 1 },
            CourseTypeIds = new List<Guid>()
        };

        // Act
        var result = await _controller.Update(teacher.Id, dto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedTeacher = Assert.IsType<TeacherDto>(okResult.Value);
        Assert.Equal("Updated", returnedTeacher.FirstName);
    }

    [Fact]
    public async Task Update_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var teachers = new List<Teacher>();
        var mockTeacherRepo = MockHelpers.CreateMockTeacherRepository(teachers);
        _mockUnitOfWork.Setup(u => u.Teachers).Returns(mockTeacherRepo.Object);

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

        // Act
        var result = await _controller.Update(Guid.NewGuid(), dto, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Update_WithDuplicateEmail_ReturnsBadRequest()
    {
        // Arrange
        var teacher1 = CreateTeacher("John", "Doe", "john@example.com");
        teacher1.TeacherInstruments = new List<TeacherInstrument>();
        teacher1.TeacherCourseTypes = new List<TeacherCourseType>();
        var teacher2 = CreateTeacher("Jane", "Smith", "jane@example.com");
        var teachers = new List<Teacher> { teacher1, teacher2 };
        var mockTeacherRepo = MockHelpers.CreateMockTeacherRepository(teachers);
        _mockUnitOfWork.Setup(u => u.Teachers).Returns(mockTeacherRepo.Object);

        var dto = new UpdateTeacherDto
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "jane@example.com", // Trying to use teacher2's email
            HourlyRate = 55m,
            IsActive = true,
            Role = TeacherRole.Teacher,
            InstrumentIds = new List<int>(),
            CourseTypeIds = new List<Guid>()
        };

        // Act
        var result = await _controller.Update(teacher1.Id, dto, CancellationToken.None);

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
        var teacher = CreateTeacher();
        var teachers = new List<Teacher> { teacher };
        var mockTeacherRepo = MockHelpers.CreateMockTeacherRepository(teachers);
        _mockUnitOfWork.Setup(u => u.Teachers).Returns(mockTeacherRepo.Object);

        // Act
        var result = await _controller.Delete(teacher.Id, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
        Assert.False(teacher.IsActive);
    }

    [Fact]
    public async Task Delete_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var teachers = new List<Teacher>();
        var mockTeacherRepo = MockHelpers.CreateMockTeacherRepository(teachers);
        _mockUnitOfWork.Setup(u => u.Teachers).Returns(mockTeacherRepo.Object);

        // Act
        var result = await _controller.Delete(Guid.NewGuid(), CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    #endregion

    #region GetAvailableCourseTypes Tests

    [Fact]
    public async Task GetAvailableCourseTypes_WithValidTeacherAndInstruments_ReturnsCourseTypes()
    {
        // Arrange
        var teacher = CreateTeacher();
        var teachers = new List<Teacher> { teacher };
        var mockTeacherRepo = MockHelpers.CreateMockTeacherRepository(teachers);
        _mockUnitOfWork.Setup(u => u.Teachers).Returns(mockTeacherRepo.Object);

        var instrument = CreateInstrument(1, "Piano");
        var courseType = new CourseType
        {
            Id = Guid.NewGuid(),
            Name = "Beginner Piano",
            InstrumentId = 1,
            Instrument = instrument,
            IsActive = true,
            Type = CourseTypeCategory.Individual,
            MaxStudents = 1,
            DurationMinutes = 30
        };
        await _context.Instruments.AddAsync(instrument);
        await _context.CourseTypes.AddAsync(courseType);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetAvailableCourseTypes(teacher.Id, "1", CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var courseTypes = Assert.IsAssignableFrom<IEnumerable<CourseTypeSimpleDto>>(okResult.Value);
        Assert.Single(courseTypes);
    }

    [Fact]
    public async Task GetAvailableCourseTypes_WithNoInstrumentIds_ReturnsEmptyList()
    {
        // Arrange
        var teacher = CreateTeacher();
        var teachers = new List<Teacher> { teacher };
        var mockTeacherRepo = MockHelpers.CreateMockTeacherRepository(teachers);
        _mockUnitOfWork.Setup(u => u.Teachers).Returns(mockTeacherRepo.Object);

        // Act
        var result = await _controller.GetAvailableCourseTypes(teacher.Id, null, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var courseTypes = Assert.IsAssignableFrom<IEnumerable<CourseTypeSimpleDto>>(okResult.Value);
        Assert.Empty(courseTypes);
    }

    [Fact]
    public async Task GetAvailableCourseTypes_WithInvalidTeacherId_ReturnsNotFound()
    {
        // Arrange
        var teachers = new List<Teacher>();
        var mockTeacherRepo = MockHelpers.CreateMockTeacherRepository(teachers);
        _mockUnitOfWork.Setup(u => u.Teachers).Returns(mockTeacherRepo.Object);

        // Act
        var result = await _controller.GetAvailableCourseTypes(Guid.NewGuid(), "1", CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    #endregion
}
