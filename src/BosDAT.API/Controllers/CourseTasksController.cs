using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BosDAT.Core.DTOs;
using BosDAT.Core.Interfaces;

namespace BosDAT.API.Controllers;

[ApiController]
[Route("api/courses/{courseId:guid}/tasks")]
[Authorize]
public class CourseTasksController(ICourseTaskService courseTaskService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CourseTaskDto>>> GetByCourse(
        Guid courseId, CancellationToken cancellationToken)
    {
        var tasks = await courseTaskService.GetByCourseAsync(courseId, cancellationToken);
        return Ok(tasks);
    }

    [HttpPost]
    [Authorize(Policy = "TeacherOrAdmin")]
    public async Task<ActionResult<CourseTaskDto>> Create(
        Guid courseId, [FromBody] CreateCourseTaskDto dto, CancellationToken cancellationToken)
    {
        var task = await courseTaskService.CreateAsync(courseId, dto, cancellationToken);
        if (task == null)
        {
            return NotFound();
        }

        return Ok(task);
    }

    [HttpDelete("{taskId:guid}")]
    [Authorize(Policy = "TeacherOrAdmin")]
    public async Task<IActionResult> Delete(
        Guid courseId, Guid taskId, CancellationToken cancellationToken)
    {
        var success = await courseTaskService.DeleteAsync(taskId, cancellationToken);
        if (!success)
        {
            return NotFound();
        }

        return NoContent();
    }
}
