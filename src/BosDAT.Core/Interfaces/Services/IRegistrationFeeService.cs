using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;

namespace BosDAT.Core.Interfaces.Services;

public interface IRegistrationFeeService
{
    /// <summary>
    /// Checks if the student has not yet paid the registration fee.
    /// </summary>
    Task<bool> IsStudentEligibleForFeeAsync(Guid studentId, CancellationToken ct = default);

    /// <summary>
    /// Checks if the course is a non-trial course (fee applies to non-trial courses only).
    /// </summary>
    Task<bool> ShouldApplyFeeForCourseAsync(Guid courseId, CancellationToken ct = default);

    /// <summary>
    /// Gets the registration fee status for a student.
    /// </summary>
    Task<RegistrationFeeStatusDto> GetFeeStatusAsync(Guid studentId, CancellationToken ct = default);

    Task<decimal> GetFeeAmountAsync(CancellationToken ct = default);
    Task<string> GetFeeDescriptionAsync(CancellationToken ct = default);
}
