using Microsoft.EntityFrameworkCore;
using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Interfaces;

namespace BosDAT.Infrastructure.Services;

public class HolidayService(IUnitOfWork unitOfWork) : IHolidayService
{
    public async Task<IEnumerable<HolidayDto>> GetAllAsync(CancellationToken ct = default)
    {
        return await unitOfWork.Repository<Holiday>().Query()
            .OrderBy(h => h.StartDate)
            .Select(h => new HolidayDto
            {
                Id = h.Id,
                Name = h.Name,
                StartDate = h.StartDate,
                EndDate = h.EndDate
            })
            .ToListAsync(ct);
    }

    public async Task<HolidayDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var holiday = await unitOfWork.Repository<Holiday>().GetByIdAsync(id, ct);

        if (holiday == null)
        {
            return null;
        }

        return new HolidayDto
        {
            Id = holiday.Id,
            Name = holiday.Name,
            StartDate = holiday.StartDate,
            EndDate = holiday.EndDate
        };
    }

    public async Task<HolidayDto> CreateAsync(CreateHolidayDto dto, CancellationToken ct = default)
    {
        var holiday = new Holiday
        {
            Name = dto.Name,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate
        };

        await unitOfWork.Repository<Holiday>().AddAsync(holiday, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return new HolidayDto
        {
            Id = holiday.Id,
            Name = holiday.Name,
            StartDate = holiday.StartDate,
            EndDate = holiday.EndDate
        };
    }

    public async Task<HolidayDto?> UpdateAsync(int id, UpdateHolidayDto dto, CancellationToken ct = default)
    {
        var holiday = await unitOfWork.Repository<Holiday>().GetByIdAsync(id, ct);

        if (holiday == null)
        {
            return null;
        }

        holiday.Name = dto.Name;
        holiday.StartDate = dto.StartDate;
        holiday.EndDate = dto.EndDate;

        await unitOfWork.SaveChangesAsync(ct);

        return new HolidayDto
        {
            Id = holiday.Id,
            Name = holiday.Name,
            StartDate = holiday.StartDate,
            EndDate = holiday.EndDate
        };
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var holiday = await unitOfWork.Repository<Holiday>().GetByIdAsync(id, ct);

        if (holiday == null)
        {
            return false;
        }

        await unitOfWork.Repository<Holiday>().DeleteAsync(holiday, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return true;
    }
}
