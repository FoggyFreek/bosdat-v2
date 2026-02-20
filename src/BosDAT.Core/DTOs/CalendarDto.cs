namespace BosDAT.Core.DTOs;

public record WeekCalendarDto
{
    public DateOnly WeekStart { get; init; }
    public DateOnly WeekEnd { get; init; }
    public List<CalendarLessonDto> Lessons { get; init; } = new();
    public List<HolidayDto> Holidays { get; init; } = new();
    public List<AbsenceDto> TeacherAbsences { get; init; } = new();
}

public record DayCalendarDto
{
    public DateOnly Date { get; init; }
    public DayOfWeek DayOfWeek { get; init; }
    public List<CalendarLessonDto> Lessons { get; init; } = new();
    public bool IsHoliday { get; init; }
    public string? HolidayName { get; init; }
}

public record MonthCalendarDto
{
    public int Year { get; init; }
    public int Month { get; init; }
    public DateOnly MonthStart { get; init; }
    public DateOnly MonthEnd { get; init; }
    public Dictionary<DateOnly, List<CalendarLessonDto>> LessonsByDate { get; init; } = new();
    public List<HolidayDto> Holidays { get; init; } = new();
    public int TotalLessons { get; init; }
}

public record AvailabilityDto
{
    public DateOnly Date { get; init; }
    public TimeOnly StartTime { get; init; }
    public TimeOnly EndTime { get; init; }
    public bool IsAvailable { get; init; }
    public List<ConflictDto> Conflicts { get; init; } = new();
}

public record ConflictDto
{
    public string Type { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
}
