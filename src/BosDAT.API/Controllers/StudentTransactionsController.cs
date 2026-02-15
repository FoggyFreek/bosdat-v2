using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BosDAT.Core.DTOs;
using BosDAT.Core.Interfaces;

namespace BosDAT.API.Controllers;

[ApiController]
[Route("api/students/{studentId:guid}/transactions")]
[Authorize]
public class StudentTransactionsController(
    IStudentTransactionService transactionService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "TeacherOrAdmin")]
    public async Task<ActionResult<StudentLedgerViewDto>> GetStudentLedger(
        Guid studentId, CancellationToken cancellationToken)
    {
        try
        {
            var ledger = await transactionService.GetStudentLedgerAsync(studentId, cancellationToken);
            return Ok(ledger);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet("balance")]
    [Authorize(Policy = "TeacherOrAdmin")]
    public async Task<ActionResult<object>> GetStudentBalance(
        Guid studentId, CancellationToken cancellationToken)
    {
        var balance = await transactionService.GetStudentBalanceAsync(studentId, cancellationToken);
        return Ok(new { balance });
    }
}
