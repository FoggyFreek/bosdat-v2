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
public class CourseTypesController(
    IUnitOfWork unitOfWork,
    ICourseTypePricingService pricingService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CourseTypeDto>>> GetAll(
        [FromQuery] bool? activeOnly,
        [FromQuery] int? instrumentId,
        CancellationToken cancellationToken)
    {
        IQueryable<CourseType> query = unitOfWork.Repository<CourseType>().Query()
            .Include(ct => ct.Instrument)
            .Include(ct => ct.PricingVersions);

        if (activeOnly == true)
        {
            query = query.Where(ct => ct.IsActive);
        }

        if (instrumentId.HasValue)
        {
            query = query.Where(ct => ct.InstrumentId == instrumentId.Value);
        }

        var courseRepo = unitOfWork.Repository<Course>();
        var teacherCourseTypeRepo = unitOfWork.Repository<TeacherCourseType>();

        var courseTypes = await query
            .OrderBy(ct => ct.Instrument.Name)
            .ThenBy(ct => ct.Name)
            .ToListAsync(cancellationToken);

        var result = new List<CourseTypeDto>();
        foreach (var ct in courseTypes)
        {
            var isInvoiced = await pricingService.IsCurrentPricingInvoicedAsync(ct.Id, cancellationToken);
            result.Add(await MapToDtoAsync(ct, !isInvoiced, cancellationToken));
        }

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CourseTypeDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var courseType = await unitOfWork.Repository<CourseType>().Query()
            .Include(ct => ct.Instrument)
            .Include(ct => ct.PricingVersions)
            .FirstOrDefaultAsync(ct => ct.Id == id, cancellationToken);

        if (courseType == null)
        {
            return NotFound();
        }

        var isInvoiced = await pricingService.IsCurrentPricingInvoicedAsync(id, cancellationToken);
        return Ok(await MapToDtoAsync(courseType, !isInvoiced, cancellationToken));
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<CourseTypeDto>> Create([FromBody] CreateCourseTypeDto dto, CancellationToken cancellationToken)
    {
        var instrument = await unitOfWork.Repository<Instrument>().GetByIdAsync(dto.InstrumentId, cancellationToken);
        if (instrument == null)
        {
            return BadRequest(new { message = "Instrument not found" });
        }

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
            MaxStudents = dto.MaxStudents,
            IsActive = true
        };

        await unitOfWork.Repository<CourseType>().AddAsync(courseType, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Create initial pricing version
        await pricingService.CreateInitialPricingVersionAsync(
            courseType.Id,
            dto.PriceAdult,
            dto.PriceChild,
            cancellationToken);

        // Reload to get navigation properties
        var createdCourseType = await unitOfWork.Repository<CourseType>().Query()
            .Include(ct => ct.Instrument)
            .Include(ct => ct.PricingVersions)
            .FirstAsync(ct => ct.Id == courseType.Id, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = courseType.Id }, await MapToDtoAsync(createdCourseType, true, cancellationToken));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<CourseTypeDto>> Update(Guid id, [FromBody] UpdateCourseTypeDto dto, CancellationToken cancellationToken)
    {
        var courseType = await unitOfWork.Repository<CourseType>().Query()
            .Include(ct => ct.Instrument)
            .Include(ct => ct.PricingVersions)
            .FirstOrDefaultAsync(ct => ct.Id == id, cancellationToken);

        if (courseType == null)
        {
            return NotFound();
        }

        var instrument = await unitOfWork.Repository<Instrument>().GetByIdAsync(dto.InstrumentId, cancellationToken);
        if (instrument == null)
        {
            return BadRequest(new { message = "Instrument not found" });
        }

        // Validate archiving: cannot archive if part of an active course
        if (courseType.IsActive && !dto.IsActive)
        {
            var activeCourseCount = await unitOfWork.Repository<Course>().Query()
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
        courseType.MaxStudents = dto.MaxStudents;
        courseType.IsActive = dto.IsActive;
        courseType.Instrument = instrument;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var isInvoiced = await pricingService.IsCurrentPricingInvoicedAsync(id, cancellationToken);
        return Ok(await MapToDtoAsync(courseType, !isInvoiced, cancellationToken));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var courseType = await unitOfWork.Repository<CourseType>().GetByIdAsync(id, cancellationToken);

        if (courseType == null)
        {
            return NotFound();
        }

        var activeCourseCount = await unitOfWork.Repository<Course>().Query()
            .CountAsync(c => c.CourseTypeId == id && c.Status == CourseStatus.Active, cancellationToken);

        if (activeCourseCount > 0)
        {
            return BadRequest(new { message = $"Cannot archive course type: {activeCourseCount} active course(s) are using it" });
        }

        courseType.IsActive = false;
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpPut("{id:guid}/reactivate")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<CourseTypeDto>> Reactivate(Guid id, CancellationToken cancellationToken)
    {
        var courseType = await unitOfWork.Repository<CourseType>().Query()
            .Include(ct => ct.Instrument)
            .Include(ct => ct.PricingVersions)
            .FirstOrDefaultAsync(ct => ct.Id == id, cancellationToken);

        if (courseType == null)
        {
            return NotFound();
        }

        courseType.IsActive = true;
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var isInvoiced = await pricingService.IsCurrentPricingInvoicedAsync(id, cancellationToken);
        return Ok(await MapToDtoAsync(courseType, !isInvoiced, cancellationToken));
    }

    [HttpGet("{id:guid}/pricing/history")]
    public async Task<ActionResult<IEnumerable<CourseTypePricingVersionDto>>> GetPricingHistory(Guid id, CancellationToken cancellationToken)
    {
        var courseType = await unitOfWork.Repository<CourseType>().GetByIdAsync(id, cancellationToken);
        if (courseType == null)
        {
            return NotFound();
        }

        var history = await pricingService.GetPricingHistoryAsync(id, cancellationToken);
        return Ok(history.Select(MapPricingVersionToDto));
    }

    [HttpGet("{id:guid}/pricing/can-edit")]
    public async Task<ActionResult<PricingEditabilityDto>> CheckPricingEditability(Guid id, CancellationToken cancellationToken)
    {
        var courseType = await unitOfWork.Repository<CourseType>().GetByIdAsync(id, cancellationToken);
        if (courseType == null)
        {
            return NotFound();
        }

        var isInvoiced = await pricingService.IsCurrentPricingInvoicedAsync(id, cancellationToken);

        return Ok(new PricingEditabilityDto
        {
            CanEditDirectly = !isInvoiced,
            IsInvoiced = isInvoiced,
            Reason = isInvoiced ? "Current pricing has been used in invoices. Create a new version instead." : null
        });
    }

    [HttpPut("{id:guid}/pricing")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<CourseTypePricingVersionDto>> UpdatePricing(
        Guid id,
        [FromBody] UpdateCourseTypePricingDto dto,
        CancellationToken cancellationToken)
    {
        var courseType = await unitOfWork.Repository<CourseType>().GetByIdAsync(id, cancellationToken);
        if (courseType == null)
        {
            return NotFound();
        }

        if (dto.PriceChild > dto.PriceAdult)
        {
            return BadRequest(new { message = "Child price cannot be higher than adult price" });
        }

        try
        {
            var updatedPricing = await pricingService.UpdateCurrentPricingAsync(
                id,
                dto.PriceAdult,
                dto.PriceChild,
                cancellationToken);

            return Ok(MapPricingVersionToDto(updatedPricing));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:guid}/pricing/versions")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<CourseTypePricingVersionDto>> CreatePricingVersion(
        Guid id,
        [FromBody] CreateCourseTypePricingVersionDto dto,
        CancellationToken cancellationToken)
    {
        var courseType = await unitOfWork.Repository<CourseType>().GetByIdAsync(id, cancellationToken);
        if (courseType == null)
        {
            return NotFound();
        }

        if (dto.PriceChild > dto.PriceAdult)
        {
            return BadRequest(new { message = "Child price cannot be higher than adult price" });
        }

        try
        {
            var newVersion = await pricingService.CreateNewPricingVersionAsync(
                id,
                dto.PriceAdult,
                dto.PriceChild,
                dto.ValidFrom,
                cancellationToken);

            return CreatedAtAction(nameof(GetPricingHistory), new { id }, MapPricingVersionToDto(newVersion));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("teachers-for-instrument/{instrumentId:int}")]
    public async Task<ActionResult<int>> GetTeachersCountForInstrument(int instrumentId, CancellationToken cancellationToken)
    {
        var instrument = await unitOfWork.Repository<Instrument>().GetByIdAsync(instrumentId, cancellationToken);
        if (instrument == null)
        {
            return NotFound(new { message = "Instrument not found" });
        }

        var teacherCount = await unitOfWork.Repository<TeacherInstrument>().Query()
            .Where(ti => ti.InstrumentId == instrumentId && ti.Teacher.IsActive)
            .CountAsync(cancellationToken);

        return Ok(teacherCount);
    }

    private async Task<CourseTypeDto> MapToDtoAsync(CourseType courseType, bool canEditPricingDirectly, CancellationToken cancellationToken)
    {
        var activeCourseCount = await unitOfWork.Repository<Course>().Query()
            .CountAsync(c => c.CourseTypeId == courseType.Id && c.Status == CourseStatus.Active, cancellationToken);

        var hasTeachers = await unitOfWork.Repository<TeacherCourseType>().Query()
            .AnyAsync(tct => tct.CourseTypeId == courseType.Id && tct.Teacher.IsActive, cancellationToken);

        var currentPricing = courseType.PricingVersions.FirstOrDefault(pv => pv.IsCurrent);
        var pricingHistory = courseType.PricingVersions
            .OrderByDescending(pv => pv.ValidFrom)
            .Select(MapPricingVersionToDto)
            .ToList();

        return new CourseTypeDto
        {
            Id = courseType.Id,
            InstrumentId = courseType.InstrumentId,
            InstrumentName = courseType.Instrument.Name,
            Name = courseType.Name,
            DurationMinutes = courseType.DurationMinutes,
            Type = courseType.Type,
            MaxStudents = courseType.MaxStudents,
            IsActive = courseType.IsActive,
            ActiveCourseCount = activeCourseCount,
            HasTeachersForCourseType = hasTeachers,
            CurrentPricing = currentPricing != null ? MapPricingVersionToDto(currentPricing) : null,
            PricingHistory = pricingHistory,
            CanEditPricingDirectly = canEditPricingDirectly
        };
    }

    private static CourseTypePricingVersionDto MapPricingVersionToDto(CourseTypePricingVersion pv) => new()
    {
        Id = pv.Id,
        CourseTypeId = pv.CourseTypeId,
        PriceAdult = pv.PriceAdult,
        PriceChild = pv.PriceChild,
        ValidFrom = pv.ValidFrom,
        ValidUntil = pv.ValidUntil,
        IsCurrent = pv.IsCurrent,
        CreatedAt = pv.CreatedAt
    };
}
