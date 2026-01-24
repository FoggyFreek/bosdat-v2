using Microsoft.EntityFrameworkCore;
using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Interfaces;
using BosDAT.Infrastructure.Data;

namespace BosDAT.Infrastructure.Services;

public class RegistrationFeeService(
    ApplicationDbContext context,
    IInvoiceRepository invoiceRepository) : IRegistrationFeeService
{
    public async Task<bool> IsStudentEligibleForFeeAsync(Guid studentId, CancellationToken ct = default)
    {
        var student = await context.Students
            .FirstOrDefaultAsync(s => s.Id == studentId, ct);

        if (student == null)
        {
            throw new InvalidOperationException($"Student with ID {studentId} not found.");
        }

        return student.RegistrationFeePaidAt == null;
    }

    public async Task<bool> ShouldApplyFeeForCourseAsync(Guid courseId, CancellationToken ct = default)
    {
        var course = await context.Courses
            .FirstOrDefaultAsync(c => c.Id == courseId, ct);

        if (course == null)
        {
            throw new InvalidOperationException($"Course with ID {courseId} not found.");
        }

        return !course.IsTrial;
    }

    public async Task<Guid> ApplyRegistrationFeeAsync(Guid studentId, CancellationToken ct = default)
    {
        var student = await context.Students
            .FirstOrDefaultAsync(s => s.Id == studentId, ct);

        if (student == null)
        {
            throw new InvalidOperationException($"Student with ID {studentId} not found.");
        }

        if (student.RegistrationFeePaidAt != null)
        {
            throw new InvalidOperationException($"Registration fee has already been applied for student {studentId}.");
        }

        var feeAmount = await GetFeeAmountAsync(ct);
        var feeDescription = await GetFeeDescriptionAsync(ct);
        var vatRate = await GetVatRateAsync(ct);

        var invoice = await GetOrCreateDraftInvoiceAsync(student, ct);

        var invoiceLine = new InvoiceLine
        {
            InvoiceId = invoice.Id,
            LessonId = null,
            PricingVersionId = null,
            Description = feeDescription,
            Quantity = 1,
            UnitPrice = feeAmount,
            VatRate = vatRate,
            LineTotal = feeAmount
        };

        context.InvoiceLines.Add(invoiceLine);

        invoice.Subtotal += feeAmount;
        invoice.VatAmount += feeAmount * (vatRate / 100m);
        invoice.Total = invoice.Subtotal + invoice.VatAmount;

        student.RegistrationFeePaidAt = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);

        return invoice.Id;
    }

    public async Task<RegistrationFeeStatusDto> GetFeeStatusAsync(Guid studentId, CancellationToken ct = default)
    {
        var student = await context.Students
            .FirstOrDefaultAsync(s => s.Id == studentId, ct);

        if (student == null)
        {
            throw new InvalidOperationException($"Student with ID {studentId} not found.");
        }

        if (student.RegistrationFeePaidAt == null)
        {
            var feeAmount = await GetFeeAmountAsync(ct);
            return new RegistrationFeeStatusDto
            {
                HasPaid = false,
                PaidAt = null,
                Amount = feeAmount,
                InvoiceId = null
            };
        }

        var feeDescription = await GetFeeDescriptionAsync(ct);
        var invoiceLine = await context.InvoiceLines
            .Include(il => il.Invoice)
            .Where(il => il.Invoice.StudentId == studentId && il.Description == feeDescription && il.LessonId == null)
            .OrderByDescending(il => il.Invoice.CreatedAt)
            .FirstOrDefaultAsync(ct);

        return new RegistrationFeeStatusDto
        {
            HasPaid = true,
            PaidAt = student.RegistrationFeePaidAt,
            Amount = invoiceLine?.UnitPrice,
            InvoiceId = invoiceLine?.InvoiceId
        };
    }

    private async Task<decimal> GetFeeAmountAsync(CancellationToken ct)
    {
        var setting = await context.Settings
            .FirstOrDefaultAsync(s => s.Key == "registration_fee", ct);

        if (setting == null || !decimal.TryParse(setting.Value, out var amount))
        {
            return 25m;
        }

        return amount;
    }

    private async Task<string> GetFeeDescriptionAsync(CancellationToken ct)
    {
        var setting = await context.Settings
            .FirstOrDefaultAsync(s => s.Key == "registration_fee_description", ct);

        return setting?.Value ?? "Eenmalig inschrijfgeld";
    }

    private async Task<decimal> GetVatRateAsync(CancellationToken ct)
    {
        var setting = await context.Settings
            .FirstOrDefaultAsync(s => s.Key == "vat_rate", ct);

        if (setting == null || !decimal.TryParse(setting.Value, out var rate))
        {
            return 21m;
        }

        return rate;
    }

    private async Task<Invoice> GetOrCreateDraftInvoiceAsync(Student student, CancellationToken ct)
    {
        var existingDraft = await context.Invoices
            .Include(i => i.Lines)
            .FirstOrDefaultAsync(i => i.StudentId == student.Id && i.Status == InvoiceStatus.Draft, ct);

        if (existingDraft != null)
        {
            return existingDraft;
        }

        var paymentDueDays = await GetPaymentDueDaysAsync(ct);
        var invoiceNumber = await invoiceRepository.GenerateInvoiceNumberAsync(ct);

        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = invoiceNumber,
            StudentId = student.Id,
            IssueDate = DateOnly.FromDateTime(DateTime.UtcNow),
            DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(paymentDueDays)),
            Subtotal = 0,
            VatAmount = 0,
            Total = 0,
            DiscountAmount = 0,
            Status = InvoiceStatus.Draft,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Invoices.Add(invoice);

        return invoice;
    }

    private async Task<int> GetPaymentDueDaysAsync(CancellationToken ct)
    {
        var setting = await context.Settings
            .FirstOrDefaultAsync(s => s.Key == "payment_due_days", ct);

        if (setting == null || !int.TryParse(setting.Value, out var days))
        {
            return 14;
        }

        return days;
    }
}
