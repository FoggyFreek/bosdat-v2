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
public class LessonsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public LessonsController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<LessonDto>>> GetAll(
        [FromQuery] DateOnly? startDate,
        [FromQuery] DateOnly? endDate,
        [FromQuery] Guid? teacherId,
        [FromQuery] Guid? studentId,
        [FromQuery] int? roomId,
        [FromQuery] LessonStatus? status,
        CancellationToken cancellationToken)
    {
        IQueryable<Lesson> query = _unitOfWork.Lessons.Query()
            .Include(l => l.Course)
                .ThenInclude(c => c.LessonType)
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

        if (roomId.HasValue)
        {
            query = query.Where(l => l.RoomId == roomId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(l => l.Status == status.Value);
        }

        var lessons = await query
            .OrderBy(l => l.ScheduledDate)
            .ThenBy(l => l.StartTime)
            .Select(l => MapToDto(l))
            .ToListAsync(cancellationToken);

        return Ok(lessons);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<LessonDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var lesson = await _unitOfWork.Lessons.Query()
            .Where(l => l.Id == id)
            .Include(l => l.Course)
                .ThenInclude(c => c.LessonType)
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
            LessonTypeName = l.Course.LessonType.Name,
            InstrumentName = l.Course.LessonType.Instrument.Name,
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

        _unitOfWork.Lessons.Delete(lesson);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpPost("generate")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<GenerateLessonsResultDto>> GenerateLessons([FromBody] GenerateLessonsDto dto, CancellationToken cancellationToken)
    {
        var course = await _unitOfWork.Courses.Query()
            .Where(c => c.Id == dto.CourseId)
            .Include(c => c.LessonType)
            .Include(c => c.Enrollments.Where(e => e.Status == EnrollmentStatus.Active))
                .ThenInclude(e => e.Student)
            .FirstOrDefaultAsync(cancellationToken);

        if (course == null)
        {
            return BadRequest(new { message = "Course not found" });
        }

        // Get holidays if we need to skip them
        var holidays = new List<Holiday>();
        if (dto.SkipHolidays)
        {
            holidays = await _unitOfWork.Repository<Holiday>().Query()
                .Where(h => h.EndDate >= dto.StartDate && h.StartDate <= dto.EndDate)
                .ToListAsync(cancellationToken);
        }

        // Get existing lessons for this course in the date range to avoid duplicates
        var existingLessons = await _unitOfWork.Lessons.Query()
            .Where(l => l.CourseId == dto.CourseId && l.ScheduledDate >= dto.StartDate && l.ScheduledDate <= dto.EndDate)
            .ToListAsync(cancellationToken);

        var existingDates = existingLessons.Select(l => l.ScheduledDate).ToHashSet();

        // Generate lessons
        var lessonsCreated = 0;
        var lessonsSkipped = 0;
        var currentDate = dto.StartDate;

        // Find first occurrence of the course day
        while (currentDate.DayOfWeek != course.DayOfWeek && currentDate <= dto.EndDate)
        {
            currentDate = currentDate.AddDays(1);
        }

        while (currentDate <= dto.EndDate)
        {
            // Check if date is in a holiday period
            var isHoliday = holidays.Any(h => currentDate >= h.StartDate && currentDate <= h.EndDate);

            if (isHoliday)
            {
                lessonsSkipped++;
            }
            else if (existingDates.Contains(currentDate))
            {
                lessonsSkipped++;
            }
            else
            {
                // For individual lessons, create one lesson per enrolled student
                // For group lessons, create one lesson without a specific student
                var enrolledStudents = course.Enrollments.ToList();

                if (enrolledStudents.Count == 0)
                {
                    // Create a lesson without a student (for courses with no enrollments yet)
                    var lesson = new Lesson
                    {
                        Id = Guid.NewGuid(),
                        CourseId = course.Id,
                        StudentId = null,
                        TeacherId = course.TeacherId,
                        RoomId = course.RoomId,
                        ScheduledDate = currentDate,
                        StartTime = course.StartTime,
                        EndTime = course.EndTime,
                        Status = LessonStatus.Scheduled
                    };
                    await _unitOfWork.Lessons.AddAsync(lesson, cancellationToken);
                    lessonsCreated++;
                }
                else if (course.LessonType?.Type == LessonTypeCategory.Individual)
                {
                    // For individual lessons, create separate lessons for each student
                    foreach (var enrollment in enrolledStudents)
                    {
                        var lesson = new Lesson
                        {
                            Id = Guid.NewGuid(),
                            CourseId = course.Id,
                            StudentId = enrollment.StudentId,
                            TeacherId = course.TeacherId,
                            RoomId = course.RoomId,
                            ScheduledDate = currentDate,
                            StartTime = course.StartTime,
                            EndTime = course.EndTime,
                            Status = LessonStatus.Scheduled
                        };
                        await _unitOfWork.Lessons.AddAsync(lesson, cancellationToken);
                        lessonsCreated++;
                    }
                }
                else
                {
                    // For group lessons, create one lesson (students are tracked via enrollments)
                    var lesson = new Lesson
                    {
                        Id = Guid.NewGuid(),
                        CourseId = course.Id,
                        StudentId = null,
                        TeacherId = course.TeacherId,
                        RoomId = course.RoomId,
                        ScheduledDate = currentDate,
                        StartTime = course.StartTime,
                        EndTime = course.EndTime,
                        Status = LessonStatus.Scheduled
                    };
                    await _unitOfWork.Lessons.AddAsync(lesson, cancellationToken);
                    lessonsCreated++;
                }
            }

            // Move to next occurrence based on frequency
            currentDate = course.Frequency switch
            {
                CourseFrequency.Weekly => currentDate.AddDays(7),
                CourseFrequency.Biweekly => currentDate.AddDays(14),
                CourseFrequency.Monthly => currentDate.AddMonths(1),
                _ => currentDate.AddDays(7)
            };
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(new GenerateLessonsResultDto
        {
            CourseId = dto.CourseId,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            LessonsCreated = lessonsCreated,
            LessonsSkipped = lessonsSkipped
        });
    }

    [HttpPost("generate-bulk")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<BulkGenerateLessonsResultDto>> GenerateLessonsBulk([FromBody] BulkGenerateLessonsDto dto, CancellationToken cancellationToken)
    {
        var activeCourses = await _unitOfWork.Courses.Query()
            .Where(c => c.Status == CourseStatus.Active)
            .Include(c => c.Enrollments.Where(e => e.Status == EnrollmentStatus.Active))
            .Include(c => c.LessonType)
            .ToListAsync(cancellationToken);

        var holidays = new List<Holiday>();
        if (dto.SkipHolidays)
        {
            holidays = await _unitOfWork.Repository<Holiday>().Query()
                .Where(h => h.EndDate >= dto.StartDate && h.StartDate <= dto.EndDate)
                .ToListAsync(cancellationToken);
        }

        var totalCreated = 0;
        var totalSkipped = 0;
        var courseResults = new List<GenerateLessonsResultDto>();

        foreach (var course in activeCourses)
        {
            var existingLessons = await _unitOfWork.Lessons.Query()
                .Where(l => l.CourseId == course.Id && l.ScheduledDate >= dto.StartDate && l.ScheduledDate <= dto.EndDate)
                .ToListAsync(cancellationToken);

            var existingDates = existingLessons.Select(l => l.ScheduledDate).ToHashSet();

            var lessonsCreated = 0;
            var lessonsSkipped = 0;
            var currentDate = dto.StartDate;

            while (currentDate.DayOfWeek != course.DayOfWeek && currentDate <= dto.EndDate)
            {
                currentDate = currentDate.AddDays(1);
            }

            while (currentDate <= dto.EndDate)
            {
                var isHoliday = holidays.Any(h => currentDate >= h.StartDate && currentDate <= h.EndDate);

                if (isHoliday || existingDates.Contains(currentDate))
                {
                    lessonsSkipped++;
                }
                else
                {
                    var lesson = new Lesson
                    {
                        Id = Guid.NewGuid(),
                        CourseId = course.Id,
                        StudentId = null,
                        TeacherId = course.TeacherId,
                        RoomId = course.RoomId,
                        ScheduledDate = currentDate,
                        StartTime = course.StartTime,
                        EndTime = course.EndTime,
                        Status = LessonStatus.Scheduled
                    };
                    await _unitOfWork.Lessons.AddAsync(lesson, cancellationToken);
                    lessonsCreated++;
                }

                currentDate = course.Frequency switch
                {
                    CourseFrequency.Weekly => currentDate.AddDays(7),
                    CourseFrequency.Biweekly => currentDate.AddDays(14),
                    CourseFrequency.Monthly => currentDate.AddMonths(1),
                    _ => currentDate.AddDays(7)
                };
            }

            totalCreated += lessonsCreated;
            totalSkipped += lessonsSkipped;

            if (lessonsCreated > 0)
            {
                courseResults.Add(new GenerateLessonsResultDto
                {
                    CourseId = course.Id,
                    StartDate = dto.StartDate,
                    EndDate = dto.EndDate,
                    LessonsCreated = lessonsCreated,
                    LessonsSkipped = lessonsSkipped
                });
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(new BulkGenerateLessonsResultDto
        {
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            TotalCoursesProcessed = activeCourses.Count,
            TotalLessonsCreated = totalCreated,
            TotalLessonsSkipped = totalSkipped,
            CourseResults = courseResults
        });
    }

    private async Task<LessonDto?> GetLessonDto(Guid id, CancellationToken cancellationToken)
    {
        var lesson = await _unitOfWork.Lessons.Query()
            .Where(l => l.Id == id)
            .Include(l => l.Course)
                .ThenInclude(c => c.LessonType)
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
            LessonTypeName = l.Course.LessonType.Name,
            InstrumentName = l.Course.LessonType.Instrument.Name,
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
    public LessonStatus Status { get; init; }
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
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
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
