using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;

namespace BosDAT.Core.Interfaces.Services;

public interface IEnrollmentService
{
    Task<IEnumerable<EnrollmentDto>> GetAllAsync(
        Guid? studentId = null,
        Guid? courseId = null,
        EnrollmentStatus? status = null,
        CancellationToken ct = default);

    Task<EnrollmentDetailDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<IEnumerable<StudentEnrollmentDto>> GetByStudentAsync(Guid studentId, CancellationToken ct = default);

    Task<(EnrollmentDto? Enrollment, bool NotFound, string? Error)> CreateAsync(
        Guid courseId, CreateEnrollmentDto dto, CancellationToken ct = default);

    Task<EnrollmentDto?> UpdateAsync(Guid id, UpdateEnrollmentDto dto, CancellationToken ct = default);

    Task<EnrollmentDto?> PromoteFromTrailAsync(Guid id, CancellationToken ct = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}

        