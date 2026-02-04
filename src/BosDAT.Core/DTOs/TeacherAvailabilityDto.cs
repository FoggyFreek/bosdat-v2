namespace BosDAT.Core.DTOs;

public record TeacherAvailabilityDto
{
    public Guid Id { get; init; }
    public DayOfWeek DayOfWeek { get; init; }
    public TimeOnly FromTime { get; init; }
    public TimeOnly UntilTime { get; init; }
}

public record UpdateTeacherAvailabilityDto
{
    public DayOfWeek DayOfWeek { get; init; }
    public TimeOnly FromTime { get; init; }
    public TimeOnly UntilTime { get; init; }
}
