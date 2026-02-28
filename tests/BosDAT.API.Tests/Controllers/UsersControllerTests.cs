using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;
using BosDAT.API.Controllers;
using BosDAT.Core.Common;
using BosDAT.Core.DTOs;
using BosDAT.Core.Enums;
using BosDAT.Core.Interfaces;

namespace BosDAT.API.Tests.Controllers;

public class UsersControllerTests
{
    private readonly Mock<IUserManagementService> _mockService;
    private readonly IConfiguration _configuration;
    private readonly UsersController _controller;
    private readonly Guid _adminId = Guid.NewGuid();

    public UsersControllerTests()
    {
        _mockService = new Mock<IUserManagementService>();
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["App:FrontendBaseUrl"] = "http://localhost:5173"
            })
            .Build();

        _controller = new UsersController(_mockService.Object, _configuration);
        SetupAdminContext();
    }

    private void SetupAdminContext()
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, _adminId.ToString()),
            new(ClaimTypes.Role, "Admin")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    private static UserListItemDto CreateUserListItem(Guid? id = null) => new()
    {
        Id = id ?? Guid.NewGuid(),
        DisplayName = "Test User",
        Email = "test@example.com",
        Role = "Teacher",
        AccountStatus = AccountStatus.Active,
        CreatedAt = DateTime.UtcNow
    };

    private static UserDetailDto CreateUserDetail(Guid? id = null) => new()
    {
        Id = id ?? Guid.NewGuid(),
        DisplayName = "Test User",
        Email = "test@example.com",
        Role = "Teacher",
        AccountStatus = AccountStatus.Active,
        CreatedAt = DateTime.UtcNow,
        HasPendingInvitation = false
    };

    #region GetUsers

    [Fact]
    public async Task GetUsers_ReturnsOkWithPagedResult()
    {
        var paged = new PagedResult<UserListItemDto>
        {
            Items = [CreateUserListItem()],
            TotalCount = 1,
            Page = 1,
            PageSize = 20
        };
        _mockService.Setup(s => s.GetUsersAsync(It.IsAny<UserListQueryDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(paged);

        var result = await _controller.GetUsers(new UserListQueryDto(), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var value = Assert.IsType<PagedResult<UserListItemDto>>(ok.Value);
        Assert.Single(value.Items);
    }

    [Fact]
    public async Task GetUsers_EmptyResult_ReturnsOkWithEmptyList()
    {
        var paged = new PagedResult<UserListItemDto> { Items = [], TotalCount = 0, Page = 1, PageSize = 20 };
        _mockService.Setup(s => s.GetUsersAsync(It.IsAny<UserListQueryDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(paged);

        var result = await _controller.GetUsers(new UserListQueryDto(), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var value = Assert.IsType<PagedResult<UserListItemDto>>(ok.Value);
        Assert.Empty(value.Items);
    }

    #endregion

    #region GetUserById

    [Fact]
    public async Task GetUserById_UserExists_ReturnsOk()
    {
        var userId = Guid.NewGuid();
        var detail = CreateUserDetail(userId);
        _mockService.Setup(s => s.GetUserByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(detail);

        var result = await _controller.GetUserById(userId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var value = Assert.IsType<UserDetailDto>(ok.Value);
        Assert.Equal(userId, value.Id);
    }

    [Fact]
    public async Task GetUserById_UserNotFound_ReturnsNotFound()
    {
        _mockService.Setup(s => s.GetUserByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserDetailDto?)null);

        var result = await _controller.GetUserById(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    #endregion

    #region CreateUser

    [Fact]
    public async Task CreateUser_ValidDto_ReturnsCreated()
    {
        var userId = Guid.NewGuid();
        var dto = new CreateUserDto { Role = "Admin", DisplayName = "New Admin", Email = "newadmin@example.com" };
        var response = new InvitationResponseDto
        {
            InvitationUrl = "http://localhost:5173/set-password?token=abc",
            ExpiresAt = DateTime.UtcNow.AddHours(72),
            UserId = userId
        };
        _mockService.Setup(s => s.CreateUserAsync(dto, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<InvitationResponseDto>.Success(response));

        var result = await _controller.CreateUser(dto, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(201, created.StatusCode);
    }

    [Fact]
    public async Task CreateUser_DuplicateEmail_ReturnsBadRequest()
    {
        var dto = new CreateUserDto { Role = "Admin", DisplayName = "Test", Email = "existing@example.com" };
        _mockService.Setup(s => s.CreateUserAsync(dto, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<InvitationResponseDto>.Failure("Unable to create the user account."));

        var result = await _controller.CreateUser(dto, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateUser_TeacherRoleWithoutLinkedObject_ReturnsBadRequest()
    {
        var dto = new CreateUserDto { Role = "Teacher", DisplayName = "Test Teacher", Email = "teacher@example.com" };
        _mockService.Setup(s => s.CreateUserAsync(dto, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<InvitationResponseDto>.Failure("A linked Teacher or Student must be provided for this role."));

        var result = await _controller.CreateUser(dto, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    #endregion

    #region UpdateDisplayName

    [Fact]
    public async Task UpdateDisplayName_ValidDto_ReturnsNoContent()
    {
        var userId = Guid.NewGuid();
        var dto = new UpdateDisplayNameDto { DisplayName = "Updated Name" };
        _mockService.Setup(s => s.UpdateDisplayNameAsync(userId, dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(true));

        var result = await _controller.UpdateDisplayName(userId, dto, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task UpdateDisplayName_UserNotFound_ReturnsBadRequest()
    {
        var dto = new UpdateDisplayNameDto { DisplayName = "Test" };
        _mockService.Setup(s => s.UpdateDisplayNameAsync(It.IsAny<Guid>(), dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Failure("User not found."));

        var result = await _controller.UpdateDisplayName(Guid.NewGuid(), dto, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    #endregion

    #region UpdateAccountStatus

    [Fact]
    public async Task UpdateAccountStatus_SuspendUser_ReturnsNoContent()
    {
        var userId = Guid.NewGuid();
        var dto = new UpdateAccountStatusDto { AccountStatus = AccountStatus.Suspended };
        _mockService.Setup(s => s.UpdateAccountStatusAsync(userId, dto, _adminId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(true));

        var result = await _controller.UpdateAccountStatus(userId, dto, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task UpdateAccountStatus_SelfSuspend_ReturnsBadRequest()
    {
        var dto = new UpdateAccountStatusDto { AccountStatus = AccountStatus.Suspended };
        _mockService.Setup(s => s.UpdateAccountStatusAsync(_adminId, dto, _adminId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Failure("You cannot suspend your own account."));

        var result = await _controller.UpdateAccountStatus(_adminId, dto, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UpdateAccountStatus_LastAdminSuspend_ReturnsBadRequest()
    {
        var otherAdmin = Guid.NewGuid();
        var dto = new UpdateAccountStatusDto { AccountStatus = AccountStatus.Suspended };
        _mockService.Setup(s => s.UpdateAccountStatusAsync(otherAdmin, dto, _adminId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Failure("Cannot suspend the last Admin account."));

        var result = await _controller.UpdateAccountStatus(otherAdmin, dto, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UpdateAccountStatus_MissingUserClaim_ReturnsBadRequest()
    {
        var controller = new UsersController(_mockService.Object, _configuration);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) }
        };
        var dto = new UpdateAccountStatusDto { AccountStatus = AccountStatus.Suspended };

        var result = await controller.UpdateAccountStatus(Guid.NewGuid(), dto, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    #endregion

    #region ResendInvitation

    [Fact]
    public async Task ResendInvitation_PendingUser_ReturnsOkWithUrl()
    {
        var userId = Guid.NewGuid();
        var response = new InvitationResponseDto
        {
            InvitationUrl = "http://localhost:5173/set-password?token=newtoken",
            ExpiresAt = DateTime.UtcNow.AddHours(72),
            UserId = userId
        };
        _mockService.Setup(s => s.ResendInvitationAsync(userId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<InvitationResponseDto>.Success(response));

        var result = await _controller.ResendInvitation(userId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var value = Assert.IsType<InvitationResponseDto>(ok.Value);
        Assert.Contains("set-password", value.InvitationUrl);
    }

    [Fact]
    public async Task ResendInvitation_ActiveUser_ReturnsBadRequest()
    {
        var userId = Guid.NewGuid();
        _mockService.Setup(s => s.ResendInvitationAsync(userId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<InvitationResponseDto>.Failure("Invitations can only be resent for users with Pending First Login status."));

        var result = await _controller.ResendInvitation(userId, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    #endregion
}
