using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using BosDAT.API.Controllers;
using BosDAT.Core.DTOs;
using BosDAT.Core.Interfaces;
using BosDAT.API.Tests.Helpers;

namespace BosDAT.API.Tests.Controllers;

public class AuthControllerTests
{
    private readonly Mock<IAuthService> _mockAuthService;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _mockAuthService = MockHelpers.CreateMockAuthService();
        _controller = new AuthController(_mockAuthService.Object);
    }

    private static AuthResponseDto CreateAuthResponse(string email = "test@example.com")
    {
        return new AuthResponseDto
        {
            Token = "test-jwt-token",
            RefreshToken = "test-refresh-token",
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            User = new UserDto
            {
                Id = Guid.NewGuid(),
                Email = email,
                FirstName = "Test",
                LastName = "User",
                Roles = new List<string> { "User" }
            }
        };
    }

    private void SetupControllerWithAuthenticatedUser(Guid userId)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    private void SetupControllerWithUnauthenticatedUser()
    {
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
        };
    }

    #region Login Tests

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsOkWithAuthResponse()
    {
        // Arrange
        var loginDto = new LoginDto { Email = "test@example.com", Password = "ValidPassword123!" };
        var authResponse = CreateAuthResponse(loginDto.Email);
        _mockAuthService.Setup(s => s.LoginAsync(loginDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(authResponse);

        // Act
        var result = await _controller.Login(loginDto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<AuthResponseDto>(okResult.Value);
        Assert.Equal(loginDto.Email, response.User.Email);
        Assert.NotEmpty(response.Token);
        Assert.NotEmpty(response.RefreshToken);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var loginDto = new LoginDto { Email = "test@example.com", Password = "WrongPassword" };
        _mockAuthService.Setup(s => s.LoginAsync(loginDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AuthResponseDto?)null);

        // Act
        var result = await _controller.Login(loginDto, CancellationToken.None);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        Assert.NotNull(unauthorizedResult.Value);
    }

    [Fact]
    public async Task Login_WithNonexistentEmail_ReturnsUnauthorized()
    {
        // Arrange
        var loginDto = new LoginDto { Email = "nonexistent@example.com", Password = "AnyPassword123!" };
        _mockAuthService.Setup(s => s.LoginAsync(loginDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AuthResponseDto?)null);

        // Act
        var result = await _controller.Login(loginDto, CancellationToken.None);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result.Result);
    }

    #endregion

    #region Register Tests

    [Fact]
    public async Task Register_WithValidData_ReturnsOkWithAuthResponse()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Email = "newuser@example.com",
            Password = "StrongPassword123!",
            FirstName = "New",
            LastName = "User"
        };
        var authResponse = CreateAuthResponse(registerDto.Email);
        _mockAuthService.Setup(s => s.RegisterAsync(registerDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(authResponse);

        // Act
        var result = await _controller.Register(registerDto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<AuthResponseDto>(okResult.Value);
        Assert.Equal(registerDto.Email, response.User.Email);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsBadRequest()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Email = "existing@example.com",
            Password = "StrongPassword123!",
            FirstName = "Test",
            LastName = "User"
        };
        _mockAuthService.Setup(s => s.RegisterAsync(registerDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AuthResponseDto?)null);

        // Act
        var result = await _controller.Register(registerDto, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task Register_WithValidDataButServiceFails_ReturnsBadRequest()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Email = "newuser@example.com",
            Password = "WeakPass",
            FirstName = "New",
            LastName = "User"
        };
        _mockAuthService.Setup(s => s.RegisterAsync(registerDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AuthResponseDto?)null);

        // Act
        var result = await _controller.Register(registerDto, CancellationToken.None);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    #endregion

    #region Refresh Token Tests

    [Fact]
    public async Task Refresh_WithValidToken_ReturnsOkWithNewTokens()
    {
        // Arrange
        var refreshDto = new RefreshTokenDto { RefreshToken = "valid-refresh-token" };
        var authResponse = CreateAuthResponse();
        _mockAuthService.Setup(s => s.RefreshTokenAsync(refreshDto.RefreshToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(authResponse);

        // Act
        var result = await _controller.Refresh(refreshDto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<AuthResponseDto>(okResult.Value);
        Assert.NotEmpty(response.Token);
        Assert.NotEmpty(response.RefreshToken);
    }

    [Fact]
    public async Task Refresh_WithExpiredToken_ReturnsUnauthorized()
    {
        // Arrange
        var refreshDto = new RefreshTokenDto { RefreshToken = "expired-refresh-token" };
        _mockAuthService.Setup(s => s.RefreshTokenAsync(refreshDto.RefreshToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AuthResponseDto?)null);

        // Act
        var result = await _controller.Refresh(refreshDto, CancellationToken.None);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        Assert.NotNull(unauthorizedResult.Value);
    }

    [Fact]
    public async Task Refresh_WithRevokedToken_ReturnsUnauthorized()
    {
        // Arrange
        var refreshDto = new RefreshTokenDto { RefreshToken = "revoked-refresh-token" };
        _mockAuthService.Setup(s => s.RefreshTokenAsync(refreshDto.RefreshToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AuthResponseDto?)null);

        // Act
        var result = await _controller.Refresh(refreshDto, CancellationToken.None);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result.Result);
    }

    [Fact]
    public async Task Refresh_WithInvalidToken_ReturnsUnauthorized()
    {
        // Arrange
        var refreshDto = new RefreshTokenDto { RefreshToken = "invalid-token-format" };
        _mockAuthService.Setup(s => s.RefreshTokenAsync(refreshDto.RefreshToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AuthResponseDto?)null);

        // Act
        var result = await _controller.Refresh(refreshDto, CancellationToken.None);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result.Result);
    }

    #endregion

    #region Logout Tests

    [Fact]
    public async Task Logout_WithValidToken_ReturnsOk()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupControllerWithAuthenticatedUser(userId);
        var refreshDto = new RefreshTokenDto { RefreshToken = "valid-refresh-token" };
        _mockAuthService.Setup(s => s.RevokeTokenAsync(refreshDto.RefreshToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Logout(refreshDto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        _mockAuthService.Verify(s => s.RevokeTokenAsync(refreshDto.RefreshToken, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Logout_AlwaysCallsRevokeToken()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupControllerWithAuthenticatedUser(userId);
        var refreshDto = new RefreshTokenDto { RefreshToken = "any-token" };
        _mockAuthService.Setup(s => s.RevokeTokenAsync(refreshDto.RefreshToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.Logout(refreshDto, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        _mockAuthService.Verify(s => s.RevokeTokenAsync(refreshDto.RefreshToken, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region GetCurrentUser Tests

    [Fact]
    public async Task GetCurrentUser_WhenAuthenticated_ReturnsOkWithUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupControllerWithAuthenticatedUser(userId);
        var userDto = new UserDto
        {
            Id = userId,
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            Roles = new List<string> { "User" }
        };
        _mockAuthService.Setup(s => s.GetCurrentUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userDto);

        // Act
        var result = await _controller.GetCurrentUser(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<UserDto>(okResult.Value);
        Assert.Equal(userId, response.Id);
        Assert.Equal("test@example.com", response.Email);
    }

    [Fact]
    public async Task GetCurrentUser_WhenNotAuthenticated_ReturnsUnauthorized()
    {
        // Arrange
        SetupControllerWithUnauthenticatedUser();

        // Act
        var result = await _controller.GetCurrentUser(CancellationToken.None);

        // Assert
        Assert.IsType<UnauthorizedResult>(result.Result);
    }

    [Fact]
    public async Task GetCurrentUser_WhenUserNotFound_ReturnsNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupControllerWithAuthenticatedUser(userId);
        _mockAuthService.Setup(s => s.GetCurrentUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserDto?)null);

        // Act
        var result = await _controller.GetCurrentUser(CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetCurrentUser_WithInvalidUserIdFormat_ReturnsUnauthorized()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "not-a-guid")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        // Act
        var result = await _controller.GetCurrentUser(CancellationToken.None);

        // Assert
        Assert.IsType<UnauthorizedResult>(result.Result);
    }

    #endregion

    #region ChangePassword Tests

    [Fact]
    public async Task ChangePassword_WithValidData_ReturnsOk()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupControllerWithAuthenticatedUser(userId);
        var changePasswordDto = new ChangePasswordDto
        {
            CurrentPassword = "OldPassword123!",
            NewPassword = "NewPassword456!"
        };
        _mockAuthService.Setup(s => s.ChangePasswordAsync(userId, changePasswordDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.ChangePassword(changePasswordDto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task ChangePassword_WithWrongCurrentPassword_ReturnsBadRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupControllerWithAuthenticatedUser(userId);
        var changePasswordDto = new ChangePasswordDto
        {
            CurrentPassword = "WrongPassword",
            NewPassword = "NewPassword456!"
        };
        _mockAuthService.Setup(s => s.ChangePasswordAsync(userId, changePasswordDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.ChangePassword(changePasswordDto, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task ChangePassword_WhenNotAuthenticated_ReturnsUnauthorized()
    {
        // Arrange
        SetupControllerWithUnauthenticatedUser();
        var changePasswordDto = new ChangePasswordDto
        {
            CurrentPassword = "OldPassword123!",
            NewPassword = "NewPassword456!"
        };

        // Act
        var result = await _controller.ChangePassword(changePasswordDto, CancellationToken.None);

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task ChangePassword_ServiceFails_ReturnsBadRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupControllerWithAuthenticatedUser(userId);
        var changePasswordDto = new ChangePasswordDto
        {
            CurrentPassword = "OldPassword123!",
            NewPassword = "Weak"
        };
        _mockAuthService.Setup(s => s.ChangePasswordAsync(userId, changePasswordDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.ChangePassword(changePasswordDto, CancellationToken.None);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    #endregion
}
