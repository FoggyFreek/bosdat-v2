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
    private readonly Mock<ICourseTypePricingService> _mockPricingService;
    private readonly CourseTypesController _controller;

    public CourseTypesControllerTests()
    {
        _mockUnitOfWork = MockHelpers.CreateMockUnitOfWork();
        _mockPricingService = new Mock<ICourseTypePricingService>();
        _controller = new CourseTypesController(_mockUnitOfWork.Object, _mockPricingService.Object);
    }

    private static CourseTypePricingVersion CreatePricingVersion(Guid courseTypeId, decimal priceAdult = 50, decimal priceChild = 40)
    {
        return new CourseTypePricingVersion
        {
            Id = Guid.NewGuid(),
            CourseTypeId = courseTypeId,
            PriceAdult = priceAdult,
            PriceChild = priceChild,
            ValidFrom = DateOnly.FromDateTime(DateTime.Today),
            ValidUntil = null,
            IsCurrent = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    private static CourseType CreateCourseType(Instrument instrument, string name, decimal priceAdult = 50, decimal priceChild = 40)
    {
        var courseTypeId = Guid.NewGuid();
        var pricingVersion = CreatePricingVersion(courseTypeId, priceAdult, priceChild);
        return new CourseType
        {
            Id = courseTypeId,
            InstrumentId = instrument.Id,
            Instrument = instrument,
            Name = name,
            DurationMinutes = 30,
            Type = CourseTypeCategory.Individual,
            MaxStudents = 1,
            IsActive = true,
            PricingVersions = new List<CourseTypePricingVersion> { pricingVersion }
        };
    }

    #region GetAll Tests

    [Fact]
    public async Task GetAll_ReturnsAllCourseTypes()
    {
        // Arrange
        var instrument = new Instrument { Id = 1, Name = "Piano", IsActive = true };
        var courseType1 = CreateCourseType(instrument, "Beginner Piano");
        var courseType2 = CreateCourseType(instrument, "Intermediate Piano");
        var courseTypes = new List<CourseType> { courseType1, courseType2 };

        var courses = new List<Course>();
        var teacherCourseTypes = new List<TeacherCourseType>();

        var mockCourseTypeRepo = MockHelpers.CreateMockRepository(courseTypes);
        var mockCourseRepo = MockHelpers.CreateMockRepository(courses);
        var mockTeacherCourseTypeRepo = MockHelpers.CreateMockRepository(teacherCourseTypes);

        _mockUnitOfWork.Setup(u => u.Repository<CourseType>()).Returns(mockCourseTypeRepo.Object);
        _mockUnitOfWork.Setup(u => u.Repository<Course>()).Returns(mockCourseRepo.Object);
        _mockUnitOfWork.Setup(u => u.Repository<TeacherCourseType>()).Returns(mockTeacherCourseTypeRepo.Object);

        _mockPricingService.Setup(s => s.IsCurrentPricingInvoicedAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

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
        var activeCourseType = CreateCourseType(instrument, "Active Piano");
        var archivedCourseType = CreateCourseType(instrument, "Archived Piano");
        archivedCourseType.IsActive = false;

        var courseTypes = new List<CourseType> { activeCourseType, archivedCourseType };

        var courses = new List<Course>();
        var teacherCourseTypes = new List<TeacherCourseType>();

        var mockCourseTypeRepo = MockHelpers.CreateMockRepository(courseTypes);
        var mockCourseRepo = MockHelpers.CreateMockRepository(courses);
        var mockTeacherCourseTypeRepo = MockHelpers.CreateMockRepository(teacherCourseTypes);

        _mockUnitOfWork.Setup(u => u.Repository<CourseType>()).Returns(mockCourseTypeRepo.Object);
        _mockUnitOfWork.Setup(u => u.Repository<Course>()).Returns(mockCourseRepo.Object);
        _mockUnitOfWork.Setup(u => u.Repository<TeacherCourseType>()).Returns(mockTeacherCourseTypeRepo.Object);

        _mockPricingService.Setup(s => s.IsCurrentPricingInvoicedAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

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
        var pianoCourseType = CreateCourseType(piano, "Beginner Piano");
        var guitarCourseType = CreateCourseType(guitar, "Beginner Guitar");
        guitarCourseType.InstrumentId = 2;

        var courseTypes = new List<CourseType> { pianoCourseType, guitarCourseType };

        var courses = new List<Course>();
        var teacherCourseTypes = new List<TeacherCourseType>();

        var mockCourseTypeRepo = MockHelpers.CreateMockRepository(courseTypes);
        var mockCourseRepo = MockHelpers.CreateMockRepository(courses);
        var mockTeacherCourseTypeRepo = MockHelpers.CreateMockRepository(teacherCourseTypes);

        _mockUnitOfWork.Setup(u => u.Repository<CourseType>()).Returns(mockCourseTypeRepo.Object);
        _mockUnitOfWork.Setup(u => u.Repository<Course>()).Returns(mockCourseRepo.Object);
        _mockUnitOfWork.Setup(u => u.Repository<TeacherCourseType>()).Returns(mockTeacherCourseTypeRepo.Object);

        _mockPricingService.Setup(s => s.IsCurrentPricingInvoicedAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

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
        var instrument = new Instrument { Id = 1, Name = "Piano", IsActive = true };
        var courseType = CreateCourseType(instrument, "Beginner Piano");

        var courseTypes = new List<CourseType> { courseType };
        var courses = new List<Course>();
        var teacherCourseTypes = new List<TeacherCourseType>();

        var mockCourseTypeRepo = MockHelpers.CreateMockRepository(courseTypes);
        var mockCourseRepo = MockHelpers.CreateMockRepository(courses);
        var mockTeacherCourseTypeRepo = MockHelpers.CreateMockRepository(teacherCourseTypes);

        _mockUnitOfWork.Setup(u => u.Repository<CourseType>()).Returns(mockCourseTypeRepo.Object);
        _mockUnitOfWork.Setup(u => u.Repository<Course>()).Returns(mockCourseRepo.Object);
        _mockUnitOfWork.Setup(u => u.Repository<TeacherCourseType>()).Returns(mockTeacherCourseTypeRepo.Object);

        _mockPricingService.Setup(s => s.IsCurrentPricingInvoicedAsync(courseType.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.GetById(courseType.Id, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedCourseType = Assert.IsType<CourseTypeDto>(okResult.Value);
        Assert.Equal(courseType.Id, returnedCourseType.Id);
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

        // Setup mock to capture created course type and add pricing version
        mockCourseTypeRepo.Setup(r => r.AddAsync(It.IsAny<CourseType>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CourseType ct, CancellationToken _) =>
            {
                ct.Instrument = instrument;
                ct.PricingVersions = new List<CourseTypePricingVersion> { CreatePricingVersion(ct.Id, 50, 40) };
                courseTypes.Add(ct);
                return ct;
            });

        _mockPricingService.Setup(s => s.CreateInitialPricingVersionAsync(
            It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, decimal adult, decimal child, CancellationToken _) => CreatePricingVersion(id, adult, child));

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
        var instrument = new Instrument { Id = 1, Name = "Piano", IsActive = true };
        var courseType = CreateCourseType(instrument, "Old Name");

        var dto = new UpdateCourseTypeDto
        {
            InstrumentId = 1,
            Name = "Updated Name",
            DurationMinutes = 45,
            Type = CourseTypeCategory.Individual,
            MaxStudents = 1,
            IsActive = true
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

        _mockPricingService.Setup(s => s.IsCurrentPricingInvoicedAsync(courseType.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.Update(courseType.Id, dto, CancellationToken.None);

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
            MaxStudents = 1,
            IsActive = true
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
    public async Task Update_ArchivingWithActiveCourses_ReturnsBadRequest()
    {
        // Arrange
        var instrument = new Instrument { Id = 1, Name = "Piano", IsActive = true };
        var courseType = CreateCourseType(instrument, "Test");

        var activeCourse = new Course
        {
            Id = Guid.NewGuid(),
            CourseTypeId = courseType.Id,
            Status = CourseStatus.Active
        };

        var dto = new UpdateCourseTypeDto
        {
            InstrumentId = 1,
            Name = "Test",
            DurationMinutes = 30,
            Type = CourseTypeCategory.Individual,
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
        var result = await _controller.Update(courseType.Id, dto, CancellationToken.None);

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
        var instrument = new Instrument { Id = 1, Name = "Piano", IsActive = true };
        var courseType = CreateCourseType(instrument, "Test");

        var courseTypes = new List<CourseType> { courseType };
        var courses = new List<Course>();

        var mockCourseTypeRepo = MockHelpers.CreateMockRepository(courseTypes);
        var mockCourseRepo = MockHelpers.CreateMockRepository(courses);

        _mockUnitOfWork.Setup(u => u.Repository<CourseType>()).Returns(mockCourseTypeRepo.Object);
        _mockUnitOfWork.Setup(u => u.Repository<Course>()).Returns(mockCourseRepo.Object);

        mockCourseTypeRepo.Setup(r => r.GetByIdAsync(courseType.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(courseType);

        // Act
        var result = await _controller.Delete(courseType.Id, CancellationToken.None);

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

        mockCourseTypeRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
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
        var instrument = new Instrument { Id = 1, Name = "Piano", IsActive = true };
        var courseType = CreateCourseType(instrument, "Test");

        var activeCourse = new Course
        {
            Id = Guid.NewGuid(),
            CourseTypeId = courseType.Id,
            Status = CourseStatus.Active
        };

        var courseTypes = new List<CourseType> { courseType };
        var courses = new List<Course> { activeCourse };

        var mockCourseTypeRepo = MockHelpers.CreateMockRepository(courseTypes);
        var mockCourseRepo = MockHelpers.CreateMockRepository(courses);

        _mockUnitOfWork.Setup(u => u.Repository<CourseType>()).Returns(mockCourseTypeRepo.Object);
        _mockUnitOfWork.Setup(u => u.Repository<Course>()).Returns(mockCourseRepo.Object);

        mockCourseTypeRepo.Setup(r => r.GetByIdAsync(courseType.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(courseType);

        // Act
        var result = await _controller.Delete(courseType.Id, CancellationToken.None);

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
        var instrument = new Instrument { Id = 1, Name = "Piano", IsActive = true };
        var courseType = CreateCourseType(instrument, "Test");
        courseType.IsActive = false;

        var courseTypes = new List<CourseType> { courseType };
        var courses = new List<Course>();
        var teacherCourseTypes = new List<TeacherCourseType>();

        var mockCourseTypeRepo = MockHelpers.CreateMockRepository(courseTypes);
        var mockCourseRepo = MockHelpers.CreateMockRepository(courses);
        var mockTeacherCourseTypeRepo = MockHelpers.CreateMockRepository(teacherCourseTypes);

        _mockUnitOfWork.Setup(u => u.Repository<CourseType>()).Returns(mockCourseTypeRepo.Object);
        _mockUnitOfWork.Setup(u => u.Repository<Course>()).Returns(mockCourseRepo.Object);
        _mockUnitOfWork.Setup(u => u.Repository<TeacherCourseType>()).Returns(mockTeacherCourseTypeRepo.Object);

        _mockPricingService.Setup(s => s.IsCurrentPricingInvoicedAsync(courseType.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.Reactivate(courseType.Id, CancellationToken.None);

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

    #region Pricing Endpoints Tests

    [Fact]
    public async Task GetPricingHistory_WithValidId_ReturnsPricingHistory()
    {
        // Arrange
        var instrument = new Instrument { Id = 1, Name = "Piano", IsActive = true };
        var courseType = CreateCourseType(instrument, "Test");
        var pricingVersion = courseType.PricingVersions.First();

        var courseTypes = new List<CourseType> { courseType };
        var mockCourseTypeRepo = MockHelpers.CreateMockRepository(courseTypes);
        _mockUnitOfWork.Setup(u => u.Repository<CourseType>()).Returns(mockCourseTypeRepo.Object);

        mockCourseTypeRepo.Setup(r => r.GetByIdAsync(courseType.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(courseType);

        _mockPricingService.Setup(s => s.GetPricingHistoryAsync(courseType.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CourseTypePricingVersion> { pricingVersion });

        // Act
        var result = await _controller.GetPricingHistory(courseType.Id, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var history = Assert.IsAssignableFrom<IEnumerable<CourseTypePricingVersionDto>>(okResult.Value);
        Assert.Single(history);
    }

    [Fact]
    public async Task CheckPricingEditability_NotInvoiced_CanEditDirectly()
    {
        // Arrange
        var instrument = new Instrument { Id = 1, Name = "Piano", IsActive = true };
        var courseType = CreateCourseType(instrument, "Test");

        var courseTypes = new List<CourseType> { courseType };
        var mockCourseTypeRepo = MockHelpers.CreateMockRepository(courseTypes);
        _mockUnitOfWork.Setup(u => u.Repository<CourseType>()).Returns(mockCourseTypeRepo.Object);

        mockCourseTypeRepo.Setup(r => r.GetByIdAsync(courseType.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(courseType);

        _mockPricingService.Setup(s => s.IsCurrentPricingInvoicedAsync(courseType.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.CheckPricingEditability(courseType.Id, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var editability = Assert.IsType<PricingEditabilityDto>(okResult.Value);
        Assert.True(editability.CanEditDirectly);
        Assert.False(editability.IsInvoiced);
    }

    [Fact]
    public async Task CheckPricingEditability_Invoiced_CannotEditDirectly()
    {
        // Arrange
        var instrument = new Instrument { Id = 1, Name = "Piano", IsActive = true };
        var courseType = CreateCourseType(instrument, "Test");

        var courseTypes = new List<CourseType> { courseType };
        var mockCourseTypeRepo = MockHelpers.CreateMockRepository(courseTypes);
        _mockUnitOfWork.Setup(u => u.Repository<CourseType>()).Returns(mockCourseTypeRepo.Object);

        mockCourseTypeRepo.Setup(r => r.GetByIdAsync(courseType.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(courseType);

        _mockPricingService.Setup(s => s.IsCurrentPricingInvoicedAsync(courseType.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.CheckPricingEditability(courseType.Id, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var editability = Assert.IsType<PricingEditabilityDto>(okResult.Value);
        Assert.False(editability.CanEditDirectly);
        Assert.True(editability.IsInvoiced);
    }

    [Fact]
    public async Task UpdatePricing_NotInvoiced_UpdatesSuccessfully()
    {
        // Arrange
        var instrument = new Instrument { Id = 1, Name = "Piano", IsActive = true };
        var courseType = CreateCourseType(instrument, "Test");
        var pricingVersion = courseType.PricingVersions.First();

        var courseTypes = new List<CourseType> { courseType };
        var mockCourseTypeRepo = MockHelpers.CreateMockRepository(courseTypes);
        _mockUnitOfWork.Setup(u => u.Repository<CourseType>()).Returns(mockCourseTypeRepo.Object);

        mockCourseTypeRepo.Setup(r => r.GetByIdAsync(courseType.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(courseType);

        var dto = new UpdateCourseTypePricingDto { PriceAdult = 60, PriceChild = 50 };

        var updatedPricing = new CourseTypePricingVersion
        {
            Id = pricingVersion.Id,
            CourseTypeId = courseType.Id,
            PriceAdult = 60,
            PriceChild = 50,
            ValidFrom = pricingVersion.ValidFrom,
            IsCurrent = true,
            CreatedAt = pricingVersion.CreatedAt
        };

        _mockPricingService.Setup(s => s.UpdateCurrentPricingAsync(
            courseType.Id, dto.PriceAdult, dto.PriceChild, It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedPricing);

        // Act
        var result = await _controller.UpdatePricing(courseType.Id, dto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedPricing = Assert.IsType<CourseTypePricingVersionDto>(okResult.Value);
        Assert.Equal(60, returnedPricing.PriceAdult);
        Assert.Equal(50, returnedPricing.PriceChild);
    }

    [Fact]
    public async Task CreatePricingVersion_WithValidData_CreatesNewVersion()
    {
        // Arrange
        var instrument = new Instrument { Id = 1, Name = "Piano", IsActive = true };
        var courseType = CreateCourseType(instrument, "Test");

        var courseTypes = new List<CourseType> { courseType };
        var mockCourseTypeRepo = MockHelpers.CreateMockRepository(courseTypes);
        _mockUnitOfWork.Setup(u => u.Repository<CourseType>()).Returns(mockCourseTypeRepo.Object);

        mockCourseTypeRepo.Setup(r => r.GetByIdAsync(courseType.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(courseType);

        var validFrom = DateOnly.FromDateTime(DateTime.Today.AddDays(30));
        var dto = new CreateCourseTypePricingVersionDto
        {
            PriceAdult = 70,
            PriceChild = 60,
            ValidFrom = validFrom
        };

        var newVersion = new CourseTypePricingVersion
        {
            Id = Guid.NewGuid(),
            CourseTypeId = courseType.Id,
            PriceAdult = 70,
            PriceChild = 60,
            ValidFrom = validFrom,
            IsCurrent = true,
            CreatedAt = DateTime.UtcNow
        };

        _mockPricingService.Setup(s => s.CreateNewPricingVersionAsync(
            courseType.Id, dto.PriceAdult, dto.PriceChild, dto.ValidFrom, It.IsAny<CancellationToken>()))
            .ReturnsAsync(newVersion);

        // Act
        var result = await _controller.CreatePricingVersion(courseType.Id, dto, CancellationToken.None);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var returnedPricing = Assert.IsType<CourseTypePricingVersionDto>(createdResult.Value);
        Assert.Equal(70, returnedPricing.PriceAdult);
        Assert.Equal(60, returnedPricing.PriceChild);
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
        Assert.Equal(0, okResult.Value);
    }

    #endregion
}
