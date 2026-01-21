using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BosDAT.Core.Entities;
using BosDAT.Core.Interfaces;

namespace BosDAT.API.Controllers;

[ApiController]
[Route("api/settings")]
[Authorize]
public class SettingsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public SettingsController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Setting>>> GetAll(CancellationToken cancellationToken)
    {
        var settings = await _unitOfWork.Repository<Setting>().Query()
            .OrderBy(s => s.Key)
            .ToListAsync(cancellationToken);

        return Ok(settings);
    }

    [HttpGet("{key}")]
    public async Task<ActionResult<Setting>> GetByKey(string key, CancellationToken cancellationToken)
    {
        var setting = await _unitOfWork.Repository<Setting>().Query()
            .FirstOrDefaultAsync(s => s.Key == key, cancellationToken);

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
        var setting = await _unitOfWork.Repository<Setting>().Query()
            .FirstOrDefaultAsync(s => s.Key == key, cancellationToken);

        if (setting == null)
        {
            return NotFound();
        }

        setting.Value = dto.Value;
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(setting);
    }
}

public record UpdateSettingDto
{
    public required string Value { get; init; }
}
