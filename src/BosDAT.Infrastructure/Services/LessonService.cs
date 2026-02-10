using Microsoft.EntityFrameworkCore;
using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Enums;
using BosDAT.Core.Interfaces;

namespace BosDAT.Infrastructure.Services;

public class LessonService(IUnitOfWork unitOfWork) : ILessonService
{
    public async Task<List<LessonDto>> GetAllAsync(
        DateOnly? startDate, DateOnly? endDate,
        Guid? teacherId, Guid? studentId, Guid? courseId,
        int? roomId, LessonStatus? status, int? top,
        CancellationToken ct = default)
    {
        IQueryable<Lesson> query = unitOfWork.Lessons.Query()
            .Include(l => l.Course)
                .ThenInclude(c => c.CourseType)
                    .ThenInclude(lt => lt.Instrument)
            .Include(l => l.Student)
            .Include(l => l.Teacher)
            .Include(l => l.Room);

        query = ApplyFilters(query, startDate, endDate, teacherId, studentId, courseId, roomId, status);

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

        return await orderedQuery.ToListAsync(ct);
    }

    public async Task<LessonDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var lesson = await unitOfWork.Lessons.Query()
            .Where(l => l.Id == id)
            .Include(l => l.Course)
                .ThenInclude(c => c.CourseType)
                    .ThenInclude(lt => lt.Instrument)
            .Include(l => l.Student)
            .Include(l => l.Teacher)
            .Include(l => l.Room)
            .FirstOrDefaultAsync(ct);

        return lesson == null ? null : MapToDto(lesson);
    }

    public async Task<List<LessonDto>> GetByStudentAsync(Guid studentId, CancellationToken ct = default)
    {
        var lessons = await unitOfWork.Lessons.GetByStudentAsync(studentId, ct);

        return lessons.Select(l => new LessonDto
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
        }).ToList();
    }

    public async Task<(LessonDto? Lesson, string? Error)> CreateAsync(CreateLessonDto dto, CancellationToken ct = default)
    {
        var course = await unitOfWork.Courses.GetByIdAsync(dto.CourseId, ct);
        if (course == null)
        {
            return (null, "Course not found");
        }

        var teacher = await unitOfWork.Teachers.GetByIdAsync(dto.TeacherId, ct);
        if (teacher == null)
        {
            return (null, "Teacher not found");
        }

        if (dto.StudentId.HasValue)
        {
            var student = await unitOfWork.Students.GetByIdAsync(dto.StudentId.Value, ct);
            if (student == null)
            {
                return (null, "Student not found");
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

        await unitOfWork.Lessons.AddAsync(lesson, ct);
        await unitOfWork.SaveChangesAsync(ct);

        var created = await GetByIdAsync(lesson.Id, ct);
        return (created, null);
    }

    public async Task<(LessonDto? Lesson, bool NotFound)> UpdateAsync(Guid id, UpdateLessonDto dto, CancellationToken ct = default)
    {
        var lesson = await unitOfWork.Lessons.GetByIdAsync(id, ct);

        if (lesson == null)
        {
            return (null, true);
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

        await unitOfWork.SaveChangesAsync(ct);

        var updated = await GetByIdAsync(id, ct);
        return (updated, false);
    }

    public async Task<(LessonDto? Lesson, bool NotFound)> UpdateStatusAsync(
        Guid id, LessonStatus status, string? cancellationReason, CancellationToken ct = default)
    {
        var lesson = await unitOfWork.Lessons.GetByIdAsync(id, ct);

        if (lesson == null)
        {
            return (null, true);
        }

        lesson.Status = status;
        lesson.CancellationReason = cancellationReason;

        await unitOfWork.SaveChangesAsync(ct);

        var updated = await GetByIdAsync(id, ct);
        return (updated, false);
    }

    public async Task<(int LessonsUpdated, bool NotFound)> UpdateGroupStatusAsync(
        Guid courseId, DateOnly scheduledDate, LessonStatus status, string? cancellationReason,
        CancellationToken ct = default)
    {
        var lessons = await unitOfWork.Lessons.Query()
            .Where(l => l.CourseId == courseId && l.ScheduledDate == scheduledDate)
            .ToListAsync(ct);

        if (lessons.Count == 0)
        {
            return (0, true);
        }

        foreach (var lesson in lessons)
        {
            lesson.Status = status;
            lesson.CancellationReason = cancellationReason;
        }

        await unitOfWork.SaveChangesAsync(ct);

        return (lessons.Count, false);
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var lesson = await unitOfWork.Lessons.GetByIdAsync(id, ct);

        if (lesson == null)
        {
            return (false, "Lesson not found");
        }

        if (lesson.IsInvoiced)
        {
            return (false, "Cannot delete an invoiced lesson");
        }

        await unitOfWork.Lessons.DeleteAsync(lesson, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return (true, null);
    }

    private static IQueryable<Lesson> ApplyFilters(
        IQueryable<Lesson> query,
        DateOnly? startDate, DateOnly? endDate,
        Guid? teacherId, Guid? studentId, Guid? courseId,
        int? roomId, LessonStatus? status)
    {
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

        return query;
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
