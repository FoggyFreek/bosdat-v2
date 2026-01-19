using BosDAT.Core.Entities;

namespace BosDAT.Core.DTOs;

public record InvoiceDto
{
    public Guid Id { get; init; }
    public required string InvoiceNumber { get; init; }
    public Guid StudentId { get; init; }
    public string StudentName { get; init; } = string.Empty;
    public string StudentEmail { get; init; } = string.Empty;
    public DateOnly IssueDate { get; init; }
    public DateOnly DueDate { get; init; }
    public decimal Subtotal { get; init; }
    public decimal VatAmount { get; init; }
    public decimal Total { get; init; }
    public decimal DiscountAmount { get; init; }
    public InvoiceStatus Status { get; init; }
    public DateTime? PaidAt { get; init; }
    public string? PaymentMethod { get; init; }
    public string? Notes { get; init; }
    public List<InvoiceLineDto> Lines { get; init; } = new();
    public List<PaymentDto> Payments { get; init; } = new();
    public decimal AmountPaid { get; init; }
    public decimal Balance { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public record InvoiceLineDto
{
    public int Id { get; init; }
    public Guid? LessonId { get; init; }
    public required string Description { get; init; }
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal VatRate { get; init; }
    public decimal LineTotal { get; init; }
}

public record CreateInvoiceDto
{
    public Guid StudentId { get; init; }
    public DateOnly? IssueDate { get; init; }
    public DateOnly? DueDate { get; init; }
    public decimal DiscountAmount { get; init; }
    public string? Notes { get; init; }
    public List<CreateInvoiceLineDto> Lines { get; init; } = new();
}

public record CreateInvoiceLineDto
{
    public Guid? LessonId { get; init; }
    public required string Description { get; init; }
    public int Quantity { get; init; } = 1;
    public decimal UnitPrice { get; init; }
    public decimal VatRate { get; init; }
}

public record UpdateInvoiceDto
{
    public DateOnly IssueDate { get; init; }
    public DateOnly DueDate { get; init; }
    public decimal DiscountAmount { get; init; }
    public string? Notes { get; init; }
}

public record InvoiceListDto
{
    public Guid Id { get; init; }
    public required string InvoiceNumber { get; init; }
    public string StudentName { get; init; } = string.Empty;
    public DateOnly IssueDate { get; init; }
    public DateOnly DueDate { get; init; }
    public decimal Total { get; init; }
    public InvoiceStatus Status { get; init; }
    public decimal Balance { get; init; }
}

public record PaymentDto
{
    public Guid Id { get; init; }
    public Guid InvoiceId { get; init; }
    public decimal Amount { get; init; }
    public DateOnly PaymentDate { get; init; }
    public PaymentMethod Method { get; init; }
    public string? Reference { get; init; }
    public string? Notes { get; init; }
}

public record CreatePaymentDto
{
    public Guid InvoiceId { get; init; }
    public decimal Amount { get; init; }
    public DateOnly? PaymentDate { get; init; }
    public PaymentMethod Method { get; init; }
    public string? Reference { get; init; }
    public string? Notes { get; init; }
}
