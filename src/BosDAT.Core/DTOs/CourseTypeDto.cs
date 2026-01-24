using BosDAT.Core.Entities;

namespace BosDAT.Core.DTOs;

public record CourseTypeSimpleDto
{
    public Guid Id { get; init; }
    public required string Name { get; init; }
    public int InstrumentId { get; init; }
    public string InstrumentName { get; init; } = string.Empty;
    public int DurationMinutes { get; init; }
    public CourseTypeCategory Type { get; init; }
}

public record CourseTypeDto
{
    public Guid Id { get; init; }
    public int InstrumentId { get; init; }
    public string InstrumentName { get; init; } = string.Empty;
    public required string Name { get; init; }
    public int DurationMinutes { get; init; }
    public CourseTypeCategory Type { get; init; }
    public int MaxStudents { get; init; }
    public bool IsActive { get; init; }
    public int ActiveCourseCount { get; init; }
    public bool HasTeachersForCourseType { get; init; }
    public CourseTypePricingVersionDto? CurrentPricing { get; init; }
    public List<CourseTypePricingVersionDto> PricingHistory { get; init; } = new();
    public bool CanEditPricingDirectly { get; init; }
}

public record CreateCourseTypeDto
{
    public int InstrumentId { get; init; }
    public required string Name { get; init; }
    public int DurationMinutes { get; init; } = 30;
    public CourseTypeCategory Type { get; init; } = CourseTypeCategory.Individual;
    public decimal PriceAdult { get; init; }
    public decimal PriceChild { get; init; }
    public int MaxStudents { get; init; } = 1;
}

public record UpdateCourseTypeDto
{
    public int InstrumentId { get; init; }
    public required string Name { get; init; }
    public int DurationMinutes { get; init; }
    public CourseTypeCategory Type { get; init; }
    public int MaxStudents { get; init; }
    public bool IsActive { get; init; }
}
