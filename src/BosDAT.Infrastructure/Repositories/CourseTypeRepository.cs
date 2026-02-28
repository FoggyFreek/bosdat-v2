using Microsoft.EntityFrameworkCore;
using BosDAT.Core.Entities;
using BosDAT.Core.Interfaces;
using BosDAT.Core.Interfaces.Repositories;
using BosDAT.Infrastructure.Data;

namespace BosDAT.Infrastructure.Repositories;

public class CourseTypeRepository : Repository<CourseType>, ICourseTypeRepository
{
    public CourseTypeRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<List<CourseType>> GetByIdsAsync(List<Guid> ids, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(ct => ids.Contains(ct.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<List<CourseType>> GetActiveByInstrumentIdsAsync(
        List<int> instrumentIds,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(ct => ct.Instrument)
            .Where(ct => ct.IsActive && instrumentIds.Contains(ct.InstrumentId))
            .OrderBy(ct => ct.Instrument.Name)
            .ThenBy(ct => ct.Name)
            .ToListAsync(cancellationToken);
    }
}
