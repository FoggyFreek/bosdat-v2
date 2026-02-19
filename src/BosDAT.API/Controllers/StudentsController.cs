using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Interfaces;

namespace BosDAT.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StudentsController(
    IStudentService studentService,
    IDuplicateDetectionService duplicateDetectionService) : ControllerBase
{
    private readonly IStudentService _studentService = studentService;
    private readonly IDuplicateDetectionService _duplicateDetectionService = duplicateDetectionService;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<StudentListDto>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] StudentStatus? status,
        CancellationToken cancellationToken)
    {
        var students = await _studentService.GetAllAsync(search, status, cancellationToken);
        return Ok(students);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<StudentDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var student = await _studentService.GetByIdAsync(id, cancellationToken);

        if (student == null)
        {
            return NotFound();
        }

        return Ok(student);
    }

    [HttpGet("{id:guid}/enrollments")]
    public async Task<ActionResult<StudentDto>> GetWithEnrollments(Guid id, CancellationToken cancellationToken)
    {
        var (student, enrollments, notFound) = await _studentService.GetWithEnrollmentsAsync(id, cancellationToken);

        if (notFound)
        {
            return NotFound();
        }

        return Ok(new
        {
            Student = student,
            Enrollments = enrollments
        });
    }

    [HttpPost("check-duplicates")]
    public async Task<ActionResult<DuplicateCheckResultDto>> CheckDuplicates(
        [FromBody] CheckDuplicatesDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _duplicateDetectionService.CheckForDuplicatesAsync(dto, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<StudentDto>> Create([FromBody] CreateStudentDto dto, CancellationToken cancellationToken)
    {
        var (student, error) = await _studentService.CreateAsync(dto, cancellationToken);

        if (error != null)
        {
            return BadRequest(new { message = error });
        }

        return CreatedAtAction(nameof(GetById), new { id = student!.Id }, student);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<StudentDto>> Update(Guid id, [FromBody] UpdateStudentDto dto, CancellationToken cancellationToken)
    {
        var (student, error, notFound) = await _studentService.UpdateAsync(id, dto, cancellationToken);

        if (notFound)
        {
            return NotFound();
        }

        if (error != null)
        {
            return BadRequest(new { message = error });
        }

        return Ok(student);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var (_, notFound) = await _studentService.DeleteAsync(id, cancellationToken);

        if (notFound)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpGet("{id:guid}/registration-fee")]
    public async Task<ActionResult<RegistrationFeeStatusDto>> GetRegistrationFeeStatus(Guid id, CancellationToken cancellationToken)
    {
        var (feeStatus, notFound) = await _studentService.GetRegistrationFeeStatusAsync(id, cancellationToken);

        if (notFound)
        {
            return NotFound();
        }

        return Ok(feeStatus);
    }

    [HttpGet("{id:guid}/has-active-enrollments")]
    public async Task<ActionResult<bool>> HasActiveEnrollments(Guid id, CancellationToken cancellationToken)
    {
        var (hasActiveEnrollments, notFound) = await _studentService.HasActiveEnrollmentsAsync(id, cancellationToken);

        if (notFound)
        {
            return NotFound();
        }

        return Ok(hasActiveEnrollments);
    }
}
