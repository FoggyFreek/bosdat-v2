using BosDAT.Core.Entities;

namespace BosDAT.Core.DTOs;

public record LessonDto
{
    public Guid Id { get; init; }
    public Guid CourseId { get; init; }
    public Guid? StudentId { get; init; }
    public string? StudentName { get; init; }
    public Guid TeacherId { get; init; }
    public string TeacherName { get; init; } = string.Empty;
    public int? RoomId { get; init; }
    public string? RoomName { get; init; }
    public string CourseTypeName { get; init; } = string.Empty;
    public string InstrumentName { get; init; } = string.Empty;
    public DateOnly ScheduledDate { get; init; }
    public TimeOnly StartTime { get; init; }
    public TimeOnly EndTime { get; init; }
    public LessonStatus Status { get; init; }
    public string? CancellationReason { get; init; }
    public bool IsInvoiced { get; init; }
    public bool IsPaidToTeacher { get; init; }
    public string? Notes { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public record CreateLessonDto
{
    public Guid CourseId { get; init; }
    public Guid? StudentId { get; init; }
    public Guid TeacherId { get; init; }
    public int? RoomId { get; init; }
    public DateOnly ScheduledDate { get; init; }
    public TimeOnly StartTime { get; init; }
    public TimeOnly EndTime { get; init; }
    public string? Notes { get; init; }
}

public record UpdateLessonDto
{
    public Guid? StudentId { get; init; }
    public Guid TeacherId { get; init; }
    public int? RoomId { get; init; }
    public DateOnly ScheduledDate { get; init; }
    public TimeOnly StartTime { get; init; }
    public TimeOnly EndTime { get; init; }
    public LessonStatus Status { get; init; }
    public string? CancellationReason { get; init; }
    public string? Notes { get; init; }
}

public record GenerateLessonsDto
{
    public Guid CourseId { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
    public bool SkipHolidays { get; init; } = true;
}

public record CalendarLessonDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public DateOnly Date { get; init; }
    public TimeOnly StartTime { get; init; }
    public TimeOnly EndTime { get; init; }
    public string? StudentName { get; init; }
    public string TeacherName { get; init; } = string.Empty;
    public string? RoomName { get; init; }
    public string InstrumentName { get; init; } = string.Empty;
    public LessonStatus Status { get; init; }
}
