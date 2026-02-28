using Microsoft.EntityFrameworkCore;
using BosDAT.Core.Entities;
using BosDAT.Core.Interfaces;
using BosDAT.Core.Interfaces.Repositories;
using BosDAT.Infrastructure.Data;

namespace BosDAT.Infrastructure.Repositories;

public class InstrumentRepository : Repository<Instrument>, IInstrumentRepository
{
    public InstrumentRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Instrument>> GetFilteredAsync(bool? activeOnly, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsNoTracking().AsQueryable();

        if (activeOnly == true)
            query = query.Where(i => i.IsActive);

        return await query.OrderBy(i => i.Name).ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(string name, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(i => EF.Functions.ILike(i.Name, name));

        if (excludeId.HasValue)
        {
            query = query.Where(i => i.Id != excludeId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }
}
