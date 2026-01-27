using BosDAT.Core.Entities;

namespace BosDAT.Core.Interfaces;

public interface ICourseTypePricingService
{
    Task<CourseTypePricingVersion?> GetCurrentPricingAsync(Guid courseTypeId, CancellationToken ct = default);
    Task<IReadOnlyList<CourseTypePricingVersion>> GetPricingHistoryAsync(Guid courseTypeId, CancellationToken ct = default);
    Task<bool> IsCurrentPricingInvoicedAsync(Guid courseTypeId, CancellationToken ct = default);
    Task<CourseTypePricingVersion> UpdateCurrentPricingAsync(Guid courseTypeId, decimal priceAdult, decimal priceChild, CancellationToken ct = default);
    Task<CourseTypePricingVersion> CreateNewPricingVersionAsync(Guid courseTypeId, decimal priceAdult, decimal priceChild, DateOnly validFrom, CancellationToken ct = default);
    Task<CourseTypePricingVersion?> GetPricingForDateAsync(Guid courseTypeId, DateOnly date, CancellationToken ct = default);
    Task<CourseTypePricingVersion> CreateInitialPricingVersionAsync(Guid courseTypeId, decimal priceAdult, decimal priceChild, CancellationToken ct = default);
}
