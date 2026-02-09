using Microsoft.EntityFrameworkCore;
using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Enums;
using BosDAT.Core.Interfaces;

namespace BosDAT.Infrastructure.Services;

public class EnrollmentService(
    IUnitOfWork unitOfWork,
    IScheduleConflictService scheduleConflictService,
    IRegistrationFeeService registrationFeeService) : IEnrollmentService
{
    public async Task<IEnumerable<EnrollmentDto>> GetAllAsync(
        Guid? studentId = null,
        Guid? courseId = null,
        EnrollmentStatus? status = null,
        CancellationToken ct = default)
    {
        IQueryable<Enrollment> query = unitOfWork.Repository<Enrollment>().Query()
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
                InvoicingPreference = e.InvoicingPreference,
                Notes = e.Notes
            })
            .ToListAsync(ct);

        return enrollments;
    }

    public async Task<EnrollmentDetailDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var enrollment = await unitOfWork.Repository<Enrollment>().Query()
            .Where(e => e.Id == id)
            .Include(e => e.Student)
            .Include(e => e.Course)
                .ThenInclude(c => c.CourseType)
                    .ThenInclude(lt => lt.Instrument)
            .Include(e => e.Course.Teacher)
            .Include(e => e.Course.Room)
            .FirstOrDefaultAsync(ct);

        if (enrollment == null)
        {
            return null;
        }

        return new EnrollmentDetailDto
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
            InvoicingPreference = enrollment.InvoicingPreference,
            Notes = enrollment.Notes
        };
    }

    public async Task<IEnumerable<StudentEnrollmentDto>> GetByStudentAsync(Guid studentId, CancellationToken ct = default)
    {
        var student = await unitOfWork.Students.GetByIdAsync(studentId, ct);
        if (student == null)
        {
            return Enumerable.Empty<StudentEnrollmentDto>();
        }

        var enrollments = await unitOfWork.Repository<Enrollment>().Query()
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
                Status = e.Status,
                InvoicingPreference = e.InvoicingPreference
            })
            .ToListAsync(ct);

        return enrollments;
    }

    public async Task<(EnrollmentDto? Enrollment, bool NotFound, string? Error)> CreateAsync(
        Guid courseId, CreateEnrollmentDto dto, CancellationToken ct = default)
    {
        var course = await unitOfWork.Courses.GetByIdAsync(courseId, ct);
        if (course == null)
            return (null, true, "Course not found");

        var student = await unitOfWork.Students.GetByIdAsync(dto.StudentId, ct);
        if (student == null)
            return (null, true, "Student not found");

        var existing = await unitOfWork.Repository<Enrollment>()
            .FirstOrDefaultAsync(e => e.StudentId == dto.StudentId && e.CourseId == courseId, ct);
        if (existing != null)
            return (null, false, "Student is already enrolled in this course");

        // Check for schedule conflicts
        var conflictCheck = await scheduleConflictService.HasConflictAsync(dto.StudentId, dto.CourseId);
        if (conflictCheck.HasConflict)
            return (null, false, "Schedule conflict detected. This course overlaps with existing enrollment(s).");

        var enrollmentStatus = course.IsTrial ? EnrollmentStatus.Trail : EnrollmentStatus.Active;

        var enrollment = new Enrollment
        {
            Id = Guid.NewGuid(),
            StudentId = dto.StudentId,
            CourseId = courseId,
            DiscountPercent = dto.DiscountPercent,
            DiscountType = dto.DiscountType,
            InvoicingPreference = dto.InvoicingPreference,
            Notes = dto.Notes,
            Status = enrollmentStatus
        };

        await unitOfWork.Repository<Enrollment>().AddAsync(enrollment, ct);
        await unitOfWork.SaveChangesAsync(ct);

        // Apply registration fee for non-trial courses if student is eligible
        if (!course.IsTrial && await registrationFeeService.IsStudentEligibleForFeeAsync(dto.StudentId, ct))
        {
            await registrationFeeService.ApplyRegistrationFeeAsync(dto.StudentId, ct);
        }

        return (new EnrollmentDto
        {
            Id = enrollment.Id,
            StudentId = enrollment.StudentId,
            StudentName = student.FullName,
            CourseId = enrollment.CourseId,
            EnrolledAt = enrollment.EnrolledAt,
            DiscountPercent = enrollment.DiscountPercent,
            DiscountType = enrollment.DiscountType,
            InvoicingPreference = enrollment.InvoicingPreference,
            Status = enrollment.Status,
            Notes = enrollment.Notes
        }, false, null);
    }

    public async Task<EnrollmentDto?> UpdateAsync(Guid id, UpdateEnrollmentDto dto, CancellationToken ct = default)
    {
        var enrollment = await unitOfWork.Repository<Enrollment>().Query()
            .Where(e => e.Id == id)
            .Include(e => e.Student)
            .FirstOrDefaultAsync(ct);

        if (enrollment == null)
        {
            return null;
        }

        enrollment.DiscountPercent = dto.DiscountPercent;
        enrollment.Status = dto.Status;
        enrollment.InvoicingPreference = dto.InvoicingPreference;
        enrollment.Notes = dto.Notes;

        await unitOfWork.SaveChangesAsync(ct);

        return new EnrollmentDto
        {
            Id = enrollment.Id,
            StudentId = enrollment.StudentId,
            StudentName = enrollment.Student.FullName,
            CourseId = enrollment.CourseId,
            EnrolledAt = enrollment.EnrolledAt,
            DiscountPercent = enrollment.DiscountPercent,
            DiscountType = enrollment.DiscountType,
            Status = enrollment.Status,
            InvoicingPreference = enrollment.InvoicingPreference,
            Notes = enrollment.Notes
        };
    }

    public async Task<EnrollmentDto?> PromoteFromTrailAsync(Guid id, CancellationToken ct = default)
    {
        var enrollment = await unitOfWork.Repository<Enrollment>().Query()
            .Where(e => e.Id == id)
            .Include(e => e.Student)
            .FirstOrDefaultAsync(ct);

        if (enrollment == null)
        {
            return null;
        }

        if (enrollment.Status != EnrollmentStatus.Trail)
        {
            throw new InvalidOperationException("Only trial enrollments can be promoted");
        }

        enrollment.Status = EnrollmentStatus.Active;
        await unitOfWork.SaveChangesAsync(ct);

        // Apply registration fee if student is eligible
        if (await registrationFeeService.IsStudentEligibleForFeeAsync(enrollment.StudentId, ct))
        {
            await registrationFeeService.ApplyRegistrationFeeAsync(enrollment.StudentId, ct);
        }

        return new EnrollmentDto
        {
            Id = enrollment.Id,
            StudentId = enrollment.StudentId,
            StudentName = enrollment.Student.FullName,
            CourseId = enrollment.CourseId,
            EnrolledAt = enrollment.EnrolledAt,
            DiscountPercent = enrollment.DiscountPercent,
            DiscountType = enrollment.DiscountType,
            Status = enrollment.Status,
            InvoicingPreference = enrollment.InvoicingPreference,
            Notes = enrollment.Notes
        };
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var enrollment = await unitOfWork.Repository<Enrollment>().GetByIdAsync(id, ct);

        if (enrollment == null)
        {
            return false;
        }

        enrollment.Status = EnrollmentStatus.Withdrawn;
        await unitOfWork.SaveChangesAsync(ct);

        return true;
    }
}