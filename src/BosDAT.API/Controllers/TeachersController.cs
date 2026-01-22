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
        [FromQuery] int? lessonTypeId,
        CancellationToken cancellationToken)
    {
        IQueryable<Teacher> query = _unitOfWork.Teachers.Query()
            .Include(t => t.TeacherInstruments)
                .ThenInclude(ti => ti.Instrument)
            .Include(t => t.TeacherLessonTypes)
                .ThenInclude(tlt => tlt.LessonType);

        if (activeOnly == true)
        {
            query = query.Where(t => t.IsActive);
        }

        if (instrumentId.HasValue)
        {
            query = query.Where(t => t.TeacherInstruments.Any(ti => ti.InstrumentId == instrumentId.Value));
        }

        if (lessonTypeId.HasValue)
        {
            query = query.Where(t => t.TeacherLessonTypes.Any(tlt => tlt.LessonTypeId == lessonTypeId.Value));
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
                Instruments = t.TeacherInstruments.Select(ti => ti.Instrument.Name).ToList(),
                LessonTypes = t.TeacherLessonTypes.Select(tlt => tlt.LessonType.Name).ToList()
            })
            .ToListAsync(cancellationToken);

        return Ok(teachers);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TeacherDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var teacher = await _unitOfWork.Teachers.GetWithInstrumentsAndLessonTypesAsync(id, cancellationToken);

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

        // Validate lesson types
        if (dto.LessonTypeIds.Count > 0)
        {
            var lessonTypes = await _context.LessonTypes
                .Where(lt => dto.LessonTypeIds.Contains(lt.Id))
                .ToListAsync(cancellationToken);

            // Check all lesson types exist
            if (lessonTypes.Count != dto.LessonTypeIds.Count)
            {
                return BadRequest(new { message = "One or more lesson types not found" });
            }

            // Check all lesson types are active
            var inactiveLessonTypes = lessonTypes.Where(lt => !lt.IsActive).ToList();
            if (inactiveLessonTypes.Count > 0)
            {
                return BadRequest(new { message = $"Cannot assign inactive lesson types: {string.Join(", ", inactiveLessonTypes.Select(lt => lt.Name))}" });
            }

            // Check lesson type instruments match teacher's instruments
            var mismatchedLessonTypes = lessonTypes
                .Where(lt => !dto.InstrumentIds.Contains(lt.InstrumentId))
                .ToList();
            if (mismatchedLessonTypes.Count > 0)
            {
                return BadRequest(new { message = $"Lesson types must match teacher's instruments: {string.Join(", ", mismatchedLessonTypes.Select(lt => lt.Name))}" });
            }
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

        // Add lesson types
        foreach (var lessonTypeId in dto.LessonTypeIds)
        {
            _context.TeacherLessonTypes.Add(new TeacherLessonType
            {
                TeacherId = teacher.Id,
                LessonTypeId = lessonTypeId
            });
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Reload with instruments and lesson types
        var createdTeacher = await _unitOfWork.Teachers.GetWithInstrumentsAndLessonTypesAsync(teacher.Id, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = teacher.Id }, MapToDto(createdTeacher!));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<TeacherDto>> Update(Guid id, [FromBody] UpdateTeacherDto dto, CancellationToken cancellationToken)
    {
        var teacher = await _unitOfWork.Teachers.GetWithInstrumentsAndLessonTypesAsync(id, cancellationToken);

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

        // Validate new lesson types
        if (dto.LessonTypeIds.Count > 0)
        {
            var lessonTypes = await _context.LessonTypes
                .Where(lt => dto.LessonTypeIds.Contains(lt.Id))
                .ToListAsync(cancellationToken);

            // Check all lesson types exist
            if (lessonTypes.Count != dto.LessonTypeIds.Count)
            {
                return BadRequest(new { message = "One or more lesson types not found" });
            }

            // Check all lesson types are active
            var inactiveLessonTypes = lessonTypes.Where(lt => !lt.IsActive).ToList();
            if (inactiveLessonTypes.Count > 0)
            {
                return BadRequest(new { message = $"Cannot assign inactive lesson types: {string.Join(", ", inactiveLessonTypes.Select(lt => lt.Name))}" });
            }

            // Check lesson type instruments match teacher's new instruments
            var mismatchedLessonTypes = lessonTypes
                .Where(lt => !dto.InstrumentIds.Contains(lt.InstrumentId))
                .ToList();
            if (mismatchedLessonTypes.Count > 0)
            {
                return BadRequest(new { message = $"Lesson types must match teacher's instruments: {string.Join(", ", mismatchedLessonTypes.Select(lt => lt.Name))}" });
            }
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
        var instrumentsToRemove = currentInstruments.Where(ti => !dto.InstrumentIds.Contains(ti.InstrumentId)).ToList();
        var instrumentsToAdd = dto.InstrumentIds.Where(iid => !currentInstruments.Any(ti => ti.InstrumentId == iid));

        foreach (var ti in instrumentsToRemove)
        {
            _context.TeacherInstruments.Remove(ti);
        }

        foreach (var instrumentId in instrumentsToAdd)
        {
            _context.TeacherInstruments.Add(new TeacherInstrument
            {
                TeacherId = teacher.Id,
                InstrumentId = instrumentId
            });
        }

        // Get instrument IDs being removed
        var removedInstrumentIds = instrumentsToRemove.Select(ti => ti.InstrumentId).ToHashSet();

        // Update lesson types (also cascade-remove any lesson types whose instrument was removed)
        var currentLessonTypes = teacher.TeacherLessonTypes.ToList();
        var lessonTypesToRemove = currentLessonTypes
            .Where(tlt => !dto.LessonTypeIds.Contains(tlt.LessonTypeId) || removedInstrumentIds.Contains(tlt.LessonType.InstrumentId))
            .ToList();
        var lessonTypesToAdd = dto.LessonTypeIds
            .Where(ltid => !currentLessonTypes.Any(tlt => tlt.LessonTypeId == ltid));

        foreach (var tlt in lessonTypesToRemove)
        {
            _context.TeacherLessonTypes.Remove(tlt);
        }

        foreach (var lessonTypeId in lessonTypesToAdd)
        {
            _context.TeacherLessonTypes.Add(new TeacherLessonType
            {
                TeacherId = teacher.Id,
                LessonTypeId = lessonTypeId
            });
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Reload with updated instruments and lesson types
        var updatedTeacher = await _unitOfWork.Teachers.GetWithInstrumentsAndLessonTypesAsync(id, cancellationToken);

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

    [HttpGet("{id:guid}/available-lesson-types")]
    public async Task<ActionResult<IEnumerable<LessonTypeSimpleDto>>> GetAvailableLessonTypes(
        Guid id,
        [FromQuery] string? instrumentIds,
        CancellationToken cancellationToken)
    {
        var teacher = await _unitOfWork.Teachers.GetByIdAsync(id, cancellationToken);
        if (teacher == null)
        {
            return NotFound();
        }

        // Parse instrument IDs from query string (comma-separated)
        var instrumentIdList = string.IsNullOrEmpty(instrumentIds)
            ? new List<int>()
            : instrumentIds.Split(',').Select(int.Parse).ToList();

        if (instrumentIdList.Count == 0)
        {
            return Ok(new List<LessonTypeSimpleDto>());
        }

        var lessonTypes = await _context.LessonTypes
            .Include(lt => lt.Instrument)
            .Where(lt => lt.IsActive && instrumentIdList.Contains(lt.InstrumentId))
            .OrderBy(lt => lt.Instrument.Name)
            .ThenBy(lt => lt.Name)
            .Select(lt => new LessonTypeSimpleDto
            {
                Id = lt.Id,
                Name = lt.Name,
                InstrumentId = lt.InstrumentId,
                InstrumentName = lt.Instrument.Name,
                DurationMinutes = lt.DurationMinutes,
                Type = lt.Type
            })
            .ToListAsync(cancellationToken);

        return Ok(lessonTypes);
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
            LessonTypes = teacher.TeacherLessonTypes.Select(tlt => new LessonTypeSimpleDto
            {
                Id = tlt.LessonType.Id,
                Name = tlt.LessonType.Name,
                InstrumentId = tlt.LessonType.InstrumentId,
                InstrumentName = tlt.LessonType.Instrument?.Name ?? "",
                DurationMinutes = tlt.LessonType.DurationMinutes,
                Type = tlt.LessonType.Type
            }).ToList(),
            CreatedAt = teacher.CreatedAt,
            UpdatedAt = teacher.UpdatedAt
        };
    }
}
