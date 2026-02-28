using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BosDAT.Core.DTOs;
using BosDAT.Core.Interfaces.Services;

namespace BosDAT.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TeachersController(ITeacherService teacherService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TeacherListDto>>> GetAll(
        [FromQuery] bool? activeOnly,
        [FromQuery] int? instrumentId,
        [FromQuery] Guid? CourseTypeId,
        CancellationToken cancellationToken)
    {
        var teachers = await teacherService.GetAllAsync(activeOnly, instrumentId, CourseTypeId, cancellationToken);
        return Ok(teachers);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TeacherDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var teacher = await teacherService.GetByIdAsync(id, cancellationToken);

        if (teacher == null)
        {
            return NotFound();
        }

        return Ok(teacher);
    }

    [HttpGet("{id:guid}/courses")]
    public async Task<ActionResult> GetWithCourses(Guid id, CancellationToken cancellationToken)
    {
        var result = await teacherService.GetWithCoursesAsync(id, cancellationToken);

        if (result == null)
        {
            return NotFound();
        }

        return Ok(new { Teacher = result.Value.Teacher, Courses = result.Value.Courses });
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<TeacherDto>> Create([FromBody] CreateTeacherDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var teacher = await teacherService.CreateAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = teacher.Id }, teacher);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<TeacherDto>> Update(Guid id, [FromBody] UpdateTeacherDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var teacher = await teacherService.UpdateAsync(id, dto, cancellationToken);

            if (teacher == null)
            {
                return NotFound();
            }

            return Ok(teacher);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var success = await teacherService.DeleteAsync(id, cancellationToken);

        if (!success)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpGet("{id:guid}/availability")]
    public async Task<ActionResult<IEnumerable<TeacherAvailabilityDto>>> GetAvailability(
        Guid id,
        CancellationToken cancellationToken)
    {
        var availability = await teacherService.GetAvailabilityAsync(id, cancellationToken);

        if (availability == null)
        {
            return NotFound();
        }

        return Ok(availability);
    }

    [HttpPut("{id:guid}/availability")]
    [Authorize(Policy = "TeacherOrAdmin")]
    public async Task<ActionResult<IEnumerable<TeacherAvailabilityDto>>> UpdateAvailability(
        Guid id,
        [FromBody] List<UpdateTeacherAvailabilityDto> dtos,
        CancellationToken cancellationToken)
    {
        try
        {
            var availability = await teacherService.UpdateAvailabilityAsync(id, dtos, cancellationToken);

            if (availability == null)
            {
                return NotFound();
            }

            return Ok(availability);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id:guid}/available-lesson-types")]
    public async Task<ActionResult<IEnumerable<CourseTypeSimpleDto>>> GetAvailableCourseTypes(
        Guid id,
        [FromQuery] string? instrumentIds,
        CancellationToken cancellationToken)
    {
        var courseTypes = await teacherService.GetAvailableCourseTypesAsync(id, instrumentIds, cancellationToken);

        if (courseTypes == null)
        {
            return NotFound();
        }

        return Ok(courseTypes);
    }
}
