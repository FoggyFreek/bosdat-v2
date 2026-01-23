using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Interfaces;

namespace BosDAT.API.Controllers;

[ApiController]
[Route("api/course-types")]
[Authorize]
public class CourseTypesController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public CourseTypesController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CourseTypeDto>>> GetAll(
        [FromQuery] bool? activeOnly,
        [FromQuery] int? instrumentId,
        CancellationToken cancellationToken)
    {
        IQueryable<CourseType> query = _unitOfWork.Repository<CourseType>().Query()
            .Include(ct => ct.Instrument);

        if (activeOnly == true)
        {
            query = query.Where(ct => ct.IsActive);
        }

        if (instrumentId.HasValue)
        {
            query = query.Where(ct => ct.InstrumentId == instrumentId.Value);
        }

        var courseRepo = _unitOfWork.Repository<Course>();
        var teacherCourseTypeRepo = _unitOfWork.Repository<TeacherCourseType>();

        var courseTypes = await query
            .OrderBy(ct => ct.Instrument.Name)
            .ThenBy(ct => ct.Name)
            .Select(ct => new CourseTypeDto
            {
                Id = ct.Id,
                InstrumentId = ct.InstrumentId,
                InstrumentName = ct.Instrument.Name,
                Name = ct.Name,
                DurationMinutes = ct.DurationMinutes,
                Type = ct.Type,
                PriceAdult = ct.PriceAdult,
                PriceChild = ct.PriceChild,
                MaxStudents = ct.MaxStudents,
                IsActive = ct.IsActive,
                ActiveCourseCount = courseRepo.Query().Count(c => c.CourseTypeId == ct.Id && c.Status == CourseStatus.Active),
                HasTeachersForCourseType = teacherCourseTypeRepo.Query().Any(tct => tct.CourseTypeId == ct.Id && tct.Teacher.IsActive)
            })
            .ToListAsync(cancellationToken);

        return Ok(courseTypes);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CourseTypeDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var courseType = await _unitOfWork.Repository<CourseType>().Query()
            .Include(ct => ct.Instrument)
            .FirstOrDefaultAsync(ct => ct.Id == id, cancellationToken);

        if (courseType == null)
        {
            return NotFound();
        }

        return Ok(await MapToDtoAsync(courseType, cancellationToken));
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<CourseTypeDto>> Create([FromBody] CreateCourseTypeDto dto, CancellationToken cancellationToken)
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

        var courseType = new CourseType
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

        await _unitOfWork.Repository<CourseType>().AddAsync(courseType, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        courseType.Instrument = instrument;
        return CreatedAtAction(nameof(GetById), new { id = courseType.Id }, await MapToDtoAsync(courseType, cancellationToken));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<CourseTypeDto>> Update(Guid id, [FromBody] UpdateCourseTypeDto dto, CancellationToken cancellationToken)
    {
        var courseType = await _unitOfWork.Repository<CourseType>().Query()
            .Include(ct => ct.Instrument)
            .FirstOrDefaultAsync(ct => ct.Id == id, cancellationToken);

        if (courseType == null)
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
        if (courseType.IsActive && !dto.IsActive)
        {
            var activeCourseCount = await _unitOfWork.Repository<Course>().Query()
                .CountAsync(c => c.CourseTypeId == id && c.Status == CourseStatus.Active, cancellationToken);

            if (activeCourseCount > 0)
            {
                return BadRequest(new { message = $"Cannot archive course type: {activeCourseCount} active course(s) are using it" });
            }
        }

        courseType.InstrumentId = dto.InstrumentId;
        courseType.Name = dto.Name;
        courseType.DurationMinutes = dto.DurationMinutes;
        courseType.Type = dto.Type;
        courseType.PriceAdult = dto.PriceAdult;
        courseType.PriceChild = dto.PriceChild;
        courseType.MaxStudents = dto.MaxStudents;
        courseType.IsActive = dto.IsActive;
        courseType.Instrument = instrument;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(await MapToDtoAsync(courseType, cancellationToken));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var courseType = await _unitOfWork.Repository<CourseType>().GetByIdAsync(id, cancellationToken);

        if (courseType == null)
        {
            return NotFound();
        }

        // Validate: cannot archive if part of an active course
        var activeCourseCount = await _unitOfWork.Repository<Course>().Query()
            .CountAsync(c => c.CourseTypeId == id && c.Status == CourseStatus.Active, cancellationToken);

        if (activeCourseCount > 0)
        {
            return BadRequest(new { message = $"Cannot archive course type: {activeCourseCount} active course(s) are using it" });
        }

        courseType.IsActive = false;
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpPut("{id:guid}/reactivate")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<CourseTypeDto>> Reactivate(Guid id, CancellationToken cancellationToken)
    {
        var courseType = await _unitOfWork.Repository<CourseType>().Query()
            .Include(ct => ct.Instrument)
            .FirstOrDefaultAsync(ct => ct.Id == id, cancellationToken);

        if (courseType == null)
        {
            return NotFound();
        }

        courseType.IsActive = true;
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(await MapToDtoAsync(courseType, cancellationToken));
    }

    [HttpGet("teachers-for-instrument/{instrumentId:int}")]
    public async Task<ActionResult<int>> GetTeachersCountForInstrument(int instrumentId, CancellationToken cancellationToken)
    {
        var instrument = await _unitOfWork.Repository<Instrument>().GetByIdAsync(instrumentId, cancellationToken);
        if (instrument == null)
        {
            return NotFound(new { message = "Instrument not found" });
        }

        var teacherCount = await _unitOfWork.Repository<TeacherInstrument>().Query()
            .Where(ti => ti.InstrumentId == instrumentId && ti.Teacher.IsActive)
            .CountAsync(cancellationToken);

        return Ok(teacherCount);
    }

    private async Task<CourseTypeDto> MapToDtoAsync(CourseType courseType, CancellationToken cancellationToken)
    {
        var activeCourseCount = await _unitOfWork.Repository<Course>().Query()
            .CountAsync(c => c.CourseTypeId == courseType.Id && c.Status == CourseStatus.Active, cancellationToken);

        var hasTeachers = await _unitOfWork.Repository<TeacherCourseType>().Query()
            .AnyAsync(tct => tct.CourseTypeId == courseType.Id && tct.Teacher.IsActive, cancellationToken);

        return new CourseTypeDto
        {
            Id = courseType.Id,
            InstrumentId = courseType.InstrumentId,
            InstrumentName = courseType.Instrument.Name,
            Name = courseType.Name,
            DurationMinutes = courseType.DurationMinutes,
            Type = courseType.Type,
            PriceAdult = courseType.PriceAdult,
            PriceChild = courseType.PriceChild,
            MaxStudents = courseType.MaxStudents,
            IsActive = courseType.IsActive,
            ActiveCourseCount = activeCourseCount,
            HasTeachersForCourseType = hasTeachers
        };
    }
}
