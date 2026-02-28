using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BosDAT.Core.DTOs;
using BosDAT.Core.Interfaces;

namespace BosDAT.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "AdminOnly")]
public class UsersController(IUserManagementService userManagementService, IConfiguration configuration) : ControllerBase
{
    private string FrontendBaseUrl =>
        configuration["App:FrontendBaseUrl"] ?? "http://localhost:5173";

    [HttpGet]
    public async Task<ActionResult<PagedResult<UserListItemDto>>> GetUsers(
        [FromQuery] UserListQueryDto query, CancellationToken cancellationToken)
    {
        var result = await userManagementService.GetUsersAsync(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserDetailDto>> GetUserById(Guid id, CancellationToken cancellationToken)
    {
        var user = await userManagementService.GetUserByIdAsync(id, cancellationToken);
        if (user is null) return NotFound(new { message = "User not found." });
        return Ok(user);
    }

    [HttpPost]
    public async Task<ActionResult<InvitationResponseDto>> CreateUser(
        [FromBody] CreateUserDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var result = await userManagementService.CreateUserAsync(dto, FrontendBaseUrl, cancellationToken);
        if (!result.IsSuccess) return BadRequest(new { message = result.Error });

        return CreatedAtAction(nameof(GetUserById), new { id = result.Value!.UserId }, result.Value);
    }

    [HttpPatch("{id:guid}/display-name")]
    public async Task<IActionResult> UpdateDisplayName(
        Guid id, [FromBody] UpdateDisplayNameDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var result = await userManagementService.UpdateDisplayNameAsync(id, dto, cancellationToken);
        if (!result.IsSuccess) return BadRequest(new { message = result.Error });

        return NoContent();
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> UpdateAccountStatus(
        Guid id, [FromBody] UpdateAccountStatusDto dto, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId))
            return BadRequest(new { message = "Invalid user context." });
        var result = await userManagementService.UpdateAccountStatusAsync(id, dto, actorId, cancellationToken);
        if (!result.IsSuccess) return BadRequest(new { message = result.Error });

        return NoContent();
    }

    [HttpPost("{id:guid}/resend-invitation")]
    public async Task<ActionResult<InvitationResponseDto>> ResendInvitation(
        Guid id, CancellationToken cancellationToken)
    {
        var result = await userManagementService.ResendInvitationAsync(id, FrontendBaseUrl, cancellationToken);
        if (!result.IsSuccess) return BadRequest(new { message = result.Error });

        return Ok(result.Value);
    }
}
