namespace BosDAT.Core.DTOs;

public record CourseTypePricingVersionDto
{
    public Guid Id { get; init; }
    public Guid CourseTypeId { get; init; }
    public decimal PriceAdult { get; init; }
    public decimal PriceChild { get; init; }
    public DateOnly ValidFrom { get; init; }
    public DateOnly? ValidUntil { get; init; }
    public bool IsCurrent { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record UpdateCourseTypePricingDto
{
    public decimal PriceAdult { get; init; }
    public decimal PriceChild { get; init; }
}

public record CreateCourseTypePricingVersionDto
{
    public decimal PriceAdult { get; init; }
    public decimal PriceChild { get; init; }
    public DateOnly ValidFrom { get; init; }
}

public record PricingEditabilityDto
{
    public bool CanEditDirectly { get; init; }
    public bool IsInvoiced { get; init; }
    public string? Reason { get; init; }
}
