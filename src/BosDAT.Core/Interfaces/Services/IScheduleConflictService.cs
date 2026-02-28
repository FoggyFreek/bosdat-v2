using BosDAT.Core.DTOs;

namespace BosDAT.Core.Interfaces.Services;

/// <summary>
/// Service for detecting schedule conflicts when enrolling students in courses.
/// </summary>
public interface IScheduleConflictService
{
    /// <summary>
    /// Checks if enrolling a student in a course would create a schedule conflict
    /// with their existing enrollments.
    /// </summary>
    /// <param name="studentId">The ID of the student to check.</param>
    /// <param name="courseId">The ID of the course to enroll in.</param>
    /// <returns>A conflict check result indicating if conflicts exist and details of any conflicting courses.</returns>
    Task<ConflictCheckResult> HasConflictAsync(Guid studentId, Guid courseId);
}
