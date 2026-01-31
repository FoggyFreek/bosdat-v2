namespace BosDAT.Core.DTOs;

/// <summary>
/// Request DTO for validating if a student can enroll in a course without conflicts.
/// </summary>
public record ValidateEnrollmentDto
{
    public Guid StudentId { get; init; }
    public Guid CourseId { get; init; }
}

/// <summary>
/// Response DTO for enrollment validation result.
/// </summary>
public record EnrollmentValidationResultDto
{
    public bool IsValid { get; init; }
    public IEnumerable<ConflictingCourseDto> Conflicts { get; init; } = new List<ConflictingCourseDto>();
}

/// <summary>
/// Information about a course that conflicts with an enrollment attempt.
/// </summary>
public record ConflictingCourseDto
{
    public Guid CourseId { get; init; }
    public string CourseName { get; init; } = string.Empty;
    public string DayOfWeek { get; init; } = string.Empty;
    public string TimeSlot { get; init; } = string.Empty; // e.g., "10:00 - 11:30"
    public string Frequency { get; init; } = string.Empty;
    public string? WeekParity { get; init; }

    /// <summary>
    /// Maps from ConflictingCourse domain object to DTO.
    /// </summary>
    public static ConflictingCourseDto FromConflict(ConflictingCourse conflict)
    {
        return new ConflictingCourseDto
        {
            CourseId = conflict.CourseId,
            CourseName = conflict.CourseName,
            DayOfWeek = conflict.DayOfWeek.ToString(),
            TimeSlot = $"{conflict.StartTime:HH:mm} - {conflict.EndTime:HH:mm}",
            Frequency = conflict.Frequency,
            WeekParity = conflict.WeekParity
        };
    }
}
