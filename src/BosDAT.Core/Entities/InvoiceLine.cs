namespace BosDAT.Core.Entities;

public class InvoiceLine
{
    public int Id { get; set; }
    public Guid InvoiceId { get; set; }
    public Guid? LessonId { get; set; }
    public Guid? PricingVersionId { get; set; }

    public required string Description { get; set; }
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal VatRate { get; set; }
    public decimal LineTotal { get; set; }

    // Navigation properties
    public virtual Invoice Invoice { get; set; } = null!;
    public virtual Lesson? Lesson { get; set; }
    public virtual CourseTypePricingVersion? PricingVersion { get; set; }
}
