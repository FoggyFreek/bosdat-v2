using BosDAT.Core.Enums;

namespace BosDAT.Core.DTOs;

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
