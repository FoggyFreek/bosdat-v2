using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Interfaces;

namespace BosDAT.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StudentsController(
    IUnitOfWork unitOfWork,
    IDuplicateDetectionService duplicateDetectionService,
    IRegistrationFeeService registrationFeeService) : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IDuplicateDetectionService _duplicateDetectionService = duplicateDetectionService;
    private readonly IRegistrationFeeService _registrationFeeService = registrationFeeService;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<StudentListDto>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] StudentStatus? status,
        CancellationToken cancellationToken)
    {
        var query = _unitOfWork.Students.Query();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLower();
            query = query.Where(s =>
                s.FirstName.ToLower().Contains(term) ||
                s.LastName.ToLower().Contains(term) ||
                s.Email.ToLower().Contains(term));
        }

        if (status.HasValue)
        {
            query = query.Where(s => s.Status == status.Value);
        }

        var students = await query
            .OrderBy(s => s.LastName)
            .ThenBy(s => s.FirstName)
            .Select(s => new StudentListDto
            {
                Id = s.Id,
                FullName = s.FullName,
                Email = s.Email,
                Phone = s.Phone,
                Status = s.Status,
                EnrolledAt = s.EnrolledAt
            })
            .ToListAsync(cancellationToken);

        return Ok(students);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<StudentDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var student = await _unitOfWork.Students.GetByIdAsync(id, cancellationToken);

        if (student == null)
        {
            return NotFound();
        }

        return Ok(MapToDto(student));
    }

    [HttpGet("{id:guid}/enrollments")]
    public async Task<ActionResult<StudentDto>> GetWithEnrollments(Guid id, CancellationToken cancellationToken)
    {
        var student = await _unitOfWork.Students.GetWithEnrollmentsAsync(id, cancellationToken);

        if (student == null)
        {
            return NotFound();
        }

        var dto = MapToDto(student);
        return Ok(new
        {
            Student = dto,
            Enrollments = student.Enrollments.Select(e => new EnrollmentDto
            {
                Id = e.Id,
                StudentId = e.StudentId,
                StudentName = student.FullName,
                CourseId = e.CourseId,
                EnrolledAt = e.EnrolledAt,
                DiscountPercent = e.DiscountPercent,
                Status = e.Status,
                Notes = e.Notes
            })
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
        // Check for duplicate email
        var existing = await _unitOfWork.Students.GetByEmailAsync(dto.Email, cancellationToken);
        if (existing != null)
        {
            return BadRequest(new { message = "A student with this email already exists" });
        }

        var student = new Student
        {
            Id = Guid.NewGuid(),
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Prefix = dto.Prefix,
            Email = dto.Email,
            Phone = dto.Phone,
            PhoneAlt = dto.PhoneAlt,
            Address = dto.Address,
            PostalCode = dto.PostalCode,
            City = dto.City,
            DateOfBirth = dto.DateOfBirth,
            Gender = dto.Gender,
            Status = dto.Status,
            EnrolledAt = DateTime.UtcNow,
            BillingContactName = dto.BillingContactName,
            BillingContactEmail = dto.BillingContactEmail,
            BillingContactPhone = dto.BillingContactPhone,
            BillingAddress = dto.BillingAddress,
            BillingPostalCode = dto.BillingPostalCode,
            BillingCity = dto.BillingCity,
            AutoDebit = dto.AutoDebit,
            Notes = dto.Notes
        };

        await _unitOfWork.Students.AddAsync(student, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = student.Id }, MapToDto(student));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<StudentDto>> Update(Guid id, [FromBody] UpdateStudentDto dto, CancellationToken cancellationToken)
    {
        var student = await _unitOfWork.Students.GetByIdAsync(id, cancellationToken);

        if (student == null)
        {
            return NotFound();
        }

        // Check for duplicate email (excluding current student)
        var existing = await _unitOfWork.Students.GetByEmailAsync(dto.Email, cancellationToken);
        if (existing != null && existing.Id != id)
        {
            return BadRequest(new { message = "A student with this email already exists" });
        }

        student.FirstName = dto.FirstName;
        student.LastName = dto.LastName;
        student.Prefix = dto.Prefix;
        student.Email = dto.Email;
        student.Phone = dto.Phone;
        student.PhoneAlt = dto.PhoneAlt;
        student.Address = dto.Address;
        student.PostalCode = dto.PostalCode;
        student.City = dto.City;
        student.DateOfBirth = dto.DateOfBirth;
        student.Gender = dto.Gender;
        student.Status = dto.Status;
        student.BillingContactName = dto.BillingContactName;
        student.BillingContactEmail = dto.BillingContactEmail;
        student.BillingContactPhone = dto.BillingContactPhone;
        student.BillingAddress = dto.BillingAddress;
        student.BillingPostalCode = dto.BillingPostalCode;
        student.BillingCity = dto.BillingCity;
        student.AutoDebit = dto.AutoDebit;
        student.Notes = dto.Notes;

        await _unitOfWork.Students.UpdateAsync(student, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(MapToDto(student));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var student = await _unitOfWork.Students.GetByIdAsync(id, cancellationToken);

        if (student == null)
        {
            return NotFound();
        }

        await _unitOfWork.Students.DeleteAsync(student, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpGet("{id:guid}/registration-fee")]
    public async Task<ActionResult<RegistrationFeeStatusDto>> GetRegistrationFeeStatus(Guid id, CancellationToken cancellationToken)
    {
        var student = await _unitOfWork.Students.GetByIdAsync(id, cancellationToken);

        if (student == null)
        {
            return NotFound();
        }

        var feeStatus = await _registrationFeeService.GetFeeStatusAsync(id, cancellationToken);
        return Ok(feeStatus);
    }

    private static StudentDto MapToDto(Student student)
    {
        return new StudentDto
        {
            Id = student.Id,
            FirstName = student.FirstName,
            LastName = student.LastName,
            Prefix = student.Prefix,
            FullName = student.FullName,
            Email = student.Email,
            Phone = student.Phone,
            PhoneAlt = student.PhoneAlt,
            Address = student.Address,
            PostalCode = student.PostalCode,
            City = student.City,
            DateOfBirth = student.DateOfBirth,
            Gender = student.Gender,
            Status = student.Status,
            EnrolledAt = student.EnrolledAt,
            BillingContactName = student.BillingContactName,
            BillingContactEmail = student.BillingContactEmail,
            BillingContactPhone = student.BillingContactPhone,
            BillingAddress = student.BillingAddress,
            BillingPostalCode = student.BillingPostalCode,
            BillingCity = student.BillingCity,
            AutoDebit = student.AutoDebit,
            Notes = student.Notes,
            RegistrationFeePaidAt = student.RegistrationFeePaidAt,
            CreatedAt = student.CreatedAt,
            UpdatedAt = student.UpdatedAt
        };
    }
}
