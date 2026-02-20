using System.Globalization;
using Microsoft.EntityFrameworkCore;
using BosDAT.Core.Entities;
using BosDAT.Core.Enums;
using BosDAT.Infrastructure.Data;
using System.Reflection.Metadata.Ecma335;

namespace BosDAT.Infrastructure.Seeding.DataGenerators;

/// <summary>
/// Generates lesson data with proper scheduling based on course frequency and week parity.
/// </summary>
public class LessonDataGenerator
{
    private readonly ApplicationDbContext _context;
    private readonly SeederContext _seederContext;

    public LessonDataGenerator(ApplicationDbContext context, SeederContext seederContext)
    {
        _context = context;
        _seederContext = seederContext;
    }

    public async Task<List<Lesson>> GenerateAsync(
        List<Course> courses,
        List<Enrollment> enrollments,
        CancellationToken cancellationToken)
    {
        var existingCount = await _context.Lessons.CountAsync(cancellationToken);
        if (existingCount > 0)
        {
            return await _context.Lessons.ToListAsync(cancellationToken);
        }

        var lessons = new List<Lesson>();
        var courseTypes = await _context.CourseTypes.ToDictionaryAsync(ct => ct.Id, cancellationToken);

        foreach (var course in courses)
        {
            var courseEnrollments = enrollments.Where(e => e.CourseId == course.Id).ToList();
            if (courseEnrollments.Count == 0) continue;

            courseTypes.TryGetValue(course.CourseTypeId, out var courseType);
            var courseLessons = GenerateLessonsForCourse(course, courseEnrollments, courseType);
            lessons.AddRange(courseLessons);
        }

        await _context.Lessons.AddRangeAsync(lessons, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _seederContext.Lessons = lessons;
        return lessons;
    }

    private List<Lesson> GenerateLessonsForCourse(
        Course course,
        List<Enrollment> enrollments,
        CourseType? courseType)
    {
        var lessons = new List<Lesson>();
        var endDate = course.EndDate ?? _seederContext.Today.AddMonths(2);
        var currentDate = course.StartDate;

        while (currentDate <= endDate)
        {
            if (ShouldGenerateLesson(course, currentDate))
            {
                // Generate lessons for each enrolled student
                foreach (var enrollment in enrollments)
                {
                    lessons.Add(CreateLesson(course, currentDate, enrollment.StudentId, courseType));
                }
            }

            currentDate = AdvanceDate(currentDate);
        }

        return lessons;
    }

    private static bool ShouldGenerateLesson(Course course, DateOnly date)
    {
        // Check day of week matches
        if (date.DayOfWeek != course.DayOfWeek)
            return false;

        // Check week parity for biweekly courses
        if (course.Frequency == CourseFrequency.Biweekly)
        {
            var isoWeek = ISOWeek.GetWeekOfYear(date.ToDateTime(TimeOnly.MinValue));
            var isOddWeek = isoWeek % 2 == 1;

            if ((course.WeekParity == WeekParity.Odd && !isOddWeek) ||
                (course.WeekParity == WeekParity.Even && isOddWeek))
            {
                return false;
            }
        }

        return true;
    }

    private static DateOnly AdvanceDate(DateOnly currentDate) =>
        currentDate.AddDays(1);

    private Lesson CreateLesson(Course course, DateOnly date, Guid? studentId, CourseType? courseType)
    {
        var isPast = date < _seederContext.Today;
        var status = DetermineStatus(isPast, courseType?.Type);
        var isCompleted = status == LessonStatus.Completed;

        return new Lesson
        {
            Id = _seederContext.NextLessonId(),
            CourseId = course.Id,
            StudentId = studentId,
            TeacherId = course.TeacherId,
            RoomId = course.RoomId,
            ScheduledDate = date,
            StartTime = course.StartTime,
            EndTime = course.EndTime,
            Status = status,
            CancellationReason = GetCancellationReason(status, courseType?.Type),
            IsInvoiced = isPast && isCompleted,
            IsPaidToTeacher = isPast && isCompleted && _seederContext.NextBool(80),
            Notes = GetLessonNotes(courseType?.Type),
            CreatedAt = course.StartDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            UpdatedAt = isPast
                ? date.ToDateTime(course.EndTime, DateTimeKind.Utc)
                : DateTime.UtcNow
        };
    }

    private LessonStatus DetermineStatus(bool isPast, CourseTypeCategory? type)
    {
        if (!isPast)
            return LessonStatus.Scheduled;

        // Past lessons have higher completion rate for group/workshop
        var completionRate = type == CourseTypeCategory.Individual ? 90 : 95;

        if (_seederContext.NextBool(completionRate))
            return LessonStatus.Completed;

        return _seederContext.NextBool(50) ? LessonStatus.Cancelled : LessonStatus.NoShow;
    }

    private string? GetCancellationReason(LessonStatus status, CourseTypeCategory? type)
    {
        if (status != LessonStatus.Cancelled)
            return null;

        if (type != CourseTypeCategory.Individual)
            return "Low attendance";
        return _seederContext.NextBool() ? "Student sick" : "Teacher unavailable";
    }

    private static string? GetLessonNotes(CourseTypeCategory? type) =>
        type switch
        {
            CourseTypeCategory.Workshop => "Workshop session",
            CourseTypeCategory.Group => "Group lesson",
            _ => null
        };
}
