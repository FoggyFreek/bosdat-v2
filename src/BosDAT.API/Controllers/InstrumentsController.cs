using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BosDAT.Core.DTOs;
using BosDAT.Core.Interfaces.Services;

namespace BosDAT.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InstrumentsController(IInstrumentService instrumentService) : ControllerBase
{

    [HttpGet]
    public async Task<ActionResult<IEnumerable<InstrumentDto>>> GetAll(
        [FromQuery] bool? activeOnly,
        CancellationToken cancellationToken)
    {
        var instruments = await instrumentService.GetAllAsync(activeOnly, cancellationToken);
        return Ok(instruments);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<InstrumentDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var (instrument, notFound) = await instrumentService.GetByIdAsync(id, cancellationToken);

        if (notFound)
        {
            return NotFound();
        }

        return Ok(instrument);
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<InstrumentDto>> Create([FromBody] CreateInstrumentDto dto, CancellationToken cancellationToken)
    {
        var (instrument, error) = await instrumentService.CreateAsync(dto, cancellationToken);

        if (error != null)
        {
            return BadRequest(new { message = error });
        }

        return CreatedAtAction(nameof(GetById), new { id = instrument!.Id }, instrument);
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<InstrumentDto>> Update(int id, [FromBody] UpdateInstrumentDto dto, CancellationToken cancellationToken)
    {
        var (instrument, error, notFound) = await instrumentService.UpdateAsync(id, dto, cancellationToken);

        if (notFound)
        {
            return NotFound();
        }

        if (error != null)
        {
            return BadRequest(new { message = error });
        }

        return Ok(instrument);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var (_, notFound) = await instrumentService.DeleteAsync(id, cancellationToken);

        if (notFound)
        {
            return NotFound();
        }

        return NoContent();
    }
}
