using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BosDAT.Core.Entities;
using BosDAT.Core.Interfaces;

namespace BosDAT.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class HolidaysController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public HolidaysController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<HolidayDto>>> GetAll(CancellationToken cancellationToken)
    {
        var holidays = await _unitOfWork.Repository<Holiday>().Query()
            .OrderBy(h => h.StartDate)
            .Select(h => new HolidayDto
            {
                Id = h.Id,
                Name = h.Name,
                StartDate = h.StartDate,
                EndDate = h.EndDate
            })
            .ToListAsync(cancellationToken);

        return Ok(holidays);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<HolidayDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var holiday = await _unitOfWork.Repository<Holiday>().GetByIdAsync(id, cancellationToken);

        if (holiday == null)
        {
            return NotFound();
        }

        return Ok(new HolidayDto
        {
            Id = holiday.Id,
            Name = holiday.Name,
            StartDate = holiday.StartDate,
            EndDate = holiday.EndDate
        });
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<HolidayDto>> Create([FromBody] CreateHolidayDto dto, CancellationToken cancellationToken)
    {
        var holiday = new Holiday
        {
            Name = dto.Name,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate
        };

        await _unitOfWork.Repository<Holiday>().AddAsync(holiday, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = holiday.Id }, new HolidayDto
        {
            Id = holiday.Id,
            Name = holiday.Name,
            StartDate = holiday.StartDate,
            EndDate = holiday.EndDate
        });
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<HolidayDto>> Update(int id, [FromBody] UpdateHolidayDto dto, CancellationToken cancellationToken)
    {
        var holiday = await _unitOfWork.Repository<Holiday>().GetByIdAsync(id, cancellationToken);

        if (holiday == null)
        {
            return NotFound();
        }

        holiday.Name = dto.Name;
        holiday.StartDate = dto.StartDate;
        holiday.EndDate = dto.EndDate;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(new HolidayDto
        {
            Id = holiday.Id,
            Name = holiday.Name,
            StartDate = holiday.StartDate,
            EndDate = holiday.EndDate
        });
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var holiday = await _unitOfWork.Repository<Holiday>().GetByIdAsync(id, cancellationToken);

        if (holiday == null)
        {
            return NotFound();
        }

        _unitOfWork.Repository<Holiday>().Delete(holiday);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return NoContent();
    }
}

public record HolidayDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
}

public record CreateHolidayDto
{
    public required string Name { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
}

public record UpdateHolidayDto
{
    public required string Name { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
}
