using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BosDAT.Core.DTOs;
using BosDAT.Core.Interfaces.Services;
using BosDAT.Core.Utilities;

namespace BosDAT.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CalendarController(ICalendarService calendarService) : ControllerBase
{
    [HttpGet("week")]
    public async Task<ActionResult<WeekCalendarDto>> GetWeek(
        [FromQuery] DateOnly? date,
        [FromQuery] Guid? teacherId,
        [FromQuery] int? roomId,
        CancellationToken cancellationToken)
    {
        var targetDate = date ?? DateOnly.FromDateTime(DateTime.Today);
        var weekStart = IsoDateHelper.GetWeekStart(targetDate);
        var weekEnd = weekStart.AddDays(6);

        var lessons = await calendarService.GetLessonsForRangeAsync(weekStart, weekEnd, teacherId, roomId, cancellationToken);
        var holidays = await calendarService.GetHolidaysForRangeAsync(weekStart, weekEnd, cancellationToken);

        return Ok(new WeekCalendarDto
        {
            WeekStart = weekStart,
            WeekEnd = weekEnd,
            Lessons = lessons,
            Holidays = holidays
        });
    }

    [HttpGet("day")]
    public async Task<ActionResult<DayCalendarDto>> GetDay(
        [FromQuery] DateOnly? date,
        [FromQuery] Guid? teacherId,
        [FromQuery] int? roomId,
        CancellationToken cancellationToken)
    {
        var targetDate = date ?? DateOnly.FromDateTime(DateTime.Today);

        var lessons = await calendarService.GetLessonsForRangeAsync(targetDate, targetDate, teacherId, roomId, cancellationToken);
        var holidays = await calendarService.GetHolidaysForRangeAsync(targetDate, targetDate, cancellationToken);

        return Ok(new DayCalendarDto
        {
            Date = targetDate,
            DayOfWeek = targetDate.DayOfWeek,
            Lessons = lessons,
            IsHoliday = holidays.Count != 0,
            HolidayName = holidays.FirstOrDefault()?.Name
        });
    }

    [HttpGet("month")]
    public async Task<ActionResult<MonthCalendarDto>> GetMonth(
        [FromQuery] int? year,
        [FromQuery] int? month,
        [FromQuery] Guid? teacherId,
        [FromQuery] int? roomId,
        CancellationToken cancellationToken)
    {
        var targetYear = year ?? DateTime.Today.Year;
        var targetMonth = month ?? DateTime.Today.Month;

        var monthStart = new DateOnly(targetYear, targetMonth, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);

        var lessons = await calendarService.GetLessonsForRangeAsync(monthStart, monthEnd, teacherId, roomId, cancellationToken);
        var holidays = await calendarService.GetHolidaysForRangeAsync(monthStart, monthEnd, cancellationToken);

        var lessonsByDate = lessons
            .GroupBy(l => l.Date)
            .ToDictionary(g => g.Key, g => g.ToList());

        return Ok(new MonthCalendarDto
        {
            Year = targetYear,
            Month = targetMonth,
            MonthStart = monthStart,
            MonthEnd = monthEnd,
            LessonsByDate = lessonsByDate,
            Holidays = holidays,
            TotalLessons = lessons.Count
        });
    }

    [HttpGet("teacher/{teacherId:guid}")]
    public async Task<ActionResult<WeekCalendarDto>> GetTeacherSchedule(
        Guid teacherId,
        [FromQuery] DateOnly? date,
        CancellationToken cancellationToken)
    {
        var schedule = await calendarService.GetTeacherScheduleAsync(teacherId, date, cancellationToken);
        if (schedule == null)
        {
            return NotFound(new { message = "Teacher not found" });
        }

        return Ok(schedule);
    }

    [HttpGet("room/{roomId:int}")]
    public async Task<ActionResult<WeekCalendarDto>> GetRoomSchedule(
        int roomId,
        [FromQuery] DateOnly? date,
        CancellationToken cancellationToken)
    {
        var schedule = await calendarService.GetRoomScheduleAsync(roomId, date, cancellationToken);
        if (schedule == null)
        {
            return NotFound(new { message = "Room not found" });
        }

        return Ok(schedule);
    }

    [HttpGet("availability")]
    public async Task<ActionResult<AvailabilityDto>> CheckAvailability(
        [FromQuery] DateOnly date,
        [FromQuery] TimeOnly startTime,
        [FromQuery] TimeOnly endTime,
        [FromQuery] Guid? teacherId,
        [FromQuery] int? roomId,
        CancellationToken cancellationToken)
    {
        var conflicts = await calendarService.CheckConflictsAsync(date, startTime, endTime, teacherId, roomId, cancellationToken);

        return Ok(new AvailabilityDto
        {
            Date = date,
            StartTime = startTime,
            EndTime = endTime,
            IsAvailable = conflicts.Count == 0,
            Conflicts = conflicts
        });
    }
}
