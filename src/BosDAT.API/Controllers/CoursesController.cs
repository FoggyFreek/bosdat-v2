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
public class CoursesController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public CoursesController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<IEnumerable<CourseListDto>>> GetSummary(
        [FromQuery] CourseStatus? status,
        [FromQuery] Guid? teacherId,
        [FromQuery] DayOfWeek? dayOfWeek,
        [FromQuery] int? roomId,
        CancellationToken cancellationToken)
    {
        IQueryable<Course> query = _unitOfWork.Courses.Query()
            .Include(c => c.Teacher)
            .Include(c => c.CourseType)
                .ThenInclude(ct => ct.Instrument)
            .Include(c => c.Room);

        if (status.HasValue)
        {
            query = query.Where(c => c.Status == status.Value);
        }

        if (teacherId.HasValue)
        {
            query = query.Where(c => c.TeacherId == teacherId.Value);
        }

        if (dayOfWeek.HasValue)
        {
            query = query.Where(c => c.DayOfWeek == dayOfWeek.Value);
        }

        if (roomId.HasValue)
        {
            query = query.Where(c => c.RoomId == roomId.Value);
        }

        var courses = await query
            .OrderBy(c => c.DayOfWeek)
            .ThenBy(c => c.StartTime)
            .Select(c => new CourseListDto
            {
                Id = c.Id,
                TeacherName = c.Teacher.FirstName + " " + c.Teacher.LastName,
                CourseTypeName = c.CourseType.Name,
                InstrumentName = c.CourseType.Instrument.Name,
                RoomName = c.Room != null ? c.Room.Name : null,
                DayOfWeek = c.DayOfWeek,
                StartTime = c.StartTime,
                EndTime = c.EndTime,
                Frequency = c.Frequency,
                WeekParity = c.WeekParity,
                Status = c.Status
            })
            .ToListAsync(cancellationToken);

        return Ok(courses);
    }

    [HttpGet("count")]
    public async Task<ActionResult<int>> GetCount(
        [FromQuery] CourseStatus? status,
        [FromQuery] Guid? teacherId,
        [FromQuery] DayOfWeek? dayOfWeek,
        [FromQuery] int? roomId,
        CancellationToken cancellationToken)
    {
        IQueryable<Course> query = _unitOfWork.Courses.Query();

        if (status.HasValue)
        {
            query = query.Where(c => c.Status == status.Value);
        }

        if (teacherId.HasValue)
        {
            query = query.Where(c => c.TeacherId == teacherId.Value);
        }

        if (dayOfWeek.HasValue)
        {
            query = query.Where(c => c.DayOfWeek == dayOfWeek.Value);
        }

        if (roomId.HasValue)
        {
            query = query.Where(c => c.RoomId == roomId.Value);
        }

        var count = await query.CountAsync(cancellationToken);

        return Ok(count);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CourseDto>>> GetAll(
        [FromQuery] CourseStatus? status,
        [FromQuery] Guid? teacherId,
        [FromQuery] DayOfWeek? dayOfWeek,
        [FromQuery] int? roomId,
        CancellationToken cancellationToken)
    {
        IQueryable<Course> query = _unitOfWork.Courses.Query()
            .Include(c => c.Teacher)
            .Include(c => c.CourseType)
                .ThenInclude(ct => ct.Instrument)
            .Include(c => c.Room)
            .Include(c => c.Enrollments);

        if (status.HasValue)
        {
            query = query.Where(c => c.Status == status.Value);
        }

        if (teacherId.HasValue)
        {
            query = query.Where(c => c.TeacherId == teacherId.Value);
        }

        if (dayOfWeek.HasValue)
        {
            query = query.Where(c => c.DayOfWeek == dayOfWeek.Value);
        }

        if (roomId.HasValue)
        {
            query = query.Where(c => c.RoomId == roomId.Value);
        }

        var courses = await query
            .OrderBy(c => c.DayOfWeek)
            .ThenBy(c => c.StartTime)
            .Select(c => new CourseDto
            {
                Id = c.Id,
                TeacherId = c.TeacherId,
                TeacherName = c.Teacher.FirstName + " " + c.Teacher.LastName,
                CourseTypeName = c.CourseType.Name,
                InstrumentName = c.CourseType.Instrument.Name,
                RoomName = c.Room != null ? c.Room.Name : null,
                DayOfWeek = c.DayOfWeek,
                StartTime = c.StartTime,
                EndTime = c.EndTime,
                Frequency = c.Frequency,
                WeekParity = c.WeekParity,
                StartDate = c.StartDate,
                EndDate = c.EndDate,
                Status = c.Status,
                Enrollments = c.Enrollments
                    .Where(e => e.Status == EnrollmentStatus.Active)
                    .Select(e => new EnrollmentDto
                    {
                        StudentName = e.Student.FirstName + " " + e.Student.LastName,
                    })
                    .ToList(),
            })
            .ToListAsync(cancellationToken);

        return Ok(courses);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CourseDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var course = await _unitOfWork.Courses.GetWithEnrollmentsAsync(id, cancellationToken);

        if (course == null)
        {
            return NotFound();
        }

        return Ok(MapToDto(course));
    }

    [HttpPost]
    [Authorize(Policy = "TeacherOrAdmin")]
    public async Task<ActionResult<CourseDto>> Create([FromBody] CreateCourseDto dto, CancellationToken cancellationToken)
    {
        var teacher = await _unitOfWork.Teachers.GetByIdAsync(dto.TeacherId, cancellationToken);
        if (teacher == null)
        {
            return BadRequest(new { message = "Teacher not found" });
        }

        var lessonType = await _unitOfWork.Repository<CourseType>().GetByIdAsync(dto.CourseTypeId, cancellationToken);
        if (lessonType == null)
        {
            return BadRequest(new { message = "Course type not found" });
        }

        var course = new Course
        {
            Id = Guid.NewGuid(),
            TeacherId = dto.TeacherId,
            CourseTypeId = dto.CourseTypeId,
            RoomId = dto.RoomId,
            DayOfWeek = dto.DayOfWeek,
            StartTime = dto.StartTime,
            EndTime = dto.EndTime,
            Frequency = dto.Frequency,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            IsWorkshop = dto.IsWorkshop,
            IsTrial = dto.IsTrial,
            Notes = dto.Notes,
            Status = CourseStatus.Active
        };

        await _unitOfWork.Courses.AddAsync(course, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var created = await _unitOfWork.Courses.GetWithEnrollmentsAsync(course.Id, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = course.Id }, MapToDto(created!));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "TeacherOrAdmin")]
    public async Task<ActionResult<CourseDto>> Update(Guid id, [FromBody] UpdateCourseDto dto, CancellationToken cancellationToken)
    {
        var course = await _unitOfWork.Courses.GetByIdAsync(id, cancellationToken);

        if (course == null)
        {
            return NotFound();
        }

        course.TeacherId = dto.TeacherId;
        course.CourseTypeId = dto.CourseTypeId;
        course.RoomId = dto.RoomId;
        course.DayOfWeek = dto.DayOfWeek;
        course.StartTime = dto.StartTime;
        course.EndTime = dto.EndTime;
        course.Frequency = dto.Frequency;
        course.StartDate = dto.StartDate;
        course.EndDate = dto.EndDate;
        course.Status = dto.Status;
        course.IsWorkshop = dto.IsWorkshop;
        course.IsTrial = dto.IsTrial;
        course.Notes = dto.Notes;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var updated = await _unitOfWork.Courses.GetWithEnrollmentsAsync(id, cancellationToken);
        return Ok(MapToDto(updated!));
    }

    [HttpPost("{id:guid}/enroll")]
    [Authorize(Policy = "TeacherOrAdmin")]
    public async Task<ActionResult<EnrollmentDto>> EnrollStudent(Guid id, [FromBody] CreateEnrollmentDto dto, CancellationToken cancellationToken)
    {
        var course = await _unitOfWork.Courses.GetByIdAsync(id, cancellationToken);
        if (course == null)
        {
            return NotFound();
        }

        var student = await _unitOfWork.Students.GetByIdAsync(dto.StudentId, cancellationToken);
        if (student == null)
        {
            return BadRequest(new { message = "Student not found" });
        }

        // Check if already enrolled
        var existing = await _unitOfWork.Repository<Enrollment>()
            .FirstOrDefaultAsync(e => e.StudentId == dto.StudentId && e.CourseId == id, cancellationToken);

        if (existing != null)
        {
            return BadRequest(new { message = "Student is already enrolled in this course" });
        }

        var enrollment = new Enrollment
        {
            Id = Guid.NewGuid(),
            StudentId = dto.StudentId,
            CourseId = id,
            DiscountPercent = dto.DiscountPercent,
            Notes = dto.Notes,
            Status = EnrollmentStatus.Active
        };

        await _unitOfWork.Repository<Enrollment>().AddAsync(enrollment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(new EnrollmentDto
        {
            Id = enrollment.Id,
            StudentId = enrollment.StudentId,
            StudentName = student.FullName,
            CourseId = enrollment.CourseId,
            EnrolledAt = enrollment.EnrolledAt,
            DiscountPercent = enrollment.DiscountPercent,
            Status = enrollment.Status,
            Notes = enrollment.Notes
        });
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var course = await _unitOfWork.Courses.GetByIdAsync(id, cancellationToken);

        if (course == null)
        {
            return NotFound();
        }

        course.Status = CourseStatus.Cancelled;
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private static CourseDto MapToDto(Course course)
    {
        return new CourseDto
        {
            Id = course.Id,
            TeacherId = course.TeacherId,
            TeacherName = course.Teacher.FullName,
            CourseTypeId = course.CourseTypeId,
            CourseTypeName = course.CourseType.Name,
            InstrumentName = course.CourseType.Instrument.Name,
            RoomId = course.RoomId,
            RoomName = course.Room?.Name,
            DayOfWeek = course.DayOfWeek,
            StartTime = course.StartTime,
            EndTime = course.EndTime,
            Frequency = course.Frequency,
            StartDate = course.StartDate,
            EndDate = course.EndDate,
            Status = course.Status,
            IsWorkshop = course.IsWorkshop,
            IsTrial = course.IsTrial,
            Notes = course.Notes,
            EnrollmentCount = course.Enrollments.Count(e => e.Status == EnrollmentStatus.Active),
            Enrollments = course.Enrollments.Select(e => new EnrollmentDto
            {
                Id = e.Id,
                StudentId = e.StudentId,
                StudentName = e.Student.FullName,
                CourseId = e.CourseId,
                EnrolledAt = e.EnrolledAt,
                DiscountPercent = e.DiscountPercent,
                Status = e.Status,
                Notes = e.Notes
            }).ToList(),
            CreatedAt = course.CreatedAt,
            UpdatedAt = course.UpdatedAt
        };
    }
}
