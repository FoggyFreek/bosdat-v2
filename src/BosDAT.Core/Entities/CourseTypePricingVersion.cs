namespace BosDAT.Core.Entities;

public class CourseTypePricingVersion : BaseEntity
{
    public Guid CourseTypeId { get; set; }
    public decimal PriceAdult { get; set; }
    public decimal PriceChild { get; set; }
    public DateOnly ValidFrom { get; set; }
    public DateOnly? ValidUntil { get; set; }
    public bool IsCurrent { get; set; }

    // Navigation properties
    public virtual CourseType CourseType { get; set; } = null!;
    public virtual ICollection<InvoiceLine> InvoiceLines { get; set; } = new List<InvoiceLine>();
}
