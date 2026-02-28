using Moq;
using Xunit;
using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Interfaces;
using BosDAT.Core.Interfaces.Repositories;
using BosDAT.Core.Interfaces.Services;
using BosDAT.Infrastructure.Services;
using BosDAT.API.Tests.Helpers;
using static BosDAT.API.Tests.Helpers.TestDataFactory;

namespace BosDAT.API.Tests.Services;

public class TeacherServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly TeacherService _service;

    public TeacherServiceTests()
    {
        _mockUnitOfWork = MockHelpers.CreateMockUnitOfWork();
        _service = new TeacherService(_mockUnitOfWork.Object);
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsTeacherDto()
    {
        var instrument = CreateInstrument();
        var teacher = CreateTeacher();
        teacher.TeacherInstruments = new List<TeacherInstrument>
        {
            new() { TeacherId = teacher.Id, InstrumentId = instrument.Id, Instrument = instrument }
        };
        teacher.TeacherCourseTypes = new List<TeacherCourseType>();

        var mockTeacherRepo = MockHelpers.CreateMockTeacherRepository(new List<Teacher> { teacher });
        _mockUnitOfWork.Setup(u => u.Teachers).Returns(mockTeacherRepo.Object);

        var result = await _service.GetByIdAsync(teacher.Id);

        Assert.NotNull(result);
        Assert.Equal(teacher.Id, result.Id);
        Assert.Equal(teacher.FullName, result.FullName);
        Assert.Equal(teacher.Email, result.Email);
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ReturnsNull()
    {
        var mockTeacherRepo = MockHelpers.CreateMockTeacherRepository(new List<Teacher>());
        _mockUnitOfWork.Setup(u => u.Teachers).Returns(mockTeacherRepo.Object);

        var result = await _service.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    #endregion

    #region GetWithCoursesAsync Tests

    [Fact]
    public async Task GetWithCoursesAsync_WithInvalidId_ReturnsNull()
    {
        var mockTeacherRepo = MockHelpers.CreateMockTeacherRepository(new List<Teacher>());
        _mockUnitOfWork.Setup(u => u.Teachers).Returns(mockTeacherRepo.Object);

        var result = await _service.GetWithCoursesAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetWithCoursesAsync_WithValidId_ReturnsTeacherAndCourses()
    {
        var instrument = CreateInstrument();
        var courseType = CreateCourseType(instrument);
        var teacher = CreateTeacher();
        teacher.TeacherInstruments = new List<TeacherInstrument>();
        teacher.TeacherCourseTypes = new List<TeacherCourseType>();

        var course = CreateCourse(teacher, courseType);
        teacher.Courses = new List<Course> { course };

        var mockTeacherRepo = MockHelpers.CreateMockTeacherRepository(new List<Teacher> { teacher });
        _mockUnitOfWork.Setup(u => u.Teachers).Returns(mockTeacherRepo.Object);

        var result = await _service.GetWithCoursesAsync(teacher.Id);

        Assert.NotNull(result);
        var (teacherDto, courses) = result.Value;
        Assert.Equal(teacher.Id, teacherDto.Id);
        Assert.Single(courses);
        Assert.Equal(course.Id, courses[0].Id);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithValidId_DeactivatesTeacher()
    {
        var teacher = CreateTeacher();
        teacher.IsActive = true;

        var mockTeacherRepo = MockHelpers.CreateMockTeacherRepository(new List<Teacher> { teacher });
        _mockUnitOfWork.Setup(u => u.Teachers).Returns(mockTeacherRepo.Object);

        var result = await _service.DeleteAsync(teacher.Id);

        Assert.True(result);
        Assert.False(teacher.IsActive);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithInvalidId_ReturnsFalse()
    {
        var mockTeacherRepo = MockHelpers.CreateMockTeacherRepository(new List<Teacher>());
        _mockUnitOfWork.Setup(u => u.Teachers).Returns(mockTeacherRepo.Object);

        var result = await _service.DeleteAsync(Guid.NewGuid());

        Assert.False(result);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region GetAvailabilityAsync Tests

    [Fact]
    public async Task GetAvailabilityAsync_WithValidId_ReturnsAvailabilityList()
    {
        var teacher = CreateTeacher();
        var availability = new TeacherAvailability
        {
            Id = Guid.NewGuid(),
            TeacherId = teacher.Id,
            DayOfWeek = DayOfWeek.Monday,
            FromTime = new TimeOnly(9, 0),
            UntilTime = new TimeOnly(17, 0)
        };
        teacher.Availability = new List<TeacherAvailability> { availability };

        var mockTeacherRepo = MockHelpers.CreateMockTeacherRepository(new List<Teacher> { teacher });
        _mockUnitOfWork.Setup(u => u.Teachers).Returns(mockTeacherRepo.Object);

        var result = await _service.GetAvailabilityAsync(teacher.Id);

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(DayOfWeek.Monday, result[0].DayOfWeek);
        Assert.Equal(new TimeOnly(9, 0), result[0].FromTime);
    }

    [Fact]
    public async Task GetAvailabilityAsync_WithInvalidId_ReturnsNull()
    {
        var mockTeacherRepo = MockHelpers.CreateMockTeacherRepository(new List<Teacher>());
        _mockUnitOfWork.Setup(u => u.Teachers).Returns(mockTeacherRepo.Object);

        var result = await _service.GetAvailabilityAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    #endregion

    #region UpdateAvailabilityAsync Tests

    [Fact]
    public async Task UpdateAvailabilityAsync_WithInvalidId_ReturnsNull()
    {
        var mockTeacherRepo = MockHelpers.CreateMockTeacherRepository(new List<Teacher>());
        _mockUnitOfWork.Setup(u => u.Teachers).Returns(mockTeacherRepo.Object);

        var result = await _service.UpdateAvailabilityAsync(Guid.NewGuid(), new List<UpdateTeacherAvailabilityDto>());

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAvailabilityAsync_WithMoreThan7Entries_ThrowsInvalidOperationException()
    {
        var teacher = CreateTeacher();
        teacher.Availability = new List<TeacherAvailability>();

        var mockTeacherRepo = MockHelpers.CreateMockTeacherRepository(new List<Teacher> { teacher });
        _mockUnitOfWork.Setup(u => u.Teachers).Returns(mockTeacherRepo.Object);

        var dtos = Enumerable.Range(0, 8)
            .Select(i => new UpdateTeacherAvailabilityDto
            {
                DayOfWeek = (DayOfWeek)(i % 7),
                FromTime = new TimeOnly(9, 0),
                UntilTime = new TimeOnly(17, 0)
            })
            .ToList();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateAvailabilityAsync(teacher.Id, dtos));
        Assert.Contains("Maximum of 7", exception.Message);
    }

    [Fact]
    public async Task UpdateAvailabilityAsync_WithDuplicateDays_ThrowsInvalidOperationException()
    {
        var teacher = CreateTeacher();
        teacher.Availability = new List<TeacherAvailability>();

        var mockTeacherRepo = MockHelpers.CreateMockTeacherRepository(new List<Teacher> { teacher });
        _mockUnitOfWork.Setup(u => u.Teachers).Returns(mockTeacherRepo.Object);

        var dtos = new List<UpdateTeacherAvailabilityDto>
        {
            new() { DayOfWeek = DayOfWeek.Monday, FromTime = new TimeOnly(9, 0), UntilTime = new TimeOnly(17, 0) },
            new() { DayOfWeek = DayOfWeek.Monday, FromTime = new TimeOnly(10, 0), UntilTime = new TimeOnly(18, 0) }
        };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateAvailabilityAsync(teacher.Id, dtos));
        Assert.Contains("Duplicate days", exception.Message);
    }

    [Fact]
    public async Task UpdateAvailabilityAsync_WithInvalidTimeRange_ThrowsInvalidOperationException()
    {
        var teacher = CreateTeacher();
        teacher.Availability = new List<TeacherAvailability>();

        var mockTeacherRepo = MockHelpers.CreateMockTeacherRepository(new List<Teacher> { teacher });
        _mockUnitOfWork.Setup(u => u.Teachers).Returns(mockTeacherRepo.Object);

        var dtos = new List<UpdateTeacherAvailabilityDto>
        {
            new()
            {
                DayOfWeek = DayOfWeek.Monday,
                FromTime = new TimeOnly(9, 0),
                UntilTime = new TimeOnly(9, 30) // less than 1 hour
            }
        };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateAvailabilityAsync(teacher.Id, dtos));
        Assert.Contains("at least 1 hour", exception.Message);
    }

    [Fact]
    public async Task UpdateAvailabilityAsync_WithUnavailableEntry_DoesNotThrow()
    {
        var teacher = CreateTeacher();
        teacher.Availability = new List<TeacherAvailability>();

        var mockTeacherRepo = MockHelpers.CreateMockTeacherRepository(new List<Teacher> { teacher });
        _mockUnitOfWork.Setup(u => u.Teachers).Returns(mockTeacherRepo.Object);

        var dtos = new List<UpdateTeacherAvailabilityDto>
        {
            new()
            {
                DayOfWeek = DayOfWeek.Monday,
                FromTime = TimeOnly.MinValue,
                UntilTime = TimeOnly.MinValue
            }
        };

        var result = await _service.UpdateAvailabilityAsync(teacher.Id, dtos);

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(DayOfWeek.Monday, result[0].DayOfWeek);
    }

    [Fact]
    public async Task UpdateAvailabilityAsync_WithValidEntries_ReplacesExistingAvailability()
    {
        var teacher = CreateTeacher();
        var existingAvailability = new TeacherAvailability
        {
            Id = Guid.NewGuid(),
            TeacherId = teacher.Id,
            DayOfWeek = DayOfWeek.Wednesday,
            FromTime = new TimeOnly(8, 0),
            UntilTime = new TimeOnly(16, 0)
        };
        teacher.Availability = new List<TeacherAvailability> { existingAvailability };

        var mockTeacherRepo = MockHelpers.CreateMockTeacherRepository(new List<Teacher> { teacher });
        _mockUnitOfWork.Setup(u => u.Teachers).Returns(mockTeacherRepo.Object);

        var dtos = new List<UpdateTeacherAvailabilityDto>
        {
            new() { DayOfWeek = DayOfWeek.Tuesday, FromTime = new TimeOnly(9, 0), UntilTime = new TimeOnly(17, 0) },
            new() { DayOfWeek = DayOfWeek.Thursday, FromTime = new TimeOnly(10, 0), UntilTime = new TimeOnly(18, 0) }
        };

        var result = await _service.UpdateAvailabilityAsync(teacher.Id, dtos);

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Contains(result, r => r.DayOfWeek == DayOfWeek.Tuesday);
        Assert.Contains(result, r => r.DayOfWeek == DayOfWeek.Thursday);
        mockTeacherRepo.Verify(r => r.RemoveAvailability(existingAvailability), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithDuplicateEmail_ThrowsInvalidOperationException()
    {
        var existing = CreateTeacher();
        existing.Email = "john.doe@example.com";
        existing.TeacherInstruments = new List<TeacherInstrument>();
        existing.TeacherCourseTypes = new List<TeacherCourseType>();

        var mockTeacherRepo = MockHelpers.CreateMockTeacherRepository(new List<Teacher> { existing });
        _mockUnitOfWork.Setup(u => u.Teachers).Returns(mockTeacherRepo.Object);

        var dto = new CreateTeacherDto
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            HourlyRate = 50m,
            Role = TeacherRole.Teacher,
            InstrumentIds = new List<int>(),
            CourseTypeIds = new List<Guid>()
        };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateAsync(dto));
        Assert.Contains("email already exists", exception.Message);
    }

    [Fact]
    public async Task CreateAsync_WithNoInstrumentsAndCourseTypes_CreatesTeacher()
    {
        var newTeacher = CreateTeacher("New", "Teacher");
        newTeacher.TeacherInstruments = new List<TeacherInstrument>();
        newTeacher.TeacherCourseTypes = new List<TeacherCourseType>();

        var mockTeacherRepo = MockHelpers.CreateMockTeacherRepository(new List<Teacher>());
        mockTeacherRepo
            .Setup(r => r.AddAsync(It.IsAny<Teacher>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Teacher t, CancellationToken _) => t);
        mockTeacherRepo
            .Setup(r => r.GetWithInstrumentsAndCourseTypesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(newTeacher);
        _mockUnitOfWork.Setup(u => u.Teachers).Returns(mockTeacherRepo.Object);

        var dto = new CreateTeacherDto
        {
            FirstName = "New",
            LastName = "Teacher",
            Email = "new.teacher@example.com",
            HourlyRate = 60m,
            Role = TeacherRole.Teacher,
            InstrumentIds = new List<int>(),
            CourseTypeIds = new List<Guid>()
        };

        var result = await _service.CreateAsync(dto);

        Assert.NotNull(result);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region GetAvailableCourseTypesAsync Tests

    [Fact]
    public async Task GetAvailableCourseTypesAsync_WithInvalidTeacherId_ReturnsNull()
    {
        var mockTeacherRepo = MockHelpers.CreateMockTeacherRepository(new List<Teacher>());
        _mockUnitOfWork.Setup(u => u.Teachers).Returns(mockTeacherRepo.Object);

        var result = await _service.GetAvailableCourseTypesAsync(Guid.NewGuid(), "1,2");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAvailableCourseTypesAsync_WithEmptyInstrumentIds_ReturnsEmptyList()
    {
        var teacher = CreateTeacher();
        var mockTeacherRepo = MockHelpers.CreateMockTeacherRepository(new List<Teacher> { teacher });
        _mockUnitOfWork.Setup(u => u.Teachers).Returns(mockTeacherRepo.Object);

        var result = await _service.GetAvailableCourseTypesAsync(teacher.Id, null);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAvailableCourseTypesAsync_WithValidInstrumentIds_ReturnsCourseTypes()
    {
        var teacher = CreateTeacher();
        var instrument = CreateInstrument(id: 1);
        var courseType = CreateCourseType(instrument, "Piano Beginner");

        var mockTeacherRepo = MockHelpers.CreateMockTeacherRepository(new List<Teacher> { teacher });
        _mockUnitOfWork.Setup(u => u.Teachers).Returns(mockTeacherRepo.Object);

        var mockCourseTypeRepo = MockHelpers.CreateMockCourseTypeRepository(new List<CourseType> { courseType });
        _mockUnitOfWork.Setup(u => u.CourseTypes).Returns(mockCourseTypeRepo.Object);

        var result = await _service.GetAvailableCourseTypesAsync(teacher.Id, "1");

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("Piano Beginner", result[0].Name);
    }

    #endregion
}
