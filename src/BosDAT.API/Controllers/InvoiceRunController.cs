using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BosDAT.Core.DTOs;
using BosDAT.Core.Interfaces.Services;

namespace BosDAT.API.Controllers;

[ApiController]
[Route("api/admin/invoice-runs")]
[Authorize(Policy = "AdminOnly")]
public class InvoiceRunController(
    IInvoiceRunService invoiceRunService,
    ICurrentUserService currentUserService) : ControllerBase
{
    [HttpPost("run")]
    public async Task<ActionResult<InvoiceRunResultDto>> RunBulkGeneration(
        [FromBody] StartInvoiceRunDto dto, CancellationToken ct)
    {
        var userId = currentUserService.UserId;
        if (userId == null)
        {
            return Unauthorized();
        }

        var initiatedBy = currentUserService.UserEmail ?? "Manual";

        var result = await invoiceRunService.RunBulkInvoiceGenerationAsync(
            dto, initiatedBy, userId.Value, ct);

        return Ok(result);
    }

    [HttpGet("runs")]
    public async Task<ActionResult<InvoiceRunsPageDto>> GetRuns(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 5,
        CancellationToken ct = default)
    {
        var runs = await invoiceRunService.GetRunsAsync(page, pageSize, ct);
        return Ok(runs);
    }

    [HttpGet("runs/{id:guid}")]
    public async Task<ActionResult<InvoiceRunDto>> GetRunById(Guid id, CancellationToken ct)
    {
        var run = await invoiceRunService.GetRunByIdAsync(id, ct);
        if (run == null)
        {
            return NotFound();
        }

        return Ok(run);
    }
}
