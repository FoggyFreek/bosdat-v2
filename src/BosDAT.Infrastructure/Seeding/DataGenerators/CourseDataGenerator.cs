using Microsoft.EntityFrameworkCore;
using BosDAT.Core.Entities;
using BosDAT.Core.Enums;
using BosDAT.Infrastructure.Data;

namespace BosDAT.Infrastructure.Seeding.DataGenerators;

/// <summary>
/// Represents a time slot for conflict detection comparisons.
/// </summary>
internal record TimeSlotComparison(DayOfWeek DayOfWeek, TimeOnly StartTime, TimeOnly EndTime, WeekParity WeekParity);

/// <summary>
/// Generates course types, pricing versions, courses, and enrollments.
/// </summary>
public class CourseDataGenerator
{
    private readonly ApplicationDbContext _context;
    private readonly SeederContext _seederContext;

    public CourseDataGenerator(ApplicationDbContext context, SeederContext seederContext)
    {
        _context = context;
        _seederContext = seederContext;
    }

    public async Task<List<CourseType>> GenerateCourseTypesAsync(
        List<Instrument> instruments,
        CancellationToken cancellationToken)
    {
        var existingCount = await _context.CourseTypes.CountAsync(cancellationToken);
        if (existingCount > 0)
        {
            return await _context.CourseTypes.ToListAsync(cancellationToken);
        }

        var courseTypes = new List<CourseType>();
        var createdAt = DateTime.UtcNow.AddMonths(-12);

        foreach (var instrument in instruments)
        {
            // Individual lessons - 30 min
            courseTypes.Add(CreateCourseType(
                instrument, $"{instrument.Name} - Individual 30min",
                30, CourseTypeCategory.Individual, 1, createdAt));

            // Individual lessons - 45 min
            courseTypes.Add(CreateCourseType(
                instrument, $"{instrument.Name} - Individual 45min",
                45, CourseTypeCategory.Individual, 1, createdAt));

            // Group lessons (only for some instruments)
            if (SeederConstants.GroupLessonInstrumentIds.Contains(instrument.Id))
            {
                courseTypes.Add(CreateCourseType(
                    instrument, $"{instrument.Name} - Group",
                    60, CourseTypeCategory.Group, 6, createdAt));
            }

            // Workshop (only for popular instruments)
            if (SeederConstants.WorkshopInstrumentIds.Contains(instrument.Id))
            {
                courseTypes.Add(CreateCourseType(
                    instrument, $"{instrument.Name} - Workshop",
                    120, CourseTypeCategory.Workshop, 12, createdAt));
            }
        }

        await _context.CourseTypes.AddRangeAsync(courseTypes, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _seederContext.CourseTypes = courseTypes;
        return courseTypes;
    }

    private CourseType CreateCourseType(
        Instrument instrument, string name, int duration,
        CourseTypeCategory type, int maxStudents, DateTime createdAt)
    {
        return new CourseType
        {
            Id = _seederContext.NextCourseTypeId(),
            InstrumentId = instrument.Id,
            Name = name,
            DurationMinutes = duration,
            Type = type,
            MaxStudents = maxStudents,
            IsActive = true,
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };
    }

    public async Task<List<CourseTypePricingVersion>> GeneratePricingVersionsAsync(
        List<CourseType> courseTypes,
        CancellationToken cancellationToken)
    {
        var existingCount = await _context.CourseTypePricingVersions.CountAsync(cancellationToken);
        if (existingCount > 0)
        {
            return await _context.CourseTypePricingVersions.ToListAsync(cancellationToken);
        }

        var pricingVersions = new List<CourseTypePricingVersion>();

        foreach (var courseType in courseTypes)
        {
            var basePriceAdult = CalculateBasePrice(courseType);

            // Historical pricing (previous year)
            pricingVersions.Add(new CourseTypePricingVersion
            {
                Id = _seederContext.NextPricingVersionId(),
                CourseTypeId = courseType.Id,
                PriceAdult = basePriceAdult - 2m,
                PriceChild = (basePriceAdult - 2m) * 0.9m,
                ValidFrom = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-2)),
                ValidUntil = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-1).AddDays(-1)),
                IsCurrent = false,
                CreatedAt = DateTime.UtcNow.AddYears(-2),
                UpdatedAt = DateTime.UtcNow.AddYears(-1)
            });

            // Current pricing
            pricingVersions.Add(new CourseTypePricingVersion
            {
                Id = _seederContext.NextPricingVersionId(),
                CourseTypeId = courseType.Id,
                PriceAdult = basePriceAdult,
                PriceChild = basePriceAdult * 0.9m,
                ValidFrom = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-1)),
                ValidUntil = null,
                IsCurrent = true,
                CreatedAt = DateTime.UtcNow.AddYears(-1),
                UpdatedAt = DateTime.UtcNow.AddYears(-1)
            });
        }

        await _context.CourseTypePricingVersions.AddRangeAsync(pricingVersions, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _seederContext.PricingVersions = pricingVersions;
        return pricingVersions;
    }

    private static decimal CalculateBasePrice(CourseType courseType)
    {
        decimal basePrice = courseType.DurationMinutes switch
        {
            30 => 30m,
            45 => 42m,
            60 => 25m,  // per person for group
            120 => 35m, // per person for workshop
            _ => 30m
        };

        return courseType.Type switch
        {
            CourseTypeCategory.Group => basePrice * 0.8m,    // 20% discount
            CourseTypeCategory.Workshop => basePrice * 0.6m, // 40% discount
            _ => basePrice
        };
    }

    public async Task<List<Course>> GenerateCoursesAsync(
        List<CourseType> courseTypes,
        List<Room> rooms,
        CancellationToken cancellationToken)
    {
        var existingCount = await _context.Courses.CountAsync(cancellationToken);
        if (existingCount > 0)
        {
            return await _context.Courses.ToListAsync(cancellationToken);
        }

        var courses = new List<Course>();
        var teacherCourseTypes = await _context.TeacherCourseTypes.ToListAsync(cancellationToken);

        foreach (var courseType in courseTypes)
        {
            var eligibleTeacherIds = teacherCourseTypes
                .Where(tct => tct.CourseTypeId == courseType.Id)
                .Select(tct => tct.TeacherId)
                .ToList();

            if (eligibleTeacherIds.Count == 0) continue;

            var room = SelectRoom(courseType, rooms);
            var coursesPerType = GetCoursesPerType(courseType.Type);

            for (int c = 0; c < coursesPerType; c++)
            {
                var course = TryCreateCourse(courseType, eligibleTeacherIds, room, courses);
                if (course != null)
                    courses.Add(course);
            }
        }

        await _context.Courses.AddRangeAsync(courses, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _seederContext.Courses = courses;
        return courses;
    }

    private static Room? SelectRoom(CourseType courseType, List<Room> rooms) =>
        courseType.Type switch
        {
            CourseTypeCategory.Individual => rooms.FirstOrDefault(r => r.Capacity <= 2),
            CourseTypeCategory.Group => rooms.FirstOrDefault(r => r.Capacity >= 4 && r.Capacity < 10),
            CourseTypeCategory.Workshop => rooms.FirstOrDefault(r => r.Capacity >= 10),
            _ => rooms.FirstOrDefault()
        };

    private static int GetCoursesPerType(CourseTypeCategory type) =>
        type switch
        {
            CourseTypeCategory.Individual => 3,
            CourseTypeCategory.Group => 2,
            CourseTypeCategory.Workshop => 1,
            _ => 2
        };

    private Course? TryCreateCourse(CourseType courseType, List<Guid> eligibleTeacherIds, Room? room, List<Course> existingCourses)
    {
        const int maxAttempts = 20;

        // Properties that don't affect scheduling — generate once.
        var frequency = DetermineFrequency();
        var randomParity = _seederContext.NextBool() ? WeekParity.Odd : WeekParity.Even;
        var weekParity = frequency == CourseFrequency.Biweekly ? randomParity : WeekParity.All;
        var status = DetermineStatus();
        var startDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-_seederContext.NextInt(1, 18)));
        var endDate = DetermineEndDate(status);
        var isTrial = _seederContext.NextBool(10);

        // Retry teacher + day + time until no teacher/room conflict is found.
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            var teacherId = _seederContext.GetRandomItem(eligibleTeacherIds);
            var dayOfWeek = (DayOfWeek)_seederContext.NextInt(1, 6);
            var startHour = _seederContext.NextInt(9, 20);
            var startTime = new TimeOnly(startHour, _seederContext.NextInt(0, 2) * 30);
            var endTime = startTime.AddMinutes(courseType.DurationMinutes);

            if (HasTeacherOrRoomConflict(teacherId, room?.Id, dayOfWeek, startTime, endTime, weekParity, existingCourses))
                continue;

            return new Course
            {
                Id = _seederContext.NextCourseId(),
                TeacherId = teacherId,
                CourseTypeId = courseType.Id,
                RoomId = room?.Id,
                DayOfWeek = dayOfWeek,
                StartTime = startTime,
                EndTime = endTime,
                Frequency = frequency,
                WeekParity = weekParity,
                StartDate = startDate,
                EndDate = endDate,
                Status = status,
                IsWorkshop = courseType.Type == CourseTypeCategory.Workshop,
                IsTrial = isTrial,
                Notes = isTrial ? "Trial course for new students" : null,
                CreatedAt = startDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                UpdatedAt = DateTime.UtcNow.AddDays(-_seederContext.NextInt(1, 30))
            };
        }

        return null;
    }

    private CourseFrequency DetermineFrequency()
    {
        var roll = _seederContext.NextInt(0, 100);
        if (roll < 70) return CourseFrequency.Weekly;
        if (roll < 90) return CourseFrequency.Biweekly;
        return CourseFrequency.Once;
    }

    private CourseStatus DetermineStatus()
    {
        var roll = _seederContext.NextInt(0, 100);
        if (roll < 75) return CourseStatus.Active;
        if (roll < 85) return CourseStatus.Completed;
        if (roll < 92) return CourseStatus.Paused;
        return CourseStatus.Cancelled;
    }

    private DateOnly? DetermineEndDate(CourseStatus status) =>
        status switch
        {
            CourseStatus.Completed => DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-_seederContext.NextInt(1, 3))),
            CourseStatus.Cancelled => DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-_seederContext.NextInt(0, 2))),
            _ => null
        };

    public async Task<List<Enrollment>> GenerateEnrollmentsAsync(
        List<Student> students,
        List<Course> courses,
        CancellationToken cancellationToken)
    {
        var existingCount = await _context.Enrollments.CountAsync(cancellationToken);
        if (existingCount > 0)
        {
            return await _context.Enrollments.ToListAsync(cancellationToken);
        }

        var enrollments = new List<Enrollment>();
        var courseTypes = await _context.CourseTypes.ToListAsync(cancellationToken);

        var activeStudents = students.Where(s => s.Status != StudentStatus.Inactive).ToList();
        var activeCourses = courses.Where(c =>
            c.Status == CourseStatus.Active || c.Status == CourseStatus.Completed).ToList();

        foreach (var student in activeStudents)
        {
            var coursesToEnroll = _seederContext.NextInt(1, 4);
            var enrolledCourseIds = new HashSet<Guid>();
            var enrolledCourses = new List<Course>();

            for (int i = 0; i < coursesToEnroll; i++)
            {
                var availableCourses = activeCourses
                    .Where(c => !enrolledCourseIds.Contains(c.Id))
                    .Where(c => !HasStudentScheduleConflict(c, enrolledCourses))
                    .ToList();

                if (availableCourses.Count == 0) break;

                var course = _seederContext.GetRandomItem(availableCourses);
                var courseType = courseTypes.First(ct => ct.Id == course.CourseTypeId);

                // Check max students for group/workshop
                if (courseType.Type != CourseTypeCategory.Individual)
                {
                    var courseEnrollmentCount = enrollments.Count(e => e.CourseId == course.Id);
                    if (courseEnrollmentCount >= courseType.MaxStudents) continue;
                }

                enrolledCourseIds.Add(course.Id);
                enrolledCourses.Add(course);

                var enrollment = CreateEnrollment(student, course, enrolledCourseIds.Count);
                enrollments.Add(enrollment);
            }
        }

        await _context.Enrollments.AddRangeAsync(enrollments, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _seederContext.Enrollments = enrollments;
        return enrollments;
    }

    private Enrollment CreateEnrollment(Student student, Course course, int courseCount)
    {
        EnrollmentStatus enrollmentStatus;
        if (student.Status == StudentStatus.Trial)
            enrollmentStatus = EnrollmentStatus.Trail;
        else if (course.Status == CourseStatus.Completed)
            enrollmentStatus = EnrollmentStatus.Completed;
        else
            enrollmentStatus = EnrollmentStatus.Active;

        var (discountType, discountPercent) = DetermineDiscount(courseCount);

        return new Enrollment
        {
            Id = _seederContext.NextEnrollmentId(),
            StudentId = student.Id,
            CourseId = course.Id,
            EnrolledAt = course.StartDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)
                .AddDays(_seederContext.NextInt(-7, 14)),
            DiscountPercent = discountPercent,
            DiscountType = discountType,
            Status = enrollmentStatus,
            Notes = discountType == DiscountType.Family ? "Family member discount applied" : null,
            CreatedAt = course.StartDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            UpdatedAt = DateTime.UtcNow.AddDays(-_seederContext.NextInt(1, 30))
        };
    }

    private (DiscountType Type, decimal Percent) DetermineDiscount(int courseCount)
    {
        if (courseCount > 1)
            return (DiscountType.Course, 10m);

        if (_seederContext.NextBool(15))
            return (DiscountType.Family, 10m);

        return (DiscountType.None, 0m);
    }

    // ── Conflict detection (mirrors ScheduleConflictService + TimeSlot logic) ──

    private static bool HasTeacherOrRoomConflict(
        Guid teacherId, int? roomId, DayOfWeek dayOfWeek,
        TimeOnly startTime, TimeOnly endTime, WeekParity weekParity,
        List<Course> existingCourses)
    {
        var newSlot = new TimeSlotComparison(dayOfWeek, startTime, endTime, weekParity);

        foreach (var existing in existingCourses)
        {
            var existingSlot = new TimeSlotComparison(existing.DayOfWeek, existing.StartTime, existing.EndTime, existing.WeekParity);

            if (!HasTimeSlotOverlap(newSlot, existingSlot))
                continue;

            if (teacherId == existing.TeacherId)
                return true;

            if (roomId.HasValue && roomId == existing.RoomId)
                return true;
        }

        return false;
    }

    private static bool HasStudentScheduleConflict(Course target, List<Course> studentCourses)
    {
        var targetSlot = new TimeSlotComparison(target.DayOfWeek, target.StartTime, target.EndTime, target.WeekParity);
        return studentCourses.Any(existing =>
        {
            var existingSlot = new TimeSlotComparison(existing.DayOfWeek, existing.StartTime, existing.EndTime, existing.WeekParity);
            return HasTimeSlotOverlap(targetSlot, existingSlot);
        });
    }

    private static bool HasTimeSlotOverlap(TimeSlotComparison slot1, TimeSlotComparison slot2)
    {
        if (slot1.DayOfWeek != slot2.DayOfWeek) return false;
        if (slot1.StartTime >= slot2.EndTime || slot1.EndTime <= slot2.StartTime) return false;
        return HasWeekParityConflict(slot1.WeekParity, slot2.WeekParity);
    }

    private static bool HasWeekParityConflict(WeekParity a, WeekParity b)
    {
        if (a == WeekParity.All || b == WeekParity.All) return true;
        return a == b;
    }
}
