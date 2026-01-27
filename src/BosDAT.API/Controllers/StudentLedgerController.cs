using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BosDAT.Core.DTOs;
using BosDAT.Core.Interfaces;

namespace BosDAT.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StudentLedgerController(
    IStudentLedgerService ledgerService,
    ICurrentUserService currentUserService) : ControllerBase
{
    private readonly IStudentLedgerService _ledgerService = ledgerService;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    /// <summary>
    /// Gets all ledger entries for a student.
    /// </summary>
    /// <remarks>
    /// Requires Admin or Staff role to access any student's ledger.
    /// </remarks>
    [HttpGet("student/{studentId:guid}")]
    [Authorize(Policy = "TeacherOrAdmin")]  // CRITICAL-2: Add authorization
    public async Task<ActionResult<IEnumerable<StudentLedgerEntryDto>>> GetByStudent(
        Guid studentId,
        CancellationToken cancellationToken)
    {
        var entries = await _ledgerService.GetStudentLedgerAsync(studentId, cancellationToken);
        return Ok(entries);
    }

    /// <summary>
    /// Gets the ledger summary for a student.
    /// </summary>
    /// <remarks>
    /// Requires Admin or Staff role to access any student's ledger.
    /// </remarks>
    [HttpGet("student/{studentId:guid}/summary")]
    [Authorize(Policy = "TeacherOrAdmin")]  // CRITICAL-2: Add authorization
    public async Task<ActionResult<StudentLedgerSummaryDto>> GetStudentSummary(
        Guid studentId,
        CancellationToken cancellationToken)
    {
        try
        {
            var summary = await _ledgerService.GetStudentLedgerSummaryAsync(studentId, cancellationToken);
            return Ok(summary);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Gets a single ledger entry with all its applications.
    /// </summary>
    /// <remarks>
    /// Requires Admin or Staff role to access ledger entries.
    /// </remarks>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = "TeacherOrAdmin")]  // CRITICAL-2: Add authorization
    public async Task<ActionResult<StudentLedgerEntryDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var entry = await _ledgerService.GetEntryAsync(id, cancellationToken);

        if (entry == null)
        {
            return NotFound();
        }

        return Ok(entry);
    }

    /// <summary>
    /// Creates a new ledger entry (credit or debit).
    /// </summary>
    /// <remarks>
    /// Requires Admin role. Financial entries should only be created by administrators.
    /// </remarks>
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]  // CRITICAL-2: Only admins can create financial entries
    public async Task<ActionResult<StudentLedgerEntryDto>> Create(
        [FromBody] CreateStudentLedgerEntryDto dto,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
        {
            return Unauthorized();
        }

        try
        {
            var entry = await _ledgerService.CreateEntryAsync(dto, userId.Value, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = entry.Id }, entry);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Reverses an existing entry by creating an offsetting entry.
    /// </summary>
    /// <remarks>
    /// Requires Admin role. Financial reversals should only be performed by administrators.
    /// </remarks>
    [HttpPost("{id:guid}/reverse")]
    [Authorize(Policy = "AdminOnly")]  // CRITICAL-2: Only admins can reverse entries
    public async Task<ActionResult<StudentLedgerEntryDto>> Reverse(
        Guid id,
        [FromBody] ReverseLedgerEntryDto dto,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
        {
            return Unauthorized();
        }

        // HIGH-6: Validate reason at controller level for better UX
        if (string.IsNullOrWhiteSpace(dto.Reason))
        {
            return BadRequest(new { message = "Reason is required for reversal." });
        }

        try
        {
            var reversalEntry = await _ledgerService.ReverseEntryAsync(id, dto.Reason, userId.Value, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = reversalEntry.Id }, reversalEntry);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Applies available credits to an invoice.
    /// </summary>
    /// <remarks>
    /// Requires Admin role. Credit application should only be performed by administrators.
    /// </remarks>
    [HttpPost("apply/{invoiceId:guid}")]
    [Authorize(Policy = "AdminOnly")]  // CRITICAL-2: Only admins can apply credits
    public async Task<ActionResult<ApplyCreditResultDto>> ApplyCreditsToInvoice(
        Guid invoiceId,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
        {
            return Unauthorized();
        }

        try
        {
            var result = await _ledgerService.ApplyCreditsToInvoiceAsync(invoiceId, userId.Value, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Gets the available credit balance for a student.
    /// </summary>
    /// <remarks>
    /// Requires Admin or Staff role to access any student's credit balance.
    /// </remarks>
    [HttpGet("student/{studentId:guid}/available-credit")]
    [Authorize(Policy = "TeacherOrAdmin")]  // CRITICAL-2: Add authorization
    public async Task<ActionResult<AvailableCreditDto>> GetAvailableCredit(
        Guid studentId,
        CancellationToken cancellationToken)
    {
        var availableCredit = await _ledgerService.GetAvailableCreditForStudentAsync(studentId, cancellationToken);
        return Ok(new AvailableCreditDto { AvailableCredit = availableCredit });
    }
}

/// <summary>
/// DTO for returning available credit.
/// </summary>
public record AvailableCreditDto
{
    public decimal AvailableCredit { get; init; }
}
