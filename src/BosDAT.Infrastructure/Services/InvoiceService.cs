using System.Globalization;
using Microsoft.EntityFrameworkCore;
using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Enums;
using BosDAT.Core.Interfaces;
using BosDAT.Infrastructure.Data;

namespace BosDAT.Infrastructure.Services;

public class InvoiceService(
    ApplicationDbContext context,
    IUnitOfWork unitOfWork,
    ICourseTypePricingService pricingService) : IInvoiceService
{
    private static readonly string[] MonthAbbreviations =
    [
        "jan", "feb", "mar", "apr", "may", "jun",
        "jul", "aug", "sep", "oct", "nov", "dec"
    ];

    public async Task<InvoiceDto> GenerateInvoiceAsync(GenerateInvoiceDto dto, Guid userId, CancellationToken ct = default)
    {
        var enrollment = await context.Enrollments
            .Include(e => e.Student)
            .Include(e => e.Course)
                .ThenInclude(c => c.CourseType)
                    .ThenInclude(ct => ct.Instrument)
            .Include(e => e.Course.Teacher)
            .FirstOrDefaultAsync(e => e.Id == dto.EnrollmentId, ct)
            ?? throw new InvalidOperationException($"Enrollment with ID {dto.EnrollmentId} not found.");

        // Check if invoice already exists for this period
        var existingInvoice = await unitOfWork.Invoices.GetByPeriodAsync(
            enrollment.StudentId, enrollment.Id, dto.PeriodStart, dto.PeriodEnd, ct);

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

        var vatRate = await GetVatRateAsync(ct);
        var pricePerLesson = CalculatePricePerLesson(pricing, enrollment);

        await unitOfWork.BeginTransactionAsync(ct);
        try
        {
            var invoiceNumber = await unitOfWork.Invoices.GenerateInvoiceNumberAsync(ct);
            var periodType = enrollment.InvoicingPreference;
            var description = GeneratePeriodDescription(dto.PeriodStart, dto.PeriodEnd, periodType);
            var courseName = enrollment.Course.CourseType.Name;

            var paymentDueDays = await GetPaymentDueDaysAsync(ct);
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

            AddInvoiceLines(invoice, lessons, pricing.Id, pricePerLesson, vatRate, courseName);

            invoice.Subtotal = invoice.Lines.Sum(l => l.UnitPrice * l.Quantity);
            invoice.VatAmount = invoice.Lines.Sum(l => l.UnitPrice * l.Quantity * (l.VatRate / 100m));
            invoice.Total = invoice.Subtotal + invoice.VatAmount;

            if (dto.ApplyLedgerCorrections)
            {
                await ApplyLedgerCorrectionsToInvoiceAsync(invoice, enrollment.StudentId, userId, ct);
            }

            context.Invoices.Add(invoice);
            await context.SaveChangesAsync(ct);
            await unitOfWork.CommitTransactionAsync(ct);

            return await GetInvoiceAsync(invoice.Id, ct) ?? throw new InvalidOperationException("Failed to retrieve created invoice.");
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
                    ApplyLedgerCorrections = dto.ApplyLedgerCorrections
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
        var invoice = await context.Invoices
            .Include(i => i.Lines)
                .ThenInclude(l => l.Lesson)
            .Include(i => i.Payments)
            .Include(i => i.LedgerApplications)
            .Include(i => i.Enrollment)
                .ThenInclude(e => e!.Student)
            .Include(i => i.Enrollment)
                .ThenInclude(e => e!.Course)
                    .ThenInclude(c => c.CourseType)
            .FirstOrDefaultAsync(i => i.Id == invoiceId, ct)
            ?? throw new InvalidOperationException($"Invoice with ID {invoiceId} not found.");

        if (invoice.Status == InvoiceStatus.Paid || invoice.Status == InvoiceStatus.Cancelled)
        {
            throw new InvalidOperationException($"Cannot recalculate invoice with status {invoice.Status}.");
        }

        if (invoice.Enrollment == null || !invoice.PeriodStart.HasValue || !invoice.PeriodEnd.HasValue)
        {
            throw new InvalidOperationException("Invoice is missing enrollment or period information required for recalculation.");
        }

        await unitOfWork.BeginTransactionAsync(ct);
        try
        {
            await RevertLedgerApplicationsAsync(invoice, ct);
            UnInvoiceAndClearLines(invoice);

            var lessons = await GetInvoiceableLessonsAsync(
                invoice.Enrollment, invoice.PeriodStart.Value, invoice.PeriodEnd.Value, ct);

            if (lessons.Count == 0)
            {
                CancelInvoice(invoice);
            }
            else
            {
                var pricing = await pricingService.GetPricingForDateAsync(
                    invoice.Enrollment.Course.CourseTypeId, invoice.IssueDate, ct)
                    ?? await pricingService.GetCurrentPricingAsync(invoice.Enrollment.Course.CourseTypeId, ct)
                    ?? throw new InvalidOperationException("No pricing found.");

                var pricePerLesson = CalculatePricePerLesson(pricing, invoice.Enrollment);
                var vatRate = await GetVatRateAsync(ct);
                var courseName = invoice.Enrollment.Course.CourseType.Name;

                AddInvoiceLines(invoice, lessons, pricing.Id, pricePerLesson, vatRate, courseName);
                RecalculateInvoiceTotals(invoice);
                await ApplyLedgerCorrectionsToInvoiceAsync(invoice, invoice.StudentId, userId, ct);
            }

            await context.SaveChangesAsync(ct);
            await unitOfWork.CommitTransactionAsync(ct);

            return await GetInvoiceAsync(invoice.Id, ct) ?? throw new InvalidOperationException("Failed to retrieve recalculated invoice.");
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(ct);
            throw;
        }
    }

    public async Task<InvoiceDto?> GetInvoiceAsync(Guid invoiceId, CancellationToken ct = default)
    {
        var invoice = await context.Invoices
            .Include(i => i.Student)
            .Include(i => i.Lines)
                .ThenInclude(l => l.Lesson)
                    .ThenInclude(le => le!.Course)
                        .ThenInclude(c => c.CourseType)
            .Include(i => i.Payments)
            .Include(i => i.LedgerApplications)
                .ThenInclude(la => la.LedgerEntry)
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == invoiceId, ct);

        if (invoice == null) return null;

        return MapToDto(invoice);
    }

    public async Task<InvoiceDto?> GetByInvoiceNumberAsync(string invoiceNumber, CancellationToken ct = default)
    {
        var invoice = await context.Invoices
            .Include(i => i.Student)
            .Include(i => i.Lines)
                .ThenInclude(l => l.Lesson)
                    .ThenInclude(le => le!.Course)
                        .ThenInclude(c => c.CourseType)
            .Include(i => i.Payments)
            .Include(i => i.LedgerApplications)
                .ThenInclude(la => la.LedgerEntry)
            .AsNoTracking()
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

    public async Task<InvoiceDto> ApplyLedgerCorrectionAsync(
        Guid invoiceId, Guid ledgerEntryId, decimal amount, Guid userId, CancellationToken ct = default)
    {
        var invoice = await context.Invoices
            .Include(i => i.Payments)
            .Include(i => i.LedgerApplications)
            .FirstOrDefaultAsync(i => i.Id == invoiceId, ct)
            ?? throw new InvalidOperationException($"Invoice with ID {invoiceId} not found.");

        if (invoice.Status == InvoiceStatus.Paid || invoice.Status == InvoiceStatus.Cancelled)
        {
            throw new InvalidOperationException($"Cannot apply correction to invoice with status {invoice.Status}.");
        }

        var ledgerEntry = await context.StudentLedgerEntries
            .Include(e => e.Applications)
            .FirstOrDefaultAsync(e => e.Id == ledgerEntryId, ct)
            ?? throw new InvalidOperationException($"Ledger entry with ID {ledgerEntryId} not found.");

        if (ledgerEntry.StudentId != invoice.StudentId)
        {
            throw new InvalidOperationException("Ledger entry does not belong to the invoice's student.");
        }

        var appliedAmount = ledgerEntry.Applications.Sum(a => a.AppliedAmount);
        var availableAmount = ledgerEntry.Amount - appliedAmount;

        if (amount > availableAmount)
        {
            throw new InvalidOperationException($"Requested amount ({amount}) exceeds available amount ({availableAmount}).");
        }

        await unitOfWork.BeginTransactionAsync(ct);
        try
        {
            var application = new StudentLedgerApplication
            {
                Id = Guid.NewGuid(),
                LedgerEntryId = ledgerEntryId,
                InvoiceId = invoiceId,
                AppliedAmount = amount,
                AppliedById = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.StudentLedgerApplications.Add(application);

            // Update ledger entry status
            var newAppliedAmount = appliedAmount + amount;
            ledgerEntry.Status = newAppliedAmount >= ledgerEntry.Amount
                ? LedgerEntryStatus.FullyApplied
                : LedgerEntryStatus.PartiallyApplied;

            // Update invoice ledger amounts
            if (ledgerEntry.EntryType == LedgerEntryType.Credit)
            {
                invoice.LedgerCreditsApplied += amount;
            }
            else
            {
                invoice.LedgerDebitsApplied += amount;
            }

            // Check if invoice is fully paid
            var totalPaid = invoice.Payments.Sum(p => p.Amount) + invoice.LedgerCreditsApplied;
            var totalOwed = invoice.Total + invoice.LedgerDebitsApplied;

            if (totalPaid >= totalOwed && invoice.Status != InvoiceStatus.Paid)
            {
                invoice.Status = InvoiceStatus.Paid;
                invoice.PaidAt = DateTime.UtcNow;
            }

            await context.SaveChangesAsync(ct);
            await unitOfWork.CommitTransactionAsync(ct);

            return await GetInvoiceAsync(invoiceId, ct) ?? throw new InvalidOperationException("Failed to retrieve invoice after applying ledger correction.");
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(ct);
            throw;
        }
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

    private async Task RevertLedgerApplicationsAsync(Invoice invoice, CancellationToken ct)
    {
        foreach (var application in invoice.LedgerApplications.ToList())
        {
            var ledgerEntry = await context.StudentLedgerEntries
                .Include(e => e.Applications)
                .FirstOrDefaultAsync(e => e.Id == application.LedgerEntryId, ct);

            if (ledgerEntry != null)
            {
                RecalculateLedgerEntryStatus(ledgerEntry, application.Id);
            }

            context.StudentLedgerApplications.Remove(application);
        }
    }

    private static void RecalculateLedgerEntryStatus(StudentLedgerEntry entry, Guid excludedApplicationId)
    {
        var remainingApplied = entry.Applications
            .Where(a => a.Id != excludedApplicationId)
            .Sum(a => a.AppliedAmount);

        if (remainingApplied <= 0)
            entry.Status = LedgerEntryStatus.Open;
        else if (remainingApplied < entry.Amount)
            entry.Status = LedgerEntryStatus.PartiallyApplied;
        else
            entry.Status = LedgerEntryStatus.FullyApplied;
    }

    private void UnInvoiceAndClearLines(Invoice invoice)
    {
        foreach (var line in invoice.Lines.Where(l => l.Lesson != null))
        {
            line.Lesson!.IsInvoiced = false;
        }

        context.InvoiceLines.RemoveRange(invoice.Lines);
        invoice.Lines.Clear();
    }

    private static void CancelInvoice(Invoice invoice)
    {
        invoice.Status = InvoiceStatus.Cancelled;
        invoice.Subtotal = 0;
        invoice.VatAmount = 0;
        invoice.Total = 0;
        invoice.LedgerCreditsApplied = 0;
        invoice.LedgerDebitsApplied = 0;
    }

    private static decimal CalculatePricePerLesson(CourseTypePricingVersion pricing, Enrollment enrollment)
    {
        var isChild = IsChildStudent(enrollment.Student.DateOfBirth);
        var basePrice = isChild ? pricing.PriceChild : pricing.PriceAdult;
        var discountAmount = basePrice * (enrollment.DiscountPercent / 100m);
        return basePrice - discountAmount;
    }

    private static void AddInvoiceLines(
        Invoice invoice, List<Lesson> lessons, Guid pricingVersionId,
        decimal pricePerLesson, decimal vatRate, string courseName)
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
                VatRate = vatRate,
                LineTotal = pricePerLesson * (1 + vatRate / 100m)
            };

            invoice.Lines.Add(line);
            lesson.IsInvoiced = true;
        }
    }

    private static void RecalculateInvoiceTotals(Invoice invoice)
    {
        invoice.Subtotal = invoice.Lines.Sum(l => l.UnitPrice * l.Quantity);
        invoice.VatAmount = invoice.Lines.Sum(l => l.UnitPrice * l.Quantity * (l.VatRate / 100m));
        invoice.Total = invoice.Subtotal + invoice.VatAmount;
        invoice.LedgerCreditsApplied = 0;
        invoice.LedgerDebitsApplied = 0;
    }

    private async Task<List<Lesson>> GetInvoiceableLessonsAsync(
        Enrollment enrollment, DateOnly periodStart, DateOnly periodEnd, CancellationToken ct)
    {
        var courseTypeCategory = enrollment.Course.CourseType.Type;

        // For individual lessons, filter by student
        // For group/workshop lessons, the lesson belongs to all enrolled students
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

    private async Task ApplyLedgerCorrectionsToInvoiceAsync(
        Invoice invoice, Guid studentId, Guid userId, CancellationToken ct)
    {
        // Get open credits and debits
        var openCredits = await context.StudentLedgerEntries
            .Include(e => e.Applications)
            .Where(e => e.StudentId == studentId &&
                        e.EntryType == LedgerEntryType.Credit &&
                        (e.Status == LedgerEntryStatus.Open || e.Status == LedgerEntryStatus.PartiallyApplied))
            .ToListAsync(ct);

        var openDebits = await context.StudentLedgerEntries
            .Include(e => e.Applications)
            .Where(e => e.StudentId == studentId &&
                        e.EntryType == LedgerEntryType.Debit &&
                        (e.Status == LedgerEntryStatus.Open || e.Status == LedgerEntryStatus.PartiallyApplied))
            .ToListAsync(ct);

        // Apply debits first (increase amount owed)
        foreach (var debit in openDebits)
        {
            var appliedAmount = debit.Applications.Sum(a => a.AppliedAmount);
            var availableAmount = debit.Amount - appliedAmount;

            if (availableAmount <= 0) continue;

            var application = new StudentLedgerApplication
            {
                Id = Guid.NewGuid(),
                LedgerEntryId = debit.Id,
                InvoiceId = invoice.Id,
                AppliedAmount = availableAmount,
                AppliedById = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.StudentLedgerApplications.Add(application);
            invoice.LedgerApplications.Add(application);
            invoice.LedgerDebitsApplied += availableAmount;

            debit.Status = LedgerEntryStatus.FullyApplied;
        }

        // Apply credits (reduce amount owed)
        var amountOwed = invoice.Total + invoice.LedgerDebitsApplied;

        foreach (var credit in openCredits)
        {
            if (amountOwed <= 0) break;

            var appliedAmount = credit.Applications.Sum(a => a.AppliedAmount);
            var availableAmount = credit.Amount - appliedAmount;

            if (availableAmount <= 0) continue;

            var amountToApply = Math.Min(availableAmount, amountOwed);

            var application = new StudentLedgerApplication
            {
                Id = Guid.NewGuid(),
                LedgerEntryId = credit.Id,
                InvoiceId = invoice.Id,
                AppliedAmount = amountToApply,
                AppliedById = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.StudentLedgerApplications.Add(application);
            invoice.LedgerApplications.Add(application);
            invoice.LedgerCreditsApplied += amountToApply;
            amountOwed -= amountToApply;

            var newAppliedAmount = appliedAmount + amountToApply;
            credit.Status = newAppliedAmount >= credit.Amount
                ? LedgerEntryStatus.FullyApplied
                : LedgerEntryStatus.PartiallyApplied;
        }

        // Check if fully paid by credits
        if (amountOwed <= 0)
        {
            invoice.Status = InvoiceStatus.Paid;
            invoice.PaidAt = DateTime.UtcNow;
        }
    }

    private async Task<decimal> GetVatRateAsync(CancellationToken ct)
    {
        var setting = await context.Settings
            .FirstOrDefaultAsync(s => s.Key == "vat_rate", ct);

        return decimal.TryParse(setting?.Value, out var rate) ? rate : 21m;
    }

    private async Task<int> GetPaymentDueDaysAsync(CancellationToken ct)
    {
        var setting = await context.Settings
            .FirstOrDefaultAsync(s => s.Key == "payment_due_days", ct);

        return int.TryParse(setting?.Value, out var days) ? days : 14;
    }

    private static bool IsChildStudent(DateOnly? dateOfBirth)
    {
        if (!dateOfBirth.HasValue) return false;

        var today = DateOnly.FromDateTime(DateTime.Today);
        var age = today.Year - dateOfBirth.Value.Year;

        if (dateOfBirth.Value > today.AddYears(-age))
        {
            age--;
        }

        return age < 18;
    }

    private InvoiceDto MapToDto(Invoice invoice)
    {
        var student = invoice.Student;
        var billingContact = new BillingContactDto
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

        var amountPaid = invoice.Payments.Sum(p => p.Amount) + invoice.LedgerCreditsApplied;
        var totalOwed = invoice.Total + invoice.LedgerDebitsApplied;
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

    private static InvoiceListDto MapToListDto(Invoice invoice)
    {
        var amountPaid = invoice.Payments.Sum(p => p.Amount) + invoice.LedgerCreditsApplied;
        var totalOwed = invoice.Total + invoice.LedgerDebitsApplied;

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
}
