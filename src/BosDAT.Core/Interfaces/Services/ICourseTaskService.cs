using BosDAT.Core.DTOs;

namespace BosDAT.Core.Interfaces.Services;

public interface ICourseTaskService
{
    Task<IEnumerable<CourseTaskDto>> GetByCourseAsync(Guid courseId, CancellationToken ct = default);
    Task<CourseTaskDto?> CreateAsync(Guid courseId, CreateCourseTaskDto dto, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid taskId, CancellationToken ct = default);
}
