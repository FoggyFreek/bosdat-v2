using BosDAT.Core.Entities;

namespace BosDAT.Core.Interfaces;

public interface ITeacherRepository : IRepository<Teacher>
{
    Task<Teacher?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<Teacher?> GetWithInstrumentsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Teacher?> GetWithCoursesAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Teacher>> GetActiveTeachersAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Teacher>> GetTeachersByInstrumentAsync(int instrumentId, CancellationToken cancellationToken = default);
}
