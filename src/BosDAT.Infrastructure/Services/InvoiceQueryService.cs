using Microsoft.EntityFrameworkCore;
using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Enums;
using BosDAT.Core.Interfaces;
using BosDAT.Infrastructure.Data;

namespace BosDAT.Infrastructure.Services;

public class InvoiceQueryService(ApplicationDbContext context) : IInvoiceQueryService
{
    private static readonly string[] MonthAbbreviations =
    [
        "jan", "feb", "mar", "apr", "may", "jun",
        "jul", "aug", "sep", "oct", "nov", "dec"
    ];

    public async Task<InvoiceDto?> GetInvoiceAsync(Guid invoiceId, CancellationToken ct = default)
    {
        var invoice = await InvoiceDetailQuery()
            .FirstOrDefaultAsync(i => i.Id == invoiceId, ct);

        if (invoice == null) return null;

        return MapToDto(invoice);
    }

    public async Task<InvoiceDto?> GetByInvoiceNumberAsync(string invoiceNumber, CancellationToken ct = default)
    {
        var invoice = await InvoiceDetailQuery()
            .FirstOrDefaultAsync(i => i.InvoiceNumber == invoiceNumber, ct);

        if (invoice == null) return null;

        return MapToDto(invoice);
    }
    public async Task<IReadOnlyList<InvoiceListDto>> GetStudentInvoicesAsync(Guid studentId, CancellationToken ct = default)
    {
        var invoices = await context.Invoices
            .Include(i => i.Student)
            .Include(i => i.Payments)
            .Include(i => i.LedgerApplications)
            .Where(i => i.StudentId == studentId)
            .OrderByDescending(i => i.IssueDate)
            .AsNoTracking()
            .ToListAsync(ct);

        return invoices.Select(MapToListDto).ToList();
    }

    public async Task<IReadOnlyList<InvoiceListDto>> GetByStatusAsync(InvoiceStatus status, CancellationToken ct = default)
    {
        var invoices = await context.Invoices
            .Include(i => i.Student)
            .Include(i => i.Payments)
            .Include(i => i.LedgerApplications)
            .Where(i => i.Status == status)
            .OrderByDescending(i => i.IssueDate)
            .AsNoTracking()
            .ToListAsync(ct);

        return invoices.Select(MapToListDto).ToList();
    }

    public async Task<SchoolBillingInfoDto> GetSchoolBillingInfoAsync(CancellationToken ct = default)
    {
        var settings = await context.Settings
            .Where(s => s.Key.StartsWith("school_") || s.Key == "vat_rate")
            .ToDictionaryAsync(s => s.Key, s => s.Value, ct);

        return new SchoolBillingInfoDto
        {
            Name = settings.GetValueOrDefault("school_name", ""),
            Address = settings.GetValueOrDefault("school_address"),
            PostalCode = settings.GetValueOrDefault("school_postal_code"),
            City = settings.GetValueOrDefault("school_city"),
            Phone = settings.GetValueOrDefault("school_phone"),
            Email = settings.GetValueOrDefault("school_email"),
            KvkNumber = settings.GetValueOrDefault("school_kvk"),
            Iban = settings.GetValueOrDefault("school_iban"),
            VatRate = decimal.TryParse(settings.GetValueOrDefault("vat_rate", "21"), out var vat) ? vat : 21m
        };
    }

    public string GeneratePeriodDescription(DateOnly periodStart, DateOnly periodEnd, InvoicingPreference periodType)
    {
        var startMonth = MonthAbbreviations[periodStart.Month - 1];
        var yearSuffix = (periodStart.Year % 100).ToString("D2");

        if (periodType == InvoicingPreference.Monthly)
        {
            return $"{startMonth}{yearSuffix}";
        }

        // Quarterly
        var endMonth = MonthAbbreviations[periodEnd.Month - 1];
        return $"{startMonth}-{endMonth}{yearSuffix}";
    }

    public async Task<decimal> GetVatRateAsync(CancellationToken ct = default)
    {
        var setting = await context.Settings
            .FirstOrDefaultAsync(s => s.Key == "vat_rate", ct);

        return decimal.TryParse(setting?.Value, out var rate) ? rate : 21m;
    }

    public async Task<int> GetPaymentDueDaysAsync(CancellationToken ct = default)
    {
        var setting = await context.Settings
            .FirstOrDefaultAsync(s => s.Key == "payment_due_days", ct);

        return int.TryParse(setting?.Value, out var days) ? days : 14;
    }

     internal static PaymentDto MapToDto(Payment payment)
    {
        return new PaymentDto
            {
                Id = payment.Id,
                InvoiceId = payment.InvoiceId,
                Amount = payment.Amount,
                PaymentDate = payment.PaymentDate,
                Method = payment.Method,
                Reference = payment.Reference,
                Notes = payment.Notes,
                CreatedAt = payment.CreatedAt
            };
    }
    
