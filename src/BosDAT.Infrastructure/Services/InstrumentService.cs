using Microsoft.EntityFrameworkCore;
using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Interfaces;

namespace BosDAT.Infrastructure.Services;

public class InstrumentService(IUnitOfWork unitOfWork) : IInstrumentService
{
    public async Task<List<InstrumentDto>> GetAllAsync(
        bool? activeOnly,
        CancellationToken ct = default)
    {
        var query = unitOfWork.Repository<Instrument>().Query();

        if (activeOnly == true)
        {
            query = query.Where(i => i.IsActive);
        }

        var instruments = await query
            .OrderBy(i => i.Name)
            .Select(i => new InstrumentDto
            {
                Id = i.Id,
                Name = i.Name,
                Category = i.Category,
                IsActive = i.IsActive
            })
            .ToListAsync(ct);

        return instruments;
    }

    public async Task<(InstrumentDto? Instrument, bool NotFound)> GetByIdAsync(
        int id,
        CancellationToken ct = default)
    {
        var instrument = await unitOfWork.Repository<Instrument>().GetByIdAsync(id, ct);

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
        var existing = await unitOfWork.Repository<Instrument>()
            .FirstOrDefaultAsync(i => string.Equals(i.Name, dto.Name, StringComparison.OrdinalIgnoreCase), ct);

        if (existing != null)
        {
            return (null, "An instrument with this name already exists");
        }

        var instrument = new Instrument
        {
            Name = dto.Name,
            Category = dto.Category,
            IsActive = true
        };

        await unitOfWork.Repository<Instrument>().AddAsync(instrument, ct);
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
        var instrument = await unitOfWork.Repository<Instrument>().GetByIdAsync(id, ct);

        if (instrument == null)
        {
            return (null, null, true);
        }

        // Check for duplicate name (excluding current)
        var existing = await unitOfWork.Repository<Instrument>()
            .FirstOrDefaultAsync(i => string.Equals(i.Name, dto.Name, StringComparison.OrdinalIgnoreCase) && i.Id != id, ct);

        if (existing != null)
        {
            return (null, "An instrument with this name already exists", false);
        }

        instrument.Name = dto.Name;
        instrument.Category = dto.Category;
        instrument.IsActive = dto.IsActive;

        await unitOfWork.Repository<Instrument>().UpdateAsync(instrument, ct);
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
        var instrument = await unitOfWork.Repository<Instrument>().GetByIdAsync(id, ct);

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
