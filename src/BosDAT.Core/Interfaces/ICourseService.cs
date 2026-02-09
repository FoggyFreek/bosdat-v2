using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;

namespace BosDAT.Core.Interfaces;

public interface ICourseService
{
    Task<List<CourseListDto>> GetSummaryAsync(
        CourseStatus? status, Guid? teacherId, DayOfWeek? dayOfWeek, int? roomId,
        CancellationToken ct = default);

    Task<int> GetCountAsync(
        CourseStatus? status, Guid? teacherId, DayOfWeek? dayOfWeek, int? roomId,
        CancellationToken ct = default);

    Task<List<CourseDto>> GetAllAsync(
        CourseStatus? status, Guid? teacherId, DayOfWeek? dayOfWeek, int? roomId,
        CancellationToken ct = default);

    Task<CourseDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<(CourseDto? Course, string? Error)> CreateAsync(CreateCourseDto dto, CancellationToken ct = default);

    Task<(CourseDto? Course, bool NotFound)> UpdateAsync(Guid id, UpdateCourseDto dto, CancellationToken ct = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}
