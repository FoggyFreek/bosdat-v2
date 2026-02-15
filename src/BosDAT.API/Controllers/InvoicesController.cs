using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Interfaces;

namespace BosDAT.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InvoicesController(
    IInvoiceService invoiceService,
    ICurrentUserService currentUserService
    ) : ControllerBase
{
    /// <summary>
    /// Gets an invoice by ID with all details including lines, payments, and billing info.
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = "TeacherOrAdmin")]
    public async Task<ActionResult<InvoiceDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var invoice = await invoiceService.GetInvoiceAsync(id, cancellationToken);

        if (invoice == null)
        {
            return NotFound();
        }

        return Ok(invoice);
    }

    /// <summary>
    /// Gets an invoice by invoice number.
    /// </summary>
    [HttpGet("number/{invoiceNumber}")]
    [Authorize(Policy = "TeacherOrAdmin")]
    public async Task<ActionResult<InvoiceDto>> GetByNumber(string invoiceNumber, CancellationToken cancellationToken)
    {
        var invoice = await invoiceService.GetByInvoiceNumberAsync(invoiceNumber, cancellationToken);

        if (invoice == null)
        {
            return NotFound();
        }

        return Ok(invoice);
    }

    /// <summary>
    /// Gets all invoices for a student.
    /// </summary>
    [HttpGet("student/{studentId:guid}")]
    [Authorize(Policy = "TeacherOrAdmin")]
    public async Task<ActionResult<IEnumerable<InvoiceListDto>>> GetByStudent(
        Guid studentId, CancellationToken cancellationToken)
    {
        var invoices = await invoiceService.GetStudentInvoicesAsync(studentId, cancellationToken);
        return Ok(invoices);
    }

    /// <summary>
    /// Gets all invoices with the specified status.
    /// </summary>
    [HttpGet("status/{status}")]
    [Authorize(Policy = "TeacherOrAdmin")]
    public async Task<ActionResult<IEnumerable<InvoiceListDto>>> GetByStatus(
        InvoiceStatus status, CancellationToken cancellationToken)
    {
        var invoices = await invoiceService.GetByStatusAsync(status, cancellationToken);
        return Ok(invoices);
    }

    /// <summary>
    /// Generates an invoice for lessons in the specified enrollment during the given period.
    /// </summary>
    [HttpPost("generate")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<InvoiceDto>> Generate(
        [FromBody] GenerateInvoiceDto dto, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        if (userId == null)
        {
            return Unauthorized();
        }

        try
        {
            var invoice = await invoiceService.GenerateInvoiceAsync(dto, userId.Value, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = invoice.Id }, invoice);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Generates invoices for all active enrollments matching the specified period type.
    /// </summary>
    [HttpPost("generate-batch")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<IEnumerable<InvoiceDto>>> GenerateBatch(
        [FromBody] GenerateBatchInvoicesDto dto, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        if (userId == null)
        {
            return Unauthorized();
        }

        try
        {
            var invoices = await invoiceService.GenerateBatchInvoicesAsync(dto, userId.Value, cancellationToken);
            return Ok(invoices);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Recalculates an unpaid invoice based on current lessons and ledger corrections.
    /// </summary>
    [HttpPost("{id:guid}/recalculate")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<InvoiceDto>> Recalculate(Guid id, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        if (userId == null)
        {
            return Unauthorized();
        }

        try
        {
            var invoice = await invoiceService.RecalculateInvoiceAsync(id, userId.Value, cancellationToken);
            return Ok(invoice);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Applies a specific ledger correction to an unpaid invoice.
    /// </summary>
    [HttpPost("{invoiceId:guid}/apply-correction")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<InvoiceDto>> ApplyLedgerCorrection(
        Guid invoiceId,
        [FromBody] ApplyLedgerCorrectionDto dto,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        if (userId == null)
        {
            return Unauthorized();
        }

        if (dto.Amount <= 0)
        {
            return BadRequest(new { message = "Amount must be greater than zero." });
        }

        try
        {
            var invoice = await invoiceService.ApplyLedgerCorrectionAsync(
                invoiceId, dto.LedgerEntryId, dto.Amount, userId.Value, cancellationToken);
            return Ok(invoice);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Gets school billing information from settings.
    /// </summary>
    [HttpGet("school-billing-info")]
    [Authorize(Policy = "TeacherOrAdmin")]
    public async Task<ActionResult<SchoolBillingInfoDto>> GetSchoolBillingInfo(CancellationToken cancellationToken)
    {
        var info = await invoiceService.GetSchoolBillingInfoAsync(cancellationToken);
        return Ok(info);
    }

    /// <summary>
    /// Gets invoice data formatted for printing/PDF generation.
    /// Includes all necessary information: invoice details, school info, and billing contact.
    /// </summary>
    [HttpGet("{id:guid}/print")]
    [Authorize(Policy = "TeacherOrAdmin")]
    public async Task<ActionResult<InvoicePrintDto>> GetForPrint(Guid id, CancellationToken cancellationToken)
    {
        var invoice = await invoiceService.GetInvoiceAsync(id, cancellationToken);
        if (invoice == null)
        {
            return NotFound();
        }

        var schoolInfo = await invoiceService.GetSchoolBillingInfoAsync(cancellationToken);

        return Ok(new InvoicePrintDto
        {
            Invoice = invoice,
            SchoolInfo = schoolInfo
        });
    }

    /// <summary>
    /// Records a payment against an invoice and creates a ledger transaction.
    /// </summary>
    [HttpPost("{invoiceId:guid}/payments")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<PaymentDto>> RecordPayment(
        Guid invoiceId,
        [FromBody] RecordPaymentDto dto,
        CancellationToken ct)
    {
        var userId = currentUserService.UserId;
        if (userId == null)
        {
            return Unauthorized();
        }
        
        if (dto.Amount <= 0)
        {
            return BadRequest(new { message = "Payment amount must be greater than zero." });
        }

        try
        {
            var payment = await invoiceService.RecordPaymentandLedgerTransaction(invoiceId, userId.Value, dto, ct);
            return CreatedAtAction(nameof(GetPayments), new { invoiceId }, payment);
        }
        catch
        {
            throw;
        }
    }

    /// <summary>
    /// Gets all payments for an invoice.
    /// </summary>
    [HttpGet("{invoiceId:guid}/payments")]
    [Authorize(Policy = "TeacherOrAdmin")]
    public async Task<ActionResult<IEnumerable<PaymentDto>>> GetPayments(
        Guid invoiceId, CancellationToken cancellationToken)
    {
        try
        {
            var paymentDtos = await invoiceService.GetPaymentsAsync(invoiceId, cancellationToken);
            return Ok(paymentDtos);
        }
        catch
        {
            return BadRequest(new { message = "Payments for invoice not found" });
        }
        
    }
}

/// <summary>
/// DTO for applying a ledger correction to an invoice.
/// </summary>
public record ApplyLedgerCorrectionDto
{
    public Guid LedgerEntryId { get; init; }
    public decimal Amount { get; init; }
}

/// <summary>
/// DTO for invoice print/PDF data containing both invoice and school info.
/// </summary>
public record InvoicePrintDto
{
    public required InvoiceDto Invoice { get; init; }
    public required SchoolBillingInfoDto SchoolInfo { get; init; }
}
