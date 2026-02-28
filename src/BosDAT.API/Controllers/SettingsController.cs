using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BosDAT.Core.Entities;
using BosDAT.Core.Interfaces.Services;

namespace BosDAT.API.Controllers;

[ApiController]
[Route("api/settings")]
[Authorize]
public class SettingsController(ISettingsService settingsService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Setting>>> GetAll(CancellationToken cancellationToken)
    {
        var settings = await settingsService.GetAllAsync(cancellationToken);
        return Ok(settings);
    }

    [HttpGet("{key}")]
    public async Task<ActionResult<Setting>> GetByKey(string key, CancellationToken cancellationToken)
    {
        var setting = await settingsService.GetByKeyAsync(key, cancellationToken);

        if (setting == null)
        {
            return NotFound();
        }

        return Ok(setting);
    }

    [HttpPut("{key}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<Setting>> Update(string key, [FromBody] UpdateSettingDto dto, CancellationToken cancellationToken)
    {
        var setting = await settingsService.UpdateAsync(key, dto.Value, cancellationToken);

        if (setting == null)
        {
            return NotFound();
        }

        return Ok(setting);
    }
}

public record UpdateSettingDto
{
    public required string Value { get; init; }
}
