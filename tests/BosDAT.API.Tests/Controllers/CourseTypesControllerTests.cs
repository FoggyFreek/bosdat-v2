using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using BosDAT.API.Controllers;
using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Interfaces;
using BosDAT.API.Tests.Helpers;

namespace BosDAT.API.Tests.Controllers;

public class CourseTypesControllerTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly CourseTypesController _controller;

    public CourseTypesControllerTests()
    {
        _mockUnitOfWork = MockHelpers.CreateMockUnitOfWork();
        _controller = new CourseTypesController(_mockUnitOfWork.Object);
    }

    #region GetAll Tests

    [Fact]
    public async Task GetAll_ReturnsAllCourseTypes()
    {
        // Arrange
        var instrument = new Instrument { Id = 1, Name = "Piano", IsActive = true };
        var courseTypes = new List<CourseType>
        {
            new CourseType
            {
                Id = Guid.NewGuid(),
                InstrumentId = 1,
                Instrument = instrument,
                Name = "Beginner Piano",
                DurationMinutes = 30,
                Type = CourseTypeCategory.Individual,
                PriceAdult = 50,
                PriceChild = 40,
                MaxStudents = 1,
                IsActive = true
            },
            new CourseType
            {
                Id = Guid.NewGuid(),
                InstrumentId = 1,
                Instrument = instrument,
                Name = "Intermediate Piano",
                DurationMinutes = 45,
                Type = CourseTypeCategory.Individual,
                PriceAdult = 60,
                PriceChild = 50,
                MaxStudents = 1,
                IsActive = true
            }
        };

        var courses = new List<Course>();
        var teacherCourseTypes = new List<TeacherCourseType>();

        var mockCourseTypeRepo = MockHelpers.CreateMockRepository(courseTypes);
        var mockCourseRepo = MockHelpers.CreateMockRepository(courses);
        var mockTeacherCourseTypeRepo = MockHelpers.CreateMockRepository(teacherCourseTypes);

        _mockUnitOfWork.Setup(u => u.Repository<CourseType>()).Returns(mockCourseTypeRepo.Object);
        _mockUnitOfWork.Setup(u => u.Repository<Course>()).Returns(mockCourseRepo.Object);
        _mockUnitOfWork.Setup(u => u.Repository<TeacherCourseType>()).Returns(mockTeacherCourseTypeRepo.Object);

        // Act
        var result = await _controller.GetAll(null, null, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedCourseTypes = Assert.IsAssignableFrom<IEnumerable<CourseTypeDto>>(okResult.Value);
        Assert.Equal(2, returnedCourseTypes.Count());
    }

    [Fact]
    public async Task GetAll_WithActiveOnlyFilter_ReturnsOnlyActiveCourseTypes()
    {
        // Arrange
        var instrument = new Instrument { Id = 1, Name = "Piano", IsActive = true };
        var courseTypes = new List<CourseType>
        {
            new CourseType
            {
                Id = Guid.NewGuid(),
                InstrumentId = 1,
                Instrument = instrument,
                Name = "Active Piano",
                DurationMinutes = 30,
                Type = CourseTypeCategory.Individual,
                PriceAdult = 50,
                PriceChild = 40,
                MaxStudents = 1,
                IsActive = true
            },
            new CourseType
            {
                Id = Guid.NewGuid(),
                InstrumentId = 1,
                Instrument = instrument,
                Name = "Archived Piano",
                DurationMinutes = 45,
                Type = CourseTypeCategory.Individual,
                PriceAdult = 60,
                PriceChild = 50,
                MaxStudents = 1,
                IsActive = false
            }
        };

        var courses = new List<Course>();
        var teacherCourseTypes = new List<TeacherCourseType>();

        var mockCourseTypeRepo = MockHelpers.CreateMockRepository(courseTypes);
        var mockCourseRepo = MockHelpers.CreateMockRepository(courses);
        var mockTeacherCourseTypeRepo = MockHelpers.CreateMockRepository(teacherCourseTypes);

        _mockUnitOfWork.Setup(u => u.Repository<CourseType>()).Returns(mockCourseTypeRepo.Object);
        _mockUnitOfWork.Setup(u => u.Repository<Course>()).Returns(mockCourseRepo.Object);
        _mockUnitOfWork.Setup(u => u.Repository<TeacherCourseType>()).Returns(mockTeacherCourseTypeRepo.Object);

        // Act
        var result = await _controller.GetAll(true, null, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedCourseTypes = Assert.IsAssignableFrom<IEnumerable<CourseTypeDto>>(okResult.Value);
        Assert.Single(returnedCourseTypes);
        Assert.True(returnedCourseTypes.First().IsActive);
    }

    [Fact]
    public async Task GetAll_WithInstrumentIdFilter_ReturnsOnlyMatchingInstrument()
    {
        // Arrange
        var piano = new Instrument { Id = 1, Name = "Piano", IsActive = true };
        var guitar = new Instrument { Id = 2, Name = "Guitar", IsActive = true };
        var courseTypes = new List<CourseType>
        {
            new CourseType
            {
                Id = Guid.NewGuid(),
                InstrumentId = 1,
                Instrument = piano,
                Name = "Beginner Piano",
                DurationMinutes = 30,
                Type = CourseTypeCategory.Individual,
                PriceAdult = 50,
                PriceChild = 40,
                MaxStudents = 1,
                IsActive = true
            },
            new CourseType
            {
                Id = Guid.NewGuid(),
                InstrumentId = 2,
                Instrument = guitar,
                Name = "Beginner Guitar",
                DurationMinutes = 30,
                Type = CourseTypeCategory.Individual,
                PriceAdult = 50,
                PriceChild = 40,
                MaxStudents = 1,
                IsActive = true
            }
        };

        var courses = new List<Course>();
        var teacherCourseTypes = new List<TeacherCourseType>();

        var mockCourseTypeRepo = MockHelpers.CreateMockRepository(courseTypes);
        var mockCourseRepo = MockHelpers.CreateMockRepository(courses);
        var mockTeacherCourseTypeRepo = MockHelpers.CreateMockRepository(teacherCourseTypes);

        _mockUnitOfWork.Setup(u => u.Repository<CourseType>()).Returns(mockCourseTypeRepo.Object);
        _mockUnitOfWork.Setup(u => u.Repository<Course>()).Returns(mockCourseRepo.Object);
        _mockUnitOfWork.Setup(u => u.Repository<TeacherCourseType>()).Returns(mockTeacherCourseTypeRepo.Object);

        // Act
        var result = await _controller.GetAll(null, 1, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedCourseTypes = Assert.IsAssignableFrom<IEnumerable<CourseTypeDto>>(okResult.Value);
        Assert.Single(returnedCourseTypes);
        Assert.Equal(1, returnedCourseTypes.First().InstrumentId);
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetById_WithValidId_ReturnsCourseType()
    {
        // Arrange
        var courseTypeId = Guid.NewGuid();
        var instrument = new Instrument { Id = 1, Name = "Piano", IsActive = true };
        var courseType = new CourseType
        {
            Id = courseTypeId,
            InstrumentId = 1,
            Instrument = instrument,
            Name = "Beginner Piano",
            DurationMinutes = 30,
            Type = CourseTypeCategory.Individual,
            PriceAdult = 50,
            PriceChild = 40,
            MaxStudents = 1,
            IsActive = true
        };

        var courseTypes = new List<CourseType> { courseType };
        var courses = new List<Course>();
        var teacherCourseTypes = new List<TeacherCourseType>();

        var mockCourseTypeRepo = MockHelpers.CreateMockRepository(courseTypes);
        var mockCourseRepo = MockHelpers.CreateMockRepository(courses);
        var mockTeacherCourseTypeRepo = MockHelpers.CreateMockRepository(teacherCourseTypes);

        _mockUnitOfWork.Setup(u => u.Repository<CourseType>()).Returns(mockCourseTypeRepo.Object);
        _mockUnitOfWork.Setup(u => u.Repository<Course>()).Returns(mockCourseRepo.Object);
        _mockUnitOfWork.Setup(u => u.Repository<TeacherCourseType>()).Returns(mockTeacherCourseTypeRepo.Object);

        // Act
        var result = await _controller.GetById(courseTypeId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedCourseType = Assert.IsType<CourseTypeDto>(okResult.Value);
        Assert.Equal(courseTypeId, returnedCourseType.Id);
        Assert.Equal("Beginner Piano", returnedCourseType.Name);
    }

    [Fact]
    public async Task GetById_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var courseTypes = new List<CourseType>();
        var mockCourseTypeRepo = MockHelpers.CreateMockRepository(courseTypes);
        _mockUnitOfWork.Setup(u => u.Repository<CourseType>()).Returns(mockCourseTypeRepo.Object);

        // Act
        var result = await _controller.GetById(Guid.NewGuid(), CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    #endregion

    #region Create Tests

    [Fact]
    public async Task Create_WithValidData_CreatesCourseType()
    {
        // Arrange
        var instrument = new Instrument { Id = 1, Name = "Piano", IsActive = true };
        var dto = new CreateCourseTypeDto
        {
            InstrumentId = 1,
            Name = "Beginner Piano",
            DurationMinutes = 30,
            Type = CourseTypeCategory.Individual,
            PriceAdult = 50,
            PriceChild = 40,
            MaxStudents = 1
        };

        var instruments = new List<Instrument> { instrument };
        var courseTypes = new List<CourseType>();
        var courses = new List<Course>();
        var teacherCourseTypes = new List<TeacherCourseType>();

        var mockInstrumentRepo = MockHelpers.CreateMockRepository(instruments);
        var mockCourseTypeRepo = MockHelpers.CreateMockRepository(courseTypes);
        var mockCourseRepo = MockHelpers.CreateMockRepository(courses);
        var mockTeacherCourseTypeRepo = MockHelpers.CreateMockRepository(teacherCourseTypes);

        _mockUnitOfWork.Setup(u => u.Repository<Instrument>()).Returns(mockInstrumentRepo.Object);
        _mockUnitOfWork.Setup(u => u.Repository<CourseType>()).Returns(mockCourseTypeRepo.Object);
        _mockUnitOfWork.Setup(u => u.Repository<Course>()).Returns(mockCourseRepo.Object);
        _mockUnitOfWork.Setup(u => u.Repository<TeacherCourseType>()).Returns(mockTeacherCourseTypeRepo.Object);

        mockInstrumentRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(instrument);

        // Act
        var result = await _controller.Create(dto, CancellationToken.None);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(CourseTypesController.GetById), createdResult.ActionName);
        var returnedDto = Assert.IsType<CourseTypeDto>(createdResult.Value);
        Assert.Equal("Beginner Piano", returnedDto.Name);
    }

    [Fact]
    public async Task Create_WithNonexistentInstrument_ReturnsBadRequest()
    {
        // Arrange
        var dto = new CreateCourseTypeDto
        {
            InstrumentId = 999,
            Name = "Invalid",
            DurationMinutes = 30,
            Type = CourseTypeCategory.Individual,
            PriceAdult = 50,
            PriceChild = 40,
            MaxStudents = 1
        };

        var instruments = new List<Instrument>();
        var mockInstrumentRepo = MockHelpers.CreateMockRepository(instruments);
        _mockUnitOfWork.Setup(u => u.Repository<Instrument>()).Returns(mockInstrumentRepo.Object);

        mockInstrumentRepo.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Instrument?)null);

        // Act
        var result = await _controller.Create(dto, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task Create_WithChildPriceHigherThanAdultPrice_ReturnsBadRequest()
    {
        // Arrange
        var instrument = new Instrument { Id = 1, Name = "Piano", IsActive = true };
        var dto = new CreateCourseTypeDto
        {
            InstrumentId = 1,
            Name = "Invalid Pricing",
            DurationMinutes = 30,
            Type = CourseTypeCategory.Individual,
            PriceAdult = 40,
            PriceChild = 50,  // Higher than adult price
            MaxStudents = 1
        };

        var instruments = new List<Instrument> { instrument };
        var mockInstrumentRepo = MockHelpers.CreateMockRepository(instruments);
        _mockUnitOfWork.Setup(u => u.Repository<Instrument>()).Returns(mockInstrumentRepo.Object);

        mockInstrumentRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(instrument);

        // Act
        var result = await _controller.Create(dto, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequestResult.Value);
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_WithValidData_UpdatesCourseType()
    {
        // Arrange
        var courseTypeId = Guid.NewGuid();
        var instrument = new Instrument { Id = 1, Name = "Piano", IsActive = true };
        var courseType = new CourseType
        {
            Id = courseTypeId,
            InstrumentId = 1,
            Instrument = instrument,
            Name = "Old Name",
            DurationMinutes = 30,
            Type = CourseTypeCategory.Individual,
            PriceAdult = 50,
            PriceChild = 40,
            MaxStudents = 1,
            IsActive = true
        };

        var dto = new UpdateCourseTypeDto
        {
            InstrumentId = 1,
            Name = "Updated Name",
            DurationMinutes = 45,
            Type = CourseTypeCategory.Individual,
            PriceAdult = 60,
            PriceChild = 50,
            MaxStudents = 1
        };

        var courseTypes = new List<CourseType> { courseType };
        var instruments = new List<Instrument> { instrument };
        var courses = new List<Course>();
        var teacherCourseTypes = new List<TeacherCourseType>();

        var mockCourseTypeRepo = MockHelpers.CreateMockRepository(courseTypes);
        var mockInstrumentRepo = MockHelpers.CreateMockRepository(instruments);
        var mockCourseRepo = MockHelpers.CreateMockRepository(courses);
        var mockTeacherCourseTypeRepo = MockHelpers.CreateMockRepository(teacherCourseTypes);

        _mockUnitOfWork.Setup(u => u.Repository<CourseType>()).Returns(mockCourseTypeRepo.Object);
        _mockUnitOfWork.Setup(u => u.Repository<Instrument>()).Returns(mockInstrumentRepo.Object);
        _mockUnitOfWork.Setup(u => u.Repository<Course>()).Returns(mockCourseRepo.Object);
        _mockUnitOfWork.Setup(u => u.Repository<TeacherCourseType>()).Returns(mockTeacherCourseTypeRepo.Object);

        mockInstrumentRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(instrument);

        // Act
        var result = await _controller.Update(courseTypeId, dto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedDto = Assert.IsType<CourseTypeDto>(okResult.Value);
        Assert.Equal("Updated Name", returnedDto.Name);
    }

    [Fact]
    public async Task Update_WithInvalidCourseTypeId_ReturnsNotFound()
    {
        // Arrange
        var dto = new UpdateCourseTypeDto
        {
            InstrumentId = 1,
            Name = "Updated Name",
            DurationMinutes = 45,
            Type = CourseTypeCategory.Individual,
            PriceAdult = 60,
            PriceChild = 50,
            MaxStudents = 1
        };

        var courseTypes = new List<CourseType>();
        var mockCourseTypeRepo = MockHelpers.CreateMockRepository(courseTypes);
        _mockUnitOfWork.Setup(u => u.Repository<CourseType>()).Returns(mockCourseTypeRepo.Object);

        // Act
        var result = await _controller.Update(Guid.NewGuid(), dto, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Update_WithChildPriceHigherThanAdultPrice_ReturnsBadRequest()
    {
        // Arrange
        var courseTypeId = Guid.NewGuid();
        var instrument = new Instrument { Id = 1, Name = "Piano", IsActive = true };
        var courseType = new CourseType
        {
            Id = courseTypeId,
            InstrumentId = 1,
            Instrument = instrument,
            Name = "Test",
            DurationMinutes = 30,
            Type = CourseTypeCategory.Individual,
            PriceAdult = 50,
            PriceChild = 40,
            MaxStudents = 1,
            IsActive = true
        };

        var dto = new UpdateCourseTypeDto
        {
            InstrumentId = 1,
            Name = "Test",
            DurationMinutes = 45,
            Type = CourseTypeCategory.Individual,
            PriceAdult = 40,
            PriceChild = 50,  // Higher than adult price
            MaxStudents = 1
        };

        var courseTypes = new List<CourseType> { courseType };
        var instruments = new List<Instrument> { instrument };

        var mockCourseTypeRepo = MockHelpers.CreateMockRepository(courseTypes);
        var mockInstrumentRepo = MockHelpers.CreateMockRepository(instruments);

        _mockUnitOfWork.Setup(u => u.Repository<CourseType>()).Returns(mockCourseTypeRepo.Object);
        _mockUnitOfWork.Setup(u => u.Repository<Instrument>()).Returns(mockInstrumentRepo.Object);

        mockInstrumentRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(instrument);

        // Act
        var result = await _controller.Update(courseTypeId, dto, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task Update_ArchivingWithActiveCourses_ReturnsBadRequest()
    {
        // Arrange
        var courseTypeId = Guid.NewGuid();
        var instrument = new Instrument { Id = 1, Name = "Piano", IsActive = true };
        var courseType = new CourseType
        {
            Id = courseTypeId,
            InstrumentId = 1,
            Instrument = instrument,
            Name = "Test",
            DurationMinutes = 30,
            Type = CourseTypeCategory.Individual,
            PriceAdult = 50,
            PriceChild = 40,
            MaxStudents = 1,
            IsActive = true
        };

        var activeCourse = new Course
        {
            Id = Guid.NewGuid(),
            CourseTypeId = courseTypeId,
            Status = CourseStatus.Active
        };

        var dto = new UpdateCourseTypeDto
        {
            InstrumentId = 1,
            Name = "Test",
            DurationMinutes = 30,
            Type = CourseTypeCategory.Individual,
            PriceAdult = 50,
            PriceChild = 40,
            MaxStudents = 1,
            IsActive = false  // Archiving
        };

        var courseTypes = new List<CourseType> { courseType };
        var instruments = new List<Instrument> { instrument };
        var courses = new List<Course> { activeCourse };

        var mockCourseTypeRepo = MockHelpers.CreateMockRepository(courseTypes);
        var mockInstrumentRepo = MockHelpers.CreateMockRepository(instruments);
        var mockCourseRepo = MockHelpers.CreateMockRepository(courses);

        _mockUnitOfWork.Setup(u => u.Repository<CourseType>()).Returns(mockCourseTypeRepo.Object);
        _mockUnitOfWork.Setup(u => u.Repository<Instrument>()).Returns(mockInstrumentRepo.Object);
        _mockUnitOfWork.Setup(u => u.Repository<Course>()).Returns(mockCourseRepo.Object);

        mockInstrumentRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(instrument);

        // Act
        var result = await _controller.Update(courseTypeId, dto, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequestResult.Value);
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_WithValidId_ArchivesCourseType()
    {
        // Arrange
        var courseTypeId = Guid.NewGuid();
        var courseType = new CourseType
        {
            Id = courseTypeId,
            InstrumentId = 1,
            Name = "Test",
            DurationMinutes = 30,
            Type = CourseTypeCategory.Individual,
            PriceAdult = 50,
            PriceChild = 40,
            MaxStudents = 1,
            IsActive = true
        };

        var courseTypes = new List<CourseType> { courseType };
        var courses = new List<Course>();

        var mockCourseTypeRepo = MockHelpers.CreateMockRepository(courseTypes);
        var mockCourseRepo = MockHelpers.CreateMockRepository(courses);

        _mockUnitOfWork.Setup(u => u.Repository<CourseType>()).Returns(mockCourseTypeRepo.Object);
        _mockUnitOfWork.Setup(u => u.Repository<Course>()).Returns(mockCourseRepo.Object);

        mockCourseTypeRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(courseType);

        // Act
        var result = await _controller.Delete(courseTypeId, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var courseTypes = new List<CourseType>();
        var mockCourseTypeRepo = MockHelpers.CreateMockRepository(courseTypes);
        _mockUnitOfWork.Setup(u => u.Repository<CourseType>()).Returns(mockCourseTypeRepo.Object);

        mockCourseTypeRepo.Setup(r => r.GetByIdAsync(Guid.NewGuid(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CourseType?)null);

        // Act
        var result = await _controller.Delete(Guid.NewGuid(), CancellationToken.None);
        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Delete_WithActiveCourses_ReturnsBadRequest()
    {
        // Arrange
        var courseTypeId = Guid.NewGuid();
        var courseType = new CourseType
        {
            Id = courseTypeId,
            InstrumentId = 1,
            Name = "Test",
            DurationMinutes = 30,
            Type = CourseTypeCategory.Individual,
            PriceAdult = 50,
            PriceChild = 40,
            MaxStudents = 1,
            IsActive = true
        };

        var activeCourse = new Course
        {
            Id = Guid.NewGuid(),
            CourseTypeId = courseTypeId,
            Status = CourseStatus.Active
        };

        var courseTypes = new List<CourseType> { courseType };
        var courses = new List<Course> { activeCourse };

        var mockCourseTypeRepo = MockHelpers.CreateMockRepository(courseTypes);
        var mockCourseRepo = MockHelpers.CreateMockRepository(courses);

        _mockUnitOfWork.Setup(u => u.Repository<CourseType>()).Returns(mockCourseTypeRepo.Object);
        _mockUnitOfWork.Setup(u => u.Repository<Course>()).Returns(mockCourseRepo.Object);

        mockCourseTypeRepo.Setup(r => r.GetByIdAsync(courseTypeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(courseType);

        // Act
        var result = await _controller.Delete(courseTypeId, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequestResult.Value);
    }

    #endregion

    #region Reactivate Tests

    [Fact]
    public async Task Reactivate_WithValidId_ReactivatesCourseType()
    {
        // Arrange
        var courseTypeId = Guid.NewGuid();
        var instrument = new Instrument { Id = 1, Name = "Piano", IsActive = true };
        var courseType = new CourseType
        {
            Id = courseTypeId,
            InstrumentId = 1,
            Instrument = instrument,
            Name = "Test",
            DurationMinutes = 30,
            Type = CourseTypeCategory.Individual,
            PriceAdult = 50,
            PriceChild = 40,
            MaxStudents = 1,
            IsActive = false
        };

        var courseTypes = new List<CourseType> { courseType };
        var courses = new List<Course>();
        var teacherCourseTypes = new List<TeacherCourseType>();

        var mockCourseTypeRepo = MockHelpers.CreateMockRepository(courseTypes);
        var mockCourseRepo = MockHelpers.CreateMockRepository(courses);
        var mockTeacherCourseTypeRepo = MockHelpers.CreateMockRepository(teacherCourseTypes);

        _mockUnitOfWork.Setup(u => u.Repository<CourseType>()).Returns(mockCourseTypeRepo.Object);
        _mockUnitOfWork.Setup(u => u.Repository<Course>()).Returns(mockCourseRepo.Object);
        _mockUnitOfWork.Setup(u => u.Repository<TeacherCourseType>()).Returns(mockTeacherCourseTypeRepo.Object);

        // Act
        var result = await _controller.Reactivate(courseTypeId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedDto = Assert.IsType<CourseTypeDto>(okResult.Value);
        Assert.True(returnedDto.IsActive);
    }

    [Fact]
    public async Task Reactivate_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var courseTypes = new List<CourseType>();
        var mockCourseTypeRepo = MockHelpers.CreateMockRepository(courseTypes);
        _mockUnitOfWork.Setup(u => u.Repository<CourseType>()).Returns(mockCourseTypeRepo.Object);

        // Act
        var result = await _controller.Reactivate(Guid.NewGuid(), CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    #endregion

    #region GetTeachersCountForInstrument Tests

    [Fact]
    public async Task GetTeachersCountForInstrument_WithValidInstrumentId_ReturnsTeacherCount()
    {
        // Arrange
        var instrument = new Instrument { Id = 1, Name = "Piano", IsActive = true };
        var teacher = new Teacher { Id = Guid.NewGuid(), FirstName = "John", LastName = "Doe", Email = "john.doe@example.com", IsActive = true };
        var teacherInstrument = new TeacherInstrument { InstrumentId = 1, TeacherId = teacher.Id, Teacher = teacher };

        var instruments = new List<Instrument> { instrument };
        var teacherInstruments = new List<TeacherInstrument> { teacherInstrument };

        var mockInstrumentRepo = MockHelpers.CreateMockRepository(instruments);
        var mockTeacherInstrumentRepo = MockHelpers.CreateMockRepository(teacherInstruments);

        _mockUnitOfWork.Setup(u => u.Repository<Instrument>()).Returns(mockInstrumentRepo.Object);
        _mockUnitOfWork.Setup(u => u.Repository<TeacherInstrument>()).Returns(mockTeacherInstrumentRepo.Object);

        mockInstrumentRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(instrument);

        // Act
        var result = await _controller.GetTeachersCountForInstrument(1, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.IsType<int>(okResult.Value);
    }

    [Fact]
    public async Task GetTeachersCountForInstrument_WithInvalidInstrumentId_ReturnsNotFound()
    {
        // Arrange
        var instruments = new List<Instrument>();
        var mockInstrumentRepo = MockHelpers.CreateMockRepository(instruments);
        _mockUnitOfWork.Setup(u => u.Repository<Instrument>()).Returns(mockInstrumentRepo.Object);

        mockInstrumentRepo.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Instrument?)null);

        // Act
        var result = await _controller.GetTeachersCountForInstrument(999, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetTeachersCountForInstrument_WithNoTeachers_ReturnsZero()
    {
        // Arrange
        var instrument = new Instrument { Id = 1, Name = "Piano", IsActive = true };
        var instruments = new List<Instrument> { instrument };
        var teacherInstruments = new List<TeacherInstrument>();

        var mockInstrumentRepo = MockHelpers.CreateMockRepository(instruments);
        var mockTeacherInstrumentRepo = MockHelpers.CreateMockRepository(teacherInstruments);

        _mockUnitOfWork.Setup(u => u.Repository<Instrument>()).Returns(mockInstrumentRepo.Object);
        _mockUnitOfWork.Setup(u => u.Repository<TeacherInstrument>()).Returns(mockTeacherInstrumentRepo.Object);

        mockInstrumentRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(instrument);

        // Act
        var result = await _controller.GetTeachersCountForInstrument(1, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(0, result.Value);
    }

    #endregion
}
