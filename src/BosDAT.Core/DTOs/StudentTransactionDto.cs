using BosDAT.Core.Entities;
using BosDAT.Core.Enums;

namespace BosDAT.Core.DTOs;

public record StudentTransactionDto
{
    public Guid Id { get; init; }
    public Guid StudentId { get; init; }
    public DateOnly TransactionDate { get; init; }
    public TransactionType Type { get; init; }
    public required string Description { get; init; }
    public required string ReferenceNumber { get; init; }
    public decimal Debit { get; init; }
    public decimal Credit { get; init; }
    public decimal RunningBalance { get; init; }
    public Guid? InvoiceId { get; init; }
    public Guid? PaymentId { get; init; }
    public Guid? LedgerEntryId { get; init; }
    public DateTime CreatedAt { get; init; }
    public string CreatedByName { get; init; } = string.Empty;
}

public record RecordPaymentDto
{
    public required decimal Amount { get; init; }
    public required DateOnly PaymentDate { get; init; }
    public required PaymentMethod Method { get; init; }
    public string? Reference { get; init; }
    public string? Notes { get; init; }
}
