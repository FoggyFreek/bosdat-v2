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
    private readonly Mock<ICourseTypeService> _mockCourseTypeService;
    private readonly CourseTypesController _controller;

    public CourseTypesControllerTests()
    {
        _mockCourseTypeService = new Mock<ICourseTypeService>();
        _controller = new CourseTypesController(_mockCourseTypeService.Object);
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
        var courseTypeDtos = new List<CourseTypeDto>
        {
            new CourseTypeDto { Id = Guid.NewGuid(), Name = "Beginner Piano" },
            new CourseTypeDto { Id = Guid.NewGuid(), Name = "Intermediate Piano" }
        };

        _mockCourseTypeService.Setup(s => s.GetAllAsync(null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(courseTypeDtos);

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
        var courseTypeDtos = new List<CourseTypeDto>
        {
            new CourseTypeDto { Id = Guid.NewGuid(), Name = "Active Piano", IsActive = true }
        };

        _mockCourseTypeService.Setup(s => s.GetAllAsync(true, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(courseTypeDtos);

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
        var courseTypeDtos = new List<CourseTypeDto>
        {
            new CourseTypeDto { Id = Guid.NewGuid(), Name = "Beginner Piano", InstrumentId = 1 }
        };

        _mockCourseTypeService.Setup(s => s.GetAllAsync(null, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(courseTypeDtos);

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
        var id = Guid.NewGuid();
        var courseTypeDto = new CourseTypeDto { Id = id, Name = "Beginner Piano" };

        _mockCourseTypeService.Setup(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(courseTypeDto);

        // Act
        var result = await _controller.GetById(id, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedCourseType = Assert.IsType<CourseTypeDto>(okResult.Value);
        Assert.Equal(id, returnedCourseType.Id);
        Assert.Equal("Beginner Piano", returnedCourseType.Name);
    }

    [Fact]
    public async Task GetById_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        _mockCourseTypeService.Setup(s => s.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CourseTypeDto?)null);

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

        var createdDto = new CourseTypeDto { Id = Guid.NewGuid(), Name = "Beginner Piano" };

        _mockCourseTypeService.Setup(s => s.CreateAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync((createdDto, (string?)null));

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

        _mockCourseTypeService.Setup(s => s.CreateAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((CourseTypeDto?)null, "Instrument not found"));

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
        var dto = new CreateCourseTypeDto
        {
            InstrumentId = 1,
            Name = "Invalid Pricing",
            DurationMinutes = 30,
            Type = CourseTypeCategory.Individual,
            PriceAdult = 40,
            PriceChild = 50,
            MaxStudents = 1
        };

        _mockCourseTypeService.Setup(s => s.CreateAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((CourseTypeDto?)null, "Child price cannot be higher than adult price"));

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
        var id = Guid.NewGuid();
        var dto = new UpdateCourseTypeDto
        {
            InstrumentId = 1,
            Name = "Updated Name",
            DurationMinutes = 45,
            Type = CourseTypeCategory.Individual,
            MaxStudents = 1,
            IsActive = true
        };

        var updatedDto = new CourseTypeDto { Id = id, Name = "Updated Name" };

        _mockCourseTypeService.Setup(s => s.UpdateAsync(id, dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync((updatedDto, (string?)null, false));

        // Act
        var result = await _controller.Update(id, dto, CancellationToken.None);

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

        _mockCourseTypeService.Setup(s => s.UpdateAsync(It.IsAny<Guid>(), dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((CourseTypeDto?)null, (string?)null, true));

        // Act
        var result = await _controller.Update(Guid.NewGuid(), dto, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Update_ArchivingWithActiveCourses_ReturnsBadRequest()
    {
        // Arrange
        var id = Guid.NewGuid();
        var dto = new UpdateCourseTypeDto
        {
            InstrumentId = 1,
            Name = "Test",
            DurationMinutes = 30,
            Type = CourseTypeCategory.Individual,
            MaxStudents = 1,
            IsActive = false
        };

        _mockCourseTypeService.Setup(s => s.UpdateAsync(id, dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((CourseTypeDto?)null, "Cannot archive course type: 1 active course(s) are using it", false));

        // Act
        var result = await _controller.Update(id, dto, CancellationToken.None);

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
        var id = Guid.NewGuid();

        _mockCourseTypeService.Setup(s => s.DeleteAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, (string?)null, false));

        // Act
        var result = await _controller.Delete(id, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        _mockCourseTypeService.Setup(s => s.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, (string?)null, true));

        // Act
        var result = await _controller.Delete(Guid.NewGuid(), CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Delete_WithActiveCourses_ReturnsBadRequest()
    {
        // Arrange
        var id = Guid.NewGuid();

        _mockCourseTypeService.Setup(s => s.DeleteAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, "Cannot archive course type: 1 active course(s) are using it", false));

        // Act
        var result = await _controller.Delete(id, CancellationToken.None);

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
        var id = Guid.NewGuid();
        var reactivatedDto = new CourseTypeDto { Id = id, Name = "Test", IsActive = true };

        _mockCourseTypeService.Setup(s => s.ReactivateAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((reactivatedDto, false));

        // Act
        var result = await _controller.Reactivate(id, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedDto = Assert.IsType<CourseTypeDto>(okResult.Value);
        Assert.True(returnedDto.IsActive);
    }

    [Fact]
    public async Task Reactivate_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        _mockCourseTypeService.Setup(s => s.ReactivateAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((CourseTypeDto?)null, true));

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
        var id = Guid.NewGuid();
        var pricingHistory = new List<CourseTypePricingVersionDto>
        {
            new CourseTypePricingVersionDto { Id = Guid.NewGuid(), CourseTypeId = id }
        };

        _mockCourseTypeService.Setup(s => s.GetPricingHistoryAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((pricingHistory, false));

        // Act
        var result = await _controller.GetPricingHistory(id, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var history = Assert.IsAssignableFrom<IEnumerable<CourseTypePricingVersionDto>>(okResult.Value);
        Assert.Single(history);
    }

    [Fact]
    public async Task CheckPricingEditability_NotInvoiced_CanEditDirectly()
    {
        // Arrange
        var id = Guid.NewGuid();
        var editability = new PricingEditabilityDto
        {
            CanEditDirectly = true,
            IsInvoiced = false
        };

        _mockCourseTypeService.Setup(s => s.CheckPricingEditabilityAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((editability, false));

        // Act
        var result = await _controller.CheckPricingEditability(id, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedEditability = Assert.IsType<PricingEditabilityDto>(okResult.Value);
        Assert.True(returnedEditability.CanEditDirectly);
        Assert.False(returnedEditability.IsInvoiced);
    }

    [Fact]
    public async Task CheckPricingEditability_Invoiced_CannotEditDirectly()
    {
        // Arrange
        var id = Guid.NewGuid();
        var editability = new PricingEditabilityDto
        {
            CanEditDirectly = false,
            IsInvoiced = true,
            Reason = "Current pricing has been used in invoices. Create a new version instead."
        };

        _mockCourseTypeService.Setup(s => s.CheckPricingEditabilityAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((editability, false));

        // Act
        var result = await _controller.CheckPricingEditability(id, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedEditability = Assert.IsType<PricingEditabilityDto>(okResult.Value);
        Assert.False(returnedEditability.CanEditDirectly);
        Assert.True(returnedEditability.IsInvoiced);
    }

    [Fact]
    public async Task UpdatePricing_NotInvoiced_UpdatesSuccessfully()
    {
        // Arrange
        var id = Guid.NewGuid();
        var dto = new UpdateCourseTypePricingDto { PriceAdult = 60, PriceChild = 50 };
        var updatedPricing = new CourseTypePricingVersionDto
        {
            Id = Guid.NewGuid(),
            CourseTypeId = id,
            PriceAdult = 60,
            PriceChild = 50
        };

        _mockCourseTypeService.Setup(s => s.UpdatePricingAsync(id, dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync((updatedPricing, (string?)null, false));

        // Act
        var result = await _controller.UpdatePricing(id, dto, CancellationToken.None);

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
        var id = Guid.NewGuid();
        var validFrom = DateOnly.FromDateTime(DateTime.Today.AddDays(30));
        var dto = new CreateCourseTypePricingVersionDto
        {
            PriceAdult = 70,
            PriceChild = 60,
            ValidFrom = validFrom
        };

        var newPricing = new CourseTypePricingVersionDto
        {
            Id = Guid.NewGuid(),
            CourseTypeId = id,
            PriceAdult = 70,
            PriceChild = 60,
            ValidFrom = validFrom
        };

        _mockCourseTypeService.Setup(s => s.CreatePricingVersionAsync(id, dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync((newPricing, (string?)null, false));

        // Act
        var result = await _controller.CreatePricingVersion(id, dto, CancellationToken.None);

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
        _mockCourseTypeService.Setup(s => s.GetTeachersCountForInstrumentAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((1, false));

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
        _mockCourseTypeService.Setup(s => s.GetTeachersCountForInstrumentAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((int?)null, true));

        // Act
        var result = await _controller.GetTeachersCountForInstrument(999, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetTeachersCountForInstrument_WithNoTeachers_ReturnsZero()
    {
        // Arrange
        _mockCourseTypeService.Setup(s => s.GetTeachersCountForInstrumentAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((0, false));

        // Act
        var result = await _controller.GetTeachersCountForInstrument(1, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(0, okResult.Value);
    }

    #endregion
}