    internal static InvoiceDto MapToDto(Invoice invoice)
    {
        var student = invoice.Student;
        var billingContact = BuildBillingContact(student);

        var amountPaid = invoice.Payments.Sum(p => p.Amount);
        var totalOwed = invoice.Total;
        var balance = totalOwed - amountPaid;

        return new InvoiceDto
        {
            Id = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            StudentId = invoice.StudentId,
            EnrollmentId = invoice.EnrollmentId,
            StudentName = student.FullName,
            StudentEmail = student.Email,
            IssueDate = invoice.IssueDate,
            DueDate = invoice.DueDate,
            Description = invoice.Description,
            PeriodStart = invoice.PeriodStart,
            PeriodEnd = invoice.PeriodEnd,
            PeriodType = invoice.PeriodType,
            Subtotal = invoice.Subtotal,
            VatAmount = invoice.VatAmount,
            Total = invoice.Total,
            DiscountAmount = invoice.DiscountAmount,
            LedgerCreditsApplied = invoice.LedgerCreditsApplied,
            LedgerDebitsApplied = invoice.LedgerDebitsApplied,
            Status = invoice.Status,
            PaidAt = invoice.PaidAt,
            PaymentMethod = invoice.PaymentMethod,
            Notes = invoice.Notes,
            Lines = invoice.Lines.Select(l => new InvoiceLineDto
            {
                Id = l.Id,
                LessonId = l.LessonId,
                PricingVersionId = l.PricingVersionId,
                Description = l.Description,
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice,
                VatRate = l.VatRate,
                LineTotal = l.LineTotal,
                LessonDate = l.Lesson?.ScheduledDate,
                CourseName = l.Lesson?.Course?.CourseType?.Name
            }).ToList(),
            Payments = invoice.Payments.Select(p => new PaymentDto
            {
                Id = p.Id,
                InvoiceId = p.InvoiceId,
                Amount = p.Amount,
                PaymentDate = p.PaymentDate,
                Method = p.Method,
                Reference = p.Reference,
                Notes = p.Notes
            }).ToList(),
            LedgerApplications = invoice.LedgerApplications.Select(la => new InvoiceLedgerApplicationDto
            {
                Id = la.Id,
                LedgerEntryId = la.LedgerEntryId,
                CorrectionRefName = la.LedgerEntry?.CorrectionRefName,
                Description = la.LedgerEntry?.Description,
                AppliedAmount = la.AppliedAmount,
                AppliedAt = la.CreatedAt,
                EntryType = la.LedgerEntry?.EntryType ?? LedgerEntryType.Credit
            }).ToList(),
            AmountPaid = amountPaid,
            Balance = balance,
            CreatedAt = invoice.CreatedAt,
            UpdatedAt = invoice.UpdatedAt,
            BillingContact = billingContact
        };
    }

    internal static InvoiceListDto MapToListDto(Invoice invoice)
    {
        var amountPaid = invoice.Payments.Sum(p => p.Amount);
        var totalOwed = invoice.Total;

        return new InvoiceListDto
        {
            Id = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            StudentName = invoice.Student.FullName,
            Description = invoice.Description,
            IssueDate = invoice.IssueDate,
            DueDate = invoice.DueDate,
            PeriodStart = invoice.PeriodStart,
            PeriodEnd = invoice.PeriodEnd,
            Total = totalOwed,
            Status = invoice.Status,
            Balance = totalOwed - amountPaid
        };
    }

    private IQueryable<Invoice> InvoiceDetailQuery()
    {
        return context.Invoices
            .Include(i => i.Student)
            .Include(i => i.Lines)
                .ThenInclude(l => l.Lesson)
                    .ThenInclude(le => le!.Course)
                        .ThenInclude(c => c.CourseType)
            .Include(i => i.Payments)
            .Include(i => i.LedgerApplications)
                .ThenInclude(la => la.LedgerEntry)
            .AsNoTracking();
    }

    private static BillingContactDto BuildBillingContact(Student student)
    {
        return new BillingContactDto
        {
            Name = !string.IsNullOrEmpty(student.BillingContactName)
                ? student.BillingContactName
                : student.FullName,
            Email = !string.IsNullOrEmpty(student.BillingContactEmail)
                ? student.BillingContactEmail
                : student.Email,
            Phone = !string.IsNullOrEmpty(student.BillingContactPhone)
                ? student.BillingContactPhone
                : student.Phone,
            Address = !string.IsNullOrEmpty(student.BillingAddress)
                ? student.BillingAddress
                : student.Address,
            PostalCode = !string.IsNullOrEmpty(student.BillingPostalCode)
                ? student.BillingPostalCode
                : student.PostalCode,
            City = !string.IsNullOrEmpty(student.BillingCity)
                ? student.BillingCity
                : student.City
        };
    }
}
