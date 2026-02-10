using Microsoft.EntityFrameworkCore;
using BosDAT.Core.Entities;
using BosDAT.Core.Interfaces;

namespace BosDAT.Infrastructure.Services;

public class SettingsService(IUnitOfWork unitOfWork) : ISettingsService
{
    public async Task<List<Setting>> GetAllAsync(CancellationToken ct = default)
    {
        return await unitOfWork.Repository<Setting>().Query()
            .OrderBy(s => s.Key)
            .ToListAsync(ct);
    }

    public async Task<Setting?> GetByKeyAsync(string key, CancellationToken ct = default)
    {
        return await unitOfWork.Repository<Setting>().Query()
            .FirstOrDefaultAsync(s => s.Key == key, ct);
    }

    public async Task<Setting?> UpdateAsync(string key, string value, CancellationToken ct = default)
    {
        var setting = await GetByKeyAsync(key, ct);

        if (setting == null)
        {
            return null;
        }

        setting.Value = value;
        await unitOfWork.SaveChangesAsync(ct);

        return setting;
    }
}
