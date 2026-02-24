using Microsoft.EntityFrameworkCore;
using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Enums;
using BosDAT.Core.Interfaces;
using BosDAT.Core.Utilities;
using BosDAT.Infrastructure.Data;
using System.Diagnostics.CodeAnalysis;
using System.Security.AccessControl;

namespace BosDAT.Infrastructure.Services;

public class InvoiceGenerationService(
    ApplicationDbContext context,
    IUnitOfWork unitOfWork,
    ICourseTypePricingService pricingService,
    IInvoiceQueryService queryService,
    IRegistrationFeeService registrationFeeService,
    IStudentTransactionService studentTransactionService) : IInvoiceGenerationService
{
    public async Task<InvoiceDto> GenerateInvoiceAsync(GenerateInvoiceDto dto, Guid userId, CancellationToken ct = default)
    {
        //get enrollment to invoice
        var enrollment = await unitOfWork.Enrollments.GetByIdAsync(dto.EnrollmentId, ct)
            ?? throw new InvalidOperationException($"Enrollment with ID {dto.EnrollmentId} not found.");   

        // Check if invoice already exists for this period
        var existingInvoice = await unitOfWork.Invoices.GetByPeriodAsync(enrollment.StudentId, enrollment.Id, dto.PeriodStart, dto.PeriodEnd, ct);

        if (existingInvoice != null)
        {
            throw new InvalidOperationException(
                $"Invoice already exists for this enrollment and period (Invoice #{existingInvoice.InvoiceNumber}).");
        }

        // Get lessons in the period that haven't been invoiced
        var lessons = await GetInvoiceableLessonsAsync(enrollment, dto.PeriodStart, dto.PeriodEnd, ct);

        if (lessons.Count == 0)
        {
            throw new InvalidOperationException("No invoiceable lessons found for the specified period.");
        }

        // Get pricing for the invoice date
        var invoiceDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var pricing = await pricingService.GetPricingForDateAsync(enrollment.Course.CourseTypeId, invoiceDate, ct)
            ?? await pricingService.GetCurrentPricingAsync(enrollment.Course.CourseTypeId, ct)
            ?? throw new InvalidOperationException($"No pricing found for course type {enrollment.Course.CourseType.Name}.");

        var vatRate = await queryService.GetVatRateAsync(ct);
        var pricePerLesson = CalculatePricePerLesson(pricing, enrollment);

        //one-time registration fee
        var isEligibleForFee = await registrationFeeService.IsStudentEligibleForFeeAsync(enrollment.StudentId, ct) 
            && await registrationFeeService.ShouldApplyFeeForCourseAsync(enrollment.CourseId, ct);
        var feeAmount = await registrationFeeService.GetFeeAmountAsync(ct);
        var feeDescription = await registrationFeeService.GetFeeDescriptionAsync(ct);

        await unitOfWork.BeginTransactionAsync(ct);
        try
        {
            var invoiceNumber = await unitOfWork.Invoices.GenerateInvoiceNumberAsync(ct);
            var periodType = enrollment.InvoicingPreference;
            var description = queryService.GeneratePeriodDescription(dto.PeriodStart, dto.PeriodEnd, periodType);
            var courseName = enrollment.Course.CourseType.Name;
            var paymentDueDays = await queryService.GetPaymentDueDaysAsync(ct);
            var dueDate = invoiceDate.AddDays(paymentDueDays);

            var invoice = new Invoice
            {
                Id = Guid.NewGuid(),
                InvoiceNumber = invoiceNumber,
                StudentId = enrollment.StudentId,
                EnrollmentId = enrollment.Id,
                IssueDate = invoiceDate,
                DueDate = dueDate,
                Description = $"{courseName} {description}",
                PeriodStart = dto.PeriodStart,
                PeriodEnd = dto.PeriodEnd,
                PeriodType = periodType,
                Status = InvoiceStatus.Draft
            };

            //Add the lesson lines to the invoice
            AddInvoiceLinesForLessons(invoice, lessons, pricing.Id, pricePerLesson, courseName, vatRate);

            if(isEligibleForFee)
            {
                AddLineForRegistrationFeeAsync(enrollment.Student, invoice, feeAmount, feeDescription, vatRate);
            }

            //calculate subtotal of all lesson lines
            invoice.Subtotal = invoice.Lines.Sum(l => l.LineTotal);

            invoice.CalculateInvoiceTotals();
            
            await unitOfWork.Invoices.AddAsync(invoice, ct);
            await unitOfWork.SaveChangesAsync(ct);
            
            await studentTransactionService.RecordInvoiceChargeAsync(invoice, userId, ct);

            await unitOfWork.SaveChangesAsync(ct);
            await unitOfWork.CommitTransactionAsync(ct);

            return await queryService.GetInvoiceAsync(invoice.Id, ct)
                ?? throw new InvalidOperationException("Failed to retrieve created invoice.");
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(ct);
            throw;
        }
    }

    public async Task<IReadOnlyList<InvoiceDto>> GenerateBatchInvoicesAsync(GenerateBatchInvoicesDto dto, Guid userId, CancellationToken ct = default)
    {
        // Get all active enrollments with matching invoicing preference
        var enrollments = await context.Enrollments
            .Include(e => e.Student)
            .Include(e => e.Course)
                .ThenInclude(c => c.CourseType)
            .Where(e => e.Status == EnrollmentStatus.Active && e.InvoicingPreference == dto.PeriodType)
            .ToListAsync(ct);

        var generatedInvoices = new List<InvoiceDto>();

        foreach (var enrollment in enrollments)
        {
            try
            {
                var generateDto = new GenerateInvoiceDto
                {
                    EnrollmentId = enrollment.Id,
                    PeriodStart = dto.PeriodStart,
                    PeriodEnd = dto.PeriodEnd,
                };

                var invoice = await GenerateInvoiceAsync(generateDto, userId, ct);
                generatedInvoices.Add(invoice);
            }
            catch (InvalidOperationException)
            {
                // Skip enrollments that already have invoices or have no lessons
                continue;
            }
        }

        return generatedInvoices;
    }

    public async Task<InvoiceDto> RecalculateInvoiceAsync(Guid invoiceId, Guid userId, CancellationToken ct = default)
    {
        var invoice = await unitOfWork.Invoices.GetWithLinesAndEnrollmentsAsync(invoiceId, ct)
            ?? throw new InvalidOperationException($"Invoice with ID {invoiceId} not found.");

        if (invoice.Status != InvoiceStatus.Draft)
        {
            throw new InvalidOperationException($"Cannot recalculate invoice with status other than Draft.");
        }

        if (invoice.Enrollment == null || !invoice.PeriodStart.HasValue || !invoice.PeriodEnd.HasValue)
        {
            throw new InvalidOperationException("Invoice is missing enrollment or period information required for recalculation.");
        }

        var oldTotal = invoice.Total;

        await unitOfWork.BeginTransactionAsync(ct);
        try
        {
            invoice.UnInvoiceAndClearLines();
            await unitOfWork.SaveChangesAsync(ct);

            var lessons = await GetInvoiceableLessonsAsync(
                invoice.Enrollment, invoice.PeriodStart.Value, invoice.PeriodEnd.Value, ct);

            if (lessons.Count == 0)
            {
                invoice.CancelInvoice();
                await unitOfWork.SaveChangesAsync(ct);
                await studentTransactionService.RecordInvoiceCancellationAsync(invoice, oldTotal, userId, ct);
            }
            else
            {
                var pricing = await pricingService.GetPricingForDateAsync(
                    invoice.Enrollment.Course.CourseTypeId, invoice.IssueDate, ct)
                    ?? await pricingService.GetCurrentPricingAsync(invoice.Enrollment.Course.CourseTypeId, ct)
                    ?? throw new InvalidOperationException("No pricing found.");

                var pricePerLesson = CalculatePricePerLesson(pricing, invoice.Enrollment);
                var vatRate = await queryService.GetVatRateAsync(ct);
                var courseName = invoice.Enrollment.Course.CourseType.Name;

                AddInvoiceLinesForLessons(invoice, lessons, pricing.Id, pricePerLesson, courseName, vatRate);
                invoice.CalculateInvoiceTotals();
                await studentTransactionService.RecordInvoiceAdjustmentAsync(invoice, oldTotal, userId, ct);
                await unitOfWork.SaveChangesAsync(ct);
            }

            await unitOfWork.CommitTransactionAsync(ct);

            return await queryService.GetInvoiceAsync(invoice.Id, ct)
                ?? throw new InvalidOperationException("Failed to retrieve recalculated invoice.");
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(ct);
            throw;
        }
    }

    private async Task<List<Lesson>> GetInvoiceableLessonsAsync(
        Enrollment enrollment, DateOnly periodStart, DateOnly periodEnd, CancellationToken ct)
    {
        var courseTypeCategory = enrollment.Course.CourseType.Type;

        var query = context.Lessons
            .Where(l => l.CourseId == enrollment.CourseId &&
                        l.ScheduledDate >= periodStart &&
                        l.ScheduledDate <= periodEnd &&
                        !l.IsInvoiced &&
                        (l.Status == LessonStatus.Completed || l.Status == LessonStatus.Scheduled));

        if (courseTypeCategory == CourseTypeCategory.Individual)
        {
            query = query.Where(l => l.StudentId == enrollment.StudentId);
        }

        return await query.OrderBy(l => l.ScheduledDate).ToListAsync(ct);
    }

    private static decimal CalculatePricePerLesson(CourseTypePricingVersion pricing, Enrollment enrollment)
    {
        var isChild = IsoDateHelper.IsChild(enrollment.Student.DateOfBirth);
        var basePrice = isChild ? pricing.PriceChild : pricing.PriceAdult;
        var discountAmount = basePrice * (enrollment.DiscountPercent / 100m);
        return basePrice - discountAmount;
    }

     private static void AddInvoiceLinesForLessons(
        Invoice invoice, List<Lesson> lessons, Guid pricingVersionId,
        decimal pricePerLesson, string courseName, decimal vatRate)
    {
        foreach (var lesson in lessons)
        {
            var line = new InvoiceLine
            {
                InvoiceId = invoice.Id,
                LessonId = lesson.Id,
                PricingVersionId = pricingVersionId,
                Description = $"{courseName} - {lesson.ScheduledDate:dd MMM yyyy}",
                Quantity = 1,
                UnitPrice = pricePerLesson,
                LineTotal = pricePerLesson,
                VatRate = vatRate,
                VatAmount = pricePerLesson * (vatRate / 100m)
            };

            invoice.Lines.Add(line);
            lesson.IsInvoiced = true;
        }
    }

     private static void AddLineForRegistrationFeeAsync(Student student, Invoice invoice, decimal feeAmount, string feeDescription,
     decimal vatRate)
    {
        if (student == null)
        {
            throw new InvalidOperationException($"Student not found.");
        }

        if (student.RegistrationFeePaidAt != null)
        {
            throw new InvalidOperationException($"Registration fee has already been applied for student {student.Id}.");
        }

        var invoiceLine = new InvoiceLine
        {
            InvoiceId = invoice.Id,
            LessonId = null,
            PricingVersionId = null,
            Description = feeDescription,
            Quantity = 1,
            UnitPrice = feeAmount,
            LineTotal = feeAmount,
            VatRate = vatRate,
            VatAmount = feeAmount * (vatRate / 100m)
        };

        invoice.Lines.Add(invoiceLine);
        student.RegistrationFeePaidAt = DateTime.UtcNow;
    }
}
