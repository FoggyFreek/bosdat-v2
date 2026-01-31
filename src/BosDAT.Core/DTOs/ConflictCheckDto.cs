namespace BosDAT.Core.DTOs;

/// <summary>
/// Result of a schedule conflict check.
/// </summary>
public record ConflictCheckResult
{
    public bool HasConflict { get; init; }
    public IEnumerable<ConflictingCourse> ConflictingCourses { get; init; } = new List<ConflictingCourse>();
}

/// <summary>
/// Information about a course that conflicts with an enrollment attempt.
/// </summary>
public record ConflictingCourse
{
    public Guid CourseId { get; init; }
    public string CourseName { get; init; } = string.Empty;
    public DayOfWeek DayOfWeek { get; init; }
    public TimeOnly StartTime { get; init; }
    public TimeOnly EndTime { get; init; }
    public string Frequency { get; init; } = string.Empty;
    public string? WeekParity { get; init; }
}
