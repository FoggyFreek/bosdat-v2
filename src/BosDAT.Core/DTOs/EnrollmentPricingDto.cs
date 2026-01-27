namespace BosDAT.Core.DTOs;

public record EnrollmentPricingDto
{
    public Guid EnrollmentId { get; init; }
    public Guid CourseId { get; init; }
    public string CourseName { get; init; } = string.Empty;
    public decimal BasePriceAdult { get; init; }
    public decimal BasePriceChild { get; init; }
    public bool IsChildPricing { get; init; }
    public decimal ApplicableBasePrice { get; init; }
    public decimal DiscountPercent { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal PricePerLesson { get; init; }
}
