using Moq;
using Xunit;
using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Enums;
using BosDAT.Core.Interfaces;
using BosDAT.Infrastructure.Services;
using BosDAT.API.Tests.Helpers;
using static BosDAT.API.Tests.Helpers.TestDataFactory;

namespace BosDAT.API.Tests.Services;

public class CourseTypeServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ICourseTypePricingService> _mockPricingService;
    private readonly CourseTypeService _service;

    public CourseTypeServiceTests()
    {
        _mockUnitOfWork = MockHelpers.CreateMockUnitOfWork();
        _mockPricingService = new Mock<ICourseTypePricingService>();
        _service = new CourseTypeService(_mockUnitOfWork.Object, _mockPricingService.Object);
    }

    private static CourseType CreateCourseTypeWithInstrument(Instrument instrument, string name = "Beginner Piano")
    {
        return new CourseType
        {
            Id = Guid.NewGuid(),
            Name = name,
            InstrumentId = instrument.Id,
            Instrument = instrument,
            IsActive = true,
            Type = CourseTypeCategory.Individual,
            MaxStudents = 1,
            DurationMinutes = 30,
            PricingVersions = new List<CourseTypePricingVersion>()
        };
    }

    private void SetupMappingRepositories(Guid courseTypeId, int activeCourses = 0, bool hasTeachers = false)
    {
        var mockCourseRepo = MockHelpers.CreateMockRepository(new List<Course>());
        _mockUnitOfWork.Setup(u => u.Repository<Course>()).Returns(mockCourseRepo.Object);

        var teacherCourseTypes = hasTeachers
            ? new List<TeacherCourseType>
            {
                new() { CourseTypeId = courseTypeId, Teacher = CreateTeacher() }
            }
            : new List<TeacherCourseType>();

        var mockTeacherCourseTypeRepo = MockHelpers.CreateMockRepository(teacherCourseTypes);
        _mockUnitOfWork.Setup(u => u.Repository<TeacherCourseType>()).Returns(mockTeacherCourseTypeRepo.Object);
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsCourseTypeDto()
    {
        var instrument = CreateInstrument();
        var courseType = CreateCourseTypeWithInstrument(instrument);

        var mockCourseTypeRepo = MockHelpers.CreateMockRepository(new List<CourseType> { courseType });
        _mockUnitOfWork.Setup(u => u.Repository<CourseType>()).Returns(mockCourseTypeRepo.Object);

        _mockPricingService
            .Setup(s => s.IsCurrentPricingInvoicedAsync(courseType.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        SetupMappingRepositories(courseType.Id);

        var result = await _service.GetByIdAsync(courseType.Id);

        Assert.NotNull(result);
        Assert.Equal(courseType.Id, result.Id);
        Assert.Equal(courseType.Name, result.Name);
        Assert.Equal(instrument.Name, result.InstrumentName);
        Assert.True(result.CanEditPricingDirectly);
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ReturnsNull()
    {
        var mockCourseTypeRepo = MockHelpers.CreateMockRepository(new List<CourseType>());
        _mockUnitOfWork.Setup(u => u.Repository<CourseType>()).Returns(mockCourseTypeRepo.Object);

        var result = await _service.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_WhenPricingIsInvoiced_ReturnsCanEditFalse()
    {
        var instrument = CreateInstrument();
        var courseType = CreateCourseTypeWithInstrument(instrument);

        var mockCourseTypeRepo = MockHelpers.CreateMockRepository(new List<CourseType> { courseType });
        _mockUnitOfWork.Setup(u => u.Repository<CourseType>()).Returns(mockCourseTypeRepo.Object);

        _mockPricingService
            .Setup(s => s.IsCurrentPricingInvoicedAsync(courseType.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        SetupMappingRepositories(courseType.Id);

        var result = await _service.GetByIdAsync(courseType.Id);

        Assert.NotNull(result);
        Assert.False(result.CanEditPricingDirectly);
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithInvalidInstrument_ReturnsError()
    {
        var mockInstrumentRepo = MockHelpers.CreateMockRepository(new List<Instrument>());
        mockInstrumentRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Instrument?)null);
        _mockUnitOfWork.Setup(u => u.Repository<Instrument>()).Returns(mockInstrumentRepo.Object);

        var dto = new CreateCourseTypeDto
        {
            Name = "New Type",
            InstrumentId = 999,
            DurationMinutes = 30,
            PriceAdult = 50m,
            PriceChild = 40m
        };

        var (courseType, error) = await _service.CreateAsync(dto);

        Assert.Null(courseType);
        Assert.Equal("Instrument not found", error);
    }

    [Fact]
    public async Task CreateAsync_WhenChildPriceExceedsAdultPrice_ReturnsError()
    {
        var instrument = CreateInstrument();
        var mockInstrumentRepo = MockHelpers.CreateMockRepository(new List<Instrument> { instrument });
        mockInstrumentRepo
            .Setup(r => r.GetByIdAsync(instrument.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(instrument);
        _mockUnitOfWork.Setup(u => u.Repository<Instrument>()).Returns(mockInstrumentRepo.Object);

        var dto = new CreateCourseTypeDto
        {
            Name = "New Type",
            InstrumentId = instrument.Id,
            DurationMinutes = 30,
            PriceAdult = 40m,
            PriceChild = 50m
        };

        var (courseType, error) = await _service.CreateAsync(dto);

        Assert.Null(courseType);
        Assert.Contains("Child price", error);
    }

    [Fact]
    public async Task CreateAsync_WithValidData_CreatesCourseTypeWithPricing()
    {
        var instrument = CreateInstrument();
        var mockInstrumentRepo = MockHelpers.CreateMockRepository(new List<Instrument> { instrument });
        mockInstrumentRepo
            .Setup(r => r.GetByIdAsync(instrument.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(instrument);

        CourseType? addedCourseType = null;
        var mockCourseTypeRepo = MockHelpers.CreateMockRepository(new List<CourseType>());
        mockCourseTypeRepo
            .Setup(r => r.AddAsync(It.IsAny<CourseType>(), It.IsAny<CancellationToken>()))
            .Callback<CourseType, CancellationToken>((ct, _) =>
            {
                ct.Instrument = instrument;
                ct.PricingVersions = new List<CourseTypePricingVersion>();
                addedCourseType = ct;
            })
            .ReturnsAsync((CourseType ct, CancellationToken _) => ct);
        mockCourseTypeRepo.Setup(r => r.Query())
            .Returns(() =>
            {
                var list = addedCourseType != null
                    ? new List<CourseType> { addedCourseType }
                    : new List<CourseType>();
                return list.AsQueryable().BuildMockDbSet().Object;
            });

        _mockUnitOfWork.Setup(u => u.Repository<Instrument>()).Returns(mockInstrumentRepo.Object);
        _mockUnitOfWork.Setup(u => u.Repository<CourseType>()).Returns(mockCourseTypeRepo.Object);

        var pricingVersion = new CourseTypePricingVersion
        {
            Id = Guid.NewGuid(),
            PriceAdult = 50m,
            PriceChild = 40m,
            IsCurrent = true
        };
        _mockPricingService
            .Setup(s => s.CreateInitialPricingVersionAsync(It.IsAny<Guid>(), 50m, 40m, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pricingVersion);
        _mockPricingService
            .Setup(s => s.IsCurrentPricingInvoicedAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        SetupMappingRepositories(Guid.Empty);

        var dto = new CreateCourseTypeDto
        {
            Name = "New Type",
            InstrumentId = instrument.Id,
            DurationMinutes = 30,
            PriceAdult = 50m,
            PriceChild = 40m,
            Type = CourseTypeCategory.Individual,
            MaxStudents = 1
        };

        var (courseType, error) = await _service.CreateAsync(dto);

        Assert.Null(error);
        Assert.NotNull(courseType);
        Assert.Equal("New Type", courseType.Name);
        _mockPricingService.Verify(
            s => s.CreateInitialPricingVersionAsync(It.IsAny<Guid>(), 50m, 40m, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithInvalidId_ReturnsNotFound()
    {
        var mockCourseTypeRepo = MockHelpers.CreateMockRepository(new List<CourseType>());
        _mockUnitOfWork.Setup(u => u.Repository<CourseType>()).Returns(mockCourseTypeRepo.Object);

        var dto = new UpdateCourseTypeDto { Name = "X", InstrumentId = 1, IsActive = true };

        var (courseType, error, notFound) = await _service.UpdateAsync(Guid.NewGuid(), dto);

        Assert.True(notFound);
        Assert.Null(courseType);
    }

    [Fact]
    public async Task UpdateAsync_WithInvalidInstrument_ReturnsError()
    {
        var instrument = CreateInstrument();
        var courseType = CreateCourseTypeWithInstrument(instrument);

        var mockCourseTypeRepo = MockHelpers.CreateMockRepository(new List<CourseType> { courseType });
        _mockUnitOfWork.Setup(u => u.Repository<CourseType>()).Returns(mockCourseTypeRepo.Object);

        var mockInstrumentRepo = MockHelpers.CreateMockRepository(new List<Instrument>());
        mockInstrumentRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Instrument?)null);
        _mockUnitOfWork.Setup(u => u.Repository<Instrument>()).Returns(mockInstrumentRepo.Object);

        var dto = new UpdateCourseTypeDto { Name = courseType.Name, InstrumentId = 999, IsActive = true };

        var (result, error, notFound) = await _service.UpdateAsync(courseType.Id, dto);

        Assert.False(notFound);
        Assert.Null(result);
        Assert.Equal("Instrument not found", error);
    }

    [Fact]
    public async Task UpdateAsync_ArchivingCourseTypeWithActiveCourses_ReturnsError()
    {
        var instrument = CreateInstrument();
        var courseType = CreateCourseTypeWithInstrument(instrument);
        courseType.IsActive = true;

        var mockCourseTypeRepo = MockHelpers.CreateMockRepository(new List<CourseType> { courseType });
        _mockUnitOfWork.Setup(u => u.Repository<CourseType>()).Returns(mockCourseTypeRepo.Object);

        var mockInstrumentRepo = MockHelpers.CreateMockRepository(new List<Instrument> { instrument });
        mockInstrumentRepo
            .Setup(r => r.GetByIdAsync(instrument.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(instrument);
        _mockUnitOfWork.Setup(u => u.Repository<Instrument>()).Returns(mockInstrumentRepo.Object);

        var activeCourse = CreateCourse(CreateTeacher(), courseType);
        var mockCourseRepo = MockHelpers.CreateMockRepository(new List<Course> { activeCourse });
        _mockUnitOfWork.Setup(u => u.Repository<Course>()).Returns(mockCourseRepo.Object);

        var dto = new UpdateCourseTypeDto
        {
            Name = courseType.Name,
            InstrumentId = instrument.Id,
            IsActive = false,
            DurationMinutes = 30
        };

        var (result, error, notFound) = await _service.UpdateAsync(courseType.Id, dto);

        Assert.False(notFound);
        Assert.Null(result);
        Assert.Contains("Cannot archive", error);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithInvalidId_ReturnsNotFound()
    {
        var mockCourseTypeRepo = MockHelpers.CreateMockRepository(new List<CourseType>());
        mockCourseTypeRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CourseType?)null);
        _mockUnitOfWork.Setup(u => u.Repository<CourseType>()).Returns(mockCourseTypeRepo.Object);

        var (success, error, notFound) = await _service.DeleteAsync(Guid.NewGuid());

        Assert.True(notFound);
        Assert.False(success);
    }

    [Fact]
    public async Task DeleteAsync_WithActiveCourses_ReturnsError()
    {
        var instrument = CreateInstrument();
        var courseType = CreateCourseTypeWithInstrument(instrument);
        var activeCourse = CreateCourse(CreateTeacher(), courseType);

        var mockCourseTypeRepo = MockHelpers.CreateMockRepository(new List<CourseType> { courseType });
        mockCourseTypeRepo
            .Setup(r => r.GetByIdAsync(courseType.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(courseType);
        _mockUnitOfWork.Setup(u => u.Repository<CourseType>()).Returns(mockCourseTypeRepo.Object);

        var mockCourseRepo = MockHelpers.CreateMockRepository(new List<Course> { activeCourse });
        _mockUnitOfWork.Setup(u => u.Repository<Course>()).Returns(mockCourseRepo.Object);

        var (success, error, notFound) = await _service.DeleteAsync(courseType.Id);

        Assert.False(notFound);
        Assert.False(success);
        Assert.Contains("Cannot archive", error);
    }

    [Fact]
    public async Task DeleteAsync_WithNoCourses_DeactivatesCourseType()
    {
        var instrument = CreateInstrument();
        var courseType = CreateCourseTypeWithInstrument(instrument);
        courseType.IsActive = true;

        var mockCourseTypeRepo = MockHelpers.CreateMockRepository(new List<CourseType> { courseType });
        mockCourseTypeRepo
            .Setup(r => r.GetByIdAsync(courseType.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(courseType);
        _mockUnitOfWork.Setup(u => u.Repository<CourseType>()).Returns(mockCourseTypeRepo.Object);

        var mockCourseRepo = MockHelpers.CreateMockRepository(new List<Course>());
        _mockUnitOfWork.Setup(u => u.Repository<Course>()).Returns(mockCourseRepo.Object);

        var (success, error, notFound) = await _service.DeleteAsync(courseType.Id);

        Assert.False(notFound);
        Assert.True(success);
        Assert.Null(error);
        Assert.False(courseType.IsActive);
    }

    #endregion

    #region ReactivateAsync Tests

    [Fact]
    public async Task ReactivateAsync_WithInvalidId_ReturnsNotFound()
    {
        var mockCourseTypeRepo = MockHelpers.CreateMockRepository(new List<CourseType>());
        _mockUnitOfWork.Setup(u => u.Repository<CourseType>()).Returns(mockCourseTypeRepo.Object);

        var (result, notFound) = await _service.ReactivateAsync(Guid.NewGuid());

        Assert.True(notFound);
        Assert.Null(result);
    }

    [Fact]
    public async Task ReactivateAsync_WithValidId_SetsIsActiveTrue()
    {
        var instrument = CreateInstrument();
        var courseType = CreateCourseTypeWithInstrument(instrument);
        courseType.IsActive = false;

        var mockCourseTypeRepo = MockHelpers.CreateMockRepository(new List<CourseType> { courseType });
        _mockUnitOfWork.Setup(u => u.Repository<CourseType>()).Returns(mockCourseTypeRepo.Object);

        _mockPricingService
            .Setup(s => s.IsCurrentPricingInvoicedAsync(courseType.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        SetupMappingRepositories(courseType.Id);

        var (result, notFound) = await _service.ReactivateAsync(courseType.Id);

        Assert.False(notFound);
        Assert.NotNull(result);
        Assert.True(courseType.IsActive);
    }

    #endregion

    #region CheckPricingEditabilityAsync Tests

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task CheckPricingEditabilityAsync_ReturnsCorrectEditability(bool isInvoiced, bool canEditDirectly)
    {
        var instrument = CreateInstrument();
        var courseType = CreateCourseTypeWithInstrument(instrument);

        var mockCourseTypeRepo = MockHelpers.CreateMockRepository(new List<CourseType> { courseType });
        mockCourseTypeRepo
            .Setup(r => r.GetByIdAsync(courseType.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(courseType);
        _mockUnitOfWork.Setup(u => u.Repository<CourseType>()).Returns(mockCourseTypeRepo.Object);

        _mockPricingService
            .Setup(s => s.IsCurrentPricingInvoicedAsync(courseType.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(isInvoiced);

        var (result, notFound) = await _service.CheckPricingEditabilityAsync(courseType.Id);

        Assert.False(notFound);
        Assert.NotNull(result);
        Assert.Equal(canEditDirectly, result.CanEditDirectly);
        Assert.Equal(isInvoiced, result.IsInvoiced);
    }

    [Fact]
    public async Task CheckPricingEditabilityAsync_WithInvalidId_ReturnsNotFound()
    {
        var mockCourseTypeRepo = MockHelpers.CreateMockRepository(new List<CourseType>());
        mockCourseTypeRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CourseType?)null);
        _mockUnitOfWork.Setup(u => u.Repository<CourseType>()).Returns(mockCourseTypeRepo.Object);

        var (result, notFound) = await _service.CheckPricingEditabilityAsync(Guid.NewGuid());

        Assert.True(notFound);
        Assert.Null(result);
    }

    #endregion

    #region UpdatePricingAsync Tests

    [Fact]
    public async Task UpdatePricingAsync_WhenChildExceedsAdult_ReturnsError()
    {
        var instrument = CreateInstrument();
        var courseType = CreateCourseTypeWithInstrument(instrument);

        var mockCourseTypeRepo = MockHelpers.CreateMockRepository(new List<CourseType> { courseType });
        mockCourseTypeRepo
            .Setup(r => r.GetByIdAsync(courseType.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(courseType);
        _mockUnitOfWork.Setup(u => u.Repository<CourseType>()).Returns(mockCourseTypeRepo.Object);

        var dto = new UpdateCourseTypePricingDto { PriceAdult = 40m, PriceChild = 50m };

        var (pricing, error, notFound) = await _service.UpdatePricingAsync(courseType.Id, dto);

        Assert.False(notFound);
        Assert.Null(pricing);
        Assert.Contains("Child price", error);
    }

    #endregion
}
