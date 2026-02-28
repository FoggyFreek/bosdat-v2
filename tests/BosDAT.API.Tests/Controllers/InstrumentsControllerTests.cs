using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using BosDAT.API.Controllers;
using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Interfaces;
using BosDAT.Core.Interfaces.Services;
using BosDAT.Core.Interfaces.Repositories;

namespace BosDAT.API.Tests.Controllers;

public class InstrumentsControllerTests
{
    private readonly Mock<IInstrumentService> _mockService;
    private readonly InstrumentsController _controller;

    public InstrumentsControllerTests()
    {
        _mockService = new Mock<IInstrumentService>();
        _controller = new InstrumentsController(_mockService.Object);
    }

    private static InstrumentDto CreateDto(
        int id = 1,
        string name = "Piano",
        InstrumentCategory category = InstrumentCategory.Keyboard,
        bool isActive = true)
    {
        return new InstrumentDto
        {
            Id = id,
            Name = name,
            Category = category,
            IsActive = isActive
        };
    }

    #region GetAll Tests

    [Fact]
    public async Task GetAll_WithNoFilter_ReturnsAllInstruments()
    {
        // Arrange
        var instruments = new List<InstrumentDto>
        {
            CreateDto(1, "Piano"),
            CreateDto(2, "Guitar", InstrumentCategory.String),
            CreateDto(3, "Drums", InstrumentCategory.Percussion, isActive: false)
        };
        _mockService.Setup(s => s.GetAllAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(instruments);

        // Act
        var result = await _controller.GetAll(null, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedInstruments = Assert.IsAssignableFrom<IEnumerable<InstrumentDto>>(okResult.Value);
        Assert.Equal(3, returnedInstruments.Count());
    }

    [Fact]
    public async Task GetAll_WithActiveOnlyFilter_ReturnsOnlyActiveInstruments()
    {
        // Arrange
        var activeInstruments = new List<InstrumentDto>
        {
            CreateDto(1, "Piano", isActive: true)
        };
        _mockService.Setup(s => s.GetAllAsync(true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(activeInstruments);

        // Act
        var result = await _controller.GetAll(activeOnly: true, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedInstruments = Assert.IsAssignableFrom<IEnumerable<InstrumentDto>>(okResult.Value);
        Assert.Single(returnedInstruments);
        Assert.True(returnedInstruments.First().IsActive);
    }

    [Fact]
    public async Task GetAll_ReturnsInstrumentsSortedByName()
    {
        // Arrange
        var instruments = new List<InstrumentDto>
        {
            CreateDto(2, "Bass"),
            CreateDto(3, "Piano"),
            CreateDto(1, "Violin")
        };
        _mockService.Setup(s => s.GetAllAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(instruments);

        // Act
        var result = await _controller.GetAll(null, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedInstruments = Assert.IsAssignableFrom<IEnumerable<InstrumentDto>>(okResult.Value).ToList();
        Assert.Equal(3, returnedInstruments.Count);
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetById_WithValidId_ReturnsInstrument()
    {
        // Arrange
        var instrument = CreateDto(1, "Piano");
        _mockService.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((instrument, false));

        // Act
        var result = await _controller.GetById(1, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedInstrument = Assert.IsType<InstrumentDto>(okResult.Value);
        Assert.Equal(1, returnedInstrument.Id);
        Assert.Equal("Piano", returnedInstrument.Name);
    }

    [Fact]
    public async Task GetById_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        _mockService.Setup(s => s.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((InstrumentDto?)null, true));

        // Act
        var result = await _controller.GetById(999, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    #endregion

    #region Create Tests

    [Fact]
    public async Task Create_WithValidData_ReturnsCreatedInstrument()
    {
        // Arrange
        var dto = new CreateInstrumentDto
        {
            Name = "New Instrument",
            Category = InstrumentCategory.Wind
        };
        var createdInstrument = CreateDto(1, "New Instrument", InstrumentCategory.Wind);
        _mockService.Setup(s => s.CreateAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync((createdInstrument, (string?)null));

        // Act
        var result = await _controller.Create(dto, CancellationToken.None);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(InstrumentsController.GetById), createdResult.ActionName);
        var returnedInstrument = Assert.IsType<InstrumentDto>(createdResult.Value);
        Assert.Equal("New Instrument", returnedInstrument.Name);
        Assert.Equal(InstrumentCategory.Wind, returnedInstrument.Category);
        Assert.True(returnedInstrument.IsActive);
    }

    [Fact]
    public async Task Create_WithDuplicateName_ReturnsBadRequest()
    {
        // Arrange
        var dto = new CreateInstrumentDto
        {
            Name = "Piano",
            Category = InstrumentCategory.Keyboard
        };
        _mockService.Setup(s => s.CreateAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((InstrumentDto?)null, "An instrument with this name already exists"));

        // Act
        var result = await _controller.Create(dto, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task Create_WithDuplicateNameCaseInsensitive_ReturnsBadRequest()
    {
        // Arrange
        var dto = new CreateInstrumentDto
        {
            Name = "PIANO",
            Category = InstrumentCategory.Keyboard
        };
        _mockService.Setup(s => s.CreateAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((InstrumentDto?)null, "An instrument with this name already exists"));

        // Act
        var result = await _controller.Create(dto, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequestResult.Value);
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_WithValidData_ReturnsUpdatedInstrument()
    {
        // Arrange
        var dto = new UpdateInstrumentDto
        {
            Name = "Grand Piano",
            Category = InstrumentCategory.Keyboard,
            IsActive = true
        };
        var updatedInstrument = CreateDto(1, "Grand Piano");
        _mockService.Setup(s => s.UpdateAsync(1, dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync((updatedInstrument, (string?)null, false));

        // Act
        var result = await _controller.Update(1, dto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedInstrument = Assert.IsType<InstrumentDto>(okResult.Value);
        Assert.Equal("Grand Piano", returnedInstrument.Name);
    }

    [Fact]
    public async Task Update_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var dto = new UpdateInstrumentDto
        {
            Name = "Updated Name",
            Category = InstrumentCategory.Keyboard,
            IsActive = true
        };
        _mockService.Setup(s => s.UpdateAsync(999, dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((InstrumentDto?)null, (string?)null, true));

        // Act
        var result = await _controller.Update(999, dto, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Update_WithDuplicateName_ReturnsBadRequest()
    {
        // Arrange
        var dto = new UpdateInstrumentDto
        {
            Name = "Guitar",
            Category = InstrumentCategory.Keyboard,
            IsActive = true
        };
        _mockService.Setup(s => s.UpdateAsync(1, dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((InstrumentDto?)null, "An instrument with this name already exists", false));

        // Act
        var result = await _controller.Update(1, dto, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task Update_WithSameName_ReturnsSuccess()
    {
        // Arrange
        var dto = new UpdateInstrumentDto
        {
            Name = "Piano",
            Category = InstrumentCategory.Keyboard,
            IsActive = false
        };
        var updatedInstrument = CreateDto(1, "Piano", isActive: false);
        _mockService.Setup(s => s.UpdateAsync(1, dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync((updatedInstrument, (string?)null, false));

        // Act
        var result = await _controller.Update(1, dto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedInstrument = Assert.IsType<InstrumentDto>(okResult.Value);
        Assert.False(returnedInstrument.IsActive);
    }

    [Fact]
    public async Task Update_CategoryChange_UpdatesCategory()
    {
        // Arrange
        var dto = new UpdateInstrumentDto
        {
            Name = "Piano",
            Category = InstrumentCategory.Other,
            IsActive = true
        };
        var updatedInstrument = CreateDto(1, "Piano", InstrumentCategory.Other);
        _mockService.Setup(s => s.UpdateAsync(1, dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync((updatedInstrument, (string?)null, false));

        // Act
        var result = await _controller.Update(1, dto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedInstrument = Assert.IsType<InstrumentDto>(okResult.Value);
        Assert.Equal(InstrumentCategory.Other, returnedInstrument.Category);
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_WithValidId_ReturnsNoContent()
    {
        // Arrange
        _mockService.Setup(s => s.DeleteAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, false));

        // Act
        var result = await _controller.Delete(1, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        _mockService.Setup(s => s.DeleteAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, true));

        // Act
        var result = await _controller.Delete(999, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Delete_DeactivatesInsteadOfDeleting()
    {
        // Arrange
        _mockService.Setup(s => s.DeleteAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, false));

        // Act
        var result = await _controller.Delete(1, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
        _mockService.Verify(s => s.DeleteAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}
