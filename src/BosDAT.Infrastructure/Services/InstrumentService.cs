using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Interfaces;
using BosDAT.Core.Interfaces.Services;

namespace BosDAT.Infrastructure.Services;

public class InstrumentService(IUnitOfWork unitOfWork) : IInstrumentService
{
    public async Task<List<InstrumentDto>> GetAllAsync(
        bool? activeOnly,
        CancellationToken ct = default)
    {
        var instruments = await unitOfWork.Instruments.GetFilteredAsync(activeOnly, ct);

        return instruments.Select(i => new InstrumentDto
        {
            Id = i.Id,
            Name = i.Name,
            Category = i.Category,
            IsActive = i.IsActive
        }).ToList();
    }

    public async Task<(InstrumentDto? Instrument, bool NotFound)> GetByIdAsync(
        int id,
        CancellationToken ct = default)
    {
        var instrument = await unitOfWork.Instruments.GetByIdAsync(id, ct);

        if (instrument == null)
        {
            return (null, true);
        }

        return (new InstrumentDto
        {
            Id = instrument.Id,
            Name = instrument.Name,
            Category = instrument.Category,
            IsActive = instrument.IsActive
        }, false);
    }

    public async Task<(InstrumentDto? Instrument, string? Error)> CreateAsync(
        CreateInstrumentDto dto,
        CancellationToken ct = default)
    {
        // Check for duplicate name
        if (await unitOfWork.Instruments.ExistsByNameAsync(dto.Name, cancellationToken: ct))
        {
            return (null, "An instrument with this name already exists");
        }

        var instrument = new Instrument
        {
            Name = dto.Name,
            Category = dto.Category,
            IsActive = true
        };

        await unitOfWork.Instruments.AddAsync(instrument, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return (new InstrumentDto
        {
            Id = instrument.Id,
            Name = instrument.Name,
            Category = instrument.Category,
            IsActive = instrument.IsActive
        }, null);
    }

    public async Task<(InstrumentDto? Instrument, string? Error, bool NotFound)> UpdateAsync(
        int id,
        UpdateInstrumentDto dto,
        CancellationToken ct = default)
    {
        var instrument = await unitOfWork.Instruments.GetByIdAsync(id, ct);

        if (instrument == null)
        {
            return (null, null, true);
        }

        // Check for duplicate name (excluding current)
        if (await unitOfWork.Instruments.ExistsByNameAsync(dto.Name, excludeId: id, cancellationToken: ct))
        {
            return (null, "An instrument with this name already exists", false);
        }

        instrument.Name = dto.Name;
        instrument.Category = dto.Category;
        instrument.IsActive = dto.IsActive;

        await unitOfWork.Instruments.UpdateAsync(instrument, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return (new InstrumentDto
        {
            Id = instrument.Id,
            Name = instrument.Name,
            Category = instrument.Category,
            IsActive = instrument.IsActive
        }, null, false);
    }

    public async Task<(bool Success, bool NotFound)> DeleteAsync(
        int id,
        CancellationToken ct = default)
    {
        var instrument = await unitOfWork.Instruments.GetByIdAsync(id, ct);

        if (instrument == null)
        {
            return (false, true);
        }

        // Instead of deleting, deactivate
        instrument.IsActive = false;
        await unitOfWork.SaveChangesAsync(ct);

        return (true, false);
    }
}
