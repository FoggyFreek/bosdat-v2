namespace BosDAT.Core.Entities;

public enum PaymentMethod
{
    Cash,
    Bank,
    Card,
    DirectDebit,
    Other
}

public class Payment : BaseEntity
{
    public Guid InvoiceId { get; set; }

    public decimal Amount { get; set; }
    public DateOnly PaymentDate { get; set; }
    public PaymentMethod Method { get; set; }
    public string? Reference { get; set; }

    public Guid? RecordedById { get; set; }
    public string? Notes { get; set; }

    // Navigation properties
    public virtual Invoice Invoice { get; set; } = null!;
}
