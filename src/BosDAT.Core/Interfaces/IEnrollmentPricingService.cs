using BosDAT.Core.DTOs;

namespace BosDAT.Core.Interfaces;

public interface IEnrollmentPricingService
{
    Task<EnrollmentPricingDto?> GetEnrollmentPricingAsync(
        Guid studentId,
        Guid courseId,
        CancellationToken ct = default);
}
