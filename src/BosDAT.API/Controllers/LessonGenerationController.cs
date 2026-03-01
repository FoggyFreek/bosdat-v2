using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BosDAT.Core.DTOs;
using BosDAT.Core.Interfaces;
using BosDAT.Core.Interfaces.Services;

namespace BosDAT.API.Controllers;

[ApiController]
[Route("api/lessons")]
[Authorize]
public class LessonGenerationController(
    ILessonGenerationService lessonGenerationService,
    IUnitOfWork unitOfWork) : ControllerBase
{
    [HttpPost("generate")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<GenerateLessonsResultDto>> GenerateLessons([FromBody] GenerateLessonsDto dto, CancellationToken cancellationToken)
    {
        var courseExists = await unitOfWork.Courses.Query()
            .AnyAsync(c => c.Id == dto.CourseId, cancellationToken);

        if (!courseExists)
        {
            return BadRequest(new { message = "Course not found" });
        }

        var result = await lessonGenerationService.GenerateForCourseAsync(
            dto.CourseId, dto.StartDate, dto.EndDate, dto.SkipHolidays, cancellationToken);

        return Ok(new GenerateLessonsResultDto
        {
            CourseId = dto.CourseId,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            LessonsCreated = result.LessonsCreated,
            LessonsSkipped = result.LessonsSkipped
        });
    }

    [HttpPost("generate-bulk")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<BulkGenerateLessonsResultDto>> GenerateLessonsBulk([FromBody] BulkGenerateLessonsDto dto, CancellationToken cancellationToken)
    {
        var result = await lessonGenerationService.GenerateBulkAsync(
            dto.StartDate, dto.EndDate, dto.SkipHolidays, cancellationToken);

        return Ok(new BulkGenerateLessonsResultDto
        {
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            TotalCoursesProcessed = result.TotalCoursesProcessed,
            TotalLessonsCreated = result.TotalLessonsCreated,
            TotalLessonsSkipped = result.TotalLessonsSkipped,
            CourseResults = result.CourseResults.Select(r => new GenerateLessonsResultDto
            {
                CourseId = r.CourseId,
                StartDate = r.StartDate,
                EndDate = r.EndDate,
                LessonsCreated = r.LessonsCreated,
                LessonsSkipped = r.LessonsSkipped
            }).ToList()
        });
    }
}
