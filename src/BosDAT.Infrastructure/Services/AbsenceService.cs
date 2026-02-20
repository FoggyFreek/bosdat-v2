using Microsoft.EntityFrameworkCore;
using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Interfaces;

namespace BosDAT.Infrastructure.Services;

public class AbsenceService(IUnitOfWork unitOfWork) : IAbsenceService
{
    public async Task<IEnumerable<AbsenceDto>> GetAllAsync(CancellationToken ct = default)
    {
        return await unitOfWork.Repository<Absence>().Query()
            .AsNoTracking()
            .Include(a => a.Student)
            .Include(a => a.Teacher)
            .OrderByDescending(a => a.StartDate)
            .Select(a => MapToDto(a))
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<AbsenceDto>> GetByStudentAsync(Guid studentId, CancellationToken ct = default)
    {
        return await unitOfWork.Repository<Absence>().Query()
            .AsNoTracking()
            .Include(a => a.Student)
            .Where(a => a.StudentId == studentId)
            .OrderByDescending(a => a.StartDate)
            .Select(a => MapToDto(a))
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<AbsenceDto>> GetByTeacherAsync(Guid teacherId, CancellationToken ct = default)
    {
        return await unitOfWork.Repository<Absence>().Query()
            .AsNoTracking()
            .Include(a => a.Teacher)
            .Where(a => a.TeacherId == teacherId)
            .OrderByDescending(a => a.StartDate)
            .Select(a => MapToDto(a))
            .ToListAsync(ct);
    }

    public async Task<AbsenceDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var absence = await unitOfWork.Repository<Absence>().Query()
            .AsNoTracking()
            .Include(a => a.Student)
            .Include(a => a.Teacher)
            .FirstOrDefaultAsync(a => a.Id == id, ct);

        return absence == null ? null : MapToDto(absence);
    }

    public async Task<AbsenceDto> CreateAsync(CreateAbsenceDto dto, CancellationToken ct = default)
    {
        var absence = new Absence
        {
            Id = Guid.NewGuid(),
            StudentId = dto.StudentId,
            TeacherId = dto.TeacherId,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            Reason = dto.Reason,
            Notes = dto.Notes,
            InvoiceLesson = dto.InvoiceLesson
        };

        await unitOfWork.Repository<Absence>().AddAsync(absence, ct);

        // Cancel affected scheduled lessons
        await CancelAffectedLessonsAsync(absence, ct);

        await unitOfWork.SaveChangesAsync(ct);

        // Reload with navigation properties
        var created = await unitOfWork.Repository<Absence>().Query()
            .AsNoTracking()
            .Include(a => a.Student)
            .Include(a => a.Teacher)
            .FirstAsync(a => a.Id == absence.Id, ct);

        return MapToDto(created);
    }

    public async Task<AbsenceDto?> UpdateAsync(Guid id, UpdateAbsenceDto dto, CancellationToken ct = default)
    {
        var absence = await unitOfWork.Repository<Absence>().Query()
            .Include(a => a.Student)
            .Include(a => a.Teacher)
            .FirstOrDefaultAsync(a => a.Id == id, ct);

        if (absence == null)
            return null;

        absence.StartDate = dto.StartDate;
        absence.EndDate = dto.EndDate;
        absence.Reason = dto.Reason;
        absence.Notes = dto.Notes;
        absence.InvoiceLesson = dto.InvoiceLesson;

        await unitOfWork.SaveChangesAsync(ct);

        return MapToDto(absence);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var absence = await unitOfWork.Repository<Absence>().GetByIdAsync(id, ct);

        if (absence == null)
            return false;

        await unitOfWork.Repository<Absence>().DeleteAsync(absence, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return true;
    }

    public async Task<bool> IsTeacherAbsentAsync(Guid teacherId, DateOnly date, CancellationToken ct = default)
    {
        return await unitOfWork.Repository<Absence>().AnyAsync(
            a => a.TeacherId == teacherId && a.StartDate <= date && a.EndDate >= date, ct);
    }

    public async Task<bool> IsStudentAbsentAsync(Guid studentId, DateOnly date, CancellationToken ct = default)
    {
        return await unitOfWork.Repository<Absence>().AnyAsync(
            a => a.StudentId == studentId && a.StartDate <= date && a.EndDate >= date, ct);
    }

    public async Task<IEnumerable<AbsenceDto>> GetTeacherAbsencesForPeriodAsync(
        DateOnly startDate, DateOnly endDate, CancellationToken ct = default)
    {
        return await unitOfWork.Repository<Absence>().Query()
            .AsNoTracking()
            .Include(a => a.Teacher)
            .Where(a => a.TeacherId != null && a.EndDate >= startDate && a.StartDate <= endDate)
            .OrderBy(a => a.StartDate)
            .Select(a => MapToDto(a))
            .ToListAsync(ct);
    }

    private async Task CancelAffectedLessonsAsync(Absence absence, CancellationToken ct)
    {
        var query = unitOfWork.Lessons.Query()
            .Where(l => l.ScheduledDate >= absence.StartDate
                     && l.ScheduledDate <= absence.EndDate
                     && l.Status == LessonStatus.Scheduled);

        if (absence.TeacherId.HasValue)
        {
            query = query.Where(l => l.TeacherId == absence.TeacherId.Value);
        }

        if (absence.StudentId.HasValue)
        {
            query = query.Where(l => l.StudentId == absence.StudentId.Value);
        }

        var lessons = await query.ToListAsync(ct);

        foreach (var lesson in lessons)
        {
            lesson.Status = LessonStatus.Cancelled;
            lesson.CancellationReason = $"Absence: {absence.Reason}";

            if (absence.InvoiceLesson)
            {
                lesson.IsInvoiced = true;
            }
        }
    }

    private static AbsenceDto MapToDto(Absence a)
    {
        var personName = a.Student != null
            ? $"{a.Student.FirstName} {a.Student.LastName}"
            : a.Teacher != null
                ? $"{a.Teacher.FirstName} {a.Teacher.LastName}"
                : null;

        return new AbsenceDto
        {
            Id = a.Id,
            StudentId = a.StudentId,
            TeacherId = a.TeacherId,
            PersonName = personName,
            StartDate = a.StartDate,
            EndDate = a.EndDate,
            Reason = a.Reason,
            Notes = a.Notes,
            InvoiceLesson = a.InvoiceLesson
        };
    }
}
