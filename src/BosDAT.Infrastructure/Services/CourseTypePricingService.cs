using Microsoft.EntityFrameworkCore;
using BosDAT.Core.Entities;
using BosDAT.Core.Interfaces;
using BosDAT.Infrastructure.Data;

namespace BosDAT.Infrastructure.Services;

public class CourseTypePricingService(ApplicationDbContext context) : ICourseTypePricingService
{
    public async Task<CourseTypePricingVersion?> GetCurrentPricingAsync(Guid courseTypeId, CancellationToken ct = default)
    {
        return await context.CourseTypePricingVersions
            .Where(pv => pv.CourseTypeId == courseTypeId && pv.IsCurrent)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<CourseTypePricingVersion>> GetPricingHistoryAsync(Guid courseTypeId, CancellationToken ct = default)
    {
        return await context.CourseTypePricingVersions
            .Where(pv => pv.CourseTypeId == courseTypeId)
            .OrderByDescending(pv => pv.ValidFrom)
            .ToListAsync(ct);
    }

    public async Task<bool> IsCurrentPricingInvoicedAsync(Guid courseTypeId, CancellationToken ct = default)
    {
        var currentPricing = await GetCurrentPricingAsync(courseTypeId, ct);
        if (currentPricing == null)
            return false;

        return await context.InvoiceLines
            .AnyAsync(il => il.PricingVersionId == currentPricing.Id, ct);
    }

    public async Task<CourseTypePricingVersion> UpdateCurrentPricingAsync(
        Guid courseTypeId,
        decimal priceAdult,
        decimal priceChild,
        CancellationToken ct = default)
    {
        var isInvoiced = await IsCurrentPricingInvoicedAsync(courseTypeId, ct);
        if (isInvoiced)
        {
            throw new InvalidOperationException(
                "Cannot directly edit pricing that has been used in invoices. Create a new pricing version instead.");
        }

        var currentPricing = await GetCurrentPricingAsync(courseTypeId, ct);
        if (currentPricing == null)
        {
            throw new InvalidOperationException("No current pricing version found for this course type.");
        }

        currentPricing.PriceAdult = priceAdult;
        currentPricing.PriceChild = priceChild;
        currentPricing.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);
        return currentPricing;
    }

    public async Task<CourseTypePricingVersion> CreateNewPricingVersionAsync(
        Guid courseTypeId,
        decimal priceAdult,
        decimal priceChild,
        DateOnly validFrom,
        CancellationToken ct = default)
    {
        if (validFrom < DateOnly.FromDateTime(DateTime.Today))
        {
            throw new ArgumentException("ValidFrom date cannot be in the past.", nameof(validFrom));
        }

        var currentPricing = await GetCurrentPricingAsync(courseTypeId, ct);

        if (currentPricing != null)
        {
            // Close the current version by setting ValidUntil to the day before the new version starts
            currentPricing.ValidUntil = validFrom.AddDays(-1);
            currentPricing.IsCurrent = false;
            currentPricing.UpdatedAt = DateTime.UtcNow;
        }

        var newPricingVersion = new CourseTypePricingVersion
        {
            Id = Guid.NewGuid(),
            CourseTypeId = courseTypeId,
            PriceAdult = priceAdult,
            PriceChild = priceChild,
            ValidFrom = validFrom,
            ValidUntil = null,
            IsCurrent = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.CourseTypePricingVersions.Add(newPricingVersion);
        await context.SaveChangesAsync(ct);

        return newPricingVersion;
    }

    public async Task<CourseTypePricingVersion?> GetPricingForDateAsync(
        Guid courseTypeId,
        DateOnly date,
        CancellationToken ct = default)
    {
        return await context.CourseTypePricingVersions
            .Where(pv => pv.CourseTypeId == courseTypeId
                && pv.ValidFrom <= date
                && (pv.ValidUntil == null || pv.ValidUntil >= date))
            .OrderByDescending(pv => pv.ValidFrom)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<CourseTypePricingVersion> CreateInitialPricingVersionAsync(
        Guid courseTypeId,
        decimal priceAdult,
        decimal priceChild,
        CancellationToken ct = default)
    {
        var existingPricing = await GetCurrentPricingAsync(courseTypeId, ct);
        if (existingPricing != null)
        {
            throw new InvalidOperationException("A pricing version already exists for this course type.");
        }

        var initialPricingVersion = new CourseTypePricingVersion
        {
            Id = Guid.NewGuid(),
            CourseTypeId = courseTypeId,
            PriceAdult = priceAdult,
            PriceChild = priceChild,
            ValidFrom = DateOnly.FromDateTime(DateTime.Today),
            ValidUntil = null,
            IsCurrent = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.CourseTypePricingVersions.Add(initialPricingVersion);
        await context.SaveChangesAsync(ct);

        return initialPricingVersion;
    }
}
