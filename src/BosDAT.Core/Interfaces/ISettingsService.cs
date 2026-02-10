using BosDAT.Core.Entities;

namespace BosDAT.Core.Interfaces;

public interface ISettingsService
{
    Task<List<Setting>> GetAllAsync(CancellationToken ct = default);
    Task<Setting?> GetByKeyAsync(string key, CancellationToken ct = default);
    Task<Setting?> UpdateAsync(string key, string value, CancellationToken ct = default);
}
