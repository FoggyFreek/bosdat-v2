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

    public SupportDataGenerator(ApplicationDbContext context, SeederContext seederContext)
    {
        _context = context;
        _seederContext = seederContext;
    }

    public async Task GeneratePaymentsAsync(List<Invoice> invoices, CancellationToken cancellationToken)
    {
        var existingCount = await _context.Payments.CountAsync(cancellationToken);
        if (existingCount > 0)
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

    public async Task GenerateHolidaysAsync(CancellationToken cancellationToken)
    {
        var existingCount = await _context.Holidays.CountAsync(cancellationToken);
        if (existingCount > 0)
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
