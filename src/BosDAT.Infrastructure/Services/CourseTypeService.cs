using Microsoft.EntityFrameworkCore;
using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Interfaces;

namespace BosDAT.Infrastructure.Services;

public class CourseTypeService(
    IUnitOfWork unitOfWork,
    ICourseTypePricingService pricingService) : ICourseTypeService
{
    public async Task<List<CourseTypeDto>> GetAllAsync(
        bool? activeOnly,
        int? instrumentId,
        CancellationToken ct = default)
    {
        IQueryable<CourseType> query = unitOfWork.Repository<CourseType>().Query()
            .Include(ct => ct.Instrument)
            .Include(ct => ct.PricingVersions);

        if (activeOnly == true)
        {
            query = query.Where(ct => ct.IsActive);
        }

        if (instrumentId.HasValue)
        {
            query = query.Where(ct => ct.InstrumentId == instrumentId.Value);
        }

        var courseTypes = await query
            .OrderBy(ct => ct.Instrument.Name)
            .ThenBy(ct => ct.Name)
            .ToListAsync(ct);

        var result = new List<CourseTypeDto>();
        foreach (var courseType in courseTypes)
        {
            var isInvoiced = await pricingService.IsCurrentPricingInvoicedAsync(courseType.Id, ct);
            result.Add(await MapToDtoAsync(courseType, !isInvoiced, ct));
        }

        return result;
    }

    public async Task<CourseTypeDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var courseType = await unitOfWork.Repository<CourseType>().Query()
            .Include(ct => ct.Instrument)
            .Include(ct => ct.PricingVersions)
            .FirstOrDefaultAsync(ct => ct.Id == id, ct);

        if (courseType == null)
        {
            return null;
        }

        var isInvoiced = await pricingService.IsCurrentPricingInvoicedAsync(id, ct);
        return await MapToDtoAsync(courseType, !isInvoiced, ct);
    }

    public async Task<(CourseTypeDto? CourseType, string? Error)> CreateAsync(
        CreateCourseTypeDto dto,
        CancellationToken ct = default)
    {
        // Validate instrument exists
        var instrument = await unitOfWork.Repository<Instrument>().GetByIdAsync(dto.InstrumentId, ct);
        if (instrument == null)
        {
            return (null, "Instrument not found");
        }

        // Validate pricing
        if (dto.PriceChild > dto.PriceAdult)
        {
            return (null, "Child price cannot be higher than adult price");
        }

        // Create course type
        var courseType = new CourseType
        {
            InstrumentId = dto.InstrumentId,
            Name = dto.Name,
            DurationMinutes = dto.DurationMinutes,
            Type = dto.Type,
            MaxStudents = dto.MaxStudents,
            IsActive = true
        };

        await unitOfWork.Repository<CourseType>().AddAsync(courseType, ct);
        await unitOfWork.SaveChangesAsync(ct);

        // Create initial pricing version
        await pricingService.CreateInitialPricingVersionAsync(
            courseType.Id,
            dto.PriceAdult,
            dto.PriceChild,
            ct);

        // Reload to get navigation properties
        var createdCourseType = await unitOfWork.Repository<CourseType>().Query()
            .Include(ct => ct.Instrument)
            .Include(ct => ct.PricingVersions)
            .FirstAsync(ct => ct.Id == courseType.Id, ct);

        var resultDto = await MapToDtoAsync(createdCourseType, true, ct);
        return (resultDto, null);
    }

    public async Task<(CourseTypeDto? CourseType, string? Error, bool NotFound)> UpdateAsync(
        Guid id,
        UpdateCourseTypeDto dto,
        CancellationToken ct = default)
    {
        var courseType = await unitOfWork.Repository<CourseType>().Query()
            .Include(ct => ct.Instrument)
            .Include(ct => ct.PricingVersions)
            .FirstOrDefaultAsync(ct => ct.Id == id, ct);

        if (courseType == null)
        {
            return (null, null, true);
        }

        // Validate instrument exists
        var instrument = await unitOfWork.Repository<Instrument>().GetByIdAsync(dto.InstrumentId, ct);
        if (instrument == null)
        {
            return (null, "Instrument not found", false);
        }

        // Validate archiving: cannot archive if part of an active course
        if (courseType.IsActive && !dto.IsActive)
        {
            var activeCourseCount = await unitOfWork.Repository<Course>().Query()
                .CountAsync(c => c.CourseTypeId == id && c.Status == CourseStatus.Active, ct);

            if (activeCourseCount > 0)
            {
                return (null, $"Cannot archive course type: {activeCourseCount} active course(s) are using it", false);
            }
        }

        // Update properties
        courseType.InstrumentId = dto.InstrumentId;
        courseType.Name = dto.Name;
        courseType.DurationMinutes = dto.DurationMinutes;
        courseType.Type = dto.Type;
        courseType.MaxStudents = dto.MaxStudents;
        courseType.IsActive = dto.IsActive;
        courseType.Instrument = instrument;

        await unitOfWork.SaveChangesAsync(ct);

        var isInvoiced = await pricingService.IsCurrentPricingInvoicedAsync(id, ct);
        var resultDto = await MapToDtoAsync(courseType, !isInvoiced, ct);
        return (resultDto, null, false);
    }

    public async Task<(bool Success, string? Error, bool NotFound)> DeleteAsync(
        Guid id,
        CancellationToken ct = default)
    {
        var courseType = await unitOfWork.Repository<CourseType>().GetByIdAsync(id, ct);

        if (courseType == null)
        {
            return (false, null, true);
        }

        var activeCourseCount = await unitOfWork.Repository<Course>().Query()
            .CountAsync(c => c.CourseTypeId == id && c.Status == CourseStatus.Active, ct);

        if (activeCourseCount > 0)
        {
            return (false, $"Cannot archive course type: {activeCourseCount} active course(s) are using it", false);
        }

        courseType.IsActive = false;
        await unitOfWork.SaveChangesAsync(ct);

        return (true, null, false);
    }

    public async Task<(CourseTypeDto? CourseType, bool NotFound)> ReactivateAsync(
        Guid id,
        CancellationToken ct = default)
    {
        var courseType = await unitOfWork.Repository<CourseType>().Query()
            .Include(ct => ct.Instrument)
            .Include(ct => ct.PricingVersions)
            .FirstOrDefaultAsync(ct => ct.Id == id, ct);

        if (courseType == null)
        {
            return (null, true);
        }

        courseType.IsActive = true;
        await unitOfWork.SaveChangesAsync(ct);

        var isInvoiced = await pricingService.IsCurrentPricingInvoicedAsync(id, ct);
        var resultDto = await MapToDtoAsync(courseType, !isInvoiced, ct);
        return (resultDto, false);
    }

    public async Task<(int? Count, bool NotFound)> GetTeachersCountForInstrumentAsync(
        int instrumentId,
        CancellationToken ct = default)
    {
        var instrument = await unitOfWork.Repository<Instrument>().GetByIdAsync(instrumentId, ct);
        if (instrument == null)
        {
            return (null, true);
        }

        var teacherCount = await unitOfWork.Repository<TeacherInstrument>().Query()
            .Where(ti => ti.InstrumentId == instrumentId && ti.Teacher.IsActive)
            .CountAsync(ct);

        return (teacherCount, false);
    }

    public async Task<(IEnumerable<CourseTypePricingVersionDto>? History, bool NotFound)> GetPricingHistoryAsync(
        Guid id,
        CancellationToken ct = default)
    {
        var courseType = await unitOfWork.Repository<CourseType>().GetByIdAsync(id, ct);
        if (courseType == null)
        {
            return (null, true);
        }

        var history = await pricingService.GetPricingHistoryAsync(id, ct);
        var dtos = history.Select(MapPricingVersionToDto);
        return (dtos, false);
    }

    public async Task<(PricingEditabilityDto? Result, bool NotFound)> CheckPricingEditabilityAsync(
        Guid id,
        CancellationToken ct = default)
    {
        var courseType = await unitOfWork.Repository<CourseType>().GetByIdAsync(id, ct);
        if (courseType == null)
        {
            return (null, true);
        }

        var isInvoiced = await pricingService.IsCurrentPricingInvoicedAsync(id, ct);

        var result = new PricingEditabilityDto
        {
            CanEditDirectly = !isInvoiced,
            IsInvoiced = isInvoiced,
            Reason = isInvoiced ? "Current pricing has been used in invoices. Create a new version instead." : null
        };

        return (result, false);
    }

    public async Task<(CourseTypePricingVersionDto? Pricing, string? Error, bool NotFound)> UpdatePricingAsync(
        Guid id,
        UpdateCourseTypePricingDto dto,
        CancellationToken ct = default)
    {
        var courseType = await unitOfWork.Repository<CourseType>().GetByIdAsync(id, ct);
        if (courseType == null)
        {
            return (null, null, true);
        }

        if (dto.PriceChild > dto.PriceAdult)
        {
            return (null, "Child price cannot be higher than adult price", false);
        }

        try
        {
            var updatedPricing = await pricingService.UpdateCurrentPricingAsync(
                id,
                dto.PriceAdult,
                dto.PriceChild,
                ct);

            return (MapPricingVersionToDto(updatedPricing), null, false);
        }
        catch (InvalidOperationException ex)
        {
            return (null, ex.Message, false);
        }
    }

    public async Task<(CourseTypePricingVersionDto? Pricing, string? Error, bool NotFound)> CreatePricingVersionAsync(
        Guid id,
        CreateCourseTypePricingVersionDto dto,
        CancellationToken ct = default)
    {
        var courseType = await unitOfWork.Repository<CourseType>().GetByIdAsync(id, ct);
        if (courseType == null)
        {
            return (null, null, true);
        }

        if (dto.PriceChild > dto.PriceAdult)
        {
            return (null, "Child price cannot be higher than adult price", false);
        }

        try
        {
            var newVersion = await pricingService.CreateNewPricingVersionAsync(
                id,
                dto.PriceAdult,
                dto.PriceChild,
                dto.ValidFrom,
                ct);

            return (MapPricingVersionToDto(newVersion), null, false);
        }
        catch (ArgumentException ex)
        {
            return (null, ex.Message, false);
        }
    }

    private async Task<CourseTypeDto> MapToDtoAsync(
        CourseType courseType,
        bool canEditPricingDirectly,
        CancellationToken ct)
    {
        var activeCourseCount = await unitOfWork.Repository<Course>().Query()
            .CountAsync(c => c.CourseTypeId == courseType.Id && c.Status == CourseStatus.Active, ct);

        var hasTeachers = await unitOfWork.Repository<TeacherCourseType>().Query()
            .AnyAsync(tct => tct.CourseTypeId == courseType.Id && tct.Teacher.IsActive, ct);

        var currentPricing = courseType.PricingVersions.FirstOrDefault(pv => pv.IsCurrent);
        var pricingHistory = courseType.PricingVersions
            .OrderByDescending(pv => pv.ValidFrom)
            .Select(MapPricingVersionToDto)
            .ToList();

        return new CourseTypeDto
        {
            Id = courseType.Id,
            InstrumentId = courseType.InstrumentId,
            InstrumentName = courseType.Instrument.Name,
            Name = courseType.Name,
            DurationMinutes = courseType.DurationMinutes,
            Type = courseType.Type,
            MaxStudents = courseType.MaxStudents,
            IsActive = courseType.IsActive,
            ActiveCourseCount = activeCourseCount,
            HasTeachersForCourseType = hasTeachers,
            CurrentPricing = currentPricing != null ? MapPricingVersionToDto(currentPricing) : null,
            PricingHistory = pricingHistory,
            CanEditPricingDirectly = canEditPricingDirectly
        };
    }

    private static CourseTypePricingVersionDto MapPricingVersionToDto(CourseTypePricingVersion pv) => new()
    {
        Id = pv.Id,
        CourseTypeId = pv.CourseTypeId,
        PriceAdult = pv.PriceAdult,
        PriceChild = pv.PriceChild,
        ValidFrom = pv.ValidFrom,
        ValidUntil = pv.ValidUntil,
        IsCurrent = pv.IsCurrent,
        CreatedAt = pv.CreatedAt
    };
}
