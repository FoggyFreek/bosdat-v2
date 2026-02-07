using BosDAT.Core.Enums;

namespace BosDAT.Core.Entities;

public class ScheduleRun : BaseEntity
{
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public int TotalCoursesProcessed { get; set; }
    public int TotalLessonsCreated { get; set; }
    public int TotalLessonsSkipped { get; set; }
    public bool SkipHolidays { get; set; }
    public ScheduleRunStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
    public required string InitiatedBy { get; set; }
}
