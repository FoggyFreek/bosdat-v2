using Microsoft.EntityFrameworkCore;
using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Interfaces;
using BosDAT.Core.Interfaces.Services;
using BosDAT.Core.Utilities;

namespace BosDAT.Infrastructure.Services;

public class CalendarService(IUnitOfWork unitOfWork, IAbsenceService absenceService) : ICalendarService
{
    public async Task<List<CalendarLessonDto>> GetLessonsForRangeAsync(
        DateOnly startDate, DateOnly endDate,
        Guid? teacherId, int? roomId,
        CancellationToken ct = default)
    {
        IQueryable<Lesson> query = unitOfWork.Lessons.Query()
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
                CourseId = l.CourseId,
                StudentId = l.StudentId,
                Title = l.Student != null
                    ? l.Course.CourseType.Instrument.Name + " - " + l.Student.FirstName + " " + l.Student.LastName
                    : l.Course.CourseType.Instrument.Name + " - Group",
                Date = l.ScheduledDate,
                StartTime = l.StartTime,
                EndTime = l.EndTime,
                Frequency = l.Course.Frequency,
                IsTrial = l.Course.IsTrial,
                IsWorkshop = l.Course.IsWorkshop,
                StudentName = l.Student != null ? l.Student.FirstName + " " + l.Student.LastName : null,
                TeacherName = l.Teacher.FirstName + " " + l.Teacher.LastName,
                RoomName = l.Room != null ? l.Room.Name : null,
                InstrumentName = l.Course.CourseType.Instrument.Name,
                Status = l.Status
            })
            .ToListAsync(ct);
    }

    public async Task<List<HolidayDto>> GetHolidaysForRangeAsync(
        DateOnly startDate, DateOnly endDate,
        CancellationToken ct = default)
    {
        return await unitOfWork.Repository<Holiday>().Query()
            .Where(h => h.EndDate >= startDate && h.StartDate <= endDate)
            .Select(h => new HolidayDto
            {
                Id = h.Id,
                Name = h.Name,
                StartDate = h.StartDate,
                EndDate = h.EndDate
            })
            .ToListAsync(ct);
    }

    public async Task<List<ConflictDto>> CheckConflictsAsync(
        DateOnly date, TimeOnly startTime, TimeOnly endTime,
        Guid? teacherId, int? roomId,
        CancellationToken ct = default)
    {
        var conflicts = new List<ConflictDto>();

        if (teacherId.HasValue)
            conflicts.AddRange(await FindLessonConflictsAsync(
                l => l.TeacherId == teacherId.Value,
                date, startTime, endTime,
                "Teacher", l => $"Teacher has another lesson from {l.StartTime} to {l.EndTime}", ct));

        if (roomId.HasValue)
            conflicts.AddRange(await FindLessonConflictsAsync(
                l => l.RoomId == roomId.Value,
                date, startTime, endTime,
                "Room", l => $"Room is occupied from {l.StartTime} to {l.EndTime}", ct));

        conflicts.AddRange(await FindHolidayConflictsAsync(date, ct));

        return conflicts;
    }

    private async Task<List<ConflictDto>> FindLessonConflictsAsync(
        System.Linq.Expressions.Expression<Func<Lesson, bool>> filter,
        DateOnly date, TimeOnly startTime, TimeOnly endTime,
        string conflictType, Func<Lesson, string> descriptionFactory,
        CancellationToken ct)
    {
        var lessons = await unitOfWork.Lessons.Query()
            .Where(filter)
            .Where(l => l.ScheduledDate == date && l.Status != LessonStatus.Cancelled)
            .ToListAsync(ct);

        return lessons
            .Where(l => TimesOverlap(startTime, endTime, l.StartTime, l.EndTime))
            .Select(l => new ConflictDto { Type = conflictType, Description = descriptionFactory(l) })
            .ToList();
    }

    private async Task<List<ConflictDto>> FindHolidayConflictsAsync(DateOnly date, CancellationToken ct)
    {
        return await unitOfWork.Repository<Holiday>().Query()
            .Where(h => date >= h.StartDate && date <= h.EndDate)
            .Select(h => new ConflictDto
            {
                Type = "Holiday",
                Description = $"Date falls within holiday: {h.Name}"
            })
            .ToListAsync(ct);
    }

    private static bool TimesOverlap(TimeOnly start1, TimeOnly end1, TimeOnly start2, TimeOnly end2)
    {
        return start1 < end2 && end1 > start2;
    }

    public async Task<WeekCalendarDto?> GetTeacherScheduleAsync(
        Guid teacherId, DateOnly? date,
        CancellationToken ct = default)
    {
        var teacher = await unitOfWork.Teachers.GetByIdAsync(teacherId, ct);
        if (teacher == null)
        {
            return null;
        }

        var targetDate = date ?? DateOnly.FromDateTime(DateTime.Today);
        var weekStart = IsoDateHelper.GetWeekStart(targetDate);
        var weekEnd = weekStart.AddDays(6);

        var lessons = await GetLessonsForRangeAsync(weekStart, weekEnd, teacherId, null, ct);
        var holidays = await GetHolidaysForRangeAsync(weekStart, weekEnd, ct);
        var teacherAbsences = (await absenceService.GetByTeacherAsync(teacherId, ct))
            .Where(a => a.EndDate >= weekStart && a.StartDate <= weekEnd)
            .ToList();

        return new WeekCalendarDto
        {
            WeekStart = weekStart,
            WeekEnd = weekEnd,
            Lessons = lessons,
            Holidays = holidays,
            TeacherAbsences = teacherAbsences
        };
    }

    public async Task<WeekCalendarDto?> GetRoomScheduleAsync(
        int roomId, DateOnly? date,
        CancellationToken ct = default)
    {
        var room = await unitOfWork.Repository<Room>().GetByIdAsync(roomId, ct);
        if (room == null)
        {
            return null;
        }

        var targetDate = date ?? DateOnly.FromDateTime(DateTime.Today);
        var weekStart = IsoDateHelper.GetWeekStart(targetDate);
        var weekEnd = weekStart.AddDays(6);

        var lessons = await GetLessonsForRangeAsync(weekStart, weekEnd, null, roomId, ct);
        var holidays = await GetHolidaysForRangeAsync(weekStart, weekEnd, ct);
        var teacherAbsences = (await absenceService.GetTeacherAbsencesForPeriodAsync(weekStart, weekEnd, ct)).ToList();

        return new WeekCalendarDto
        {
            WeekStart = weekStart,
            WeekEnd = weekEnd,
            Lessons = lessons,
            Holidays = holidays,
            TeacherAbsences = teacherAbsences
        };
    }
}
