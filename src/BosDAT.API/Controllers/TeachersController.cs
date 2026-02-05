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
        [FromQuery] Guid? CourseTypeId,
        CancellationToken cancellationToken)
    {
        IQueryable<Teacher> query = _unitOfWork.Teachers.Query()
            .Include(t => t.TeacherInstruments)
                .ThenInclude(ti => ti.Instrument)
            .Include(t => t.TeacherCourseTypes)
                .ThenInclude(tlt => tlt.CourseType);

        if (activeOnly == true)
        {
            query = query.Where(t => t.IsActive);
        }

        if (instrumentId.HasValue)
        {
            query = query.Where(t => t.TeacherInstruments.Any(ti => ti.InstrumentId == instrumentId.Value));
        }

        if (CourseTypeId.HasValue)
        {
            query = query.Where(t => t.TeacherCourseTypes.Any(tlt => tlt.CourseTypeId == CourseTypeId.Value));
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
                CourseTypes = t.TeacherCourseTypes.Select(tlt => tlt.CourseType.Name).ToList()
            })
            .ToListAsync(cancellationToken);

        return Ok(teachers);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TeacherDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var teacher = await _unitOfWork.Teachers.GetWithInstrumentsAndCourseTypesAsync(id, cancellationToken);

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
            CourseTypeName = c.CourseType.Name,
            InstrumentName = c.CourseType.Instrument.Name,
            RoomName = c.Room?.Name,
            DayOfWeek = c.DayOfWeek,
            StartTime = c.StartTime,
            EndTime = c.EndTime,
            Frequency = c.Frequency,
            WeekParity = c.WeekParity,
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
        if (dto.CourseTypeIds.Count > 0)
        {
            var CourseTypes = await _context.CourseTypes
                .Where(lt => dto.CourseTypeIds.Contains(lt.Id))
                .ToListAsync(cancellationToken);

            // Check all lesson types exist
            if (CourseTypes.Count != dto.CourseTypeIds.Count)
            {
                return BadRequest(new { message = "One or more lesson types not found" });
            }

            // Check all lesson types are active
            var inactiveCourseTypes = CourseTypes.Where(lt => !lt.IsActive).ToList();
            if (inactiveCourseTypes.Count > 0)
            {
                return BadRequest(new { message = $"Cannot assign inactive lesson types: {string.Join(", ", inactiveCourseTypes.Select(lt => lt.Name))}" });
            }

            // Check lesson type instruments match teacher's instruments
            var mismatchedCourseTypes = CourseTypes
                .Where(lt => !dto.InstrumentIds.Contains(lt.InstrumentId))
                .ToList();
            if (mismatchedCourseTypes.Count > 0)
            {
                return BadRequest(new { message = $"Lesson types must match teacher's instruments: {string.Join(", ", mismatchedCourseTypes.Select(lt => lt.Name))}" });
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
        foreach (var CourseTypeId in dto.CourseTypeIds)
        {
            _context.TeacherCourseTypes.Add(new TeacherCourseType
            {
                TeacherId = teacher.Id,
                CourseTypeId = CourseTypeId
            });
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Reload with instruments and lesson types
        var createdTeacher = await _unitOfWork.Teachers.GetWithInstrumentsAndCourseTypesAsync(teacher.Id, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = teacher.Id }, MapToDto(createdTeacher!));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<TeacherDto>> Update(Guid id, [FromBody] UpdateTeacherDto dto, CancellationToken cancellationToken)
    {
        var teacher = await _unitOfWork.Teachers.GetWithInstrumentsAndCourseTypesAsync(id, cancellationToken);

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
        if (dto.CourseTypeIds.Count > 0)
        {
            var CourseTypes = await _context.CourseTypes
                .Where(lt => dto.CourseTypeIds.Contains(lt.Id))
                .ToListAsync(cancellationToken);

            // Check all lesson types exist
            if (CourseTypes.Count != dto.CourseTypeIds.Count)
            {
                return BadRequest(new { message = "One or more lesson types not found" });
            }

            // Check all lesson types are active
            var inactiveCourseTypes = CourseTypes.Where(lt => !lt.IsActive).ToList();
            if (inactiveCourseTypes.Count > 0)
            {
                return BadRequest(new { message = $"Cannot assign inactive lesson types: {string.Join(", ", inactiveCourseTypes.Select(lt => lt.Name))}" });
            }

            // Check lesson type instruments match teacher's new instruments
            var mismatchedCourseTypes = CourseTypes
                .Where(lt => !dto.InstrumentIds.Contains(lt.InstrumentId))
                .ToList();
            if (mismatchedCourseTypes.Count > 0)
            {
                return BadRequest(new { message = $"Lesson types must match teacher's instruments: {string.Join(", ", mismatchedCourseTypes.Select(lt => lt.Name))}" });
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
        var currentCourseTypes = teacher.TeacherCourseTypes.ToList();
        var CourseTypesToRemove = currentCourseTypes
            .Where(tlt => !dto.CourseTypeIds.Contains(tlt.CourseTypeId) || removedInstrumentIds.Contains(tlt.CourseType.InstrumentId))
            .ToList();
        var CourseTypesToAdd = dto.CourseTypeIds
            .Where(ltid => !currentCourseTypes.Any(tlt => tlt.CourseTypeId == ltid));

        foreach (var tlt in CourseTypesToRemove)
        {
            _context.TeacherCourseTypes.Remove(tlt);
        }

        foreach (var CourseTypeId in CourseTypesToAdd)
        {
            _context.TeacherCourseTypes.Add(new TeacherCourseType
            {
                TeacherId = teacher.Id,
                CourseTypeId = CourseTypeId
            });
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Reload with updated instruments and lesson types
        var updatedTeacher = await _unitOfWork.Teachers.GetWithInstrumentsAndCourseTypesAsync(id, cancellationToken);

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

    [HttpGet("{id:guid}/availability")]
    public async Task<ActionResult<IEnumerable<TeacherAvailabilityDto>>> GetAvailability(
        Guid id,
        CancellationToken cancellationToken)
    {
        var teacher = await _unitOfWork.Teachers.GetByIdAsync(id, cancellationToken);
        if (teacher == null)
        {
            return NotFound();
        }

        var availability = await _unitOfWork.Teachers.GetAvailabilityAsync(id, cancellationToken);

        var dtos = availability.Select(a => new TeacherAvailabilityDto
        {
            Id = a.Id,
            DayOfWeek = a.DayOfWeek,
            FromTime = a.FromTime,
            UntilTime = a.UntilTime
        });

        return Ok(dtos);
    }

    [HttpPut("{id:guid}/availability")]
    [Authorize(Policy = "TeacherOrAdmin")]
    public async Task<ActionResult<IEnumerable<TeacherAvailabilityDto>>> UpdateAvailability(
        Guid id,
        [FromBody] List<UpdateTeacherAvailabilityDto> dtos,
        CancellationToken cancellationToken)
    {
        var teacher = await _unitOfWork.Teachers.GetWithAvailabilityAsync(id, cancellationToken);
        if (teacher == null)
        {
            return NotFound();
        }

        // Validate: max 7 entries
        if (dtos.Count > 7)
        {
            return BadRequest(new { message = "Maximum of 7 availability entries allowed (one per day)" });
        }

        // Validate: no duplicate days
        var duplicateDays = dtos.GroupBy(d => d.DayOfWeek).Where(g => g.Count() > 1).Select(g => g.Key);
        if (duplicateDays.Any())
        {
            return BadRequest(new { message = $"Duplicate days are not allowed: {string.Join(", ", duplicateDays)}" });
        }

        // Validate: time range (UntilTime >= FromTime + 1 hour, unless both 00:00 for unavailable)
        foreach (var dto in dtos)
        {
            var isUnavailable = dto.FromTime == TimeOnly.MinValue && dto.UntilTime == TimeOnly.MinValue;
            if (!isUnavailable)
            {
                var minEndTime = dto.FromTime.AddHours(1);
                if (dto.UntilTime < minEndTime)
                {
                    return BadRequest(new { message = $"End time must be at least 1 hour after start time for {dto.DayOfWeek}. Use 00:00-00:00 to mark as unavailable." });
                }
            }
        }

        // Remove existing availability
        var existingAvailability = teacher.Availability.ToList();
        foreach (var existing in existingAvailability)
        {
            _context.TeacherAvailabilities.Remove(existing);
        }

        // Add new availability
        var newAvailability = new List<TeacherAvailability>();
        foreach (var dto in dtos)
        {
            var availability = new TeacherAvailability
            {
                Id = Guid.NewGuid(),
                TeacherId = id,
                DayOfWeek = dto.DayOfWeek,
                FromTime = dto.FromTime,
                UntilTime = dto.UntilTime
            };
            newAvailability.Add(availability);
            _context.TeacherAvailabilities.Add(availability);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var resultDtos = newAvailability
            .OrderBy(a => a.DayOfWeek)
            .Select(a => new TeacherAvailabilityDto
            {
                Id = a.Id,
                DayOfWeek = a.DayOfWeek,
                FromTime = a.FromTime,
                UntilTime = a.UntilTime
            });

        return Ok(resultDtos);
    }

    [HttpGet("{id:guid}/available-lesson-types")]
    public async Task<ActionResult<IEnumerable<CourseTypeSimpleDto>>> GetAvailableCourseTypes(
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
            return Ok(new List<CourseTypeSimpleDto>());
        }

        var CourseTypes = await _context.CourseTypes
            .Include(lt => lt.Instrument)
            .Where(lt => lt.IsActive && instrumentIdList.Contains(lt.InstrumentId))
            .OrderBy(lt => lt.Instrument.Name)
            .ThenBy(lt => lt.Name)
            .Select(lt => new CourseTypeSimpleDto
            {
                Id = lt.Id,
                Name = lt.Name,
                InstrumentId = lt.InstrumentId,
                InstrumentName = lt.Instrument.Name,
                DurationMinutes = lt.DurationMinutes,
                Type = lt.Type
            })
            .ToListAsync(cancellationToken);

        return Ok(CourseTypes);
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
            CourseTypes = teacher.TeacherCourseTypes.Select(tlt => new CourseTypeSimpleDto
            {
                Id = tlt.CourseType.Id,
                Name = tlt.CourseType.Name,
                InstrumentId = tlt.CourseType.InstrumentId,
                InstrumentName = tlt.CourseType.Instrument?.Name ?? "",
                DurationMinutes = tlt.CourseType.DurationMinutes,
                Type = tlt.CourseType.Type
            }).ToList(),
            CreatedAt = teacher.CreatedAt,
            UpdatedAt = teacher.UpdatedAt
        };
    }
}
