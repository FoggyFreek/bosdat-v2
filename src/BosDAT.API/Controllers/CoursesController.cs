using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Interfaces;

namespace BosDAT.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CoursesController(ICourseService courseService) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<ActionResult<IEnumerable<CourseListDto>>> GetSummary(
        [FromQuery] CourseStatus? status,
        [FromQuery] Guid? teacherId,
        [FromQuery] DayOfWeek? dayOfWeek,
        [FromQuery] int? roomId,
        CancellationToken cancellationToken)
    {
        var courses = await courseService.GetSummaryAsync(status, teacherId, dayOfWeek, roomId, cancellationToken);
        return Ok(courses);
    }

    [HttpGet("count")]
    public async Task<ActionResult<int>> GetCount(
        [FromQuery] CourseStatus? status,
        [FromQuery] Guid? teacherId,
        [FromQuery] DayOfWeek? dayOfWeek,
        [FromQuery] int? roomId,
        CancellationToken cancellationToken)
    {
        var count = await courseService.GetCountAsync(status, teacherId, dayOfWeek, roomId, cancellationToken);
        return Ok(count);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CourseDto>>> GetAll(
        [FromQuery] CourseStatus? status,
        [FromQuery] Guid? teacherId,
        [FromQuery] DayOfWeek? dayOfWeek,
        [FromQuery] int? roomId,
        CancellationToken cancellationToken)
    {
        var courses = await courseService.GetAllAsync(status, teacherId, dayOfWeek, roomId, cancellationToken);
        return Ok(courses);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CourseDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var course = await courseService.GetByIdAsync(id, cancellationToken);
        if (course == null)
            return NotFound();

        return Ok(course);
    }

    [HttpPost]
    [Authorize(Policy = "TeacherOrAdmin")]
    public async Task<ActionResult<CourseDto>> Create([FromBody] CreateCourseDto dto, CancellationToken cancellationToken)
    {
        var (course, error) = await courseService.CreateAsync(dto, cancellationToken);
        if (error != null)
            return BadRequest(new { message = error });

        return CreatedAtAction(nameof(GetById), new { id = course!.Id }, course);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "TeacherOrAdmin")]
    public async Task<ActionResult<CourseDto>> Update(Guid id, [FromBody] UpdateCourseDto dto, CancellationToken cancellationToken)
    {
        var (course, notFound) = await courseService.UpdateAsync(id, dto, cancellationToken);
        if (notFound)
            return NotFound();

        return Ok(course);
    }

    [HttpPost("{id:guid}/enroll")]
    [Authorize(Policy = "TeacherOrAdmin")]
    public async Task<ActionResult<EnrollmentDto>> EnrollStudent(Guid id, [FromBody] CreateEnrollmentDto dto, CancellationToken cancellationToken)
    {
        var (enrollment, notFound, error) = await courseService.EnrollStudentAsync(id, dto, cancellationToken);
        if (notFound)
            return NotFound();
        if (error != null)
            return BadRequest(new { message = error });

        return Ok(enrollment);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var found = await courseService.DeleteAsync(id, cancellationToken);
        if (!found)
            return NotFound();

        return NoContent();
    }
}
