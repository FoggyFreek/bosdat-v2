using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;

namespace BosDAT.Core.Interfaces.Services;

public interface IStudentService
{
    Task<List<StudentListDto>> GetAllAsync(
        string? search,
        StudentStatus? status,
        CancellationToken ct = default);

    Task<StudentDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<(StudentDto? Student, List<EnrollmentDto> Enrollments, bool NotFound)> GetWithEnrollmentsAsync(
        Guid id,
        CancellationToken ct = default);

    Task<(StudentDto? Student, string? Error)> CreateAsync(
        CreateStudentDto dto,
        CancellationToken ct = default);

    Task<(StudentDto? Student, string? Error, bool NotFound)> UpdateAsync(
        Guid id,
        UpdateStudentDto dto,
        CancellationToken ct = default);

    Task<(bool Success, bool NotFound)> DeleteAsync(
        Guid id,
        CancellationToken ct = default);

    Task<(RegistrationFeeStatusDto? FeeStatus, bool NotFound)> GetRegistrationFeeStatusAsync(
        Guid id,
        CancellationToken ct = default);

    Task<(bool? HasActiveEnrollments, bool NotFound)> HasActiveEnrollmentsAsync(
        Guid id,
        CancellationToken ct = default);
}
