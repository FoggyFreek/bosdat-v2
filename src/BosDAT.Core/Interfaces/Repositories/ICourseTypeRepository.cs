using BosDAT.Core.Entities;

namespace BosDAT.Core.Interfaces.Repositories;

public interface ICourseTypeRepository : IRepository<CourseType>
{
    Task<List<CourseType>> GetByIdsAsync(List<Guid> ids, CancellationToken cancellationToken = default);
    Task<List<CourseType>> GetActiveByInstrumentIdsAsync(List<int> instrumentIds, CancellationToken cancellationToken = default);
}
