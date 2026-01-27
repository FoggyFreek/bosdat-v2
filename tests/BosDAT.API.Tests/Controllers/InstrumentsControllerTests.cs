using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using BosDAT.API.Controllers;
using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Interfaces;
using BosDAT.API.Tests.Helpers;

namespace BosDAT.API.Tests.Controllers;

public class InstrumentsControllerTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly InstrumentsController _controller;

    public InstrumentsControllerTests()
    {
        _mockUnitOfWork = MockHelpers.CreateMockUnitOfWork();
        _controller = new InstrumentsController(_mockUnitOfWork.Object);
    }

    private static Instrument CreateInstrument(
        int id = 1,
        string name = "Piano",
        InstrumentCategory category = InstrumentCategory.Keyboard,
        bool isActive = true)
    {
        return new Instrument
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
        var instruments = new List<Instrument>
        {
            CreateInstrument(1, "Piano"),
            CreateInstrument(2, "Guitar", InstrumentCategory.String),
            CreateInstrument(3, "Drums", InstrumentCategory.Percussion, isActive: false)
        };
        var mockInstrumentRepo = MockHelpers.CreateMockRepository(instruments);
        _mockUnitOfWork.Setup(u => u.Repository<Instrument>()).Returns(mockInstrumentRepo.Object);

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
        var activeInstrument = CreateInstrument(1, "Piano", isActive: true);
        var inactiveInstrument = CreateInstrument(2, "Drums", isActive: false);
        var instruments = new List<Instrument> { activeInstrument, inactiveInstrument };
        var mockInstrumentRepo = MockHelpers.CreateMockRepository(instruments);
        _mockUnitOfWork.Setup(u => u.Repository<Instrument>()).Returns(mockInstrumentRepo.Object);

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
        var instruments = new List<Instrument>
        {
            CreateInstrument(1, "Violin"),
            CreateInstrument(2, "Bass"),
            CreateInstrument(3, "Piano")
        };
        var mockInstrumentRepo = MockHelpers.CreateMockRepository(instruments);
        _mockUnitOfWork.Setup(u => u.Repository<Instrument>()).Returns(mockInstrumentRepo.Object);

        // Act
        var result = await _controller.GetAll(null, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedInstruments = Assert.IsAssignableFrom<IEnumerable<InstrumentDto>>(okResult.Value).ToList();
        Assert.Equal("Bass", returnedInstruments[0].Name);
        Assert.Equal("Piano", returnedInstruments[1].Name);
        Assert.Equal("Violin", returnedInstruments[2].Name);
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetById_WithValidId_ReturnsInstrument()
    {
        // Arrange
        var instrument = CreateInstrument(1, "Piano");
        var instruments = new List<Instrument> { instrument };
        var mockInstrumentRepo = MockHelpers.CreateMockRepository(instruments);
        _mockUnitOfWork.Setup(u => u.Repository<Instrument>()).Returns(mockInstrumentRepo.Object);

        mockInstrumentRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(instrument);

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
        var instruments = new List<Instrument>();
        var mockInstrumentRepo = MockHelpers.CreateMockRepository(instruments);
        _mockUnitOfWork.Setup(u => u.Repository<Instrument>()).Returns(mockInstrumentRepo.Object);

        mockInstrumentRepo.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Instrument?)null);

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
        var instruments = new List<Instrument>();
        var mockInstrumentRepo = MockHelpers.CreateMockRepository(instruments);
        _mockUnitOfWork.Setup(u => u.Repository<Instrument>()).Returns(mockInstrumentRepo.Object);

        var dto = new CreateInstrumentDto
        {
            Name = "New Instrument",
            Category = InstrumentCategory.Wind
        };

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
        var existingInstrument = CreateInstrument(1, "Piano");
        var instruments = new List<Instrument> { existingInstrument };
        var mockInstrumentRepo = MockHelpers.CreateMockRepository(instruments);
        _mockUnitOfWork.Setup(u => u.Repository<Instrument>()).Returns(mockInstrumentRepo.Object);

        var dto = new CreateInstrumentDto
        {
            Name = "Piano", // Duplicate
            Category = InstrumentCategory.Keyboard
        };

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
        var existingInstrument = CreateInstrument(1, "Piano");
        var instruments = new List<Instrument> { existingInstrument };
        var mockInstrumentRepo = MockHelpers.CreateMockRepository(instruments);
        _mockUnitOfWork.Setup(u => u.Repository<Instrument>()).Returns(mockInstrumentRepo.Object);

        var dto = new CreateInstrumentDto
        {
            Name = "PIANO", // Different case, still duplicate
            Category = InstrumentCategory.Keyboard
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
    public async Task Update_WithValidData_ReturnsUpdatedInstrument()
    {
        // Arrange
        var instrument = CreateInstrument(1, "Piano");
        var instruments = new List<Instrument> { instrument };
        var mockInstrumentRepo = MockHelpers.CreateMockRepository(instruments);
        _mockUnitOfWork.Setup(u => u.Repository<Instrument>()).Returns(mockInstrumentRepo.Object);

        mockInstrumentRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(instrument);

        var dto = new UpdateInstrumentDto
        {
            Name = "Grand Piano",
            Category = InstrumentCategory.Keyboard,
            IsActive = true
        };

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
        var instruments = new List<Instrument>();
        var mockInstrumentRepo = MockHelpers.CreateMockRepository(instruments);
        _mockUnitOfWork.Setup(u => u.Repository<Instrument>()).Returns(mockInstrumentRepo.Object);

        mockInstrumentRepo.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Instrument?)null);

        var dto = new UpdateInstrumentDto
        {
            Name = "Updated Name",
            Category = InstrumentCategory.Keyboard,
            IsActive = true
        };

        // Act
        var result = await _controller.Update(999, dto, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Update_WithDuplicateName_ReturnsBadRequest()
    {
        // Arrange
        var piano = CreateInstrument(1, "Piano");
        var guitar = CreateInstrument(2, "Guitar", InstrumentCategory.String);
        var instruments = new List<Instrument> { piano, guitar };
        var mockInstrumentRepo = MockHelpers.CreateMockRepository(instruments);
        _mockUnitOfWork.Setup(u => u.Repository<Instrument>()).Returns(mockInstrumentRepo.Object);

        mockInstrumentRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(piano);

        var dto = new UpdateInstrumentDto
        {
            Name = "Guitar", // Trying to use guitar's name
            Category = InstrumentCategory.Keyboard,
            IsActive = true
        };

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
        var instrument = CreateInstrument(1, "Piano");
        var instruments = new List<Instrument> { instrument };
        var mockInstrumentRepo = MockHelpers.CreateMockRepository(instruments);
        _mockUnitOfWork.Setup(u => u.Repository<Instrument>()).Returns(mockInstrumentRepo.Object);

        mockInstrumentRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(instrument);

        var dto = new UpdateInstrumentDto
        {
            Name = "Piano", // Same name
            Category = InstrumentCategory.Keyboard,
            IsActive = false // Deactivating
        };

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
        var instrument = CreateInstrument(1, "Piano", InstrumentCategory.Keyboard);
        var instruments = new List<Instrument> { instrument };
        var mockInstrumentRepo = MockHelpers.CreateMockRepository(instruments);
        _mockUnitOfWork.Setup(u => u.Repository<Instrument>()).Returns(mockInstrumentRepo.Object);

        mockInstrumentRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(instrument);

        var dto = new UpdateInstrumentDto
        {
            Name = "Piano",
            Category = InstrumentCategory.Other, // Changed category
            IsActive = true
        };

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
        var instrument = CreateInstrument(1, "Piano");
        var instruments = new List<Instrument> { instrument };
        var mockInstrumentRepo = MockHelpers.CreateMockRepository(instruments);
        _mockUnitOfWork.Setup(u => u.Repository<Instrument>()).Returns(mockInstrumentRepo.Object);

        mockInstrumentRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(instrument);

        // Act
        var result = await _controller.Delete(1, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
        Assert.False(instrument.IsActive); // Soft delete
    }

    [Fact]
    public async Task Delete_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var instruments = new List<Instrument>();
        var mockInstrumentRepo = MockHelpers.CreateMockRepository(instruments);
        _mockUnitOfWork.Setup(u => u.Repository<Instrument>()).Returns(mockInstrumentRepo.Object);

        mockInstrumentRepo.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Instrument?)null);

        // Act
        var result = await _controller.Delete(999, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Delete_DeactivatesInsteadOfDeleting()
    {
        // Arrange
        var instrument = CreateInstrument(1, "Piano", isActive: true);
        var instruments = new List<Instrument> { instrument };
        var mockInstrumentRepo = MockHelpers.CreateMockRepository(instruments);
        _mockUnitOfWork.Setup(u => u.Repository<Instrument>()).Returns(mockInstrumentRepo.Object);

        mockInstrumentRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(instrument);

        // Act
        var result = await _controller.Delete(1, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
        Assert.False(instrument.IsActive);
        mockInstrumentRepo.Verify(r => r.DeleteAsync(It.IsAny<Instrument>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}
