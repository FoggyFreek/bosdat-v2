using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Interfaces;

namespace BosDAT.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CalendarController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public CalendarController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet("week")]
    public async Task<ActionResult<WeekCalendarDto>> GetWeek(
        [FromQuery] DateOnly? date,
        [FromQuery] Guid? teacherId,
        [FromQuery] int? roomId,
        CancellationToken cancellationToken)
    {
        var targetDate = date ?? DateOnly.FromDateTime(DateTime.Today);

        // Find the start of the week (Monday)
        var daysFromMonday = ((int)targetDate.DayOfWeek - 1 + 7) % 7;
        var weekStart = targetDate.AddDays(-daysFromMonday);
        var weekEnd = weekStart.AddDays(6);

        var lessons = await GetLessonsForRange(weekStart, weekEnd, teacherId, roomId, cancellationToken);
        var holidays = await GetHolidaysForRange(weekStart, weekEnd, cancellationToken);

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

        var lessons = await GetLessonsForRange(targetDate, targetDate, teacherId, roomId, cancellationToken);
        var holidays = await GetHolidaysForRange(targetDate, targetDate, cancellationToken);

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

        var lessons = await GetLessonsForRange(monthStart, monthEnd, teacherId, roomId, cancellationToken);
        var holidays = await GetHolidaysForRange(monthStart, monthEnd, cancellationToken);

        // Group lessons by date for easier calendar rendering
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
        var teacher = await _unitOfWork.Teachers.GetByIdAsync(teacherId, cancellationToken);
        if (teacher == null)
        {
            return NotFound(new { message = "Teacher not found" });
        }

        var targetDate = date ?? DateOnly.FromDateTime(DateTime.Today);
        var daysFromMonday = ((int)targetDate.DayOfWeek - 1 + 7) % 7;
        var weekStart = targetDate.AddDays(-daysFromMonday);
        var weekEnd = weekStart.AddDays(6);

        var lessons = await GetLessonsForRange(weekStart, weekEnd, teacherId, null, cancellationToken);
        var holidays = await GetHolidaysForRange(weekStart, weekEnd, cancellationToken);

        return Ok(new WeekCalendarDto
        {
            WeekStart = weekStart,
            WeekEnd = weekEnd,
            Lessons = lessons,
            Holidays = holidays
        });
    }

    [HttpGet("room/{roomId:int}")]
    public async Task<ActionResult<WeekCalendarDto>> GetRoomSchedule(
        int roomId,
        [FromQuery] DateOnly? date,
        CancellationToken cancellationToken)
    {
        var room = await _unitOfWork.Repository<Room>().GetByIdAsync(roomId, cancellationToken);
        if (room == null)
        {
            return NotFound(new { message = "Room not found" });
        }

        var targetDate = date ?? DateOnly.FromDateTime(DateTime.Today);
        var daysFromMonday = ((int)targetDate.DayOfWeek - 1 + 7) % 7;
        var weekStart = targetDate.AddDays(-daysFromMonday);
        var weekEnd = weekStart.AddDays(6);

        var lessons = await GetLessonsForRange(weekStart, weekEnd, null, roomId, cancellationToken);
        var holidays = await GetHolidaysForRange(weekStart, weekEnd, cancellationToken);

        return Ok(new WeekCalendarDto
        {
            WeekStart = weekStart,
            WeekEnd = weekEnd,
            Lessons = lessons,
            Holidays = holidays
        });
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
        var conflicts = new List<ConflictDto>();

        if (teacherId.HasValue) conflicts.AddRange(await GetConflictsForTeacher(date, startTime, endTime, teacherId.Value, cancellationToken));
        if (roomId.HasValue) conflicts.AddRange(await GetConflictsForRoom(date, startTime, endTime, roomId, cancellationToken));
        conflicts.AddRange(await GetConflictsForHoliday(date, cancellationToken));

        return Ok(new AvailabilityDto
        {
            Date = date,
            StartTime = startTime,
            EndTime = endTime,
            IsAvailable = conflicts.Count == 0,
            Conflicts = conflicts
        });
    }

    private async Task<List<ConflictDto>> GetConflictsForHoliday(
        DateOnly date,
        CancellationToken cancellationToken)
    {
        var conflicts = new List<ConflictDto>();

        // Check for holidays
        var holidays = await _unitOfWork.Repository<Holiday>().Query()
            .Where(h => date >= h.StartDate && date <= h.EndDate)
            .ToListAsync(cancellationToken);

        foreach (var holiday in holidays)
        {
            conflicts.Add(new ConflictDto
            {
                Type = "Holiday",
                Description = $"Date falls within holiday: {holiday.Name}"
            });
        }

        return conflicts;
    }

    private async Task<List<ConflictDto>> GetConflictsForRoom(
        DateOnly date,
        TimeOnly startTime,
        TimeOnly endTime,
        int? roomId,
        CancellationToken cancellationToken)
    {
        var conflicts = new List<ConflictDto>();

        if (roomId.HasValue)
        {
            var roomLessons = await _unitOfWork.Lessons.Query()
                    .Where(l => l.RoomId == roomId.Value &&
                               l.ScheduledDate == date &&
                               l.Status != LessonStatus.Cancelled)
                    .ToListAsync(cancellationToken);

            foreach (var lesson in roomLessons)
            {
                if (TimesOverlap(startTime, endTime, lesson.StartTime, lesson.EndTime))
                {
                    conflicts.Add(new ConflictDto
                    {
                        Type = "Room",
                        Description = $"Room is occupied from {lesson.StartTime} to {lesson.EndTime}"
                    });
                }
            }
        }
        return conflicts;
    }

    private async Task<List<ConflictDto>> GetConflictsForTeacher(
        DateOnly date,
        TimeOnly startTime,
        TimeOnly endTime,
        Guid? teacherId,
        CancellationToken cancellationToken)
    {
        var conflicts = new List<ConflictDto>();

        if (teacherId.HasValue)
        {
            var teacherLessons = await _unitOfWork.Lessons.Query()
                .Where(l => l.TeacherId == teacherId.Value &&
                           l.ScheduledDate == date &&
                           l.Status != LessonStatus.Cancelled)
                .ToListAsync(cancellationToken);

            foreach (var lesson in teacherLessons)
            {
                if (TimesOverlap(startTime, endTime, lesson.StartTime, lesson.EndTime))
                {
                    conflicts.Add(new ConflictDto
                    {
                        Type = "Teacher",
                        Description = $"Teacher has another lesson from {lesson.StartTime} to {lesson.EndTime}"
                    });
                }
            }
        }

        return conflicts;
    }

    private async Task<List<CalendarLessonDto>> GetLessonsForRange(
        DateOnly startDate,
        DateOnly endDate,
        Guid? teacherId,
        int? roomId,
        CancellationToken cancellationToken)
    {
        IQueryable<Lesson> query = _unitOfWork.Lessons.Query()
            .Where(l => l.ScheduledDate >= startDate && l.ScheduledDate <= endDate)
            .Include(l => l.Course)
                .ThenInclude(c => c.CourseType)
                    .ThenInclude(lt => lt.Instrument)
            .Include(l => l.Student)
            .Include(l => l.Teacher)
            .Include(l => l.Room);

        if (teacherId.HasValue)
        {
            query = query.Where(l => l.TeacherId == teacherId.Value);
        }

        if (roomId.HasValue)
        {
            query = query.Where(l => l.RoomId == roomId.Value);
        }

        return await query
            .OrderBy(l => l.ScheduledDate)
            .ThenBy(l => l.StartTime)
            .Select(l => new CalendarLessonDto
            {
                Id = l.Id,
                Title = l.Course.CourseType.Instrument.Name + " - " + (l.Student != null ? l.Student.FirstName + " " + l.Student.LastName : "Group"),
                Date = l.ScheduledDate,
                StartTime = l.StartTime,
                EndTime = l.EndTime,
                StudentName = l.Student != null ? l.Student.FirstName + " " + l.Student.LastName : null,
                TeacherName = l.Teacher.FirstName + " " + l.Teacher.LastName,
                RoomName = l.Room != null ? l.Room.Name : null,
                InstrumentName = l.Course.CourseType.Instrument.Name,
                Status = l.Status
            })
            .ToListAsync(cancellationToken);
    }

    private async Task<List<HolidayDto>> GetHolidaysForRange(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken)
    {
        return await _unitOfWork.Repository<Holiday>().Query()
            .Where(h => h.EndDate >= startDate && h.StartDate <= endDate)
            .Select(h => new HolidayDto
            {
                Id = h.Id,
                Name = h.Name,
                StartDate = h.StartDate,
                EndDate = h.EndDate
            })
            .ToListAsync(cancellationToken);
    }

    private static bool TimesOverlap(TimeOnly start1, TimeOnly end1, TimeOnly start2, TimeOnly end2)
    {
        return start1 < end2 && end1 > start2;
    }
}

public record WeekCalendarDto
{
    public DateOnly WeekStart { get; init; }
    public DateOnly WeekEnd { get; init; }
    public List<CalendarLessonDto> Lessons { get; init; } = new();
    public List<HolidayDto> Holidays { get; init; } = new();
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

// HolidayDto is defined in HolidaysController

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
