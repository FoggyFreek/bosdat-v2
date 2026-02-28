using BosDAT.Core.DTOs;

namespace BosDAT.Core.Interfaces.Services;

public interface IEnrollmentPricingService
{
    Task<EnrollmentPricingDto?> GetEnrollmentPricingAsync(
        Guid studentId,
        Guid courseId,
        CancellationToken ct = default);
}
