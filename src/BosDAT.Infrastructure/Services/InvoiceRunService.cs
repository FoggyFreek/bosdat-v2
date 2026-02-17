using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Enums;
using BosDAT.Core.Interfaces;
using BosDAT.Infrastructure.Data;

namespace BosDAT.Infrastructure.Services;

public class InvoiceRunService(
    ApplicationDbContext context,
    IInvoiceGenerationService invoiceGenerationService) : IInvoiceRunService
{
    public async Task<InvoiceRunResultDto> RunBulkInvoiceGenerationAsync(
        StartInvoiceRunDto dto, string initiatedBy, Guid userId, CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();

        var batchDto = new GenerateBatchInvoicesDto
        {
            PeriodStart = dto.PeriodStart,
            PeriodEnd = dto.PeriodEnd,
            PeriodType = dto.PeriodType,
        };

        var enrollmentCount = await context.Enrollments
            .AsNoTracking()
            .CountAsync(e => e.Status == EnrollmentStatus.Active && e.InvoicingPreference == dto.PeriodType, ct);

        InvoiceRun invoiceRun;

        try
        {
            var invoices = await invoiceGenerationService.GenerateBatchInvoicesAsync(batchDto, userId, ct);
            stopwatch.Stop();

            var totalAmount = invoices.Sum(i => i.Total);
            var skipped = enrollmentCount - invoices.Count;

            var status = invoices.Count == 0 && enrollmentCount > 0
                ? InvoiceRunStatus.PartialSuccess
                : InvoiceRunStatus.Success;

            invoiceRun = new InvoiceRun
            {
                Id = Guid.NewGuid(),
                PeriodStart = dto.PeriodStart,
                PeriodEnd = dto.PeriodEnd,
                PeriodType = dto.PeriodType,
                TotalEnrollmentsProcessed = enrollmentCount,
                TotalInvoicesGenerated = invoices.Count,
                TotalSkipped = skipped,
                TotalFailed = 0,
                TotalAmount = totalAmount,
                DurationMs = stopwatch.ElapsedMilliseconds,
                Status = status,
                InitiatedBy = initiatedBy
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            invoiceRun = new InvoiceRun
            {
                Id = Guid.NewGuid(),
                PeriodStart = dto.PeriodStart,
                PeriodEnd = dto.PeriodEnd,
                PeriodType = dto.PeriodType,
                TotalEnrollmentsProcessed = enrollmentCount,
                TotalInvoicesGenerated = 0,
                TotalSkipped = 0,
                TotalFailed = enrollmentCount,
                TotalAmount = 0,
                DurationMs = stopwatch.ElapsedMilliseconds,
                Status = InvoiceRunStatus.Failed,
                ErrorMessage = ex.Message,
                InitiatedBy = initiatedBy
            };
        }

        context.InvoiceRuns.Add(invoiceRun);
        await context.SaveChangesAsync(ct);

        return new InvoiceRunResultDto
        {
            InvoiceRunId = invoiceRun.Id,
            PeriodStart = invoiceRun.PeriodStart,
            PeriodEnd = invoiceRun.PeriodEnd,
            PeriodType = invoiceRun.PeriodType,
            TotalEnrollmentsProcessed = invoiceRun.TotalEnrollmentsProcessed,
            TotalInvoicesGenerated = invoiceRun.TotalInvoicesGenerated,
            TotalSkipped = invoiceRun.TotalSkipped,
            TotalFailed = invoiceRun.TotalFailed,
            TotalAmount = invoiceRun.TotalAmount,
            DurationMs = invoiceRun.DurationMs,
            Status = invoiceRun.Status
        };
    }

    public async Task<InvoiceRunsPageDto> GetRunsAsync(int page, int pageSize, CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 50) pageSize = 5;

        var totalCount = await context.InvoiceRuns
            .AsNoTracking()
            .CountAsync(ct);

        var items = await context.InvoiceRuns
            .AsNoTracking()
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new InvoiceRunDto
            {
                Id = r.Id,
                PeriodStart = r.PeriodStart,
                PeriodEnd = r.PeriodEnd,
                PeriodType = r.PeriodType,
                TotalEnrollmentsProcessed = r.TotalEnrollmentsProcessed,
                TotalInvoicesGenerated = r.TotalInvoicesGenerated,
                TotalSkipped = r.TotalSkipped,
                TotalFailed = r.TotalFailed,
                TotalAmount = r.TotalAmount,
                DurationMs = r.DurationMs,
                Status = r.Status,
                ErrorMessage = r.ErrorMessage,
                InitiatedBy = r.InitiatedBy,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync(ct);

        return new InvoiceRunsPageDto
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<InvoiceRunDto?> GetRunByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await context.InvoiceRuns
            .AsNoTracking()
            .Where(r => r.Id == id)
            .Select(r => new InvoiceRunDto
            {
                Id = r.Id,
                PeriodStart = r.PeriodStart,
                PeriodEnd = r.PeriodEnd,
                PeriodType = r.PeriodType,
                TotalEnrollmentsProcessed = r.TotalEnrollmentsProcessed,
                TotalInvoicesGenerated = r.TotalInvoicesGenerated,
                TotalSkipped = r.TotalSkipped,
                TotalFailed = r.TotalFailed,
                TotalAmount = r.TotalAmount,
                DurationMs = r.DurationMs,
                Status = r.Status,
                ErrorMessage = r.ErrorMessage,
                InitiatedBy = r.InitiatedBy,
                CreatedAt = r.CreatedAt
            })
            .FirstOrDefaultAsync(ct);
    }
}
