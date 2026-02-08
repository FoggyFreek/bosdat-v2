using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BosDAT.Core.Entities;
using BosDAT.Core.Enums;
using BosDAT.Core.Interfaces;
using Microsoft.IdentityModel.Tokens;

namespace BosDAT.API.Controllers;

[ApiController]
[Route("api/admin/scheduling")]
[Authorize(Policy = "AdminOnly")]
public class SchedulingController(
    IUnitOfWork unitOfWork,
    ILessonGenerationService lessonGenerationService) : ControllerBase
{
    [HttpGet("status")]
    public async Task<ActionResult<SchedulingStatusDto>> GetStatus(CancellationToken ct)
    {
        var lastScheduledDate = await unitOfWork.Lessons.Query()
            .AsNoTracking()
            .OrderByDescending(l => l.ScheduledDate)
            .Select(l => (DateOnly?)l.ScheduledDate)
            .FirstOrDefaultAsync(ct);

        var activeCourseCount = await unitOfWork.Courses.Query()
            .AsNoTracking()
            .CountAsync(c => c.Status == CourseStatus.Active, ct);

        var daysAhead = lastScheduledDate.HasValue
            ? (lastScheduledDate.Value.ToDateTime(TimeOnly.MinValue) - DateTime.UtcNow).Days
            : 0;

        return Ok(new SchedulingStatusDto
        {
            LastScheduledDate = lastScheduledDate,
            DaysAhead = Math.Max(0, daysAhead),
            ActiveCourseCount = activeCourseCount
        });
    }

    [HttpGet("runs")]
    public async Task<ActionResult<ScheduleRunsPageDto>> GetRuns(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 5,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 50) pageSize = 5;

        var totalCount = await unitOfWork.Repository<ScheduleRun>().Query()
            .AsNoTracking()
            .CountAsync(ct);

        var items = await unitOfWork.Repository<ScheduleRun>().Query()
            .AsNoTracking()
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new ScheduleRunDto
            {
                Id = r.Id,
                StartDate = r.StartDate,
                EndDate = r.EndDate,
                TotalCoursesProcessed = r.TotalCoursesProcessed,
                TotalLessonsCreated = r.TotalLessonsCreated,
                TotalLessonsSkipped = r.TotalLessonsSkipped,
                SkipHolidays = r.SkipHolidays,
                Status = r.Status,
                ErrorMessage = r.ErrorMessage,
                InitiatedBy = r.InitiatedBy,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync(ct);

        return Ok(new ScheduleRunsPageDto
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        });
    }

    [HttpPost("run/{id}")]
    public async Task<ActionResult<ManualRunResultDto>> RunSingle(Guid id, CancellationToken ct)
    {
        //FUTURE: get nr day ahead from settings
        var startDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var endDate = startDate.AddDays(90);

        ScheduleRun scheduleRun;

        try
        {
            var result = await lessonGenerationService.GenerateForCourseAsync(
                id, startDate, endDate, skipHolidays: true, ct);

            scheduleRun = new ScheduleRun
            {
                Id = Guid.NewGuid(),
                StartDate = startDate,
                EndDate = endDate,
                TotalCoursesProcessed = 1,
                TotalLessonsCreated = result.LessonsCreated,
                TotalLessonsSkipped = result.LessonsSkipped,
                SkipHolidays = true,
                Status = ScheduleRunStatus.Success,
                InitiatedBy = "RunManualSingle"
            };
        }
        catch (Exception ex)
        {
            scheduleRun = new ScheduleRun
            {
                Id = Guid.NewGuid(),
                StartDate = startDate,
                EndDate = endDate,
                TotalCoursesProcessed = 1,
                TotalLessonsCreated = 0,
                TotalLessonsSkipped = 0,
                SkipHolidays = true,
                Status = ScheduleRunStatus.Failed,
                ErrorMessage = ex.Message,
                InitiatedBy = "ManualSingle"
            };
        }

        await unitOfWork.Repository<ScheduleRun>().AddAsync(scheduleRun, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Ok(new ManualRunResultDto
        {
            ScheduleRunId = scheduleRun.Id,
            StartDate = scheduleRun.StartDate,
            EndDate = scheduleRun.EndDate,
            TotalCoursesProcessed = scheduleRun.TotalCoursesProcessed,
            TotalLessonsCreated = scheduleRun.TotalLessonsCreated,
            TotalLessonsSkipped = scheduleRun.TotalLessonsSkipped,
            Status = scheduleRun.Status
        });
        
    }

    [HttpPost("run")]
    public async Task<ActionResult<ManualRunResultDto>> RunManual(CancellationToken ct)
    {
        var startDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var endDate = startDate.AddDays(90);

        ScheduleRun scheduleRun;

        try
        {
            var result = await lessonGenerationService.GenerateBulkAsync(
                startDate, endDate, skipHolidays: true, ct);

            scheduleRun = new ScheduleRun
            {
                Id = Guid.NewGuid(),
                StartDate = startDate,
                EndDate = endDate,
                TotalCoursesProcessed = result.TotalCoursesProcessed,
                TotalLessonsCreated = result.TotalLessonsCreated,
                TotalLessonsSkipped = result.TotalLessonsSkipped,
                SkipHolidays = true,
                Status = ScheduleRunStatus.Success,
                InitiatedBy = "Manual"
            };
        }
        catch (Exception ex)
        {
            scheduleRun = new ScheduleRun
            {
                Id = Guid.NewGuid(),
                StartDate = startDate,
                EndDate = endDate,
                TotalCoursesProcessed = 0,
                TotalLessonsCreated = 0,
                TotalLessonsSkipped = 0,
                SkipHolidays = true,
                Status = ScheduleRunStatus.Failed,
                ErrorMessage = ex.Message,
                InitiatedBy = "Manual"
            };
        }

        await unitOfWork.Repository<ScheduleRun>().AddAsync(scheduleRun, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Ok(new ManualRunResultDto
        {
            ScheduleRunId = scheduleRun.Id,
            StartDate = scheduleRun.StartDate,
            EndDate = scheduleRun.EndDate,
            TotalCoursesProcessed = scheduleRun.TotalCoursesProcessed,
            TotalLessonsCreated = scheduleRun.TotalLessonsCreated,
            TotalLessonsSkipped = scheduleRun.TotalLessonsSkipped,
            Status = scheduleRun.Status
        });
    }

    
}

public record SchedulingStatusDto
{
    public DateOnly? LastScheduledDate { get; init; }
    public int DaysAhead { get; init; }
    public int ActiveCourseCount { get; init; }
}

public record ScheduleRunDto
{
    public Guid Id { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
    public int TotalCoursesProcessed { get; init; }
    public int TotalLessonsCreated { get; init; }
    public int TotalLessonsSkipped { get; init; }
    public bool SkipHolidays { get; init; }
    public ScheduleRunStatus Status { get; init; }
    public string? ErrorMessage { get; init; }
    public required string InitiatedBy { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record ScheduleRunsPageDto
{
    public List<ScheduleRunDto> Items { get; init; } = new();
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
}

public record ManualRunResultDto
{
    public Guid ScheduleRunId { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
    public int TotalCoursesProcessed { get; init; }
    public int TotalLessonsCreated { get; init; }
    public int TotalLessonsSkipped { get; init; }
    public ScheduleRunStatus Status { get; init; }
}
