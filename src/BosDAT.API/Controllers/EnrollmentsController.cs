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
public class EnrollmentsController(
    IUnitOfWork unitOfWork,
    IScheduleConflictService scheduleConflictService,
    IRegistrationFeeService registrationFeeService,
    IEnrollmentPricingService enrollmentPricingService) : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IScheduleConflictService _scheduleConflictService = scheduleConflictService;
    private readonly IRegistrationFeeService _registrationFeeService = registrationFeeService;
    private readonly IEnrollmentPricingService _enrollmentPricingService = enrollmentPricingService;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<EnrollmentDto>>> GetAll(
        [FromQuery] Guid? studentId,
        [FromQuery] Guid? courseId,
        [FromQuery] EnrollmentStatus? status,
        CancellationToken cancellationToken)
    {
        IQueryable<Enrollment> query = _unitOfWork.Repository<Enrollment>().Query()
            .Include(e => e.Student)
            .Include(e => e.Course)
                .ThenInclude(c => c.CourseType)
                    .ThenInclude(lt => lt.Instrument)
            .Include(e => e.Course.Teacher);

        if (studentId.HasValue)
        {
            query = query.Where(e => e.StudentId == studentId.Value);
        }

        if (courseId.HasValue)
        {
            query = query.Where(e => e.CourseId == courseId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(e => e.Status == status.Value);
        }

        var enrollments = await query
            .OrderByDescending(e => e.EnrolledAt)
            .Select(e => new EnrollmentDto
            {
                Id = e.Id,
                StudentId = e.StudentId,
                StudentName = e.Student.FirstName + " " + e.Student.LastName,
                CourseId = e.CourseId,
                EnrolledAt = e.EnrolledAt,
                DiscountPercent = e.DiscountPercent,
                DiscountType = e.DiscountType,
                Status = e.Status,
                Notes = e.Notes
            })
            .ToListAsync(cancellationToken);

        return Ok(enrollments);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<EnrollmentDetailDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var enrollment = await _unitOfWork.Repository<Enrollment>().Query()
            .Where(e => e.Id == id)
            .Include(e => e.Student)
            .Include(e => e.Course)
                .ThenInclude(c => c.CourseType)
                    .ThenInclude(lt => lt.Instrument)
            .Include(e => e.Course.Teacher)
            .Include(e => e.Course.Room)
            .FirstOrDefaultAsync(cancellationToken);

        if (enrollment == null)
        {
            return NotFound();
        }

        return Ok(new EnrollmentDetailDto
        {
            Id = enrollment.Id,
            StudentId = enrollment.StudentId,
            StudentName = enrollment.Student.FullName,
            CourseId = enrollment.CourseId,
            CourseName = $"{enrollment.Course.CourseType.Name} - {enrollment.Course.Teacher.FullName}",
            InstrumentName = enrollment.Course.CourseType.Instrument.Name,
            TeacherName = enrollment.Course.Teacher.FullName,
            RoomName = enrollment.Course.Room?.Name,
            DayOfWeek = enrollment.Course.DayOfWeek,
            StartTime = enrollment.Course.StartTime,
            EndTime = enrollment.Course.EndTime,
            EnrolledAt = enrollment.EnrolledAt,
            DiscountPercent = enrollment.DiscountPercent,
            Status = enrollment.Status,
            Notes = enrollment.Notes
        });
    }

    [HttpGet("student/{studentId:guid}")]
    public async Task<ActionResult<IEnumerable<StudentEnrollmentDto>>> GetByStudent(Guid studentId, CancellationToken cancellationToken)
    {
        var student = await _unitOfWork.Students.GetByIdAsync(studentId, cancellationToken);
        if (student == null)
        {
            return NotFound(new { message = "Student not found" });
        }

        var enrollments = await _unitOfWork.Repository<Enrollment>().Query()
            .Where(e => e.StudentId == studentId)
            .Include(e => e.Course)
                .ThenInclude(c => c.CourseType)
                    .ThenInclude(lt => lt.Instrument)
            .Include(e => e.Course.Teacher)
            .Include(e => e.Course.Room)
            .OrderByDescending(e => e.EnrolledAt)
            .Select(e => new StudentEnrollmentDto
            {
                Id = e.Id,
                CourseId = e.CourseId,
                InstrumentName = e.Course.CourseType.Instrument.Name,
                CourseTypeName = e.Course.CourseType.Name,
                TeacherName = e.Course.Teacher.FirstName + " " + e.Course.Teacher.LastName,
                RoomName = e.Course.Room != null ? e.Course.Room.Name : null,
                DayOfWeek = e.Course.DayOfWeek,
                StartTime = e.Course.StartTime,
                EndTime = e.Course.EndTime,
                EnrolledAt = e.EnrolledAt,
                DiscountPercent = e.DiscountPercent,
                Status = e.Status
            })
            .ToListAsync(cancellationToken);

        return Ok(enrollments);
    }

    [HttpGet("student/{studentId:guid}/course/{courseId:guid}/pricing")]
    [Authorize(Policy = "TeacherOrAdmin")]
    public async Task<ActionResult<EnrollmentPricingDto>> GetEnrollmentPricing(
        Guid studentId,
        Guid courseId,
        CancellationToken cancellationToken)
    {
        var pricing = await _enrollmentPricingService.GetEnrollmentPricingAsync(
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
        var student = await _unitOfWork.Students.GetByIdAsync(dto.StudentId, cancellationToken);
        if (student == null)
        {
            return BadRequest(new { message = "Student not found" });
        }

        var course = await _unitOfWork.Courses.GetByIdAsync(dto.CourseId, cancellationToken);
        if (course == null)
        {
            return BadRequest(new { message = "Course not found" });
        }

        // Check for schedule conflicts
        var conflictCheck = await _scheduleConflictService.HasConflictAsync(dto.StudentId, dto.CourseId);
        if (conflictCheck.HasConflict)
        {
            var conflictDetails = conflictCheck.ConflictingCourses
                .Select(ConflictingCourseDto.FromConflict);

            return BadRequest(new
            {
                message = "Schedule conflict detected. This course overlaps with existing enrollment(s).",
                conflicts = conflictDetails
            });
        }

        // Check if already enrolled
        var existing = await _unitOfWork.Repository<Enrollment>()
            .FirstOrDefaultAsync(e => e.StudentId == dto.StudentId && e.CourseId == dto.CourseId, cancellationToken);

        if (existing != null)
        {
            return BadRequest(new { message = "Student is already enrolled in this course" });
        }

        // Auto-set enrollment status based on course type
        var enrollmentStatus = course.IsTrial ? EnrollmentStatus.Trail : EnrollmentStatus.Active;

        var enrollment = new Enrollment
        {
            Id = Guid.NewGuid(),
            StudentId = dto.StudentId,
            CourseId = dto.CourseId,
            DiscountPercent = dto.DiscountPercent,
            DiscountType = dto.DiscountType,
            Notes = dto.Notes,
            Status = enrollmentStatus
        };

        await _unitOfWork.Repository<Enrollment>().AddAsync(enrollment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Apply registration fee for non-trial courses if student is eligible
        if (!course.IsTrial && await _registrationFeeService.IsStudentEligibleForFeeAsync(dto.StudentId, cancellationToken))
        {
            await _registrationFeeService.ApplyRegistrationFeeAsync(dto.StudentId, cancellationToken);
        }

        return CreatedAtAction(nameof(GetById), new { id = enrollment.Id }, new EnrollmentDto
        {
            Id = enrollment.Id,
            StudentId = enrollment.StudentId,
            StudentName = student.FullName,
            CourseId = enrollment.CourseId,
            EnrolledAt = enrollment.EnrolledAt,
            DiscountPercent = enrollment.DiscountPercent,
            DiscountType = enrollment.DiscountType,
            Status = enrollment.Status,
            Notes = enrollment.Notes
        });
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "TeacherOrAdmin")]
    public async Task<ActionResult<EnrollmentDto>> Update(Guid id, [FromBody] UpdateEnrollmentDto dto, CancellationToken cancellationToken)
    {
        var enrollment = await _unitOfWork.Repository<Enrollment>().Query()
            .Where(e => e.Id == id)
            .Include(e => e.Student)
            .FirstOrDefaultAsync(cancellationToken);

        if (enrollment == null)
        {
            return NotFound();
        }

        enrollment.DiscountPercent = dto.DiscountPercent;
        enrollment.Status = dto.Status;
        enrollment.Notes = dto.Notes;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(new EnrollmentDto
        {
            Id = enrollment.Id,
            StudentId = enrollment.StudentId,
            StudentName = enrollment.Student.FullName,
            CourseId = enrollment.CourseId,
            EnrolledAt = enrollment.EnrolledAt,
            DiscountPercent = enrollment.DiscountPercent,
            DiscountType = enrollment.DiscountType,
            Status = enrollment.Status,
            Notes = enrollment.Notes
        });
    }

    [HttpPut("{id:guid}/promote")]
    [Authorize(Policy = "TeacherOrAdmin")]
    public async Task<ActionResult<EnrollmentDto>> PromoteFromTrail(Guid id, CancellationToken cancellationToken)
    {
        var enrollment = await _unitOfWork.Repository<Enrollment>().Query()
            .Where(e => e.Id == id)
            .Include(e => e.Student)
            .FirstOrDefaultAsync(cancellationToken);

        if (enrollment == null)
        {
            return NotFound();
        }

        if (enrollment.Status != EnrollmentStatus.Trail)
        {
            return BadRequest(new { message = "Only trial enrollments can be promoted" });
        }

        enrollment.Status = EnrollmentStatus.Active;
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Apply registration fee if student is eligible
        if (await _registrationFeeService.IsStudentEligibleForFeeAsync(enrollment.StudentId, cancellationToken))
        {
            await _registrationFeeService.ApplyRegistrationFeeAsync(enrollment.StudentId, cancellationToken);
        }

        return Ok(new EnrollmentDto
        {
            Id = enrollment.Id,
            StudentId = enrollment.StudentId,
            StudentName = enrollment.Student.FullName,
            CourseId = enrollment.CourseId,
            EnrolledAt = enrollment.EnrolledAt,
            DiscountPercent = enrollment.DiscountPercent,
            DiscountType = enrollment.DiscountType,
            Status = enrollment.Status,
            Notes = enrollment.Notes
        });
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "TeacherOrAdmin")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var enrollment = await _unitOfWork.Repository<Enrollment>().GetByIdAsync(id, cancellationToken);

        if (enrollment == null)
        {
            return NotFound();
        }

        enrollment.Status = EnrollmentStatus.Withdrawn;
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return NoContent();
    }
}

