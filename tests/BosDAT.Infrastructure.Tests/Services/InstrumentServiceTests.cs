using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Enums;
using BosDAT.Core.Interfaces;
using BosDAT.Core.Interfaces.Services;
using BosDAT.Core.Interfaces.Repositories;
using BosDAT.Infrastructure.Data;
using BosDAT.Infrastructure.Repositories;
using BosDAT.Infrastructure.Services;
using BosDAT.Infrastructure.Tests.Helpers;

namespace BosDAT.Infrastructure.Tests.Services;

public class InstrumentServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly UnitOfWork _unitOfWork;
    private readonly InstrumentService _service;

    private static int _nextId = 100;

    public InstrumentServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"InstrumentServiceTest_{Guid.NewGuid()}")
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new ApplicationDbContext(options);
        _unitOfWork = TestHelpers.CreateUnitOfWork(_context);
        _service = new InstrumentService(_unitOfWork);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    #region GetAllAsync

    [Fact]
    public async Task GetAllAsync_WithNoFilter_ReturnsAllInstruments()
    {
        // Arrange
        _context.Instruments.AddRange(
            CreateInstrument("Piano", isActive: true),
            CreateInstrument("Guitar", isActive: false));
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetAllAsync(activeOnly: null);

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetAllAsync_WithActiveOnlyTrue_ReturnsOnlyActiveInstruments()
    {
        // Arrange
        _context.Instruments.AddRange(
            CreateInstrument("Piano", isActive: true),
            CreateInstrument("Drums", isActive: false),
            CreateInstrument("Violin", isActive: true));
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetAllAsync(activeOnly: true);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, i => Assert.True(i.IsActive));
    }

    [Fact]
    public async Task GetAllAsync_WithActiveOnlyFalse_ReturnsAllInstruments()
    {
        // Arrange
        _context.Instruments.AddRange(
            CreateInstrument("Piano", isActive: true),
            CreateInstrument("Drums", isActive: false));
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetAllAsync(activeOnly: false);

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsInstrumentsOrderedByName()
    {
        // Arrange
        _context.Instruments.AddRange(
            CreateInstrument("Violin"),
            CreateInstrument("Cello"),
            CreateInstrument("Piano"));
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetAllAsync(null);

        // Assert
        Assert.Equal(new[] { "Cello", "Piano", "Violin" }, result.Select(i => i.Name));
    }

    [Fact]
    public async Task GetAllAsync_MapsDtoFieldsCorrectly()
    {
        // Arrange
        var instrument = new Instrument
        {
            Id = _nextId++,
            Name = "Saxophone",
            Category = InstrumentCategory.Wind,
            IsActive = true
        };
        _context.Instruments.Add(instrument);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetAllAsync(null);

        // Assert
        var dto = Assert.Single(result);
        Assert.Equal(instrument.Id, dto.Id);
        Assert.Equal("Saxophone", dto.Name);
        Assert.Equal(InstrumentCategory.Wind, dto.Category);
        Assert.True(dto.IsActive);
    }

    #endregion

    #region GetByIdAsync

    [Fact]
    public async Task GetByIdAsync_WhenInstrumentExists_ReturnsDto()
    {
        // Arrange
        var instrument = CreateInstrument("Trumpet");
        _context.Instruments.Add(instrument);
        await _context.SaveChangesAsync();

        // Act
        var (dto, notFound) = await _service.GetByIdAsync(instrument.Id);

        // Assert
        Assert.False(notFound);
        Assert.NotNull(dto);
        Assert.Equal(instrument.Id, dto.Id);
        Assert.Equal("Trumpet", dto.Name);
    }

    [Fact]
    public async Task GetByIdAsync_WhenInstrumentNotFound_ReturnsNotFound()
    {
        // Act
        var (dto, notFound) = await _service.GetByIdAsync(9999);

        // Assert
        Assert.True(notFound);
        Assert.Null(dto);
    }

    #endregion

    #region CreateAsync

    [Fact]
    public async Task CreateAsync_WithValidDto_PersistsAndReturnsMappedDto()
    {
        // Arrange
        var dto = new CreateInstrumentDto
        {
            Name = "Flute",
            Category = InstrumentCategory.Wind
        };

        // Act
        var (result, error) = await _service.CreateAsync(dto);

        // Assert
        Assert.Null(error);
        Assert.NotNull(result);
        Assert.Equal("Flute", result.Name);
        Assert.Equal(InstrumentCategory.Wind, result.Category);
        Assert.True(result.IsActive);

        var saved = await _context.Instruments.FindAsync(result.Id);
        Assert.NotNull(saved);
        Assert.True(saved.IsActive);
    }

    [Fact]
    public async Task CreateAsync_AlwaysSetsIsActiveToTrue()
    {
        // Arrange
        var dto = new CreateInstrumentDto { Name = "Oboe", Category = InstrumentCategory.Wind };

        // Act
        var (result, _) = await _service.CreateAsync(dto);

        // Assert
        Assert.True(result!.IsActive);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateName_ReturnsError()
    {
        // Arrange - use mock to simulate ILike finding a case-insensitive duplicate
        var (service, mockRepo) = CreateMockedService();
        mockRepo.Setup(r => r.ExistsByNameAsync("piano", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var dto = new CreateInstrumentDto { Name = "piano", Category = InstrumentCategory.Keyboard };

        // Act
        var (result, error) = await service.CreateAsync(dto);

        // Assert
        Assert.Null(result);
        Assert.NotNull(error);
        Assert.Contains("already exists", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateNameDifferentCase_ReturnsError()
    {
        // Arrange - use mock to simulate ILike finding a case-insensitive duplicate
        var (service, mockRepo) = CreateMockedService();
        mockRepo.Setup(r => r.ExistsByNameAsync("guitar", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var dto = new CreateInstrumentDto { Name = "guitar", Category = InstrumentCategory.String };

        // Act
        var (result, error) = await service.CreateAsync(dto);

        // Assert
        Assert.Null(result);
        Assert.NotNull(error);
    }

    #endregion

    #region UpdateAsync

    [Fact]
    public async Task UpdateAsync_WhenInstrumentExists_UpdatesAndReturnsDto()
    {
        // Arrange - use mock so ILike duplicate check doesn't run against in-memory EF
        var instrument = CreateInstrument("Old Name");
        var (service, mockRepo) = CreateMockedService();
        mockRepo.Setup(r => r.GetByIdAsync(instrument.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(instrument);
        mockRepo.Setup(r => r.ExistsByNameAsync("New Name", instrument.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var dto = new UpdateInstrumentDto
        {
            Name = "New Name",
            Category = InstrumentCategory.Percussion,
            IsActive = false
        };

        // Act
        var (result, error, notFound) = await service.UpdateAsync(instrument.Id, dto);

        // Assert
        Assert.False(notFound);
        Assert.Null(error);
        Assert.NotNull(result);
        Assert.Equal("New Name", result.Name);
        Assert.Equal(InstrumentCategory.Percussion, result.Category);
        Assert.False(result.IsActive);
    }

    [Fact]
    public async Task UpdateAsync_WhenInstrumentNotFound_ReturnsNotFound()
    {
        // Arrange
        var dto = new UpdateInstrumentDto { Name = "Anything", Category = InstrumentCategory.String, IsActive = true };

        // Act
        var (result, error, notFound) = await _service.UpdateAsync(9999, dto);

        // Assert
        Assert.True(notFound);
        Assert.Null(result);
        Assert.Null(error);
    }

    [Fact]
    public async Task UpdateAsync_WithDuplicateNameFromAnotherInstrument_ReturnsError()
    {
        // Arrange - use mock to simulate ILike finding a case-insensitive duplicate from another instrument
        var toUpdate = CreateInstrument("Violin");
        var (service, mockRepo) = CreateMockedService();
        mockRepo.Setup(r => r.GetByIdAsync(toUpdate.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(toUpdate);
        mockRepo.Setup(r => r.ExistsByNameAsync("cello", toUpdate.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var dto = new UpdateInstrumentDto { Name = "cello", Category = InstrumentCategory.String, IsActive = true };

        // Act
        var (result, error, notFound) = await service.UpdateAsync(toUpdate.Id, dto);

        // Assert
        Assert.False(notFound);
        Assert.Null(result);
        Assert.NotNull(error);
        Assert.Contains("already exists", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateAsync_WithSameNameAsSelf_Succeeds()
    {
        // Arrange - same name, same id → should NOT be treated as duplicate (ILike excludes same id)
        var instrument = CreateInstrument("Piano");
        var (service, mockRepo) = CreateMockedService();
        mockRepo.Setup(r => r.GetByIdAsync(instrument.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(instrument);
        mockRepo.Setup(r => r.ExistsByNameAsync("Piano", instrument.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var dto = new UpdateInstrumentDto { Name = "Piano", Category = InstrumentCategory.Keyboard, IsActive = false };

        // Act
        var (result, error, notFound) = await service.UpdateAsync(instrument.Id, dto);

        // Assert
        Assert.False(notFound);
        Assert.Null(error);
        Assert.NotNull(result);
        Assert.Equal("Piano", result.Name);
    }

    #endregion

    #region DeleteAsync

    [Fact]
    public async Task DeleteAsync_WhenInstrumentExists_DeactivatesInsteadOfDeleting()
    {
        // Arrange
        var instrument = CreateInstrument("Tuba", isActive: true);
        _context.Instruments.Add(instrument);
        await _context.SaveChangesAsync();

        // Act
        var (success, notFound) = await _service.DeleteAsync(instrument.Id);

        // Assert
        Assert.True(success);
        Assert.False(notFound);

        // Soft-delete: still in DB but inactive
        var saved = await _context.Instruments.FindAsync(instrument.Id);
        Assert.NotNull(saved);
        Assert.False(saved.IsActive);
    }

    [Fact]
    public async Task DeleteAsync_WhenInstrumentNotFound_ReturnsNotFound()
    {
        // Act
        var (success, notFound) = await _service.DeleteAsync(9999);

        // Assert
        Assert.False(success);
        Assert.True(notFound);
    }

    #endregion

    #region Helpers

    private static Instrument CreateInstrument(string name, bool isActive = true) => new()
    {
        Id = _nextId++,
        Name = name,
        Category = InstrumentCategory.String,
        IsActive = isActive
    };

    /// <summary>
    /// Creates an InstrumentService backed by a mocked IUnitOfWork for tests that involve
    /// ILike-based duplicate checks, which cannot run against the in-memory EF provider.
    /// </summary>
    private static (InstrumentService service, Mock<IInstrumentRepository> mockRepo) CreateMockedService()
    {
        var mockUoW = new Mock<IUnitOfWork>();
        var mockRepo = new Mock<IInstrumentRepository>();
        mockUoW.Setup(u => u.Instruments).Returns(mockRepo.Object);
        mockUoW.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        return (new InstrumentService(mockUoW.Object), mockRepo);
    }

    #endregion
}
