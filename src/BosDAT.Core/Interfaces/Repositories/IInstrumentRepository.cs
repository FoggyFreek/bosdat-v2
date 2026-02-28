using BosDAT.Core.Entities;

namespace BosDAT.Core.Interfaces.Repositories;

public interface IInstrumentRepository : IRepository<Instrument>
{
    Task<IReadOnlyList<Instrument>> GetFilteredAsync(bool? activeOnly, CancellationToken cancellationToken = default);
    Task<bool> ExistsByNameAsync(string name, int? excludeId = null, CancellationToken cancellationToken = default);
}
