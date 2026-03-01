using Microsoft.AspNetCore.Identity;
using Moq;
using Xunit;
using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Enums;
using BosDAT.Core.Interfaces;
using BosDAT.Core.Interfaces.Repositories;
using BosDAT.Core.Interfaces.Services;
using BosDAT.Infrastructure.Services;

namespace BosDAT.API.Tests.Services;

public class UserManagementServiceTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<IEmailService> _emailServiceMock = new();
    private readonly Mock<IInvitationTokenRepository> _invTokenRepoMock = new();
    private readonly Mock<ITeacherRepository> _teacherRepoMock = new();
    private readonly Mock<IStudentRepository> _studentRepoMock = new();
    private readonly UserManagementService _service;

    public UserManagementServiceTests()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        _uowMock.Setup(u => u.InvitationTokens).Returns(_invTokenRepoMock.Object);
        _uowMock.Setup(u => u.Teachers).Returns(_teacherRepoMock.Object);
        _uowMock.Setup(u => u.Students).Returns(_studentRepoMock.Object);
        _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _emailServiceMock
            .Setup(e => e.QueueEmailAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _service = new UserManagementService(
            _userManagerMock.Object, _uowMock.Object, _emailServiceMock.Object);
    }

    [Fact]
    public async Task CreateUserAsync_ValidAdmin_QueuesInvitationEmail()
    {
        var dto = new CreateUserDto { Role = "Admin", DisplayName = "New Admin", Email = "admin@example.com" };

        _userManagerMock.Setup(m => m.FindByEmailAsync(dto.Email))
            .ReturnsAsync((ApplicationUser?)null);
        _userManagerMock.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), "Admin"))
            .ReturnsAsync(IdentityResult.Success);

        var result = await _service.CreateUserAsync(dto, "http://localhost:5173");

        Assert.True(result.IsSuccess);
        _emailServiceMock.Verify(e => e.QueueEmailAsync(
            "admin@example.com",
            It.IsAny<string>(),
            "InvitationEmail",
            It.IsAny<object>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateUserAsync_InvalidRole_DoesNotQueueEmail()
    {
        var dto = new CreateUserDto { Role = "SuperUser", DisplayName = "Test", Email = "test@example.com" };

        var result = await _service.CreateUserAsync(dto, "http://localhost:5173");

        Assert.False(result.IsSuccess);
        _emailServiceMock.Verify(e => e.QueueEmailAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<object>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ResendInvitationAsync_PendingUser_QueuesNewEmail()
    {
        var userId = Guid.NewGuid();
        var user = new ApplicationUser
        {
            Id = userId,
            Email = "user@example.com",
            DisplayName = "Test User",
            AccountStatus = AccountStatus.PendingFirstLogin
        };

        _userManagerMock.Setup(m => m.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);
        _invTokenRepoMock.Setup(r => r.InvalidateAllForUserAsync(
            userId, InvitationTokenType.Invitation, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _service.ResendInvitationAsync(userId, "http://localhost:5173");

        Assert.True(result.IsSuccess);
        _emailServiceMock.Verify(e => e.QueueEmailAsync(
            "user@example.com",
            It.IsAny<string>(),
            "InvitationEmail",
            It.IsAny<object>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ResendInvitationAsync_ActiveUser_DoesNotQueueEmail()
    {
        var userId = Guid.NewGuid();
        var user = new ApplicationUser
        {
            Id = userId,
            Email = "user@example.com",
            DisplayName = "Test User",
            AccountStatus = AccountStatus.Active
        };

        _userManagerMock.Setup(m => m.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        var result = await _service.ResendInvitationAsync(userId, "http://localhost:5173");

        Assert.False(result.IsSuccess);
        _emailServiceMock.Verify(e => e.QueueEmailAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<object>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
