using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Moq;
using BosDAT.Infrastructure.Services;

namespace BosDAT.Infrastructure.Tests.Services;

public class CurrentUserServiceTests
{
    #region UserId

    [Fact]
    public void UserId_WhenClaimPresent_ReturnsParsedGuid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var service = CreateServiceWithClaims(new Claim(ClaimTypes.NameIdentifier, userId.ToString()));

        // Act & Assert
        Assert.Equal(userId, service.UserId);
    }

    [Fact]
    public void UserId_WhenClaimIsInvalidGuid_ReturnsNull()
    {
        // Arrange
        var service = CreateServiceWithClaims(new Claim(ClaimTypes.NameIdentifier, "not-a-guid"));

        // Act & Assert
        Assert.Null(service.UserId);
    }

    [Fact]
    public void UserId_WhenNoHttpContext_ReturnsNull()
    {
        // Arrange
        var service = CreateServiceWithNoContext();

        // Act & Assert
        Assert.Null(service.UserId);
    }

    [Fact]
    public void UserId_WhenClaimMissing_ReturnsNull()
    {
        // Arrange
        var service = CreateServiceWithClaims(); // no claims

        // Act & Assert
        Assert.Null(service.UserId);
    }

    #endregion

    #region UserEmail

    [Fact]
    public void UserEmail_WhenClaimPresent_ReturnsEmail()
    {
        // Arrange
        var service = CreateServiceWithClaims(new Claim(ClaimTypes.Email, "user@example.com"));

        // Act & Assert
        Assert.Equal("user@example.com", service.UserEmail);
    }

    [Fact]
    public void UserEmail_WhenNoHttpContext_ReturnsNull()
    {
        // Arrange
        var service = CreateServiceWithNoContext();

        // Act & Assert
        Assert.Null(service.UserEmail);
    }

    [Fact]
    public void UserEmail_WhenClaimMissing_ReturnsNull()
    {
        // Arrange
        var service = CreateServiceWithClaims();

        // Act & Assert
        Assert.Null(service.UserEmail);
    }

    #endregion

    #region IpAddress

    [Fact]
    public void IpAddress_WhenNoHttpContext_ReturnsNull()
    {
        // Arrange
        var service = CreateServiceWithNoContext();

        // Act & Assert
        Assert.Null(service.IpAddress);
    }

    [Fact]
    public void IpAddress_WhenXForwardedForHeader_ReturnsFirstIp()
    {
        // Arrange
        var service = CreateServiceWithHeaders(
            forwardedFor: "1.2.3.4, 5.6.7.8",
            remoteIp: null);

        // Act & Assert
        Assert.Equal("1.2.3.4", service.IpAddress);
    }

    [Fact]
    public void IpAddress_WhenXForwardedForContainsSingleIp_ReturnsThatIp()
    {
        // Arrange
        var service = CreateServiceWithHeaders(
            forwardedFor: "203.0.113.5",
            remoteIp: null);

        // Act & Assert
        Assert.Equal("203.0.113.5", service.IpAddress);
    }

    [Fact]
    public void IpAddress_WhenNoForwardedHeader_ReturnsRemoteIp()
    {
        // Arrange
        var service = CreateServiceWithHeaders(
            forwardedFor: null,
            remoteIp: IPAddress.Parse("9.10.11.12"));

        // Act & Assert
        Assert.Equal("9.10.11.12", service.IpAddress);
    }

    #endregion

    #region Helpers

    private static CurrentUserService CreateServiceWithNoContext()
    {
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(a => a.HttpContext).Returns((HttpContext?)null);
        return new CurrentUserService(accessor.Object);
    }

    private static CurrentUserService CreateServiceWithClaims(params Claim[] claims)
    {
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var contextMock = new Mock<HttpContext>();
        contextMock.Setup(c => c.User).Returns(principal);

        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(a => a.HttpContext).Returns(contextMock.Object);
        return new CurrentUserService(accessor.Object);
    }

    private static CurrentUserService CreateServiceWithHeaders(string? forwardedFor, IPAddress? remoteIp)
    {
        var headersMock = new Mock<IHeaderDictionary>();
        headersMock.Setup(h => h["X-Forwarded-For"])
            .Returns(forwardedFor != null
                ? new Microsoft.Extensions.Primitives.StringValues(forwardedFor)
                : Microsoft.Extensions.Primitives.StringValues.Empty);

        var requestMock = new Mock<HttpRequest>();
        requestMock.Setup(r => r.Headers).Returns(headersMock.Object);

        var connectionMock = new Mock<ConnectionInfo>();
        connectionMock.Setup(c => c.RemoteIpAddress).Returns(remoteIp);

        var contextMock = new Mock<HttpContext>();
        contextMock.Setup(c => c.Request).Returns(requestMock.Object);
        contextMock.Setup(c => c.Connection).Returns(connectionMock.Object);

        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(a => a.HttpContext).Returns(contextMock.Object);
        return new CurrentUserService(accessor.Object);
    }

    #endregion
}
