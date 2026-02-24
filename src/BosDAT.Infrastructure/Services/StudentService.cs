using Microsoft.EntityFrameworkCore;
using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Interfaces;

namespace BosDAT.Infrastructure.Services;

public class StudentService(
    IUnitOfWork unitOfWork,
    IRegistrationFeeService registrationFeeService) : IStudentService
{
    public async Task<List<StudentListDto>> GetAllAsync(
        string? search,
        StudentStatus? status,
        CancellationToken ct = default)
    {
        var query = unitOfWork.Students.Query();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search}%";
            query = query.Where(s =>
                EF.Functions.ILike(s.FirstName, pattern) ||
                EF.Functions.ILike(s.LastName, pattern) ||
                EF.Functions.ILike(s.Email, pattern));
        }

        if (status.HasValue)
        {
            query = query.Where(s => s.Status == status.Value);
        }

        var students = await query
            .OrderBy(s => s.LastName)
            .ThenBy(s => s.FirstName)
            .Select(s => new StudentListDto
            {
                Id = s.Id,
                FullName = s.FullName,
                Email = s.Email,
                Phone = s.Phone,
                Status = s.Status,
                EnrolledAt = s.EnrolledAt
            })
            .ToListAsync(ct);

        return students;
    }

    public async Task<StudentDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var student = await unitOfWork.Students.GetByIdAsync(id, ct);

        if (student == null)
        {
            return null;
        }

        return MapToDto(student);
    }

    public async Task<(StudentDto? Student, List<EnrollmentDto> Enrollments, bool NotFound)> GetWithEnrollmentsAsync(
        Guid id,
        CancellationToken ct = default)
    {
        var student = await unitOfWork.Students.GetWithEnrollmentsAsync(id, ct);

        if (student == null)
        {
            return (null, new List<EnrollmentDto>(), true);
        }

        var dto = MapToDto(student);
        var enrollments = student.Enrollments.Select(e => new EnrollmentDto
        {
            Id = e.Id,
            StudentId = e.StudentId,
            StudentName = student.FullName,
            CourseId = e.CourseId,
            EnrolledAt = e.EnrolledAt,
            DiscountPercent = e.DiscountPercent,
            DiscountType = e.DiscountType,
            Status = e.Status,
            Notes = e.Notes
        }).ToList();

        return (dto, enrollments, false);
    }

    public async Task<(StudentDto? Student, string? Error)> CreateAsync(
        CreateStudentDto dto,
        CancellationToken ct = default)
    {
        // Check for duplicate email
        var existing = await unitOfWork.Students.GetByEmailAsync(dto.Email, ct);
        if (existing != null)
        {
            return (null, "A student with this email already exists");
        }

        var student = new Student
        {
            Id = Guid.NewGuid(),
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Prefix = dto.Prefix,
            Email = dto.Email,
            Phone = dto.Phone,
            PhoneAlt = dto.PhoneAlt,
            Address = dto.Address,
            PostalCode = dto.PostalCode,
            City = dto.City,
            DateOfBirth = dto.DateOfBirth,
            Gender = dto.Gender,
            Status = dto.Status,
            EnrolledAt = DateTime.UtcNow,
            BillingContactName = dto.BillingContactName,
            BillingContactEmail = dto.BillingContactEmail,
            BillingContactPhone = dto.BillingContactPhone,
            BillingAddress = dto.BillingAddress,
            BillingPostalCode = dto.BillingPostalCode,
            BillingCity = dto.BillingCity,
            AutoDebit = dto.AutoDebit,
            Notes = dto.Notes
        };

        await unitOfWork.Students.AddAsync(student, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return (MapToDto(student), null);
    }

    public async Task<(StudentDto? Student, string? Error, bool NotFound)> UpdateAsync(
        Guid id,
        UpdateStudentDto dto,
        CancellationToken ct = default)
    {
        var student = await unitOfWork.Students.GetByIdAsync(id, ct);

        if (student == null)
        {
            return (null, null, true);
        }

        // Check for duplicate email (excluding current student)
        var existing = await unitOfWork.Students.GetByEmailAsync(dto.Email, ct);
        if (existing != null && existing.Id != id)
        {
            return (null, "A student with this email already exists", false);
        }

        student.FirstName = dto.FirstName;
        student.LastName = dto.LastName;
        student.Prefix = dto.Prefix;
        student.Email = dto.Email;
        student.Phone = dto.Phone;
        student.PhoneAlt = dto.PhoneAlt;
        student.Address = dto.Address;
        student.PostalCode = dto.PostalCode;
        student.City = dto.City;
        student.DateOfBirth = dto.DateOfBirth;
        student.Gender = dto.Gender;
        student.Status = dto.Status;
        student.BillingContactName = dto.BillingContactName;
        student.BillingContactEmail = dto.BillingContactEmail;
        student.BillingContactPhone = dto.BillingContactPhone;
        student.BillingAddress = dto.BillingAddress;
        student.BillingPostalCode = dto.BillingPostalCode;
        student.BillingCity = dto.BillingCity;
        student.AutoDebit = dto.AutoDebit;
        student.Notes = dto.Notes;

        await unitOfWork.Students.UpdateAsync(student, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return (MapToDto(student), null, false);
    }

    public async Task<(bool Success, bool NotFound)> DeleteAsync(
        Guid id,
        CancellationToken ct = default)
    {
        var student = await unitOfWork.Students.GetByIdAsync(id, ct);

        if (student == null)
        {
            return (false, true);
        }

        await unitOfWork.Students.DeleteAsync(student, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return (true, false);
    }

    public async Task<(RegistrationFeeStatusDto? FeeStatus, bool NotFound)> GetRegistrationFeeStatusAsync(
        Guid id,
        CancellationToken ct = default)
    {
        var student = await unitOfWork.Students.GetByIdAsync(id, ct);

        if (student == null)
        {
            return (null, true);
        }

        var feeStatus = await registrationFeeService.GetFeeStatusAsync(id, ct);
        return (feeStatus, false);
    }

    public async Task<(bool? HasActiveEnrollments, bool NotFound)> HasActiveEnrollmentsAsync(
        Guid id,
        CancellationToken ct = default)
    {
        var student = await unitOfWork.Students.GetByIdAsync(id, ct);

        if (student == null)
        {
            return (null, true);
        }

        var hasActiveEnrollments = await unitOfWork.Students.HasActiveEnrollmentsAsync(id, ct);
        return (hasActiveEnrollments, false);
    }

    private static StudentDto MapToDto(Student student)
    {
        return new StudentDto
        {
            Id = student.Id,
            FirstName = student.FirstName,
            LastName = student.LastName,
            Prefix = student.Prefix,
            FullName = student.FullName,
            Email = student.Email,
            Phone = student.Phone,
            PhoneAlt = student.PhoneAlt,
            Address = student.Address,
            PostalCode = student.PostalCode,
            City = student.City,
            DateOfBirth = student.DateOfBirth,
            Gender = student.Gender,
            Status = student.Status,
            EnrolledAt = student.EnrolledAt,
            BillingContactName = student.BillingContactName,
            BillingContactEmail = student.BillingContactEmail,
            BillingContactPhone = student.BillingContactPhone,
            BillingAddress = student.BillingAddress,
            BillingPostalCode = student.BillingPostalCode,
            BillingCity = student.BillingCity,
            AutoDebit = student.AutoDebit,
            Notes = student.Notes,
            RegistrationFeePaidAt = student.RegistrationFeePaidAt,
            CreatedAt = student.CreatedAt,
            UpdatedAt = student.UpdatedAt
        };
    }
}
