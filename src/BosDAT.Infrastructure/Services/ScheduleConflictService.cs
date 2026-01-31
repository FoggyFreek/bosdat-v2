using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Enums;
using BosDAT.Core.Interfaces;
using BosDAT.Core.ValueObjects;

namespace BosDAT.Infrastructure.Services;

/// <summary>
/// Service for detecting schedule conflicts when enrolling students in courses.
/// </summary>
public class ScheduleConflictService(IUnitOfWork unitOfWork) : IScheduleConflictService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<ConflictCheckResult> HasConflictAsync(Guid studentId, Guid courseId)
    {
        // Get the target course
        var targetCourse = await _unitOfWork.Courses.GetByIdAsync(courseId)
            ?? throw new InvalidOperationException($"Course with ID {courseId} not found.");

        // Get student's active enrollments
        var existingEnrollments = await _unitOfWork.Enrollments.GetActiveEnrollmentsByStudentIdAsync(studentId);

        // Check each existing enrollment for conflicts using LINQ
        var conflictingCourses = existingEnrollments
            .Where(e => HasScheduleConflict(targetCourse, e.Course))
            .Select(e => MapToConflictingCourse(e.Course))
            .ToList();

        return new ConflictCheckResult
        {
            HasConflict = conflictingCourses.Count > 0,
            ConflictingCourses = conflictingCourses
        };
    }

    /// <summary>
    /// Determines if two courses have a schedule conflict.
    /// </summary>
    private static bool HasScheduleConflict(Course targetCourse, Course existingCourse)
    {
        // Create time slots for both courses
        var targetTimeSlot = new TimeSlot(
            targetCourse.DayOfWeek,
            targetCourse.StartTime,
            targetCourse.EndTime);

        var existingTimeSlot = new TimeSlot(
            existingCourse.DayOfWeek,
            existingCourse.StartTime,
            existingCourse.EndTime);

        // Check if time slots overlap
        if (!targetTimeSlot.OverlapsWith(existingTimeSlot))
        {
            return false; // Different days or non-overlapping times
        }

        // Time slots overlap, now check week parity
        return HasWeekParityConflict(targetCourse.WeekParity, existingCourse.WeekParity);
    }

    /// <summary>
    /// Determines if two week parities conflict.
    /// </summary>
    /// <remarks>
    /// Conflict occurs when:
    /// - Either parity is All (means every week, so always conflicts)
    /// - Both parities are the same (Odd vs Odd, or Even vs Even)
    ///
    /// No conflict when:
    /// - One is Odd and the other is Even (they occur on different weeks)
    /// </remarks>
    private static bool HasWeekParityConflict(WeekParity targetParity, WeekParity existingParity)
    {
        // If either course runs every week (All), there's always a conflict
        if (targetParity == WeekParity.All || existingParity == WeekParity.All)
        {
            return true;
        }

        // Both have specific parities - conflict only if they're the same
        return targetParity == existingParity;
    }

    private static ConflictingCourse MapToConflictingCourse(Course course)
    {
        return new ConflictingCourse
        {
            CourseId = course.Id,
            CourseName = course.CourseType?.Name ?? "Unknown",
            DayOfWeek = course.DayOfWeek,
            StartTime = course.StartTime,
            EndTime = course.EndTime,
            Frequency = course.Frequency.ToString(),
            WeekParity = course.WeekParity != WeekParity.All ? course.WeekParity.ToString() : null
        };
    }
}
