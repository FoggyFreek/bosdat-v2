using Microsoft.EntityFrameworkCore;
using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Interfaces;

namespace BosDAT.Infrastructure.Services;

public class CalendarService(IUnitOfWork unitOfWork) : ICalendarService
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
        {
            var teacherLessons = await unitOfWork.Lessons.Query()
                .Where(l => l.TeacherId == teacherId.Value &&
                           l.ScheduledDate == date &&
                           l.Status != LessonStatus.Cancelled)
                .ToListAsync(ct);

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

        if (roomId.HasValue)
        {
            var roomLessons = await unitOfWork.Lessons.Query()
                .Where(l => l.RoomId == roomId.Value &&
                           l.ScheduledDate == date &&
                           l.Status != LessonStatus.Cancelled)
                .ToListAsync(ct);

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

        var holidays = await unitOfWork.Repository<Holiday>().Query()
            .Where(h => date >= h.StartDate && date <= h.EndDate)
            .ToListAsync(ct);

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

    private static bool TimesOverlap(TimeOnly start1, TimeOnly end1, TimeOnly start2, TimeOnly end2)
    {
        return start1 < end2 && end1 > start2;
    }
}
