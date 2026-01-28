using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Interfaces;

namespace BosDAT.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InstrumentsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public InstrumentsController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<InstrumentDto>>> GetAll(
        [FromQuery] bool? activeOnly,
        CancellationToken cancellationToken)
    {
        var query = _unitOfWork.Repository<Instrument>().Query();

        if (activeOnly == true)
        {
            query = query.Where(i => i.IsActive);
        }

        var instruments = await query
            .OrderBy(i => i.Name)
            .Select(i => new InstrumentDto
            {
                Id = i.Id,
                Name = i.Name,
                Category = i.Category,
                IsActive = i.IsActive
            })
            .ToListAsync(cancellationToken);

        return Ok(instruments);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<InstrumentDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var instrument = await _unitOfWork.Repository<Instrument>().GetByIdAsync(id, cancellationToken);

        if (instrument == null)
        {
            return NotFound();
        }

        return Ok(new InstrumentDto
        {
            Id = instrument.Id,
            Name = instrument.Name,
            Category = instrument.Category,
            IsActive = instrument.IsActive
        });
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<InstrumentDto>> Create([FromBody] CreateInstrumentDto dto, CancellationToken cancellationToken)
    {
        // Check for duplicate name
        var existing = await _unitOfWork.Repository<Instrument>()
            .FirstOrDefaultAsync(i => string.Equals(i.Name, dto.Name, StringComparison.OrdinalIgnoreCase), cancellationToken);

        if (existing != null)
        {
            return BadRequest(new { message = "An instrument with this name already exists" });
        }

        var instrument = new Instrument
        {
            Name = dto.Name,
            Category = dto.Category,
            IsActive = true
        };

        await _unitOfWork.Repository<Instrument>().AddAsync(instrument, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = instrument.Id }, new InstrumentDto
        {
            Id = instrument.Id,
            Name = instrument.Name,
            Category = instrument.Category,
            IsActive = instrument.IsActive
        });
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<InstrumentDto>> Update(int id, [FromBody] UpdateInstrumentDto dto, CancellationToken cancellationToken)
    {
        var instrument = await _unitOfWork.Repository<Instrument>().GetByIdAsync(id, cancellationToken);

        if (instrument == null)
        {
            return NotFound();
        }

        // Check for duplicate name (excluding current)
        var existing = await _unitOfWork.Repository<Instrument>()
            .FirstOrDefaultAsync(i => string.Equals(i.Name, dto.Name, StringComparison.OrdinalIgnoreCase) && i.Id != id, cancellationToken);

        if (existing != null)
        {
            return BadRequest(new { message = "An instrument with this name already exists" });
        }

        instrument.Name = dto.Name;
        instrument.Category = dto.Category;
        instrument.IsActive = dto.IsActive;

        await _unitOfWork.Repository<Instrument>().UpdateAsync(instrument, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(new InstrumentDto
        {
            Id = instrument.Id,
            Name = instrument.Name,
            Category = instrument.Category,
            IsActive = instrument.IsActive
        });
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var instrument = await _unitOfWork.Repository<Instrument>().GetByIdAsync(id, cancellationToken);

        if (instrument == null)
        {
            return NotFound();
        }

        // Instead of deleting, deactivate
        instrument.IsActive = false;
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return NoContent();
    }
}
