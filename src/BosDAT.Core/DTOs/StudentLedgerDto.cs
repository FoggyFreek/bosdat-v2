using BosDAT.Core.Entities;

namespace BosDAT.Core.DTOs;

public record StudentLedgerEntryDto
{
    public Guid Id { get; init; }
    public required string CorrectionRefName { get; init; }
    public required string Description { get; init; }
    public Guid StudentId { get; init; }
    public string StudentName { get; init; } = string.Empty;
    public Guid? CourseId { get; init; }
    public string? CourseName { get; init; }
    public decimal Amount { get; init; }
    public LedgerEntryType EntryType { get; init; }
    public LedgerEntryStatus Status { get; init; }
    public decimal AppliedAmount { get; init; }
    public decimal RemainingAmount { get; init; }
    public DateTime CreatedAt { get; init; }
    public string CreatedByName { get; init; } = string.Empty;
    public IReadOnlyList<LedgerApplicationDto> Applications { get; init; } = [];
}

public record CreateStudentLedgerEntryDto
{
    public required string Description { get; init; }
    public required Guid StudentId { get; init; }
    public Guid? CourseId { get; init; }
    public required decimal Amount { get; init; }
    public required LedgerEntryType EntryType { get; init; }
}

public record LedgerApplicationDto
{
    public Guid Id { get; init; }
    public Guid InvoiceId { get; init; }
    public string InvoiceNumber { get; init; } = string.Empty;
    public decimal AppliedAmount { get; init; }
    public DateTime AppliedAt { get; init; }
    public string AppliedByName { get; init; } = string.Empty;
}

public record StudentLedgerSummaryDto
{
    public Guid StudentId { get; init; }
    public string StudentName { get; init; } = string.Empty;
    public decimal TotalCredits { get; init; }
    public decimal TotalDebits { get; init; }
    public decimal AvailableCredit { get; init; }
    public int OpenEntryCount { get; init; }
}

public record StudentLedgerListDto
{
    public Guid Id { get; init; }
    public required string CorrectionRefName { get; init; }
    public required string Description { get; init; }
    public string StudentName { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public LedgerEntryType EntryType { get; init; }
    public LedgerEntryStatus Status { get; init; }
    public decimal RemainingAmount { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record ReverseLedgerEntryDto
{
    public required string Reason { get; init; }
}

public record ApplyCreditResultDto
{
    public Guid InvoiceId { get; init; }
    public string InvoiceNumber { get; init; } = string.Empty;
    public decimal AmountApplied { get; init; }
    public decimal RemainingBalance { get; init; }
    public IReadOnlyList<LedgerApplicationDto> Applications { get; init; } = [];
}

public record DecoupleApplicationDto
{
    public required string Reason { get; init; }
}

public record DecoupleApplicationResultDto
{
    public Guid LedgerEntryId { get; init; }
    public string CorrectionRefName { get; init; } = string.Empty;
    public Guid InvoiceId { get; init; }
    public string InvoiceNumber { get; init; } = string.Empty;
    public decimal DecoupledAmount { get; init; }
    public LedgerEntryStatus NewEntryStatus { get; init; }
    public InvoiceStatus NewInvoiceStatus { get; init; }
    public DateTime DecoupledAt { get; init; }
    public string DecoupledByName { get; init; } = string.Empty;
}
