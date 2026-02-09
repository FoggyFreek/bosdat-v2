using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Interfaces;

namespace BosDAT.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EnrollmentsController(
    IEnrollmentService enrollmentService,
    IEnrollmentPricingService enrollmentPricingService) : ControllerBase
{
    private readonly IEnrollmentPricingService _enrollmentPricingService = enrollmentPricingService;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<EnrollmentDto>>> GetAll(
        [FromQuery] Guid? studentId,
        [FromQuery] Guid? courseId,
        [FromQuery] EnrollmentStatus? status,
        CancellationToken cancellationToken)
    {
        var enrollments = await enrollmentService.GetAllAsync(studentId, courseId, status, cancellationToken);
        return Ok(enrollments);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<EnrollmentDetailDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var enrollment = await enrollmentService.GetByIdAsync(id, cancellationToken);

        if (enrollment == null)
        {
            return NotFound();
        }

        return Ok(enrollment);
    }

    [HttpGet("student/{studentId:guid}")]
    public async Task<ActionResult<IEnumerable<StudentEnrollmentDto>>> GetByStudent(Guid studentId, CancellationToken cancellationToken)
    {
        var enrollments = await enrollmentService.GetByStudentAsync(studentId, cancellationToken);

        if (!enrollments.Any())
        {
            return NotFound(new { message = "Student not found" });
        }

        return Ok(enrollments);
    }

    [HttpGet("student/{studentId:guid}/course/{courseId:guid}/pricing")]
    [Authorize(Policy = "TeacherOrAdmin")]
    public async Task<ActionResult<EnrollmentPricingDto>> GetEnrollmentPricing(
        Guid studentId,
        Guid courseId,
        CancellationToken cancellationToken)
    {
        var pricing = await enrollmentPricingService.GetEnrollmentPricingAsync(
            studentId, courseId, cancellationToken);

        if (pricing == null)
        {
            return NotFound(new { message = "Enrollment or pricing not found" });
        }

        return Ok(pricing);
    }

    [HttpPost]
    [Authorize(Policy = "TeacherOrAdmin")]
    public async Task<ActionResult<EnrollmentDto>> Create([FromBody] CreateEnrollmentDto dto, CancellationToken cancellationToken)
    {
        var (enrollment, notFound, error) = await enrollmentService.CreateAsync(dto.CourseId, dto, cancellationToken);
        if (notFound)
            return NotFound();
        if (error != null)
            return BadRequest(new { message = error });

        return Ok(enrollment);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "TeacherOrAdmin")]
    public async Task<ActionResult<EnrollmentDto>> Update(Guid id, [FromBody] UpdateEnrollmentDto dto, CancellationToken cancellationToken)
    {
        var enrollment = await enrollmentService.UpdateAsync(id, dto, cancellationToken);

        if (enrollment == null)
        {
            return NotFound();
        }

        return Ok(enrollment);
    }

    [HttpPut("{id:guid}/promote")]
    [Authorize(Policy = "TeacherOrAdmin")]
    public async Task<ActionResult<EnrollmentDto>> PromoteFromTrail(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var enrollment = await enrollmentService.PromoteFromTrailAsync(id, cancellationToken);

            if (enrollment == null)
            {
                return NotFound();
            }

            return Ok(enrollment);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "TeacherOrAdmin")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await enrollmentService.DeleteAsync(id, cancellationToken);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}
