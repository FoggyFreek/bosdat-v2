using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using BosDAT.API.Controllers;
using BosDAT.Core.Interfaces;
using BosDAT.Core.Interfaces.Services;

namespace BosDAT.API.Tests.Controllers;

/// <summary>
/// Unit tests for SeederController.
/// Tests API endpoint behavior with mocked dependencies.
/// </summary>
public class SeederControllerTests
{
    private readonly Mock<IDatabaseSeeder> _mockSeeder;
    private readonly Mock<IWebHostEnvironment> _mockEnvironment;
    private readonly Mock<ILogger<SeederController>> _mockLogger;

    public SeederControllerTests()
    {
        _mockSeeder = new Mock<IDatabaseSeeder>();
        _mockEnvironment = new Mock<IWebHostEnvironment>();
        _mockLogger = new Mock<ILogger<SeederController>>();
    }

    private SeederController CreateController()
    {
        return new SeederController(
            _mockSeeder.Object,
            _mockEnvironment.Object,
            _mockLogger.Object);
    }

    #region GetStatus Tests

    [Fact]
    public async Task GetStatus_WhenSeeded_ReturnsCorrectStatus()
    {
        // Arrange
        _mockSeeder.Setup(s => s.IsSeededAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _mockEnvironment.Setup(e => e.EnvironmentName).Returns("Development");
        var controller = CreateController();

        // Act
        var result = await controller.GetStatus(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<SeederStatusResponse>(okResult.Value);

        Assert.True(response.IsSeeded);
        Assert.Equal("Development", response.Environment);
        Assert.False(response.CanSeed);
        Assert.True(response.CanReset);
    }

    [Fact]
    public async Task GetStatus_WhenNotSeeded_ReturnsCorrectStatus()
    {
        // Arrange
        _mockSeeder.Setup(s => s.IsSeededAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _mockEnvironment.Setup(e => e.EnvironmentName).Returns("Development");
        var controller = CreateController();

        // Act
        var result = await controller.GetStatus(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<SeederStatusResponse>(okResult.Value);

        Assert.False(response.IsSeeded);
        Assert.True(response.CanSeed);
        Assert.False(response.CanReset);
    }

    #endregion

    #region Seed Tests

    [Fact]
    public async Task Seed_InDevelopment_WhenNotSeeded_ReturnsSuccess()
    {
        // Arrange
        _mockSeeder.Setup(s => s.IsSeededAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _mockSeeder.Setup(s => s.SeedAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockEnvironment.Setup(e => e.EnvironmentName).Returns("Development");
        var controller = CreateController();

        // Act
        var result = await controller.Seed(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<SeederActionResponse>(okResult.Value);

        Assert.True(response.Success);
        Assert.Equal("Seed", response.Action);
        _mockSeeder.Verify(s => s.SeedAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Seed_InDevelopment_WhenAlreadySeeded_ReturnsBadRequest()
    {
        // Arrange
        _mockSeeder.Setup(s => s.IsSeededAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _mockEnvironment.Setup(e => e.EnvironmentName).Returns("Development");
        var controller = CreateController();

        // Act
        var result = await controller.Seed(CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var response = Assert.IsType<SeederActionResponse>(badRequestResult.Value);

        Assert.False(response.Success);
        Assert.Contains("already seeded", response.Message);
        _mockSeeder.Verify(s => s.SeedAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Seed_InProduction_ReturnsForbid()
    {
        // Arrange
        _mockEnvironment.Setup(e => e.EnvironmentName).Returns("Production");
        var controller = CreateController();

        // Act
        var result = await controller.Seed(CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result.Result);
        _mockSeeder.Verify(s => s.SeedAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Seed_WhenExceptionThrown_ReturnsBadRequest()
    {
        // Arrange
        _mockSeeder.Setup(s => s.IsSeededAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _mockSeeder.Setup(s => s.SeedAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Admin user not found"));
        _mockEnvironment.Setup(e => e.EnvironmentName).Returns("Development");
        var controller = CreateController();

        // Act
        var result = await controller.Seed(CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var response = Assert.IsType<SeederActionResponse>(badRequestResult.Value);

        Assert.False(response.Success);
        Assert.Contains("Admin user not found", response.Message);
    }

    #endregion

    #region Reset Tests

    [Fact]
    public async Task Reset_InDevelopment_ReturnsSuccess()
    {
        // Arrange
        _mockSeeder.Setup(s => s.ResetAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockEnvironment.Setup(e => e.EnvironmentName).Returns("Development");
        var controller = CreateController();

        // Act
        var result = await controller.Reset(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<SeederActionResponse>(okResult.Value);

        Assert.True(response.Success);
        Assert.Equal("Reset", response.Action);
        Assert.Contains("preserved", response.Message);
        _mockSeeder.Verify(s => s.ResetAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Reset_InProduction_ReturnsForbid()
    {
        // Arrange
        _mockEnvironment.Setup(e => e.EnvironmentName).Returns("Production");
        var controller = CreateController();

        // Act
        var result = await controller.Reset(CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result.Result);
        _mockSeeder.Verify(s => s.ResetAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Reset_WhenExceptionThrown_ReturnsBadRequest()
    {
        // Arrange
        _mockSeeder.Setup(s => s.ResetAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));
        _mockEnvironment.Setup(e => e.EnvironmentName).Returns("Development");
        var controller = CreateController();

        // Act
        var result = await controller.Reset(CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var response = Assert.IsType<SeederActionResponse>(badRequestResult.Value);

        Assert.False(response.Success);
        Assert.Contains("Database error", response.Message);
    }

    #endregion

    #region Reseed Tests

    [Fact]
    public async Task Reseed_InDevelopment_CallsResetThenSeed()
    {
        // Arrange
        var callOrder = new List<string>();
        _mockSeeder.Setup(s => s.ResetAsync(It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("Reset"))
            .Returns(Task.CompletedTask);
        _mockSeeder.Setup(s => s.SeedAsync(It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("Seed"))
            .Returns(Task.CompletedTask);
        _mockEnvironment.Setup(e => e.EnvironmentName).Returns("Development");
        var controller = CreateController();

        // Act
        var result = await controller.Reseed(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<SeederActionResponse>(okResult.Value);

        Assert.True(response.Success);
        Assert.Equal("Reseed", response.Action);
        Assert.Equal(2, callOrder.Count);
        Assert.Equal("Reset", callOrder[0]);
        Assert.Equal("Seed", callOrder[1]);
    }

    [Fact]
    public async Task Reseed_InProduction_ReturnsForbid()
    {
        // Arrange
        _mockEnvironment.Setup(e => e.EnvironmentName).Returns("Production");
        var controller = CreateController();

        // Act
        var result = await controller.Reseed(CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result.Result);
        _mockSeeder.Verify(s => s.ResetAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockSeeder.Verify(s => s.SeedAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Reseed_WhenResetFails_ReturnsBadRequest()
    {
        // Arrange
        _mockSeeder.Setup(s => s.ResetAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Reset failed"));
        _mockEnvironment.Setup(e => e.EnvironmentName).Returns("Development");
        var controller = CreateController();

        // Act
        var result = await controller.Reseed(CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var response = Assert.IsType<SeederActionResponse>(badRequestResult.Value);

        Assert.False(response.Success);
        Assert.Contains("Reset failed", response.Message);
        _mockSeeder.Verify(s => s.SeedAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Environment Tests

    [Theory]
    [InlineData("Staging")]
    [InlineData("Production")]
    [InlineData("Test")]
    public async Task Seed_InNonDevelopmentEnvironment_ReturnsForbid(string environment)
    {
        // Arrange
        _mockEnvironment.Setup(e => e.EnvironmentName).Returns(environment);
        var controller = CreateController();

        // Act
        var result = await controller.Seed(CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result.Result);
    }

    [Theory]
    [InlineData("Staging")]
    [InlineData("Production")]
    [InlineData("Test")]
    public async Task Reset_InNonDevelopmentEnvironment_ReturnsForbid(string environment)
    {
        // Arrange
        _mockEnvironment.Setup(e => e.EnvironmentName).Returns(environment);
        var controller = CreateController();

        // Act
        var result = await controller.Reset(CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result.Result);
    }

    #endregion
}
