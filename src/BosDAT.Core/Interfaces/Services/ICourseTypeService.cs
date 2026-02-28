using BosDAT.Core.DTOs;

namespace BosDAT.Core.Interfaces.Services;

public interface ICourseTypeService
{
    Task<List<CourseTypeDto>> GetAllAsync(
        bool? activeOnly,
        int? instrumentId,
        CancellationToken ct = default);

    Task<CourseTypeDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<(CourseTypeDto? CourseType, string? Error)> CreateAsync(
        CreateCourseTypeDto dto,
        CancellationToken ct = default);

    Task<(CourseTypeDto? CourseType, string? Error, bool NotFound)> UpdateAsync(
        Guid id,
        UpdateCourseTypeDto dto,
        CancellationToken ct = default);

    Task<(bool Success, string? Error, bool NotFound)> DeleteAsync(
        Guid id,
        CancellationToken ct = default);

    Task<(CourseTypeDto? CourseType, bool NotFound)> ReactivateAsync(
        Guid id,
        CancellationToken ct = default);

    Task<(int? Count, bool NotFound)> GetTeachersCountForInstrumentAsync(
        int instrumentId,
        CancellationToken ct = default);

    Task<(IEnumerable<CourseTypePricingVersionDto>? History, bool NotFound)> GetPricingHistoryAsync(
        Guid id,
        CancellationToken ct = default);

    Task<(PricingEditabilityDto? Result, bool NotFound)> CheckPricingEditabilityAsync(
        Guid id,
        CancellationToken ct = default);

    Task<(CourseTypePricingVersionDto? Pricing, string? Error, bool NotFound)> UpdatePricingAsync(
        Guid id,
        UpdateCourseTypePricingDto dto,
        CancellationToken ct = default);

    Task<(CourseTypePricingVersionDto? Pricing, string? Error, bool NotFound)> CreatePricingVersionAsync(
        Guid id,
        CreateCourseTypePricingVersionDto dto,
        CancellationToken ct = default);
}
