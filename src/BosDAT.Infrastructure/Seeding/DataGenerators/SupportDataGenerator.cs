using Microsoft.EntityFrameworkCore;
using BosDAT.Core.Entities;
using BosDAT.Core.Enums;
using BosDAT.Infrastructure.Data;

namespace BosDAT.Infrastructure.Seeding.DataGenerators;

/// <summary>
/// Generates supporting data: holidays, payments, and ledger entries.
/// </summary>
public class SupportDataGenerator
{
    private readonly ApplicationDbContext _context;
    private readonly SeederContext _seederContext;

    // Credit descriptions for ledger entries
    private static readonly string[] CreditDescriptions =
    {
        "Overpayment refund",
        "Lesson cancellation credit",
        "Promotional credit",
        "Teacher absence compensation",
        "Early payment discount"
    };

    // Debit descriptions for ledger entries
    private static readonly string[] DebitDescriptions =
    {
        "Late payment fee",
        "Material costs",
        "Exam fee",
        "Book rental",
        "Additional practice room usage"
    };

    public SupportDataGenerator(ApplicationDbContext context, SeederContext seederContext)
    {
        _context = context;
        _seederContext = seederContext;
    }

    public async Task GeneratePaymentsAsync(List<Invoice> invoices, CancellationToken cancellationToken)
    {
        if (await _context.Payments.AnyAsync(cancellationToken))
        {
            return;
        }

        var payments = invoices
            .Where(i => i.Status == InvoiceStatus.Paid)
            .Select(CreatePayment)
            .ToList();

        await _context.Payments.AddRangeAsync(payments, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private Payment CreatePayment(Invoice invoice)
    {
        var method = invoice.PaymentMethod switch
        {
            "Bank" => PaymentMethod.Bank,
            "DirectDebit" => PaymentMethod.DirectDebit,
            "Cash" => PaymentMethod.Cash,
            "Card" => PaymentMethod.Card,
            _ => PaymentMethod.Bank
        };

        var needsReference = method is PaymentMethod.Bank or PaymentMethod.DirectDebit;

        return new Payment
        {
            Id = _seederContext.NextPaymentId(),
            InvoiceId = invoice.Id,
            Amount = invoice.Total,
            PaymentDate = DateOnly.FromDateTime(invoice.PaidAt!.Value),
            Method = method,
            Reference = needsReference ? $"TXN{_seederContext.NextInt(100000, 999999)}" : null,
            RecordedById = null,
            Notes = null,
            CreatedAt = invoice.PaidAt!.Value,
            UpdatedAt = invoice.PaidAt!.Value
        };
    }

    public async Task GenerateLedgerEntriesAsync(
        List<Student> students,
        List<Course> courses,
        Guid adminUserId,
        CancellationToken cancellationToken)
    {
        if (await _context.StudentLedgerEntries.AnyAsync(cancellationToken))
        {
            return;
        }

        var ledgerEntries = new List<StudentLedgerEntry>();
        var activeStudents = students
            .Where(s => s.Status == StudentStatus.Active)
            .Take(8)
            .ToList();

        foreach (var student in activeStudents)
        {
            var entryCount = _seederContext.NextInt(1, 3);

            for (int i = 0; i < entryCount; i++)
            {
                ledgerEntries.Add(CreateLedgerEntry(student, courses, adminUserId));
            }
        }

        await _context.StudentLedgerEntries.AddRangeAsync(ledgerEntries, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private StudentLedgerEntry CreateLedgerEntry(Student student, List<Course> courses, Guid adminUserId)
    {
        var entryType = _seederContext.NextBool(70) ? LedgerEntryType.Credit : LedgerEntryType.Debit;
        var status = DetermineLedgerStatus();
        var amount = Math.Round((decimal)(_seederContext.Random.NextDouble() * 50 + 10), 2);
        var descriptions = entryType == LedgerEntryType.Credit ? CreditDescriptions : DebitDescriptions;

        return new StudentLedgerEntry
        {
            Id = _seederContext.NextLedgerEntryId(),
            CorrectionRefName = _seederContext.NextLedgerRefName(),
            Description = _seederContext.GetRandomItem(descriptions),
            StudentId = student.Id,
            CourseId = _seederContext.NextBool(30) ? courses.FirstOrDefault()?.Id : null,
            Amount = amount,
            EntryType = entryType,
            Status = status,
            CreatedById = adminUserId,
            CreatedAt = DateTime.UtcNow.AddDays(-_seederContext.NextInt(1, 90)),
            UpdatedAt = DateTime.UtcNow.AddDays(-_seederContext.NextInt(1, 30))
        };
    }

    private LedgerEntryStatus DetermineLedgerStatus()
    {
        if (_seederContext.NextBool(60))
            return LedgerEntryStatus.Open;

        return _seederContext.NextBool(50)
            ? LedgerEntryStatus.PartiallyApplied
            : LedgerEntryStatus.FullyApplied;
    }

    public async Task GenerateHolidaysAsync(CancellationToken cancellationToken)
    {
        if (await _context.Holidays.AnyAsync(cancellationToken))
        {
            return;
        }

        var currentYear = DateTime.UtcNow.Year;
        var holidays = GetDutchHolidays(currentYear);

        await _context.Holidays.AddRangeAsync(holidays, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private static List<Holiday> GetDutchHolidays(int year) =>
        new()
        {
            new Holiday
            {
                Id = 1,
                Name = "Kerstvakantie",
                StartDate = new DateOnly(year, 12, 23),
                EndDate = new DateOnly(year + 1, 1, 5)
            },
            new Holiday
            {
                Id = 2,
                Name = "Voorjaarsvakantie",
                StartDate = new DateOnly(year, 2, 17),
                EndDate = new DateOnly(year, 2, 25)
            },
            new Holiday
            {
                Id = 3,
                Name = "Meivakantie",
                StartDate = new DateOnly(year, 4, 26),
                EndDate = new DateOnly(year, 5, 4)
            },
            new Holiday
            {
                Id = 4,
                Name = "Zomervakantie",
                StartDate = new DateOnly(year, 7, 6),
                EndDate = new DateOnly(year, 8, 17)
            },
            new Holiday
            {
                Id = 5,
                Name = "Herfstvakantie",
                StartDate = new DateOnly(year, 10, 19),
                EndDate = new DateOnly(year, 10, 27)
            },
            new Holiday
            {
                Id = 6,
                Name = "Goede Vrijdag",
                StartDate = new DateOnly(year, 4, 18),
                EndDate = new DateOnly(year, 4, 18)
            },
            new Holiday
            {
                Id = 7,
                Name = "Paasmaandag",
                StartDate = new DateOnly(year, 4, 21),
                EndDate = new DateOnly(year, 4, 21)
            },
            new Holiday
            {
                Id = 8,
                Name = "Koningsdag",
                StartDate = new DateOnly(year, 4, 27),
                EndDate = new DateOnly(year, 4, 27)
            },
            new Holiday
            {
                Id = 9,
                Name = "Bevrijdingsdag",
                StartDate = new DateOnly(year, 5, 5),
                EndDate = new DateOnly(year, 5, 5)
            },
            new Holiday
            {
                Id = 10,
                Name = "Hemelvaartsdag",
                StartDate = new DateOnly(year, 5, 29),
                EndDate = new DateOnly(year, 5, 29)
            },
            new Holiday
            {
                Id = 11,
                Name = "Pinkstermaandag",
                StartDate = new DateOnly(year, 6, 9),
                EndDate = new DateOnly(year, 6, 9)
            }
        };
}
