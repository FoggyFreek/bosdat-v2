using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Interfaces;
using BosDAT.Infrastructure.Data;

namespace BosDAT.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TeachersController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ApplicationDbContext _context;

    public TeachersController(IUnitOfWork unitOfWork, ApplicationDbContext context)
    {
        _unitOfWork = unitOfWork;
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TeacherListDto>>> GetAll(
        [FromQuery] bool? activeOnly,
        [FromQuery] int? instrumentId,
        CancellationToken cancellationToken)
    {
        IQueryable<Teacher> query = _unitOfWork.Teachers.Query()
            .Include(t => t.TeacherInstruments)
                .ThenInclude(ti => ti.Instrument);

        if (activeOnly == true)
        {
            query = query.Where(t => t.IsActive);
        }

        if (instrumentId.HasValue)
        {
            query = query.Where(t => t.TeacherInstruments.Any(ti => ti.InstrumentId == instrumentId.Value));
        }

        var teachers = await query
            .OrderBy(t => t.LastName)
            .ThenBy(t => t.FirstName)
            .Select(t => new TeacherListDto
            {
                Id = t.Id,
                FullName = t.FullName,
                Email = t.Email,
                Phone = t.Phone,
                IsActive = t.IsActive,
                Role = t.Role,
                Instruments = t.TeacherInstruments.Select(ti => ti.Instrument.Name).ToList()
            })
            .ToListAsync(cancellationToken);

        return Ok(teachers);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TeacherDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var teacher = await _unitOfWork.Teachers.GetWithInstrumentsAsync(id, cancellationToken);

        if (teacher == null)
        {
            return NotFound();
        }

        return Ok(MapToDto(teacher));
    }

    [HttpGet("{id:guid}/courses")]
    public async Task<ActionResult> GetWithCourses(Guid id, CancellationToken cancellationToken)
    {
        var teacher = await _unitOfWork.Teachers.GetWithCoursesAsync(id, cancellationToken);

        if (teacher == null)
        {
            return NotFound();
        }

        var dto = MapToDto(teacher);
        var courses = teacher.Courses.Select(c => new CourseListDto
        {
            Id = c.Id,
            TeacherName = teacher.FullName,
            LessonTypeName = c.LessonType.Name,
            InstrumentName = c.LessonType.Instrument.Name,
            RoomName = c.Room?.Name,
            DayOfWeek = c.DayOfWeek,
            StartTime = c.StartTime,
            EndTime = c.EndTime,
            Status = c.Status,
            EnrollmentCount = c.Enrollments.Count
        });

        return Ok(new { Teacher = dto, Courses = courses });
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<TeacherDto>> Create([FromBody] CreateTeacherDto dto, CancellationToken cancellationToken)
    {
        // Check for duplicate email
        var existing = await _unitOfWork.Teachers.GetByEmailAsync(dto.Email, cancellationToken);
        if (existing != null)
        {
            return BadRequest(new { message = "A teacher with this email already exists" });
        }

        var teacher = new Teacher
        {
            Id = Guid.NewGuid(),
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Prefix = dto.Prefix,
            Email = dto.Email,
            Phone = dto.Phone,
            Address = dto.Address,
            PostalCode = dto.PostalCode,
            City = dto.City,
            HourlyRate = dto.HourlyRate,
            Role = dto.Role,
            Notes = dto.Notes,
            IsActive = true
        };

        await _unitOfWork.Teachers.AddAsync(teacher, cancellationToken);

        // Add instruments
        foreach (var instrumentId in dto.InstrumentIds)
        {
            _context.TeacherInstruments.Add(new TeacherInstrument
            {
                TeacherId = teacher.Id,
                InstrumentId = instrumentId
            });
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Reload with instruments
        var createdTeacher = await _unitOfWork.Teachers.GetWithInstrumentsAsync(teacher.Id, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = teacher.Id }, MapToDto(createdTeacher!));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<TeacherDto>> Update(Guid id, [FromBody] UpdateTeacherDto dto, CancellationToken cancellationToken)
    {
        var teacher = await _unitOfWork.Teachers.GetWithInstrumentsAsync(id, cancellationToken);

        if (teacher == null)
        {
            return NotFound();
        }

        // Check for duplicate email (excluding current teacher)
        var existing = await _unitOfWork.Teachers.GetByEmailAsync(dto.Email, cancellationToken);
        if (existing != null && existing.Id != id)
        {
            return BadRequest(new { message = "A teacher with this email already exists" });
        }

        teacher.FirstName = dto.FirstName;
        teacher.LastName = dto.LastName;
        teacher.Prefix = dto.Prefix;
        teacher.Email = dto.Email;
        teacher.Phone = dto.Phone;
        teacher.Address = dto.Address;
        teacher.PostalCode = dto.PostalCode;
        teacher.City = dto.City;
        teacher.HourlyRate = dto.HourlyRate;
        teacher.IsActive = dto.IsActive;
        teacher.Role = dto.Role;
        teacher.Notes = dto.Notes;

        // Update instruments
        var currentInstruments = teacher.TeacherInstruments.ToList();
        var toRemove = currentInstruments.Where(ti => !dto.InstrumentIds.Contains(ti.InstrumentId));
        var toAdd = dto.InstrumentIds.Where(iid => !currentInstruments.Any(ti => ti.InstrumentId == iid));

        foreach (var ti in toRemove)
        {
            _context.TeacherInstruments.Remove(ti);
        }

        foreach (var instrumentId in toAdd)
        {
            _context.TeacherInstruments.Add(new TeacherInstrument
            {
                TeacherId = teacher.Id,
                InstrumentId = instrumentId
            });
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Reload with updated instruments
        var updatedTeacher = await _unitOfWork.Teachers.GetWithInstrumentsAsync(id, cancellationToken);

        return Ok(MapToDto(updatedTeacher!));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var teacher = await _unitOfWork.Teachers.GetByIdAsync(id, cancellationToken);

        if (teacher == null)
        {
            return NotFound();
        }

        // Instead of deleting, deactivate
        teacher.IsActive = false;
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private static TeacherDto MapToDto(Teacher teacher)
    {
        return new TeacherDto
        {
            Id = teacher.Id,
            FirstName = teacher.FirstName,
            LastName = teacher.LastName,
            Prefix = teacher.Prefix,
            FullName = teacher.FullName,
            Email = teacher.Email,
            Phone = teacher.Phone,
            Address = teacher.Address,
            PostalCode = teacher.PostalCode,
            City = teacher.City,
            HourlyRate = teacher.HourlyRate,
            IsActive = teacher.IsActive,
            Role = teacher.Role,
            Notes = teacher.Notes,
            Instruments = teacher.TeacherInstruments.Select(ti => new InstrumentDto
            {
                Id = ti.Instrument.Id,
                Name = ti.Instrument.Name,
                Category = ti.Instrument.Category,
                IsActive = ti.Instrument.IsActive
            }).ToList(),
            CreatedAt = teacher.CreatedAt,
            UpdatedAt = teacher.UpdatedAt
        };
    }
}
