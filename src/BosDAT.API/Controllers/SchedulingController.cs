using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BosDAT.Core.DTOs;
using BosDAT.Core.Interfaces.Services;

namespace BosDAT.API.Controllers;

[ApiController]
[Route("api/admin/scheduling")]
[Authorize(Policy = "AdminOnly")]
public class SchedulingController(ISchedulingService schedulingService) : ControllerBase
{
    [HttpGet("status")]
    public async Task<ActionResult<SchedulingStatusDto>> GetStatus(CancellationToken ct)
    {
        var status = await schedulingService.GetSchedulingStatusAsync(ct);
        return Ok(status);
    }

    [HttpGet("runs")]
    public async Task<ActionResult<ScheduleRunsPageDto>> GetRuns(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 5,
        CancellationToken ct = default)
    {
        var runs = await schedulingService.GetScheduleRunsAsync(page, pageSize, ct);
        return Ok(runs);
    }

    [HttpPost("run/{id}")]
    public async Task<ActionResult<ManualRunResultDto>> RunSingle(Guid id, CancellationToken ct)
    {
        //FUTURE: get nr day ahead from settings
        var startDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var endDate = startDate.AddDays(90);

        var result = await schedulingService.ExecuteSingleCourseRunAsync(id, startDate, endDate, ct);
        return Ok(result);
    }

    [HttpPost("run")]
    public async Task<ActionResult<ManualRunResultDto>> RunManual(CancellationToken ct)
    {
        var startDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var endDate = startDate.AddDays(90);

        var result = await schedulingService.ExecuteManualRunAsync(startDate, endDate, ct);
        return Ok(result);
    }
}
