using BosDAT.Core.DTOs;

namespace BosDAT.Core.Interfaces;

public interface ICalendarService
{
    Task<List<CalendarLessonDto>> GetLessonsForRangeAsync(
        DateOnly startDate, DateOnly endDate,
        Guid? teacherId, int? roomId,
        CancellationToken ct = default);

    Task<List<HolidayDto>> GetHolidaysForRangeAsync(
        DateOnly startDate, DateOnly endDate,
        CancellationToken ct = default);

    Task<List<ConflictDto>> CheckConflictsAsync(
        DateOnly date, TimeOnly startTime, TimeOnly endTime,
        Guid? teacherId, int? roomId,
        CancellationToken ct = default);
}
