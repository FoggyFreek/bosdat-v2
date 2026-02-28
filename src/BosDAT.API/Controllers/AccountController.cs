using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using BosDAT.Core.DTOs;
using BosDAT.Core.Interfaces.Services;

namespace BosDAT.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
[EnableRateLimiting("sensitive")]
public class AccountController(IUserManagementService userManagementService) : ControllerBase
{
    [HttpGet("validate-token")]
    public async Task<ActionResult<ValidateTokenResponseDto>> ValidateToken(
        [FromQuery] string token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
            return Ok(new ValidateTokenResponseDto { IsValid = false });

        var result = await userManagementService.ValidateTokenAsync(token, cancellationToken);
        return Ok(result);
    }

    [HttpPost("set-password")]
    public async Task<IActionResult> SetPassword(
        [FromBody] SetPasswordDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var result = await userManagementService.SetPasswordFromTokenAsync(dto, cancellationToken);
        if (!result.IsSuccess) return BadRequest(new { message = result.Error });

        return NoContent();
    }
}
