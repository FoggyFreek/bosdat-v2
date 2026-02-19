using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Interfaces;

namespace BosDAT.API.Controllers;

[ApiController]
[Route("api/course-types")]
[Authorize]
public class CourseTypesController(
    ICourseTypeService courseTypeService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CourseTypeDto>>> GetAll(
        [FromQuery] bool? activeOnly,
        [FromQuery] int? instrumentId,
        CancellationToken cancellationToken)
    {
        var result = await courseTypeService.GetAllAsync(activeOnly, instrumentId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CourseTypeDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var courseType = await courseTypeService.GetByIdAsync(id, cancellationToken);

        if (courseType == null)
        {
            return NotFound();
        }

        return Ok(courseType);
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<CourseTypeDto>> Create([FromBody] CreateCourseTypeDto dto, CancellationToken cancellationToken)
    {
        var (courseType, error) = await courseTypeService.CreateAsync(dto, cancellationToken);

        if (error != null)
        {
            return BadRequest(new { message = error });
        }

        return CreatedAtAction(nameof(GetById), new { id = courseType!.Id }, courseType);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<CourseTypeDto>> Update(Guid id, [FromBody] UpdateCourseTypeDto dto, CancellationToken cancellationToken)
    {
        var (courseType, error, notFound) = await courseTypeService.UpdateAsync(id, dto, cancellationToken);

        if (notFound)
        {
            return NotFound();
        }

        if (error != null)
        {
            return BadRequest(new { message = error });
        }

        return Ok(courseType);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var (_, error, notFound) = await courseTypeService.DeleteAsync(id, cancellationToken);

        if (notFound)
        {
            return NotFound();
        }

        if (error != null)
        {
            return BadRequest(new { message = error });
        }

        return NoContent();
    }

    [HttpPut("{id:guid}/reactivate")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<CourseTypeDto>> Reactivate(Guid id, CancellationToken cancellationToken)
    {
        var (courseType, notFound) = await courseTypeService.ReactivateAsync(id, cancellationToken);

        if (notFound)
        {
            return NotFound();
        }

        return Ok(courseType);
    }

    [HttpGet("{id:guid}/pricing/history")]
    public async Task<ActionResult<IEnumerable<CourseTypePricingVersionDto>>> GetPricingHistory(Guid id, CancellationToken cancellationToken)
    {
        var (history, notFound) = await courseTypeService.GetPricingHistoryAsync(id, cancellationToken);

        if (notFound)
        {
            return NotFound();
        }

        return Ok(history);
    }

    [HttpGet("{id:guid}/pricing/can-edit")]
    public async Task<ActionResult<PricingEditabilityDto>> CheckPricingEditability(Guid id, CancellationToken cancellationToken)
    {
        var (result, notFound) = await courseTypeService.CheckPricingEditabilityAsync(id, cancellationToken);

        if (notFound)
        {
            return NotFound();
        }

        return Ok(result);
    }

    [HttpPut("{id:guid}/pricing")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<CourseTypePricingVersionDto>> UpdatePricing(
        Guid id,
        [FromBody] UpdateCourseTypePricingDto dto,
        CancellationToken cancellationToken)
    {
        var (pricing, error, notFound) = await courseTypeService.UpdatePricingAsync(id, dto, cancellationToken);

        if (notFound)
        {
            return NotFound();
        }

        if (error != null)
        {
            return BadRequest(new { message = error });
        }

        return Ok(pricing);
    }

    [HttpPost("{id:guid}/pricing/versions")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<CourseTypePricingVersionDto>> CreatePricingVersion(
        Guid id,
        [FromBody] CreateCourseTypePricingVersionDto dto,
        CancellationToken cancellationToken)
    {
        var (pricing, error, notFound) = await courseTypeService.CreatePricingVersionAsync(id, dto, cancellationToken);

        if (notFound)
        {
            return NotFound();
        }

        if (error != null)
        {
            return BadRequest(new { message = error });
        }

        return CreatedAtAction(nameof(GetPricingHistory), new { id }, pricing);
    }

    [HttpGet("teachers-for-instrument/{instrumentId:int}")]
    public async Task<ActionResult<int>> GetTeachersCountForInstrument(int instrumentId, CancellationToken cancellationToken)
    {
        var (count, notFound) = await courseTypeService.GetTeachersCountForInstrumentAsync(instrumentId, cancellationToken);

        if (notFound)
        {
            return NotFound(new { message = "Instrument not found" });
        }

        return Ok(count);
    }
}
