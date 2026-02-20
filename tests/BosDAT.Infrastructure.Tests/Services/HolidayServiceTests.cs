using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Infrastructure.Data;
using BosDAT.Infrastructure.Repositories;
using BosDAT.Infrastructure.Services;

namespace BosDAT.Infrastructure.Tests.Services;

public class HolidayServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly UnitOfWork _unitOfWork;
    private readonly HolidayService _service;

    private static int _nextId = 200;

    public HolidayServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"HolidayServiceTest_{Guid.NewGuid()}")
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new ApplicationDbContext(options);
        _unitOfWork = new UnitOfWork(_context);
        _service = new HolidayService(_unitOfWork);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    #region GetAllAsync

    [Fact]
    public async Task GetAllAsync_ReturnsAllHolidaysOrderedByStartDate()
    {
        // Arrange
        _context.Holidays.AddRange(
            CreateHoliday("Summer", new DateOnly(2026, 7, 1), new DateOnly(2026, 8, 31)),
            CreateHoliday("Christmas", new DateOnly(2025, 12, 24), new DateOnly(2025, 12, 31)),
            CreateHoliday("Spring", new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 7)));
        await _context.SaveChangesAsync();

        // Act
        var result = (await _service.GetAllAsync()).ToList();

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal("Christmas", result[0].Name);
        Assert.Equal("Spring", result[1].Name);
        Assert.Equal("Summer", result[2].Name);
    }

    [Fact]
    public async Task GetAllAsync_WithNoHolidays_ReturnsEmpty()
    {
        // Act
        var result = await _service.GetAllAsync();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllAsync_MapsDtoFieldsCorrectly()
    {
        // Arrange
        var holiday = CreateHoliday("Easter", new DateOnly(2026, 4, 3), new DateOnly(2026, 4, 6));
        _context.Holidays.Add(holiday);
        await _context.SaveChangesAsync();

        // Act
        var result = (await _service.GetAllAsync()).ToList();

        // Assert
        var dto = Assert.Single(result);
        Assert.Equal(holiday.Id, dto.Id);
        Assert.Equal("Easter", dto.Name);
        Assert.Equal(new DateOnly(2026, 4, 3), dto.StartDate);
        Assert.Equal(new DateOnly(2026, 4, 6), dto.EndDate);
    }

    #endregion

    #region GetByIdAsync

    [Fact]
    public async Task GetByIdAsync_WhenHolidayExists_ReturnsMappedDto()
    {
        // Arrange
        var holiday = CreateHoliday("Carnival", new DateOnly(2026, 2, 14), new DateOnly(2026, 2, 16));
        _context.Holidays.Add(holiday);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetByIdAsync(holiday.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(holiday.Id, result.Id);
        Assert.Equal("Carnival", result.Name);
        Assert.Equal(new DateOnly(2026, 2, 14), result.StartDate);
        Assert.Equal(new DateOnly(2026, 2, 16), result.EndDate);
    }

    [Fact]
    public async Task GetByIdAsync_WhenHolidayNotFound_ReturnsNull()
    {
        // Act
        var result = await _service.GetByIdAsync(9999);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region CreateAsync

    [Fact]
    public async Task CreateAsync_PersistsHolidayAndReturnsMappedDto()
    {
        // Arrange
        var dto = new CreateHolidayDto
        {
            Name = "Sinterklaas",
            StartDate = new DateOnly(2026, 12, 5),
            EndDate = new DateOnly(2026, 12, 5)
        };

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(0, result.Id);
        Assert.Equal("Sinterklaas", result.Name);
        Assert.Equal(new DateOnly(2026, 12, 5), result.StartDate);
        Assert.Equal(new DateOnly(2026, 12, 5), result.EndDate);

        var saved = await _context.Holidays.FindAsync(result.Id);
        Assert.NotNull(saved);
        Assert.Equal("Sinterklaas", saved.Name);
    }

    #endregion

    #region UpdateAsync

    [Fact]
    public async Task UpdateAsync_WhenHolidayExists_UpdatesAndReturnsDto()
    {
        // Arrange
        var holiday = CreateHoliday("Old Name", new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 3));
        _context.Holidays.Add(holiday);
        await _context.SaveChangesAsync();

        var dto = new UpdateHolidayDto
        {
            Name = "New Year",
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 1, 4)
        };

        // Act
        var result = await _service.UpdateAsync(holiday.Id, dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("New Year", result.Name);
        Assert.Equal(new DateOnly(2026, 1, 4), result.EndDate);

        var saved = await _context.Holidays.FindAsync(holiday.Id);
        Assert.Equal("New Year", saved!.Name);
        Assert.Equal(new DateOnly(2026, 1, 4), saved.EndDate);
    }

    [Fact]
    public async Task UpdateAsync_WhenHolidayNotFound_ReturnsNull()
    {
        // Arrange
        var dto = new UpdateHolidayDto
        {
            Name = "Does Not Exist",
            StartDate = new DateOnly(2026, 6, 1),
            EndDate = new DateOnly(2026, 6, 7)
        };

        // Act
        var result = await _service.UpdateAsync(9999, dto);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region DeleteAsync

    [Fact]
    public async Task DeleteAsync_WhenHolidayExists_DeletesAndReturnsTrue()
    {
        // Arrange
        var holiday = CreateHoliday("King's Day", new DateOnly(2026, 4, 27), new DateOnly(2026, 4, 27));
        _context.Holidays.Add(holiday);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.DeleteAsync(holiday.Id);

        // Assert
        Assert.True(result);
        Assert.Null(await _context.Holidays.FindAsync(holiday.Id));
    }

    [Fact]
    public async Task DeleteAsync_WhenHolidayNotFound_ReturnsFalse()
    {
        // Act
        var result = await _service.DeleteAsync(9999);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region Helpers

    private static Holiday CreateHoliday(string name, DateOnly startDate, DateOnly endDate) => new()
    {
        Id = _nextId++,
        Name = name,
        StartDate = startDate,
        EndDate = endDate
    };

    #endregion
}
