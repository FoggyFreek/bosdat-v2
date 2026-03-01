using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Interfaces.Services;

namespace BosDAT.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LessonsController(ILessonService lessonService) : ControllerBase
{

    [HttpGet]
    public async Task<ActionResult<IEnumerable<LessonDto>>> GetAll(
        [FromQuery] LessonFilterCriteria criteria,
        CancellationToken cancellationToken)
    {
        var lessons = await lessonService.GetAllAsync(criteria, cancellationToken);

        return Ok(lessons);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<LessonDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var lesson = await lessonService.GetByIdAsync(id, cancellationToken);

        if (lesson == null)
        {
            return NotFound();
        }

        return Ok(lesson);
    }

    [HttpGet("student/{studentId:guid}")]
    public async Task<ActionResult<IEnumerable<LessonDto>>> GetByStudent(Guid studentId, CancellationToken cancellationToken)
    {
        var lessons = await lessonService.GetByStudentAsync(studentId, cancellationToken);

        return Ok(lessons);
    }

    [HttpPost]
    [Authorize(Policy = "TeacherOrAdmin")]
    public async Task<ActionResult<LessonDto>> Create([FromBody] CreateLessonDto dto, CancellationToken cancellationToken)
    {
        var (lesson, error) = await lessonService.CreateAsync(dto, cancellationToken);

        if (error != null)
        {
            return BadRequest(new { message = error });
        }

        return CreatedAtAction(nameof(GetById), new { id = lesson!.Id }, lesson);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "TeacherOrAdmin")]
    public async Task<ActionResult<LessonDto>> Update(Guid id, [FromBody] UpdateLessonDto dto, CancellationToken cancellationToken)
    {
        var (lesson, notFound) = await lessonService.UpdateAsync(id, dto, cancellationToken);

        if (notFound)
        {
            return NotFound();
        }

        return Ok(lesson);
    }

    [HttpPut("{id:guid}/status")]
    [Authorize(Policy = "TeacherOrAdmin")]
    public async Task<ActionResult<LessonDto>> UpdateStatus(Guid id, [FromBody] UpdateLessonStatusDto dto, CancellationToken cancellationToken)
    {
        var (lesson, notFound) = await lessonService.UpdateStatusAsync(id, dto.Status, dto.CancellationReason, cancellationToken);

        if (notFound)
        {
            return NotFound();
        }

        return Ok(lesson);
    }

    [HttpPut("group-status")]
    [Authorize(Policy = "TeacherOrAdmin")]
    public async Task<ActionResult<UpdateGroupLessonStatusResultDto>> UpdateGroupStatus(
        [FromBody] UpdateGroupLessonStatusDto dto,
        CancellationToken cancellationToken)
    {
        var (lessonsUpdated, notFound) = await lessonService.UpdateGroupStatusAsync(
            dto.CourseId, dto.ScheduledDate, dto.Status, dto.CancellationReason, cancellationToken);

        if (notFound)
        {
            return NotFound(new { message = "No lessons found for the specified course and date" });
        }

        return Ok(new UpdateGroupLessonStatusResultDto
        {
            CourseId = dto.CourseId,
            ScheduledDate = dto.ScheduledDate,
            Status = dto.Status,
            LessonsUpdated = lessonsUpdated
        });
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var (success, error) = await lessonService.DeleteAsync(id, cancellationToken);

        if (!success)
        {
            if (error == "Lesson not found")
            {
                return NotFound();
            }
            return BadRequest(new { message = error });
        }

        return NoContent();
    }


}

public record UpdateLessonStatusDto
{
    public required LessonStatus Status { get; init; }
    public string? CancellationReason { get; init; }
}

public record GenerateLessonsResultDto
{
    public Guid CourseId { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
    public int LessonsCreated { get; init; }
    public int LessonsSkipped { get; init; }
}

public record BulkGenerateLessonsDto
{
    public required DateOnly StartDate { get; init; }
    public required DateOnly EndDate { get; init; }
    public bool SkipHolidays { get; init; } = true;
}

public record BulkGenerateLessonsResultDto
{
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
    public int TotalCoursesProcessed { get; init; }
    public int TotalLessonsCreated { get; init; }
    public int TotalLessonsSkipped { get; init; }
    public List<GenerateLessonsResultDto> CourseResults { get; init; } = new();
}

public record UpdateGroupLessonStatusDto
{
    public required Guid CourseId { get; init; }
    public required DateOnly ScheduledDate { get; init; }
    public required LessonStatus Status { get; init; }
    public string? CancellationReason { get; init; }
}

public record UpdateGroupLessonStatusResultDto
{
    public Guid CourseId { get; init; }
    public DateOnly ScheduledDate { get; init; }
    public LessonStatus Status { get; init; }
    public int LessonsUpdated { get; init; }
}
