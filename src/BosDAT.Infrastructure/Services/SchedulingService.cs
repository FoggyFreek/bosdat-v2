using Microsoft.EntityFrameworkCore;
using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Enums;
using BosDAT.Core.Interfaces;

namespace BosDAT.Infrastructure.Services;

public class SchedulingService(
    IUnitOfWork unitOfWork,
    ILessonGenerationService lessonGenerationService) : ISchedulingService
{
    public async Task<SchedulingStatusDto> GetSchedulingStatusAsync(CancellationToken ct = default)
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

        return new SchedulingStatusDto
        {
            LastScheduledDate = lastScheduledDate,
            DaysAhead = Math.Max(0, daysAhead),
            ActiveCourseCount = activeCourseCount
        };
    }

    public async Task<ScheduleRunsPageDto> GetScheduleRunsAsync(
        int page, int pageSize,
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

        return new ScheduleRunsPageDto
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<ManualRunResultDto> ExecuteManualRunAsync(
        DateOnly startDate, DateOnly endDate,
        CancellationToken ct = default)
    {
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

        return new ManualRunResultDto
        {
            ScheduleRunId = scheduleRun.Id,
            StartDate = scheduleRun.StartDate,
            EndDate = scheduleRun.EndDate,
            TotalCoursesProcessed = scheduleRun.TotalCoursesProcessed,
            TotalLessonsCreated = scheduleRun.TotalLessonsCreated,
            TotalLessonsSkipped = scheduleRun.TotalLessonsSkipped,
            Status = scheduleRun.Status
        };
    }

    public async Task<ManualRunResultDto> ExecuteSingleCourseRunAsync(
        Guid courseId, DateOnly startDate, DateOnly endDate,
        CancellationToken ct = default)
    {
        ScheduleRun scheduleRun;

        try
        {
            var result = await lessonGenerationService.GenerateForCourseAsync(
                courseId, startDate, endDate, skipHolidays: true, ct);

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

        return new ManualRunResultDto
        {
            ScheduleRunId = scheduleRun.Id,
            StartDate = scheduleRun.StartDate,
            EndDate = scheduleRun.EndDate,
            TotalCoursesProcessed = scheduleRun.TotalCoursesProcessed,
            TotalLessonsCreated = scheduleRun.TotalLessonsCreated,
            TotalLessonsSkipped = scheduleRun.TotalLessonsSkipped,
            Status = scheduleRun.Status
        };
    }
}
