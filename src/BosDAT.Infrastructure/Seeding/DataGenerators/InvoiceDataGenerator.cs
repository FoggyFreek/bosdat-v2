using Microsoft.EntityFrameworkCore;
using BosDAT.Core.Entities;
using BosDAT.Core.Enums;
using BosDAT.Infrastructure.Data;

namespace BosDAT.Infrastructure.Seeding.DataGenerators;

/// <summary>
/// Generates invoices with line items, including registration fees and lesson charges.
/// </summary>
public class InvoiceDataGenerator
{
    private readonly ApplicationDbContext _context;
    private readonly SeederContext _seederContext;

    public InvoiceDataGenerator(ApplicationDbContext context, SeederContext seederContext)
    {
        _context = context;
        _seederContext = seederContext;
    }

    public async Task<List<Invoice>> GenerateAsync(
        List<Student> students,
        List<Lesson> lessons,
        List<CourseTypePricingVersion> pricingVersions,
        CancellationToken cancellationToken)
    {
        var existingCount = await _context.Invoices.CountAsync(cancellationToken);
        if (existingCount > 0)
        {
            return await _context.Invoices.ToListAsync(cancellationToken);
        }

        var invoices = new List<Invoice>();
        var invoiceLines = new List<InvoiceLine>();

        // Group completed, invoiced lessons by student and month
        var lessonGroups = lessons
            .Where(l => l.IsInvoiced && l.Status == LessonStatus.Completed && l.StudentId.HasValue)
            .GroupBy(l => new { l.StudentId, Month = new DateOnly(l.ScheduledDate.Year, l.ScheduledDate.Month, 1) })
            .ToList();

        var enrollments = await _context.Enrollments.ToListAsync(cancellationToken);
        var courses = await _context.Courses.ToListAsync(cancellationToken);
        var courseTypes = await _context.CourseTypes.ToListAsync(cancellationToken);
        var invoiceNumber = 1;

        foreach (var group in lessonGroups)
        {
            if (group.Key.StudentId == null) continue;

            var student = students.FirstOrDefault(s => s.Id == group.Key.StudentId);
            if (student == null) continue;

            var (invoice, lines) = CreateInvoice(
                student, group.Key.Month, group.ToList(), invoiceNumber++,
                enrollments, courses, courseTypes, pricingVersions, invoices);

            invoices.Add(invoice);
            invoiceLines.AddRange(lines);
        }

        await _context.Invoices.AddRangeAsync(invoices, cancellationToken);
        await _context.InvoiceLines.AddRangeAsync(invoiceLines, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _seederContext.Invoices = invoices;
        return invoices;
    }

    private (Invoice Invoice, List<InvoiceLine> Lines) CreateInvoice(
        Student student,
        DateOnly month,
        List<Lesson> lessons,
        int invoiceNumber,
        List<Enrollment> enrollments,
        List<Course> courses,
        List<CourseType> courseTypes,
        List<CourseTypePricingVersion> pricingVersions,
        List<Invoice> existingInvoices)
    {
        var invoiceId = _seederContext.NextInvoiceId();
        var issueDate = month.AddMonths(1).AddDays(_seederContext.NextInt(1, 10));
        var dueDate = issueDate.AddDays(14);

        var isChild = IsChildStudent(student);
        var lines = new List<InvoiceLine>();
        decimal subtotal = 0;

        // Create lines for each lesson
        foreach (var lesson in lessons)
        {
            var line = CreateLessonLine(
                invoiceId, lesson, student, isChild,
                enrollments, courses, courseTypes, pricingVersions);

            if (line != null)
            {
                lines.Add(line);
                subtotal += line.LineTotal;
            }
        }

        // Add registration fee for first invoice
        var hasExistingInvoice = existingInvoices.Exists(i => i.StudentId == student.Id);
        if (!hasExistingInvoice && student.RegistrationFeePaidAt.HasValue)
        {
            var regFeeLine = CreateRegistrationFeeLine(invoiceId);
            lines.Add(regFeeLine);
            subtotal += regFeeLine.LineTotal;
        }

        var vatAmount = Math.Round(subtotal * (SeederConstants.VatRate / 100), 2);
        var total = subtotal + vatAmount;
        var status = DetermineInvoiceStatus(dueDate);

        var invoice = new Invoice
        {
            Id = invoiceId,
            InvoiceNumber = $"NMI-{issueDate.Year}-{invoiceNumber:D5}",
            StudentId = student.Id,
            IssueDate = issueDate,
            DueDate = dueDate,
            Subtotal = subtotal,
            VatAmount = vatAmount,
            Total = total,
            DiscountAmount = 0,
            Status = status,
            PaidAt = status == InvoiceStatus.Paid
                ? dueDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).AddDays(_seederContext.NextInt(-5, 10))
                : null,
            PaymentMethod = status == InvoiceStatus.Paid
                ? GetPaymentMethod()
                : null,
            Notes = status == InvoiceStatus.Overdue ? "Payment reminder sent" : null,
            CreatedAt = issueDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            UpdatedAt = DateTime.UtcNow.AddDays(-_seederContext.NextInt(1, 30))
        };

        return (invoice, lines);
    }

