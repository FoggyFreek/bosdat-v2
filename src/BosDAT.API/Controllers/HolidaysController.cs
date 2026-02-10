using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BosDAT.Core.DTOs;
using BosDAT.Core.Interfaces;

namespace BosDAT.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class HolidaysController(IHolidayService holidayService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<HolidayDto>>> GetAll(CancellationToken cancellationToken)
    {
        var holidays = await holidayService.GetAllAsync(cancellationToken);
        return Ok(holidays);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<HolidayDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var holiday = await holidayService.GetByIdAsync(id, cancellationToken);

        if (holiday == null)
        {
            return NotFound();
        }

        return Ok(holiday);
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<HolidayDto>> Create([FromBody] CreateHolidayDto dto, CancellationToken cancellationToken)
    {
        var holiday = await holidayService.CreateAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = holiday.Id }, holiday);
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<HolidayDto>> Update(int id, [FromBody] UpdateHolidayDto dto, CancellationToken cancellationToken)
    {
        var holiday = await holidayService.UpdateAsync(id, dto, cancellationToken);

        if (holiday == null)
        {
            return NotFound();
        }

        return Ok(holiday);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var success = await holidayService.DeleteAsync(id, cancellationToken);

        if (!success)
        {
            return NotFound();
        }

        return NoContent();
    }
}
