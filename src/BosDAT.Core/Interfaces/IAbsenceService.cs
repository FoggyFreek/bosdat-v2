using BosDAT.Core.DTOs;

namespace BosDAT.Core.Interfaces;

public interface IAbsenceService
{
    Task<IEnumerable<AbsenceDto>> GetAllAsync(CancellationToken ct = default);
    Task<IEnumerable<AbsenceDto>> GetByStudentAsync(Guid studentId, CancellationToken ct = default);
    Task<IEnumerable<AbsenceDto>> GetByTeacherAsync(Guid teacherId, CancellationToken ct = default);
    Task<AbsenceDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<AbsenceDto> CreateAsync(CreateAbsenceDto dto, CancellationToken ct = default);
    Task<AbsenceDto?> UpdateAsync(Guid id, UpdateAbsenceDto dto, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<bool> IsTeacherAbsentAsync(Guid teacherId, DateOnly date, CancellationToken ct = default);
    Task<bool> IsStudentAbsentAsync(Guid studentId, DateOnly date, CancellationToken ct = default);
    Task<IEnumerable<AbsenceDto>> GetTeacherAbsencesForPeriodAsync(DateOnly startDate, DateOnly endDate, CancellationToken ct = default);
}