    private string GetPaymentMethod() => _seederContext.NextBool() ? "Bank" : "DirectDebit";

    private InvoiceLine? CreateLessonLine(
        Guid invoiceId,
        Lesson lesson,
        Student student,
        bool isChild,
        List<Enrollment> enrollments,
        List<Course> courses,
        List<CourseType> courseTypes,
        List<CourseTypePricingVersion> pricingVersions)
    {
        var course = courses.FirstOrDefault(c => c.Id == lesson.CourseId);
        var courseType = course != null
            ? courseTypes.FirstOrDefault(ct => ct.Id == course.CourseTypeId)
            : null;
        var enrollment = enrollments.FirstOrDefault(e =>
            e.StudentId == student.Id && e.CourseId == lesson.CourseId);

        var pricing = pricingVersions
            .FirstOrDefault(pv => pv.CourseTypeId == course?.CourseTypeId && pv.IsCurrent);

        if (pricing == null) return null;

        var unitPrice = isChild ? pricing.PriceChild : pricing.PriceAdult;

        // Apply enrollment discount
        if (enrollment?.DiscountPercent > 0)
        {
            unitPrice *= (1 - enrollment.DiscountPercent / 100);
        }

        return new InvoiceLine
        {
            Id = _seederContext.NextInvoiceLineId(),
            InvoiceId = invoiceId,
            LessonId = lesson.Id,
            PricingVersionId = pricing.Id,
            Description = $"{courseType?.Name ?? "Lesson"} - {lesson.ScheduledDate:d MMM yyyy}",
            Quantity = 1,
            UnitPrice = unitPrice,
            VatRate = SeederConstants.VatRate,
            LineTotal = unitPrice
        };
    }

    private InvoiceLine CreateRegistrationFeeLine(Guid invoiceId) =>
        new()
        {
            Id = _seederContext.NextInvoiceLineId(),
            InvoiceId = invoiceId,
            LessonId = null,
            PricingVersionId = null,
            Description = "Eenmalig inschrijfgeld",
            Quantity = 1,
            UnitPrice = SeederConstants.RegistrationFee,
            VatRate = SeederConstants.VatRate,
            LineTotal = SeederConstants.RegistrationFee
        };

    private static bool IsChildStudent(Student student)
    {
        if (!student.DateOfBirth.HasValue) return false;
        var age = DateTime.UtcNow.Year - student.DateOfBirth.Value.Year;
        return age < SeederConstants.ChildAgeLimit;
    }

    private InvoiceStatus DetermineInvoiceStatus(DateOnly dueDate)
    {
        var isPast = dueDate < _seederContext.Today;

        if (!isPast)
            return _seederContext.NextBool(30) ? InvoiceStatus.Sent : InvoiceStatus.Draft;

        if (_seederContext.NextBool(80))
            return InvoiceStatus.Paid;

        return _seederContext.NextBool(50) ? InvoiceStatus.Overdue : InvoiceStatus.Sent;
    }
}
