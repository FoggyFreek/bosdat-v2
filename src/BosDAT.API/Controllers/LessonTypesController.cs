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

        var courseRepo = _unitOfWork.Repository<Course>();
        var teacherLessonTypeRepo = _unitOfWork.Repository<TeacherLessonType>();

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
                IsActive = lt.IsActive,
                ActiveCourseCount = courseRepo.Query().Count(c => c.LessonTypeId == lt.Id && c.Status == CourseStatus.Active),
                HasTeachersForLessonType = teacherLessonTypeRepo.Query().Any(tlt => tlt.LessonTypeId == lt.Id && tlt.Teacher.IsActive)
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

        return Ok(await MapToDtoAsync(lessonType, cancellationToken));
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

        // Validate child price cannot exceed adult price
        if (dto.PriceChild > dto.PriceAdult)
        {
            return BadRequest(new { message = "Child price cannot be higher than adult price" });
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
        return CreatedAtAction(nameof(GetById), new { id = lessonType.Id }, await MapToDtoAsync(lessonType, cancellationToken));
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

        // Validate child price cannot exceed adult price
        if (dto.PriceChild > dto.PriceAdult)
        {
            return BadRequest(new { message = "Child price cannot be higher than adult price" });
        }

        // Validate archiving: cannot archive if part of an active course
        if (lessonType.IsActive && !dto.IsActive)
        {
            var activeCourseCount = await _unitOfWork.Repository<Course>().Query()
                .CountAsync(c => c.LessonTypeId == id && c.Status == CourseStatus.Active, cancellationToken);

            if (activeCourseCount > 0)
            {
                return BadRequest(new { message = $"Cannot archive lesson type: {activeCourseCount} active course(s) are using it" });
            }
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

        return Ok(await MapToDtoAsync(lessonType, cancellationToken));
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

        // Validate: cannot archive if part of an active course
        var activeCourseCount = await _unitOfWork.Repository<Course>().Query()
            .CountAsync(c => c.LessonTypeId == id && c.Status == CourseStatus.Active, cancellationToken);

        if (activeCourseCount > 0)
        {
            return BadRequest(new { message = $"Cannot archive lesson type: {activeCourseCount} active course(s) are using it" });
        }

        lessonType.IsActive = false;
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpPut("{id:int}/reactivate")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<LessonTypeDto>> Reactivate(int id, CancellationToken cancellationToken)
    {
        var lessonType = await _unitOfWork.Repository<LessonType>().Query()
            .Include(lt => lt.Instrument)
            .FirstOrDefaultAsync(lt => lt.Id == id, cancellationToken);

        if (lessonType == null)
        {
            return NotFound();
        }

        lessonType.IsActive = true;
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(await MapToDtoAsync(lessonType, cancellationToken));
    }

    [HttpGet("teachers-for-instrument/{instrumentId:int}")]
    public async Task<ActionResult<object>> GetTeachersForInstrument(int instrumentId, CancellationToken cancellationToken)
    {
        var instrument = await _unitOfWork.Repository<Instrument>().GetByIdAsync(instrumentId, cancellationToken);
        if (instrument == null)
        {
            return NotFound(new { message = "Instrument not found" });
        }

        var teacherCount = await _unitOfWork.Repository<TeacherInstrument>().Query()
            .CountAsync(ti => ti.InstrumentId == instrumentId && ti.Teacher.IsActive, cancellationToken);

        return Ok(new { instrumentId, instrumentName = instrument.Name, teacherCount, hasTeachers = teacherCount > 0 });
    }

    private async Task<LessonTypeDto> MapToDtoAsync(LessonType lessonType, CancellationToken cancellationToken)
    {
        var activeCourseCount = await _unitOfWork.Repository<Course>().Query()
            .CountAsync(c => c.LessonTypeId == lessonType.Id && c.Status == CourseStatus.Active, cancellationToken);

        var hasTeachers = await _unitOfWork.Repository<TeacherLessonType>().Query()
            .AnyAsync(tlt => tlt.LessonTypeId == lessonType.Id && tlt.Teacher.IsActive, cancellationToken);

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
            IsActive = lessonType.IsActive,
            ActiveCourseCount = activeCourseCount,
            HasTeachersForLessonType = hasTeachers
        };
    }
}
