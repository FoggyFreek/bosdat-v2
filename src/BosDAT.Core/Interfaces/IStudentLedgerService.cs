using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;

namespace BosDAT.Core.Interfaces;

public interface IStudentLedgerService
{
    /// <summary>
    /// Creates a new ledger entry for a student.
    /// </summary>
    Task<StudentLedgerEntryDto> CreateEntryAsync(CreateStudentLedgerEntryDto dto, Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Reverses an existing entry by creating an offsetting entry.
    /// </summary>
    Task<StudentLedgerEntryDto> ReverseEntryAsync(Guid entryId, string reason, Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Gets all ledger entries for a student.
    /// </summary>
    Task<IReadOnlyList<StudentLedgerEntryDto>> GetStudentLedgerAsync(Guid studentId, CancellationToken ct = default);

    /// <summary>
    /// Gets a single ledger entry with all its applications.
    /// </summary>
    Task<StudentLedgerEntryDto?> GetEntryAsync(Guid entryId, CancellationToken ct = default);

    /// <summary>
    /// Gets the ledger summary for a student.
    /// </summary>
    Task<StudentLedgerSummaryDto> GetStudentLedgerSummaryAsync(Guid studentId, CancellationToken ct = default);

    /// <summary>
    /// Applies available credits to an invoice.
    /// Returns the result of the application.
    /// </summary>
    Task<ApplyCreditResultDto> ApplyCreditsToInvoiceAsync(Guid invoiceId, Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Gets the available credit balance for a student.
    /// </summary>
    Task<decimal> GetAvailableCreditForStudentAsync(Guid studentId, CancellationToken ct = default);

    /// <summary>
    /// Generates the next correction reference name (CR-YYYY-NNNN).
    /// </summary>
    Task<string> GenerateCorrectionRefNameAsync(CancellationToken ct = default);
}
