using System.Runtime.InteropServices;
using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Enums;
using BosDAT.Core.Interfaces;
using BosDAT.Core.Interfaces.Services;
using BosDAT.Infrastructure.Services;

namespace BosDAT.Infrastructure.Tests.Services;

/// <summary>
/// Tests for InvoicePdfService. Skipped on win-arm64 where QuestPDF native libs are unavailable.
/// </summary>
public class InvoicePdfServiceTests
{
    private static readonly bool IsQuestPdfSupported =
        !(RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
          && RuntimeInformation.ProcessArchitecture == Architecture.Arm64);

    private readonly InvoicePdfService _service = new();

    private static InvoiceDto CreateTestInvoice(bool isCreditInvoice = false) => new()
    {
        Id = Guid.NewGuid(),
        InvoiceNumber = isCreditInvoice ? "C-2025-001" : "F-2025-001",
        StudentId = Guid.NewGuid(),
        StudentName = "Jan de Vries",
        StudentEmail = "jan@example.com",
        IssueDate = new DateOnly(2025, 3, 1),
        DueDate = new DateOnly(2025, 3, 31),
        PeriodStart = new DateOnly(2025, 1, 1),
        PeriodEnd = new DateOnly(2025, 3, 31),
        Subtotal = 300m,
        VatAmount = 63m,
        Total = isCreditInvoice ? -363m : 363m,
        DiscountAmount = 0m,
        Status = InvoiceStatus.Sent,
        AmountPaid = isCreditInvoice ? 0m : 100m,
        Balance = isCreditInvoice ? -363m : 263m,
        IsCreditInvoice = isCreditInvoice,
        OriginalInvoiceId = isCreditInvoice ? Guid.NewGuid() : null,
        OriginalInvoiceNumber = isCreditInvoice ? "F-2025-001" : null,
        Lines =
        [
            new InvoiceLineDto
            {
                Id = 1,
                Description = "Pianoles - Kwartaal 1",
                Quantity = 12,
                UnitPrice = 25m,
                VatRate = 21m,
                LineTotal = 300m,
            }
        ],
        BillingContact = new BillingContactDto
        {
            Name = "Pieter de Vries",
            Address = "Hoofdstraat 1",
            PostalCode = "1234 AB",
            City = "Amsterdam",
            Email = "pieter@example.com",
        },
    };

    private static SchoolBillingInfoDto CreateTestSchoolInfo() => new()
    {
        Name = "Muziekschool Test",
        Address = "Schoolstraat 10",
        PostalCode = "5678 CD",
        City = "Utrecht",
        Phone = "030-1234567",
        Email = "info@muziekschooltest.nl",
        KvkNumber = "12345678",
        BtwNumber = "NL123456789B01",
        Iban = "NL91ABNA0417164300",
        VatRate = 21m,
    };

    [Fact]
    public async Task GeneratePdfAsync_ReturnsValidPdfBytes()
    {
        if (!IsQuestPdfSupported) return; // QuestPDF does not support win-arm64

        var invoice = CreateTestInvoice();
        var schoolInfo = CreateTestSchoolInfo();

        var result = await _service.GeneratePdfAsync(invoice, schoolInfo);

        Assert.NotNull(result);
        Assert.True(result.Length > 0);
        // PDF files start with %PDF
        Assert.Equal((byte)'%', result[0]);
        Assert.Equal((byte)'P', result[1]);
        Assert.Equal((byte)'D', result[2]);
        Assert.Equal((byte)'F', result[3]);
    }

    [Fact]
    public async Task GeneratePdfAsync_CreditInvoice_ReturnsValidPdf()
    {
        if (!IsQuestPdfSupported) return;

        var invoice = CreateTestInvoice(isCreditInvoice: true);
        var schoolInfo = CreateTestSchoolInfo();

        var result = await _service.GeneratePdfAsync(invoice, schoolInfo);

        Assert.NotNull(result);
        Assert.True(result.Length > 0);
        Assert.Equal((byte)'%', result[0]);
    }

    [Fact]
    public async Task GeneratePdfAsync_WithDiscount_ReturnsValidPdf()
    {
        if (!IsQuestPdfSupported) return;

        var invoice = CreateTestInvoice();
        invoice = invoice with { DiscountAmount = 50m, Total = 313m, Balance = 213m };
        var schoolInfo = CreateTestSchoolInfo();

        var result = await _service.GeneratePdfAsync(invoice, schoolInfo);

        Assert.NotNull(result);
        Assert.True(result.Length > 0);
    }

    [Fact]
    public async Task GeneratePdfAsync_WithoutBillingContact_ReturnsValidPdf()
    {
        if (!IsQuestPdfSupported) return;

        var invoice = CreateTestInvoice() with { BillingContact = null };
        var schoolInfo = CreateTestSchoolInfo();

        var result = await _service.GeneratePdfAsync(invoice, schoolInfo);

        Assert.NotNull(result);
        Assert.True(result.Length > 0);
    }

    [Fact]
    public async Task GeneratePdfAsync_WithMinimalSchoolInfo_ReturnsValidPdf()
    {
        if (!IsQuestPdfSupported) return;

        var invoice = CreateTestInvoice();
        var schoolInfo = new SchoolBillingInfoDto { Name = "School", VatRate = 21m };

        var result = await _service.GeneratePdfAsync(invoice, schoolInfo);

        Assert.NotNull(result);
        Assert.True(result.Length > 0);
    }
}
