using Microsoft.EntityFrameworkCore;
using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Interfaces;

namespace BosDAT.Infrastructure.Services;

public class CourseService(
    IUnitOfWork unitOfWork) : ICourseService
{
    public async Task<List<CourseListDto>> GetSummaryAsync(
        CourseStatus? status, Guid? teacherId, DayOfWeek? dayOfWeek, int? roomId,
        CancellationToken ct = default)
    {
        var query = ApplyFilters(
            unitOfWork.Courses.Query()
                .Include(c => c.Teacher)
                .Include(c => c.CourseType)
                    .ThenInclude(ct => ct.Instrument)
                .Include(c => c.Room),
            status, teacherId, dayOfWeek, roomId);

        return await query
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
                EnrollmentCount = c.Enrollments.Count(e => e.Status == EnrollmentStatus.Active),
                Status = c.Status
            })
            .ToListAsync(ct);
    }

    public async Task<int> GetCountAsync(
        CourseStatus? status, Guid? teacherId, DayOfWeek? dayOfWeek, int? roomId,
        CancellationToken ct = default)
    {
        var query = ApplyFilters(unitOfWork.Courses.Query(), status, teacherId, dayOfWeek, roomId);
        return await query.CountAsync(ct);
    }

    public async Task<List<CourseDto>> GetAllAsync(
        CourseStatus? status, Guid? teacherId, DayOfWeek? dayOfWeek, int? roomId,
        CancellationToken ct = default)
    {
        var query = ApplyFilters(
            unitOfWork.Courses.Query()
                .Include(c => c.Teacher)
                .Include(c => c.CourseType)
                    .ThenInclude(ct => ct.Instrument)
                .Include(c => c.Room)
                .Include(c => c.Enrollments),
            status, teacherId, dayOfWeek, roomId);

        return await query
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
            .ToListAsync(ct);
    }

    public async Task<CourseDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var course = await unitOfWork.Courses.GetWithEnrollmentsAsync(id, ct);
        if (course == null)
            return null;

        return MapToDto(course);
    }

    public async Task<(CourseDto? Course, string? Error)> CreateAsync(CreateCourseDto dto, CancellationToken ct = default)
    {
        var teacher = await unitOfWork.Teachers.GetByIdAsync(dto.TeacherId, ct);
        if (teacher == null)
            return (null, "Teacher not found");

        var courseType = await unitOfWork.Repository<CourseType>().GetByIdAsync(dto.CourseTypeId, ct);
        if (courseType == null)
            return (null, "Course type not found");

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
            WeekParity = dto.WeekParity,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            IsTrial = dto.IsTrial,
            Status = CourseStatus.Active
        };

        await unitOfWork.Courses.AddAsync(course, ct);
        await unitOfWork.SaveChangesAsync(ct);

        var created = await unitOfWork.Courses.GetWithEnrollmentsAsync(course.Id, ct);
        return (MapToDto(created!), null);
    }

    public async Task<(CourseDto? Course, bool NotFound)> UpdateAsync(Guid id, UpdateCourseDto dto, CancellationToken ct = default)
    {
        var course = await unitOfWork.Courses.GetByIdAsync(id, ct);
        if (course == null)
            return (null, true);

        course.TeacherId = dto.TeacherId;
        course.CourseTypeId = dto.CourseTypeId;
        course.RoomId = dto.RoomId;
        course.DayOfWeek = dto.DayOfWeek;
        course.StartTime = dto.StartTime;
        course.EndTime = dto.EndTime;
        course.Frequency = dto.Frequency;
        course.WeekParity = dto.WeekParity;
        course.StartDate = dto.StartDate;
        course.EndDate = dto.EndDate;
        course.Status = dto.Status;
        course.IsWorkshop = dto.IsWorkshop;
        course.IsTrial = dto.IsTrial;
        course.Notes = dto.Notes;

        await unitOfWork.SaveChangesAsync(ct);

        var updated = await unitOfWork.Courses.GetWithEnrollmentsAsync(id, ct);
        return (MapToDto(updated!), false);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var course = await unitOfWork.Courses.GetByIdAsync(id, ct);
        if (course == null)
            return false;

        course.Status = CourseStatus.Cancelled;
        await unitOfWork.SaveChangesAsync(ct);

        return true;
    }

    private static IQueryable<Course> ApplyFilters(
        IQueryable<Course> query,
        CourseStatus? status, Guid? teacherId, DayOfWeek? dayOfWeek, int? roomId)
    {
        if (status.HasValue)
            query = query.Where(c => c.Status == status.Value);

        if (teacherId.HasValue)
            query = query.Where(c => c.TeacherId == teacherId.Value);

        if (dayOfWeek.HasValue)
            query = query.Where(c => c.DayOfWeek == dayOfWeek.Value);

        if (roomId.HasValue)
            query = query.Where(c => c.RoomId == roomId.Value);

        return query;
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
            WeekParity = course.WeekParity,
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
                DiscountType = e.DiscountType,
                InvoicingPreference = e.InvoicingPreference,
                Status = e.Status,
                Notes = e.Notes
            }).ToList(),
            CreatedAt = course.CreatedAt,
            UpdatedAt = course.UpdatedAt
        };
    }
}
