using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using BosDAT.Core.Entities;
using BosDAT.Infrastructure.Data;
using BosDAT.Infrastructure.Repositories;
using BosDAT.Infrastructure.Services;

namespace BosDAT.Infrastructure.Tests.Services;

public class SettingsServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly UnitOfWork _unitOfWork;
    private readonly SettingsService _service;

    public SettingsServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"SettingsServiceTest_{Guid.NewGuid()}")
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new ApplicationDbContext(options);
        _unitOfWork = new UnitOfWork(_context);
        _service = new SettingsService(_unitOfWork);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    #region GetAllAsync

    [Fact]
    public async Task GetAllAsync_ReturnsAllSettingsOrderedByKey()
    {
        // Arrange
        _context.Settings.AddRange(
            new Setting { Key = "vat_rate", Value = "21" },
            new Setting { Key = "school_name", Value = "Test School" },
            new Setting { Key = "payment_due_days", Value = "14" });
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal("payment_due_days", result[0].Key);
        Assert.Equal("school_name", result[1].Key);
        Assert.Equal("vat_rate", result[2].Key);
    }

    [Fact]
    public async Task GetAllAsync_WithNoSettings_ReturnsEmpty()
    {
        // Act
        var result = await _service.GetAllAsync();

        // Assert
        Assert.Empty(result);
    }

    #endregion

    #region GetByKeyAsync

    [Fact]
    public async Task GetByKeyAsync_WhenKeyExists_ReturnsSetting()
    {
        // Arrange
        _context.Settings.Add(new Setting { Key = "school_name", Value = "Test School" });
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetByKeyAsync("school_name");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("school_name", result.Key);
        Assert.Equal("Test School", result.Value);
    }

    [Fact]
    public async Task GetByKeyAsync_WhenKeyNotFound_ReturnsNull()
    {
        // Act
        var result = await _service.GetByKeyAsync("nonexistent_key");

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region UpdateAsync

    [Fact]
    public async Task UpdateAsync_WhenKeyExists_UpdatesValueAndReturnsSetting()
    {
        // Arrange
        _context.Settings.Add(new Setting { Key = "vat_rate", Value = "21" });
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.UpdateAsync("vat_rate", "9");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("vat_rate", result.Key);
        Assert.Equal("9", result.Value);

        var saved = await _context.Settings.FirstAsync(s => s.Key == "vat_rate");
        Assert.Equal("9", saved.Value);
    }

    [Fact]
    public async Task UpdateAsync_WhenKeyNotFound_ReturnsNull()
    {
        // Act
        var result = await _service.UpdateAsync("ghost_key", "value");

        // Assert
        Assert.Null(result);
    }

    #endregion
}
