using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Enums;
using BosDAT.Core.Interfaces;

namespace BosDAT.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LessonsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILessonGenerationService _lessonGenerationService;

    public LessonsController(IUnitOfWork unitOfWork, ILessonGenerationService lessonGenerationService)
    {
        _unitOfWork = unitOfWork;
        _lessonGenerationService = lessonGenerationService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<LessonDto>>> GetAll(
        [FromQuery] DateOnly? startDate,
        [FromQuery] DateOnly? endDate,
        [FromQuery] Guid? teacherId,
        [FromQuery] Guid? studentId,
        [FromQuery] Guid? courseId,
        [FromQuery] int? roomId,
        [FromQuery] LessonStatus? status,
        [FromQuery] int? top,
        CancellationToken cancellationToken)
    {
        IQueryable<Lesson> query = _unitOfWork.Lessons.Query()
            .Include(l => l.Course)
                .ThenInclude(c => c.CourseType)
                    .ThenInclude(lt => lt.Instrument)
            .Include(l => l.Student)
            .Include(l => l.Teacher)
            .Include(l => l.Room);

        if (startDate.HasValue)
        {
            query = query.Where(l => l.ScheduledDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(l => l.ScheduledDate <= endDate.Value);
        }

        if (teacherId.HasValue)
        {
            query = query.Where(l => l.TeacherId == teacherId.Value);
        }

        if (studentId.HasValue)
        {
            query = query.Where(l => l.StudentId == studentId.Value);
        }

        if (courseId.HasValue)
        {
            query = query.Where(l => l.CourseId == courseId.Value);
        }

        if (roomId.HasValue)
        {
            query = query.Where(l => l.RoomId == roomId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(l => l.Status == status.Value);
        }

        IQueryable<LessonDto> orderedQuery;

        if (top.HasValue)
        {
            orderedQuery = query
                .OrderByDescending(l => l.ScheduledDate)
                .ThenByDescending(l => l.StartTime)
                .Take(top.Value)
                .Select(l => MapToDto(l));
        }
        else
        {
            orderedQuery = query
                .OrderBy(l => l.ScheduledDate)
                .ThenBy(l => l.StartTime)
                .Select(l => MapToDto(l));
        }

        var lessons = await orderedQuery.ToListAsync(cancellationToken);

        return Ok(lessons);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<LessonDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var lesson = await _unitOfWork.Lessons.Query()
            .Where(l => l.Id == id)
            .Include(l => l.Course)
                .ThenInclude(c => c.CourseType)
                    .ThenInclude(lt => lt.Instrument)
            .Include(l => l.Student)
            .Include(l => l.Teacher)
            .Include(l => l.Room)
            .FirstOrDefaultAsync(cancellationToken);

        if (lesson == null)
        {
            return NotFound();
        }

        return Ok(MapToDto(lesson));
    }

    [HttpGet("student/{studentId:guid}")]
    public async Task<ActionResult<IEnumerable<LessonDto>>> GetByStudent(Guid studentId, CancellationToken cancellationToken)
    {
        var lessons = await _unitOfWork.Lessons.GetByStudentAsync(studentId, cancellationToken);

        return Ok(lessons.Select(l => new LessonDto
        {
            Id = l.Id,
            CourseId = l.CourseId,
            StudentId = l.StudentId,
            StudentName = l.Student?.FullName,
            TeacherId = l.TeacherId,
            TeacherName = l.Teacher.FullName,
            RoomId = l.RoomId,
            RoomName = l.Room?.Name,
            CourseTypeName = l.Course.CourseType.Name,
            InstrumentName = l.Course.CourseType.Instrument.Name,
            ScheduledDate = l.ScheduledDate,
            StartTime = l.StartTime,
            EndTime = l.EndTime,
            Status = l.Status,
            CancellationReason = l.CancellationReason,
            IsInvoiced = l.IsInvoiced,
            IsPaidToTeacher = l.IsPaidToTeacher,
            Notes = l.Notes,
            CreatedAt = l.CreatedAt,
            UpdatedAt = l.UpdatedAt
        }));
    }

    [HttpPost]
    [Authorize(Policy = "TeacherOrAdmin")]
    public async Task<ActionResult<LessonDto>> Create([FromBody] CreateLessonDto dto, CancellationToken cancellationToken)
    {
        var course = await _unitOfWork.Courses.GetByIdAsync(dto.CourseId, cancellationToken);
        if (course == null)
        {
            return BadRequest(new { message = "Course not found" });
        }

        var teacher = await _unitOfWork.Teachers.GetByIdAsync(dto.TeacherId, cancellationToken);
        if (teacher == null)
        {
            return BadRequest(new { message = "Teacher not found" });
        }

        if (dto.StudentId.HasValue)
        {
            var student = await _unitOfWork.Students.GetByIdAsync(dto.StudentId.Value, cancellationToken);
            if (student == null)
            {
                return BadRequest(new { message = "Student not found" });
            }
        }

        var lesson = new Lesson
        {
            Id = Guid.NewGuid(),
            CourseId = dto.CourseId,
            StudentId = dto.StudentId,
            TeacherId = dto.TeacherId,
            RoomId = dto.RoomId,
            ScheduledDate = dto.ScheduledDate,
            StartTime = dto.StartTime,
            EndTime = dto.EndTime,
            Status = LessonStatus.Scheduled,
            Notes = dto.Notes
        };

        await _unitOfWork.Lessons.AddAsync(lesson, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = lesson.Id }, await GetLessonDto(lesson.Id, cancellationToken));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "TeacherOrAdmin")]
    public async Task<ActionResult<LessonDto>> Update(Guid id, [FromBody] UpdateLessonDto dto, CancellationToken cancellationToken)
    {
        var lesson = await _unitOfWork.Lessons.GetByIdAsync(id, cancellationToken);

        if (lesson == null)
        {
            return NotFound();
        }

        lesson.StudentId = dto.StudentId;
        lesson.TeacherId = dto.TeacherId;
        lesson.RoomId = dto.RoomId;
        lesson.ScheduledDate = dto.ScheduledDate;
        lesson.StartTime = dto.StartTime;
        lesson.EndTime = dto.EndTime;
        lesson.Status = dto.Status;
        lesson.CancellationReason = dto.CancellationReason;
        lesson.Notes = dto.Notes;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(await GetLessonDto(id, cancellationToken));
    }

    [HttpPut("{id:guid}/status")]
    [Authorize(Policy = "TeacherOrAdmin")]
    public async Task<ActionResult<LessonDto>> UpdateStatus(Guid id, [FromBody] UpdateLessonStatusDto dto, CancellationToken cancellationToken)
    {
        var lesson = await _unitOfWork.Lessons.GetByIdAsync(id, cancellationToken);

        if (lesson == null)
        {
            return NotFound();
        }

        lesson.Status = dto.Status;
        lesson.CancellationReason = dto.CancellationReason;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(await GetLessonDto(id, cancellationToken));
    }

    [HttpPut("group-status")]
    [Authorize(Policy = "TeacherOrAdmin")]
    public async Task<ActionResult<UpdateGroupLessonStatusResultDto>> UpdateGroupStatus(
        [FromBody] UpdateGroupLessonStatusDto dto,
        CancellationToken cancellationToken)
    {
        var lessons = await _unitOfWork.Lessons.Query()
            .Where(l => l.CourseId == dto.CourseId && l.ScheduledDate == dto.ScheduledDate)
            .ToListAsync(cancellationToken);

        if (lessons.Count == 0)
        {
            return NotFound(new { message = "No lessons found for the specified course and date" });
        }

        foreach (var lesson in lessons)
        {
            lesson.Status = dto.Status;
            lesson.CancellationReason = dto.CancellationReason;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(new UpdateGroupLessonStatusResultDto
        {
            CourseId = dto.CourseId,
            ScheduledDate = dto.ScheduledDate,
            Status = dto.Status,
            LessonsUpdated = lessons.Count
        });
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var lesson = await _unitOfWork.Lessons.GetByIdAsync(id, cancellationToken);

        if (lesson == null)
        {
            return NotFound();
        }

        if (lesson.IsInvoiced)
        {
            return BadRequest(new { message = "Cannot delete an invoiced lesson" });
        }

        await _unitOfWork.Lessons.DeleteAsync(lesson, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpPost("generate")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<GenerateLessonsResultDto>> GenerateLessons([FromBody] GenerateLessonsDto dto, CancellationToken cancellationToken)
    {
        var courseExists = await _unitOfWork.Courses.Query()
            .AnyAsync(c => c.Id == dto.CourseId, cancellationToken);

        if (!courseExists)
        {
            return BadRequest(new { message = "Course not found" });
        }

        var result = await _lessonGenerationService.GenerateForCourseAsync(
            dto.CourseId, dto.StartDate, dto.EndDate, dto.SkipHolidays, cancellationToken);

        return Ok(new GenerateLessonsResultDto
        {
            CourseId = dto.CourseId,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            LessonsCreated = result.LessonsCreated,
            LessonsSkipped = result.LessonsSkipped
        });
    }

    [HttpPost("generate-bulk")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<BulkGenerateLessonsResultDto>> GenerateLessonsBulk([FromBody] BulkGenerateLessonsDto dto, CancellationToken cancellationToken)
    {
        var result = await _lessonGenerationService.GenerateBulkAsync(
            dto.StartDate, dto.EndDate, dto.SkipHolidays, cancellationToken);

        return Ok(new BulkGenerateLessonsResultDto
        {
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            TotalCoursesProcessed = result.TotalCoursesProcessed,
            TotalLessonsCreated = result.TotalLessonsCreated,
            TotalLessonsSkipped = result.TotalLessonsSkipped,
            CourseResults = result.CourseResults.Select(r => new GenerateLessonsResultDto
            {
                CourseId = r.CourseId,
                StartDate = r.StartDate,
                EndDate = r.EndDate,
                LessonsCreated = r.LessonsCreated,
                LessonsSkipped = r.LessonsSkipped
            }).ToList()
        });
    }

    private async Task<LessonDto?> GetLessonDto(Guid id, CancellationToken cancellationToken)
    {
        var lesson = await _unitOfWork.Lessons.Query()
            .Where(l => l.Id == id)
            .Include(l => l.Course)
                .ThenInclude(c => c.CourseType)
                    .ThenInclude(lt => lt.Instrument)
            .Include(l => l.Student)
            .Include(l => l.Teacher)
            .Include(l => l.Room)
            .FirstOrDefaultAsync(cancellationToken);

        return lesson == null ? null : MapToDto(lesson);
    }

    private static LessonDto MapToDto(Lesson l)
    {
        return new LessonDto
        {
            Id = l.Id,
            CourseId = l.CourseId,
            StudentId = l.StudentId,
            StudentName = l.Student?.FullName,
            TeacherId = l.TeacherId,
            TeacherName = l.Teacher.FullName,
            RoomId = l.RoomId,
            RoomName = l.Room?.Name,
            CourseTypeName = l.Course.CourseType.Name,
            InstrumentName = l.Course.CourseType.Instrument.Name,
            ScheduledDate = l.ScheduledDate,
            StartTime = l.StartTime,
            EndTime = l.EndTime,
            Status = l.Status,
            CancellationReason = l.CancellationReason,
            IsInvoiced = l.IsInvoiced,
            IsPaidToTeacher = l.IsPaidToTeacher,
            Notes = l.Notes,
            CreatedAt = l.CreatedAt,
            UpdatedAt = l.UpdatedAt
        };
    }
}

public record UpdateLessonStatusDto
{
    public required LessonStatus Status { get; init; }
    public string? CancellationReason { get; init; }
}

public record GenerateLessonsResultDto
{
    public Guid CourseId { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
    public int LessonsCreated { get; init; }
    public int LessonsSkipped { get; init; }
}

public record BulkGenerateLessonsDto
{
    public required DateOnly StartDate { get; init; }
    public required DateOnly EndDate { get; init; }
    public bool SkipHolidays { get; init; } = true;
}

public record BulkGenerateLessonsResultDto
{
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
    public int TotalCoursesProcessed { get; init; }
    public int TotalLessonsCreated { get; init; }
    public int TotalLessonsSkipped { get; init; }
    public List<GenerateLessonsResultDto> CourseResults { get; init; } = new();
}

public record UpdateGroupLessonStatusDto
{
    public Guid CourseId { get; init; }
    public DateOnly ScheduledDate { get; init; }
    public required LessonStatus Status { get; init; }
    public string? CancellationReason { get; init; }
}

public record UpdateGroupLessonStatusResultDto
{
    public Guid CourseId { get; init; }
    public DateOnly ScheduledDate { get; init; }
    public LessonStatus Status { get; init; }
    public int LessonsUpdated { get; init; }
}
