using BosDAT.Core.DTOs;

namespace BosDAT.Core.Interfaces;

public interface ISchedulingService
{
    Task<SchedulingStatusDto> GetSchedulingStatusAsync(CancellationToken ct = default);

    Task<ScheduleRunsPageDto> GetScheduleRunsAsync(
        int page, int pageSize,
        CancellationToken ct = default);

    Task<ManualRunResultDto> ExecuteManualRunAsync(
        DateOnly startDate, DateOnly endDate,
        CancellationToken ct = default);

    Task<ManualRunResultDto> ExecuteSingleCourseRunAsync(
        Guid courseId, DateOnly startDate, DateOnly endDate,
        CancellationToken ct = default);
}
