using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Enums;
using BosDAT.Core.Interfaces;
using BosDAT.Core.Utilities;

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
        var course = await _unitOfWork.Courses.Query()
            .Where(c => c.Id == dto.CourseId)
            .Include(c => c.CourseType)
            .Include(c => c.Enrollments.Where(e => e.Status == EnrollmentStatus.Active))
                .ThenInclude(e => e.Student)
            .FirstOrDefaultAsync(cancellationToken);

        if (course == null)
        {
            return BadRequest(new { message = "Course not found" });
        }

        var (lessonsCreated, lessonsSkipped) = await GenerateLessonsForCourseAsync(
            course,
            dto.StartDate,
            dto.EndDate,
            dto.SkipHolidays,
            cancellationToken);

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
            .Include(c => c.CourseType)
            .ToListAsync(cancellationToken);

        var totalCreated = 0;
        var totalSkipped = 0;
        var courseResults = new List<GenerateLessonsResultDto>();

        foreach (var course in activeCourses)
        {
            var (lessonsCreated, lessonsSkipped) = await GenerateLessonsForCourseAsync(
                course,
                dto.StartDate,
                dto.EndDate,
                dto.SkipHolidays,
                cancellationToken);

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

    private async Task<(int Created, int Skipped)> GenerateLessonsForCourseAsync(
        Course course,
        DateOnly startDate,
        DateOnly endDate,
        bool skipHolidays,
        CancellationToken cancellationToken)
    {
        var holidays = await GetHolidaysAsync(startDate, endDate, skipHolidays, cancellationToken);
        var existingDates = await GetExistingLessonDatesAsync(course.Id, startDate, endDate, cancellationToken);

        var lessonsCreated = 0;
        var lessonsSkipped = 0;
        var currentDate = FindFirstOccurrenceDate(startDate, course, endDate);

        while (currentDate <= endDate)
        {
            if (ShouldSkipDate(currentDate, holidays, existingDates))
            {
                lessonsSkipped++;
            }
            else
            {
                await CreateLessonsForDate(course, currentDate);
                lessonsCreated += course.CourseType?.Type == CourseTypeCategory.Individual
                    ? course.Enrollments.Count
                    : 1;
            }

            currentDate = GetNextOccurrenceDate(currentDate, course);
        }

        return (lessonsCreated, lessonsSkipped);
    }

    private async Task<List<Holiday>> GetHolidaysAsync(DateOnly startDate, DateOnly endDate, bool skipHolidays, CancellationToken cancellationToken)
    {
        if (!skipHolidays)
            return new List<Holiday>();

        return await _unitOfWork.Repository<Holiday>().Query()
            .Where(h => h.EndDate >= startDate && h.StartDate <= endDate)
            .ToListAsync(cancellationToken);
    }

    private async Task<HashSet<DateOnly>> GetExistingLessonDatesAsync(
        Guid courseId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken)
    {
        var existingLessons = await _unitOfWork.Lessons.Query()
            .Where(l => l.CourseId == courseId && l.ScheduledDate >= startDate && l.ScheduledDate <= endDate)
            .ToListAsync(cancellationToken);

        return existingLessons.Select(l => l.ScheduledDate).ToHashSet();
    }

    private static DateOnly FindFirstOccurrenceDate(DateOnly startDate, Course course, DateOnly endDate)
    {
        var currentDate = startDate;

        // Find first occurrence of the target day of week
        while (currentDate.DayOfWeek != course.DayOfWeek && currentDate <= endDate)
        {
            currentDate = currentDate.AddDays(1);
        }

        // For biweekly with specific parity, ensure we start on correct week
        if (course.Frequency == CourseFrequency.Biweekly && course.WeekParity != WeekParity.All)
        {
            while (!IsoDateHelper.MatchesWeekParity(currentDate.ToDateTime(TimeOnly.MinValue), course.WeekParity)
                   && currentDate <= endDate)
            {
                currentDate = currentDate.AddDays(7);
            }
        }

        return currentDate;
    }

    private static bool ShouldSkipDate(DateOnly date, List<Holiday> holidays, HashSet<DateOnly> existingDates)
    {
        var isHoliday = holidays.Any(h => date >= h.StartDate && date <= h.EndDate);
        return isHoliday || existingDates.Contains(date);
    }

    private static DateOnly GetNextOccurrenceDate(DateOnly currentDate, CourseFrequency frequency)
    {
        return frequency switch
        {
            CourseFrequency.Weekly => currentDate.AddDays(7),
            CourseFrequency.Biweekly => currentDate.AddDays(14),
            CourseFrequency.Monthly => currentDate.AddMonths(1),
            _ => currentDate.AddDays(7)
        };
    }

    private static DateOnly GetNextOccurrenceDate(DateOnly currentDate, Course course)
    {
        // For biweekly with specific parity, we need to find the next occurrence in a matching week
        if (course.Frequency == CourseFrequency.Biweekly && course.WeekParity != WeekParity.All)
        {
            // Start by adding 7 days (1 week)
            var nextDate = currentDate.AddDays(7);

            // Keep adding weeks until we find one that matches the parity
            while (!IsoDateHelper.MatchesWeekParity(nextDate.ToDateTime(TimeOnly.MinValue), course.WeekParity))
            {
                nextDate = nextDate.AddDays(7);
            }

            return nextDate;
        }

        // For other frequencies, use simple date arithmetic
        return course.Frequency switch
        {
            CourseFrequency.Weekly => currentDate.AddDays(7),
            CourseFrequency.Biweekly => currentDate.AddDays(14),
            CourseFrequency.Monthly => currentDate.AddMonths(1),
            _ => currentDate.AddDays(7)
        };
    }

    private async Task CreateLessonsForDate(Course course, DateOnly date)
    {
        var enrolledStudents = course.Enrollments.ToList();

        if (enrolledStudents.Count == 0)
        {
            await CreateLesson(course, date, null);
        }
        else if (course.CourseType?.Type == CourseTypeCategory.Individual)
        {
            foreach (var enrollment in enrolledStudents)
            {
                await CreateLesson(course, date, enrollment.StudentId);
            }
        }
        else
        {
            await CreateLesson(course, date, null);
        }
    }

    private async Task CreateLesson(Course course, DateOnly date, Guid? studentId)
    {
        var lesson = new Lesson
        {
            Id = Guid.NewGuid(),
            CourseId = course.Id,
            StudentId = studentId,
            TeacherId = course.TeacherId,
            RoomId = course.RoomId,
            ScheduledDate = date,
            StartTime = course.StartTime,
            EndTime = course.EndTime,
            Status = LessonStatus.Scheduled
        };
        await _unitOfWork.Lessons.AddAsync(lesson, CancellationToken.None);
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
