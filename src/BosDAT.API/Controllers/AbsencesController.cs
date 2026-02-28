using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BosDAT.Core.DTOs;
using BosDAT.Core.Interfaces.Services;

namespace BosDAT.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AbsencesController(IAbsenceService absenceService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AbsenceDto>>> GetAll(CancellationToken cancellationToken)
    {
        var absences = await absenceService.GetAllAsync(cancellationToken);
        return Ok(absences);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AbsenceDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var absence = await absenceService.GetByIdAsync(id, cancellationToken);

        if (absence == null)
            return NotFound();

        return Ok(absence);
    }

    [HttpGet("student/{studentId:guid}")]
    public async Task<ActionResult<IEnumerable<AbsenceDto>>> GetByStudent(Guid studentId, CancellationToken cancellationToken)
    {
        var absences = await absenceService.GetByStudentAsync(studentId, cancellationToken);
        return Ok(absences);
    }

    [HttpGet("teacher/{teacherId:guid}")]
    public async Task<ActionResult<IEnumerable<AbsenceDto>>> GetByTeacher(Guid teacherId, CancellationToken cancellationToken)
    {
        var absences = await absenceService.GetByTeacherAsync(teacherId, cancellationToken);
        return Ok(absences);
    }

    [HttpGet("teacher-absences")]
    public async Task<ActionResult<IEnumerable<AbsenceDto>>> GetTeacherAbsencesForPeriod(
        [FromQuery] DateOnly startDate,
        [FromQuery] DateOnly endDate,
        CancellationToken cancellationToken)
    {
        var absences = await absenceService.GetTeacherAbsencesForPeriodAsync(startDate, endDate, cancellationToken);
        return Ok(absences);
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<AbsenceDto>> Create([FromBody] CreateAbsenceDto dto, CancellationToken cancellationToken)
    {
        if (dto.StudentId == null && dto.TeacherId == null)
            return BadRequest("Either StudentId or TeacherId must be provided.");

        if (dto.StudentId != null && dto.TeacherId != null)
            return BadRequest("Cannot set both StudentId and TeacherId. An absence belongs to either a student or a teacher.");

        if (dto.StartDate > dto.EndDate)
            return BadRequest("StartDate must be before or equal to EndDate.");

        var absence = await absenceService.CreateAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = absence.Id }, absence);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<AbsenceDto>> Update(Guid id, [FromBody] UpdateAbsenceDto dto, CancellationToken cancellationToken)
    {
        if (dto.StartDate > dto.EndDate)
            return BadRequest("StartDate must be before or equal to EndDate.");

        var absence = await absenceService.UpdateAsync(id, dto, cancellationToken);

        if (absence == null)
            return NotFound();

        return Ok(absence);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var success = await absenceService.DeleteAsync(id, cancellationToken);

        if (!success)
            return NotFound();

        return NoContent();
    }
}
