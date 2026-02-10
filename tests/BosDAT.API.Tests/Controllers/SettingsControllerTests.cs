using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using BosDAT.API.Controllers;
using BosDAT.Core.Entities;
using BosDAT.Core.Interfaces;

namespace BosDAT.API.Tests.Controllers;

public class SettingsControllerTests
{
    private readonly Mock<ISettingsService> _mockSettingsService;
    private readonly SettingsController _controller;

    public SettingsControllerTests()
    {
        _mockSettingsService = new Mock<ISettingsService>();
        _controller = new SettingsController(_mockSettingsService.Object);
    }

    private static Setting CreateSetting(string key, string value, string? type = null, string? description = null)
    {
        return new Setting
        {
            Key = key,
            Value = value,
            Type = type,
            Description = description
        };
    }

    #region GetAll Tests

    [Fact]
    public async Task GetAll_ReturnsAllSettings()
    {
        // Arrange
        var settings = new List<Setting>
        {
            CreateSetting("app.name", "BosDAT", "string", "Application name"),
            CreateSetting("app.version", "2.0.0", "string", "Application version"),
            CreateSetting("registration.fee", "50.00", "decimal", "Registration fee amount")
        };
        _mockSettingsService.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);

        // Act
        var result = await _controller.GetAll(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedSettings = Assert.IsAssignableFrom<IEnumerable<Setting>>(okResult.Value);
        Assert.Equal(3, returnedSettings.Count());
    }

    [Fact]
    public async Task GetAll_ReturnsSettingsSortedByKey()
    {
        // Arrange
        var settings = new List<Setting>
        {
            CreateSetting("alpha.setting", "value1"),
            CreateSetting("beta.setting", "value2"),
            CreateSetting("zeta.setting", "value3")
        };
        _mockSettingsService.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);

        // Act
        var result = await _controller.GetAll(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedSettings = Assert.IsAssignableFrom<IEnumerable<Setting>>(okResult.Value).ToList();
        Assert.Equal("alpha.setting", returnedSettings[0].Key);
        Assert.Equal("beta.setting", returnedSettings[1].Key);
        Assert.Equal("zeta.setting", returnedSettings[2].Key);
    }

    [Fact]
    public async Task GetAll_WithNoSettings_ReturnsEmptyList()
    {
        // Arrange
        _mockSettingsService.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Setting>());

        // Act
        var result = await _controller.GetAll(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedSettings = Assert.IsAssignableFrom<IEnumerable<Setting>>(okResult.Value);
        Assert.Empty(returnedSettings);
    }

    #endregion

    #region GetByKey Tests

    [Fact]
    public async Task GetByKey_WithValidKey_ReturnsSetting()
    {
        // Arrange
        var setting = CreateSetting("app.name", "BosDAT", "string", "Application name");
        _mockSettingsService.Setup(s => s.GetByKeyAsync("app.name", It.IsAny<CancellationToken>()))
            .ReturnsAsync(setting);

        // Act
        var result = await _controller.GetByKey("app.name", CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedSetting = Assert.IsType<Setting>(okResult.Value);
        Assert.Equal("app.name", returnedSetting.Key);
        Assert.Equal("BosDAT", returnedSetting.Value);
    }

    [Fact]
    public async Task GetByKey_WithInvalidKey_ReturnsNotFound()
    {
        // Arrange
        _mockSettingsService.Setup(s => s.GetByKeyAsync("nonexistent.key", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Setting?)null);

        // Act
        var result = await _controller.GetByKey("nonexistent.key", CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetByKey_WithExactMatch_ReturnsSetting()
    {
        // Arrange
        var setting1 = CreateSetting("app.name", "BosDAT");
        _mockSettingsService.Setup(s => s.GetByKeyAsync("app.name", It.IsAny<CancellationToken>()))
            .ReturnsAsync(setting1);

        // Act
        var result = await _controller.GetByKey("app.name", CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedSetting = Assert.IsType<Setting>(okResult.Value);
        Assert.Equal("app.name", returnedSetting.Key);
        Assert.Equal("BosDAT", returnedSetting.Value);
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_WithValidKey_ReturnsUpdatedSetting()
    {
        // Arrange
        var updatedSetting = CreateSetting("app.name", "BosDAT v2", "string", "Application name");
        _mockSettingsService.Setup(s => s.UpdateAsync("app.name", "BosDAT v2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedSetting);

        var dto = new UpdateSettingDto { Value = "BosDAT v2" };

        // Act
        var result = await _controller.Update("app.name", dto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedSetting = Assert.IsType<Setting>(okResult.Value);
        Assert.Equal("app.name", returnedSetting.Key);
        Assert.Equal("BosDAT v2", returnedSetting.Value);
    }

    [Fact]
    public async Task Update_WithInvalidKey_ReturnsNotFound()
    {
        // Arrange
        _mockSettingsService.Setup(s => s.UpdateAsync("nonexistent.key", "new value", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Setting?)null);

        var dto = new UpdateSettingDto { Value = "new value" };

        // Act
        var result = await _controller.Update("nonexistent.key", dto, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Update_PreservesKeyAndMetadata()
    {
        // Arrange
        var updatedSetting = CreateSetting("app.name", "NewValue", "string", "Application name");
        _mockSettingsService.Setup(s => s.UpdateAsync("app.name", "NewValue", It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedSetting);

        var dto = new UpdateSettingDto { Value = "NewValue" };

        // Act
        var result = await _controller.Update("app.name", dto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedSetting = Assert.IsType<Setting>(okResult.Value);
        Assert.Equal("app.name", returnedSetting.Key);
        Assert.Equal("NewValue", returnedSetting.Value);
        Assert.Equal("string", returnedSetting.Type);
        Assert.Equal("Application name", returnedSetting.Description);
    }

    [Fact]
    public async Task Update_CallsService()
    {
        // Arrange
        var updatedSetting = CreateSetting("app.name", "NewValue");
        _mockSettingsService.Setup(s => s.UpdateAsync("app.name", "NewValue", It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedSetting);

        var dto = new UpdateSettingDto { Value = "NewValue" };

        // Act
        await _controller.Update("app.name", dto, CancellationToken.None);

        // Assert
        _mockSettingsService.Verify(s => s.UpdateAsync("app.name", "NewValue", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Update_WithEmptyValue_ReturnsSuccess()
    {
        // Arrange
        var updatedSetting = CreateSetting("optional.setting", "");
        _mockSettingsService.Setup(s => s.UpdateAsync("optional.setting", "", It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedSetting);

        var dto = new UpdateSettingDto { Value = "" };

        // Act
        var result = await _controller.Update("optional.setting", dto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedSetting = Assert.IsType<Setting>(okResult.Value);
        Assert.Equal("", returnedSetting.Value);
    }

    #endregion
}