public record EnrollmentDetailDto
{
    public Guid Id { get; init; }
    public Guid StudentId { get; init; }
    public string StudentName { get; init; } = string.Empty;
    public Guid CourseId { get; init; }
    public string CourseName { get; init; } = string.Empty;
    public string InstrumentName { get; init; } = string.Empty;
    public string TeacherName { get; init; } = string.Empty;
    public string? RoomName { get; init; }
    public DayOfWeek DayOfWeek { get; init; }
    public TimeOnly StartTime { get; init; }
    public TimeOnly EndTime { get; init; }
    public DateTime EnrolledAt { get; init; }
    public decimal DiscountPercent { get; init; }
    public EnrollmentStatus Status { get; init; }
    public string? Notes { get; init; }
}

public record StudentEnrollmentDto
{
    public Guid Id { get; init; }
    public Guid CourseId { get; init; }
    public string InstrumentName { get; init; } = string.Empty;
    public string CourseTypeName { get; init; } = string.Empty;
    public string TeacherName { get; init; } = string.Empty;
    public string? RoomName { get; init; }
    public DayOfWeek DayOfWeek { get; init; }
    public TimeOnly StartTime { get; init; }
    public TimeOnly EndTime { get; init; }
    public DateTime EnrolledAt { get; init; }
    public decimal DiscountPercent { get; init; }
    public EnrollmentStatus Status { get; init; }
}
