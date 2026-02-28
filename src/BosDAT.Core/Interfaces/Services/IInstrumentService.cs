using BosDAT.Core.DTOs;

namespace BosDAT.Core.Interfaces.Services;

public interface IInstrumentService
{
    Task<List<InstrumentDto>> GetAllAsync(
        bool? activeOnly,
        CancellationToken ct = default);

    Task<(InstrumentDto? Instrument, bool NotFound)> GetByIdAsync(
        int id,
        CancellationToken ct = default);

    Task<(InstrumentDto? Instrument, string? Error)> CreateAsync(
        CreateInstrumentDto dto,
        CancellationToken ct = default);

    Task<(InstrumentDto? Instrument, string? Error, bool NotFound)> UpdateAsync(
        int id,
        UpdateInstrumentDto dto,
        CancellationToken ct = default);

    Task<(bool Success, bool NotFound)> DeleteAsync(
        int id,
        CancellationToken ct = default);
}
