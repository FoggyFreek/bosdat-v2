using BosDAT.Core.DTOs;

namespace BosDAT.Core.Interfaces.Services;

public interface ITeacherService
{
    Task<List<TeacherListDto>> GetAllAsync(
        bool? activeOnly,
        int? instrumentId,
        Guid? courseTypeId,
        CancellationToken ct = default);

    Task<TeacherDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<(TeacherDto Teacher, List<CourseListDto> Courses)?> GetWithCoursesAsync(
        Guid id,
        CancellationToken ct = default);

    Task<TeacherDto> CreateAsync(CreateTeacherDto dto, CancellationToken ct = default);

    Task<TeacherDto?> UpdateAsync(Guid id, UpdateTeacherDto dto, CancellationToken ct = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);

    Task<List<TeacherAvailabilityDto>?> GetAvailabilityAsync(
        Guid id,
        CancellationToken ct = default);

    Task<List<TeacherAvailabilityDto>?> UpdateAvailabilityAsync(
        Guid id,
        List<UpdateTeacherAvailabilityDto> dtos,
        CancellationToken ct = default);

    Task<List<CourseTypeSimpleDto>?> GetAvailableCourseTypesAsync(
        Guid id,
        string? instrumentIds,
        CancellationToken ct = default);
}
