using Microsoft.EntityFrameworkCore;
using BosDAT.Core.Entities;
using BosDAT.Core.Enums;
using BosDAT.Core.Interfaces;
using BosDAT.Core.Utilities;

namespace BosDAT.Infrastructure.Services;

public class LessonGenerationService(IUnitOfWork unitOfWork) : ILessonGenerationService
{
    public async Task<LessonGenerationResult> GenerateForCourseAsync(
        Guid courseId,
        DateOnly startDate,
        DateOnly endDate,
        bool skipHolidays,
        CancellationToken ct = default)
    {
        var course = await unitOfWork.Courses.Query()
            .Where(c => c.Id == courseId)
            .Include(c => c.CourseType)
            .Include(c => c.Enrollments.Where(e => e.Status == EnrollmentStatus.Active))
                .ThenInclude(e => e.Student)
            .FirstOrDefaultAsync(ct);

        if (course == null)
            return new LessonGenerationResult(courseId, startDate, endDate, 0, 0);

        var (created, skipped) = await GenerateLessonsForCourseAsync(course, startDate, endDate, skipHolidays, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return new LessonGenerationResult(courseId, startDate, endDate, created, skipped);
    }

    public async Task<BulkLessonGenerationResult> GenerateBulkAsync(
        DateOnly startDate,
        DateOnly endDate,
        bool skipHolidays,
        CancellationToken ct = default)
    {
        var activeCourses = await unitOfWork.Courses.Query()
            .Where(c => c.Status == CourseStatus.Active)
            .Include(c => c.Enrollments.Where(e => e.Status == EnrollmentStatus.Active))
            .Include(c => c.CourseType)
            .ToListAsync(ct);

        var totalCreated = 0;
        var totalSkipped = 0;
        var courseResults = new List<LessonGenerationResult>();

        foreach (var course in activeCourses)
        {
            var (lessonsCreated, lessonsSkipped) = await GenerateLessonsForCourseAsync(
                course, startDate, endDate, skipHolidays, ct);

            totalCreated += lessonsCreated;
            totalSkipped += lessonsSkipped;

            if (lessonsCreated > 0)
            {
                courseResults.Add(new LessonGenerationResult(
                    course.Id, startDate, endDate, lessonsCreated, lessonsSkipped));
            }
        }

        await unitOfWork.SaveChangesAsync(ct);

        return new BulkLessonGenerationResult(
            startDate, endDate, activeCourses.Count, totalCreated, totalSkipped, courseResults);
    }

    internal async Task<(int Created, int Skipped)> GenerateLessonsForCourseAsync(
        Course course,
        DateOnly startDate,
        DateOnly endDate,
        bool skipHolidays,
        CancellationToken ct)
    {
        var holidays = await GetHolidaysAsync(startDate, endDate, skipHolidays, ct);
        var existingLessons = await GetExistingLessonsAsync(course.Id, startDate, endDate, ct);
        var teacherAbsences = await GetTeacherAbsencesAsync(course.TeacherId, startDate, endDate, ct);
        var studentAbsences = await GetStudentAbsencesAsync(startDate, endDate, ct);

        var lessonsCreated = 0;
        var lessonsSkipped = 0;
        var currentDate = FindFirstOccurrenceDate(startDate, course, endDate);

        while (currentDate <= (course.EndDate != null ? course.EndDate : endDate))
        {
            if (IsHoliday(currentDate, holidays))
            {
                lessonsSkipped++;
            }
            else if (IsAbsent(currentDate, teacherAbsences))
            {
                lessonsSkipped++;
            }
            else
            {
                var (created, skipped) = await CreateLessonsForDate(course, currentDate, existingLessons, studentAbsences);
                lessonsCreated += created;
                lessonsSkipped += skipped;
            }

            currentDate = GetNextOccurrenceDate(currentDate, course);
        }

        return (lessonsCreated, lessonsSkipped);
    }

    private async Task<List<Holiday>> GetHolidaysAsync(
        DateOnly startDate, DateOnly endDate, bool skipHolidays, CancellationToken ct)
    {
        if (!skipHolidays)
            return new List<Holiday>();

        return await unitOfWork.Repository<Holiday>().Query()
            .Where(h => h.EndDate >= startDate && h.StartDate <= endDate)
            .ToListAsync(ct);
    }

    private async Task<List<Lesson>> GetExistingLessonsAsync(
        Guid courseId, DateOnly startDate, DateOnly endDate, CancellationToken ct)
    {
        return await unitOfWork.Lessons.Query()
            .Where(l => l.CourseId == courseId && l.ScheduledDate >= startDate && l.ScheduledDate <= endDate)
            .ToListAsync(ct);
    }

    internal static DateOnly FindFirstOccurrenceDate(DateOnly startDate, Course course, DateOnly endDate)
    {
        var currentDate = startDate;

        while (currentDate.DayOfWeek != course.DayOfWeek && currentDate <= endDate)
        {
            currentDate = currentDate.AddDays(1);
        }

        if (course.Frequency == CourseFrequency.Biweekly && course.WeekParity != WeekParity.All)
        {
            while (!IsoDateHelper.MatchesWeekParity(currentDate.ToDateTime(TimeOnly.MinValue), course.WeekParity)
                   && currentDate <= endDate)
            {
                currentDate = currentDate.AddDays(7);
            }
        }

        return currentDate;
    }

    private async Task<List<Absence>> GetTeacherAbsencesAsync(
        Guid teacherId, DateOnly startDate, DateOnly endDate, CancellationToken ct)
    {
        return await unitOfWork.Repository<Absence>().Query()
            .Where(a => a.TeacherId == teacherId && a.EndDate >= startDate && a.StartDate <= endDate)
            .ToListAsync(ct);
    }

    private async Task<List<Absence>> GetStudentAbsencesAsync(
        DateOnly startDate, DateOnly endDate, CancellationToken ct)
    {
        return await unitOfWork.Repository<Absence>().Query()
            .Where(a => a.StudentId != null && a.EndDate >= startDate && a.StartDate <= endDate)
            .ToListAsync(ct);
    }

    private static bool IsAbsent(DateOnly date, List<Absence> absences)
    {
        return absences.Any(a => date >= a.StartDate && date <= a.EndDate);
    }

    private static bool IsStudentAbsent(Guid studentId, DateOnly date, List<Absence> studentAbsences)
    {
        return studentAbsences.Any(a => a.StudentId == studentId && date >= a.StartDate && date <= a.EndDate);
    }

    private static bool IsHoliday(DateOnly date, List<Holiday> holidays)
    {
        return holidays.Any(h => date >= h.StartDate && date <= h.EndDate);
    }

    private static bool HasExistingLesson(DateOnly date, Guid? studentId, List<Lesson> existingLessons)
    {
        return existingLessons.Any(l => l.ScheduledDate == date && l.StudentId == studentId);
    }

    internal static DateOnly GetNextOccurrenceDate(DateOnly currentDate, Course course)
    {
        if (course.Frequency == CourseFrequency.Biweekly && course.WeekParity != WeekParity.All)
        {
            var nextDate = currentDate.AddDays(7);
            while (!IsoDateHelper.MatchesWeekParity(nextDate.ToDateTime(TimeOnly.MinValue), course.WeekParity))
            {
                nextDate = nextDate.AddDays(7);
            }
            return nextDate;
        }

        return course.Frequency switch
        {
            CourseFrequency.Weekly => currentDate.AddDays(7),
            CourseFrequency.Biweekly => currentDate.AddDays(14),
            CourseFrequency.Once => currentDate.AddMonths(1),
            _ => currentDate.AddDays(7)
        };
    }

    private async Task<(int Created, int Skipped)> CreateLessonsForDate(
        Course course, DateOnly date, List<Lesson> existingLessons, List<Absence> studentAbsences)
    {
        var created = 0;
        var skipped = 0;

        var enrolledStudents = course.Enrollments
            .Where(e => DateOnly.FromDateTime(e.EnrolledAt) <= date)
            .ToList();

        if (enrolledStudents.Count == 0)
        {
            if (!HasExistingLesson(date, null, existingLessons))
            {
                await CreateLesson(course, date, null);
                created++;
            }
            else
            {
                skipped++;
            }
        }
        else
        {
            foreach (var enrollment in enrolledStudents)
            {
                if (IsStudentAbsent(enrollment.StudentId, date, studentAbsences))
                {
                    skipped++;
                }
                else if (!HasExistingLesson(date, enrollment.StudentId, existingLessons))
                {
                    await CreateLesson(course, date, enrollment.StudentId);
                    created++;
                }
                else
                {
                    skipped++;
                }
            }
        }

        return (created, skipped);
    }

    private async Task CreateLesson(Course course, DateOnly date, Guid? studentId)
    {
        var lesson = new Lesson
        {
            Id = Guid.NewGuid(),
            CourseId = course.Id,
            StudentId = studentId,
            TeacherId = course.TeacherId,
            RoomId = course.RoomId,
            ScheduledDate = date,
            StartTime = course.StartTime,
            EndTime = course.EndTime,
            Status = LessonStatus.Scheduled
        };
        await unitOfWork.Lessons.AddAsync(lesson, CancellationToken.None);
    }
}
