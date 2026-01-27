using BosDAT.Core.Entities;

namespace BosDAT.Core.DTOs;

public record CourseDto
{
    public Guid Id { get; init; }
    public Guid TeacherId { get; init; }
    public string TeacherName { get; init; } = string.Empty;
    public Guid CourseTypeId { get; init; }
    public string CourseTypeName { get; init; } = string.Empty;
    public string InstrumentName { get; init; } = string.Empty;
    public int? RoomId { get; init; }
    public string? RoomName { get; init; }
    public DayOfWeek DayOfWeek { get; init; }
    public TimeOnly StartTime { get; init; }
    public TimeOnly EndTime { get; init; }
    public CourseFrequency Frequency { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public CourseStatus Status { get; init; }
    public bool IsWorkshop { get; init; }
    public bool IsTrial { get; init; }
    public string? Notes { get; init; }
    public int EnrollmentCount { get; init; }
    public List<EnrollmentDto> Enrollments { get; init; } = new();
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public record CreateCourseDto
{
    public Guid TeacherId { get; init; }
    public Guid CourseTypeId { get; init; }
    public int? RoomId { get; init; }
    public DayOfWeek DayOfWeek { get; init; }
    public TimeOnly StartTime { get; init; }
    public TimeOnly EndTime { get; init; }
    public CourseFrequency Frequency { get; init; } = CourseFrequency.Weekly;
    public DateOnly StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public bool IsWorkshop { get; init; }
    public bool IsTrial { get; init; }
    public string? Notes { get; init; }
}

public record UpdateCourseDto
{
    public Guid TeacherId { get; init; }
    public Guid CourseTypeId { get; init; }
    public int? RoomId { get; init; }
    public DayOfWeek DayOfWeek { get; init; }
    public TimeOnly StartTime { get; init; }
    public TimeOnly EndTime { get; init; }
    public CourseFrequency Frequency { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public CourseStatus Status { get; init; }
    public bool IsWorkshop { get; init; }
    public bool IsTrial { get; init; }
    public string? Notes { get; init; }
}

public record CourseListDto
{
    public Guid Id { get; init; }
    public string TeacherName { get; init; } = string.Empty;
    public string CourseTypeName { get; init; } = string.Empty;
    public string InstrumentName { get; init; } = string.Empty;
    public string? RoomName { get; init; }
    public DayOfWeek DayOfWeek { get; init; }
    public TimeOnly StartTime { get; init; }
    public TimeOnly EndTime { get; init; }
    public CourseStatus Status { get; init; }
    public int EnrollmentCount { get; init; }
}
