using BosDAT.Core.DTOs;

namespace BosDAT.Core.Interfaces;

public interface IHolidayService
{
    Task<IEnumerable<HolidayDto>> GetAllAsync(CancellationToken ct = default);
    Task<HolidayDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<HolidayDto> CreateAsync(CreateHolidayDto dto, CancellationToken ct = default);
    Task<HolidayDto?> UpdateAsync(int id, UpdateHolidayDto dto, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
}
