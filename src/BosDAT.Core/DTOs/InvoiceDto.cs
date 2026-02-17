using BosDAT.Core.Entities;
using BosDAT.Core.Enums;

namespace BosDAT.Core.DTOs;

public record InvoiceDto
{
    public Guid Id { get; init; }
    public required string InvoiceNumber { get; init; }
    public Guid StudentId { get; init; }
    public Guid? EnrollmentId { get; init; }
    public string StudentName { get; init; } = string.Empty;
    public string StudentEmail { get; init; } = string.Empty;
    public DateOnly IssueDate { get; init; }
    public DateOnly DueDate { get; init; }

    // Period information
    public string? Description { get; init; }
    public DateOnly? PeriodStart { get; init; }
    public DateOnly? PeriodEnd { get; init; }
    public InvoicingPreference? PeriodType { get; init; }

    // Amounts
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

    // Billing contact info (from student or billing contact)
    public BillingContactDto? BillingContact { get; init; }
}

public record InvoiceLineDto
{
    public int Id { get; init; }
    public Guid? LessonId { get; init; }
    public Guid? PricingVersionId { get; init; }
    public required string Description { get; init; }
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal VatRate { get; init; }
    public decimal LineTotal { get; init; }

    // Additional detail for price breakdown
    public DateOnly? LessonDate { get; init; }
    public string? CourseName { get; init; }
}
public record BillingContactDto
{
    public string Name { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? Address { get; init; }
    public string? PostalCode { get; init; }
    public string? City { get; init; }
}

public record CreateInvoiceDto
{
    public Guid StudentId { get; init; }
    public Guid? EnrollmentId { get; init; }
    public DateOnly? IssueDate { get; init; }
    public DateOnly? DueDate { get; init; }
    public DateOnly? PeriodStart { get; init; }
    public DateOnly? PeriodEnd { get; init; }
    public InvoicingPreference? PeriodType { get; init; }
    public decimal DiscountAmount { get; init; }
    public string? Notes { get; init; }
    public List<CreateInvoiceLineDto> Lines { get; init; } = new();
}

/// <summary>
/// DTO for generating invoices for lessons in a billing period
/// </summary>
public record GenerateInvoiceDto
{
    public Guid EnrollmentId { get; init; }
    public DateOnly PeriodStart { get; init; }
    public DateOnly PeriodEnd { get; init; }
}

/// <summary>
/// DTO for batch invoice generation
/// </summary>
public record GenerateBatchInvoicesDto
{
    public DateOnly PeriodStart { get; init; }
    public DateOnly PeriodEnd { get; init; }
    public InvoicingPreference PeriodType { get; init; }
}

/// <summary>
/// DTO for recalculating an invoice
/// </summary>
public record RecalculateInvoiceDto
{
    public Guid InvoiceId { get; init; }
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
    public string? Description { get; init; }
    public DateOnly IssueDate { get; init; }
    public DateOnly DueDate { get; init; }
    public DateOnly? PeriodStart { get; init; }
    public DateOnly? PeriodEnd { get; init; }
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
    public string RecordedByName { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
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
