using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Interfaces;

namespace BosDAT.API.Controllers;

[ApiController]
[Route("api/lesson-types")]
[Authorize]
public class LessonTypesController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public LessonTypesController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<LessonTypeDto>>> GetAll(
        [FromQuery] bool? activeOnly,
        [FromQuery] int? instrumentId,
        CancellationToken cancellationToken)
    {
        IQueryable<LessonType> query = _unitOfWork.Repository<LessonType>().Query()
            .Include(lt => lt.Instrument);

        if (activeOnly == true)
        {
            query = query.Where(lt => lt.IsActive);
        }

        if (instrumentId.HasValue)
        {
            query = query.Where(lt => lt.InstrumentId == instrumentId.Value);
        }

        var lessonTypes = await query
            .OrderBy(lt => lt.Instrument.Name)
            .ThenBy(lt => lt.Name)
            .Select(lt => new LessonTypeDto
            {
                Id = lt.Id,
                InstrumentId = lt.InstrumentId,
                InstrumentName = lt.Instrument.Name,
                Name = lt.Name,
                DurationMinutes = lt.DurationMinutes,
                Type = lt.Type,
                PriceAdult = lt.PriceAdult,
                PriceChild = lt.PriceChild,
                MaxStudents = lt.MaxStudents,
                IsActive = lt.IsActive
            })
            .ToListAsync(cancellationToken);

        return Ok(lessonTypes);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<LessonTypeDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var lessonType = await _unitOfWork.Repository<LessonType>().Query()
            .Include(lt => lt.Instrument)
            .FirstOrDefaultAsync(lt => lt.Id == id, cancellationToken);

        if (lessonType == null)
        {
            return NotFound();
        }

        return Ok(MapToDto(lessonType));
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<LessonTypeDto>> Create([FromBody] CreateLessonTypeDto dto, CancellationToken cancellationToken)
    {
        var instrument = await _unitOfWork.Repository<Instrument>().GetByIdAsync(dto.InstrumentId, cancellationToken);
        if (instrument == null)
        {
            return BadRequest(new { message = "Instrument not found" });
        }

        var lessonType = new LessonType
        {
            InstrumentId = dto.InstrumentId,
            Name = dto.Name,
            DurationMinutes = dto.DurationMinutes,
            Type = dto.Type,
            PriceAdult = dto.PriceAdult,
            PriceChild = dto.PriceChild,
            MaxStudents = dto.MaxStudents,
            IsActive = true
        };

        await _unitOfWork.Repository<LessonType>().AddAsync(lessonType, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        lessonType.Instrument = instrument;
        return CreatedAtAction(nameof(GetById), new { id = lessonType.Id }, MapToDto(lessonType));
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<LessonTypeDto>> Update(int id, [FromBody] UpdateLessonTypeDto dto, CancellationToken cancellationToken)
    {
        var lessonType = await _unitOfWork.Repository<LessonType>().Query()
            .Include(lt => lt.Instrument)
            .FirstOrDefaultAsync(lt => lt.Id == id, cancellationToken);

        if (lessonType == null)
        {
            return NotFound();
        }

        var instrument = await _unitOfWork.Repository<Instrument>().GetByIdAsync(dto.InstrumentId, cancellationToken);
        if (instrument == null)
        {
            return BadRequest(new { message = "Instrument not found" });
        }

        lessonType.InstrumentId = dto.InstrumentId;
        lessonType.Name = dto.Name;
        lessonType.DurationMinutes = dto.DurationMinutes;
        lessonType.Type = dto.Type;
        lessonType.PriceAdult = dto.PriceAdult;
        lessonType.PriceChild = dto.PriceChild;
        lessonType.MaxStudents = dto.MaxStudents;
        lessonType.IsActive = dto.IsActive;
        lessonType.Instrument = instrument;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(MapToDto(lessonType));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var lessonType = await _unitOfWork.Repository<LessonType>().GetByIdAsync(id, cancellationToken);

        if (lessonType == null)
        {
            return NotFound();
        }

        lessonType.IsActive = false;
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private static LessonTypeDto MapToDto(LessonType lessonType)
    {
        return new LessonTypeDto
        {
            Id = lessonType.Id,
            InstrumentId = lessonType.InstrumentId,
            InstrumentName = lessonType.Instrument.Name,
            Name = lessonType.Name,
            DurationMinutes = lessonType.DurationMinutes,
            Type = lessonType.Type,
            PriceAdult = lessonType.PriceAdult,
            PriceChild = lessonType.PriceChild,
            MaxStudents = lessonType.MaxStudents,
            IsActive = lessonType.IsActive
        };
    }
}
