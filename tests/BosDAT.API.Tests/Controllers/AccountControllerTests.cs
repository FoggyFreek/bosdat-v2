using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using BosDAT.API.Controllers;
using BosDAT.Core.Common;
using BosDAT.Core.DTOs;
using BosDAT.Core.Interfaces;

namespace BosDAT.API.Tests.Controllers;

public class AccountControllerTests
{
    private readonly Mock<IUserManagementService> _mockService;
    private readonly AccountController _controller;

    public AccountControllerTests()
    {
        _mockService = new Mock<IUserManagementService>();
        _controller = new AccountController(_mockService.Object);
    }

    #region ValidateToken

    [Fact]
    public async Task ValidateToken_ValidToken_ReturnsOkWithIsValidTrue()
    {
        var response = new ValidateTokenResponseDto
        {
            IsValid = true,
            DisplayName = "Test User",
            Email = "test@example.com",
            ExpiresAt = DateTime.UtcNow.AddHours(72)
        };
        _mockService.Setup(s => s.ValidateTokenAsync("validtoken", It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await _controller.ValidateToken("validtoken", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var value = Assert.IsType<ValidateTokenResponseDto>(ok.Value);
        Assert.True(value.IsValid);
        Assert.Equal("Test User", value.DisplayName);
    }

    [Fact]
    public async Task ValidateToken_InvalidToken_ReturnsOkWithIsValidFalse()
    {
        _mockService.Setup(s => s.ValidateTokenAsync("expiredtoken", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidateTokenResponseDto { IsValid = false });

        var result = await _controller.ValidateToken("expiredtoken", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var value = Assert.IsType<ValidateTokenResponseDto>(ok.Value);
        Assert.False(value.IsValid);
    }

    [Fact]
    public async Task ValidateToken_EmptyToken_ReturnsOkWithIsValidFalse()
    {
        var result = await _controller.ValidateToken("", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var value = Assert.IsType<ValidateTokenResponseDto>(ok.Value);
        Assert.False(value.IsValid);
        _mockService.Verify(s => s.ValidateTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region SetPassword

    [Fact]
    public async Task SetPassword_ValidTokenAndPassword_ReturnsNoContent()
    {
        var dto = new SetPasswordDto { Token = "validtoken", Password = "NewPassword123!" };
        _mockService.Setup(s => s.SetPasswordFromTokenAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(true));

        var result = await _controller.SetPassword(dto, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task SetPassword_InvalidToken_ReturnsBadRequest()
    {
        var dto = new SetPasswordDto { Token = "expiredtoken", Password = "NewPassword123!" };
        _mockService.Setup(s => s.SetPasswordFromTokenAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Failure("This invitation link is invalid or has expired."));

        var result = await _controller.SetPassword(dto, CancellationToken.None);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(bad.Value);
    }

    #endregion
}
