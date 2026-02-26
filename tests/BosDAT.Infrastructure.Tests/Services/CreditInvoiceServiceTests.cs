using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Enums;
using BosDAT.Core.Interfaces;
using BosDAT.Infrastructure.Data;
using BosDAT.Infrastructure.Services;
using BosDAT.Infrastructure.Repositories;
using Moq;

namespace BosDAT.Infrastructure.Tests.Services;

public class CreditInvoiceServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly UnitOfWork _unitOfWork;
    private readonly Mock<IStudentTransactionService> _mockTransactionService;
    private readonly InvoiceQueryService _queryService;
    private readonly CreditInvoiceService _service;
    private readonly Guid _userId = Guid.NewGuid();

    public CreditInvoiceServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"CreditInvoiceServiceTest_{Guid.NewGuid()}")
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new ApplicationDbContext(options);
        _unitOfWork = new UnitOfWork(_context);
        _mockTransactionService = new Mock<IStudentTransactionService>();
        _queryService = new InvoiceQueryService(_context);
        _service = new CreditInvoiceService(_unitOfWork, _mockTransactionService.Object, _queryService);

        SeedSettings();
    }

    private void SeedSettings()
    {
        _context.Settings.AddRange(
            new Setting { Key = "vat_rate", Value = "21", Type = "decimal", Description = "VAT rate" },
            new Setting { Key = "school_name", Value = "Test Music School", Type = "string", Description = "School name" },
            new Setting { Key = "school_address", Value = "Test Street 123", Type = "string", Description = "School address" },
            new Setting { Key = "school_postal_code", Value = "1234AB", Type = "string", Description = "School postal code" },
            new Setting { Key = "school_city", Value = "Test City", Type = "string", Description = "School city" },
            new Setting { Key = "school_phone", Value = "0612345678", Type = "string", Description = "School phone" },
            new Setting { Key = "school_email", Value = "test@school.nl", Type = "string", Description = "School email" },
            new Setting { Key = "school_kvk", Value = "12345678", Type = "string", Description = "School KvK" },
            new Setting { Key = "school_btw", Value = "NL123456789B01", Type = "string", Description = "School BTW" },
            new Setting { Key = "school_iban", Value = "NL00TEST0000000001", Type = "string", Description = "School IBAN" }
        );
        _context.SaveChanges();
    }

    private async Task<(Invoice invoice, Student student)> SeedInvoiceWithLines()
    {
        var student = new Student
        {
            Id = Guid.NewGuid(),
            FirstName = "Test",
            LastName = "Student",
            Email = "test@student.nl",
            Status = StudentStatus.Active,
        };
        _context.Students.Add(student);
        await _context.SaveChangesAsync();

        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = "202601",
            StudentId = student.Id,
            IssueDate = new DateOnly(2026, 1, 15),
            DueDate = new DateOnly(2026, 1, 29),
            Description = "jan26",
            PeriodStart = new DateOnly(2026, 1, 1),
            PeriodEnd = new DateOnly(2026, 1, 31),
            Status = InvoiceStatus.Sent,
            Subtotal = 100m,
            VatAmount = 21m,
            Total = 121m,
        };
        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync();

        var line1 = new InvoiceLine
        {
            InvoiceId = invoice.Id,
            Description = "Lesson 1",
            Quantity = 1,
            UnitPrice = 50m,
            VatRate = 21m,
            VatAmount = 10.50m,
            LineTotal = 50m,
        };
        var line2 = new InvoiceLine
        {
            InvoiceId = invoice.Id,
            Description = "Lesson 2",
            Quantity = 1,
            UnitPrice = 50m,
            VatRate = 21m,
            VatAmount = 10.50m,
            LineTotal = 50m,
        };
        _context.InvoiceLines.Add(line1);
        _context.InvoiceLines.Add(line2);
        await _context.SaveChangesAsync();

        return (invoice, student);
    }

    #region CreateCreditInvoiceAsync Tests

    [Fact]
    public async Task CreateCreditInvoiceAsync_WithSelectedLines_CreatesNegativeAmounts()
    {
        // Arrange
        var (invoice, _) = await SeedInvoiceWithLines();
        var lineIds = await _context.InvoiceLines
            .Where(l => l.InvoiceId == invoice.Id)
            .Select(l => l.Id)
            .ToListAsync();

        var dto = new CreateCreditInvoiceDto
        {
            SelectedLineIds = lineIds,
        };

        // Act
        var result = await _service.CreateCreditInvoiceAsync(invoice.Id, dto, _userId);

        // Assert
        Assert.True(result.IsCreditInvoice);
        Assert.True(result.Subtotal < 0, "Credit invoice subtotal should be negative");
        Assert.True(result.VatAmount < 0, "Credit invoice VAT amount should be negative");
        Assert.True(result.Total < 0, "Credit invoice total should be negative");
        Assert.Equal(-100m, result.Subtotal);
        Assert.Equal(-21m, result.VatAmount);
        Assert.Equal(-121m, result.Total);
    }

    [Fact]
    public async Task CreateCreditInvoiceAsync_CreditLines_HaveNegativeUnitPriceAndLineTotal()
    {
        // Arrange
        var (invoice, _) = await SeedInvoiceWithLines();
        var lineIds = await _context.InvoiceLines
            .Where(l => l.InvoiceId == invoice.Id)
            .Select(l => l.Id)
            .ToListAsync();

        var dto = new CreateCreditInvoiceDto
        {
            SelectedLineIds = lineIds,
        };

        // Act
        var result = await _service.CreateCreditInvoiceAsync(invoice.Id, dto, _userId);

        // Assert
        Assert.Equal(2, result.Lines.Count);
        foreach (var line in result.Lines)
        {
            Assert.True(line.UnitPrice < 0, "Credit line unit price should be negative");
            Assert.True(line.LineTotal < 0, "Credit line total should be negative");
            Assert.Equal(-50m, line.UnitPrice);
            Assert.Equal(-50m, line.LineTotal);
        }
    }

    [Fact]
    public async Task CreateCreditInvoiceAsync_PartialCredit_NegatesOnlySelectedLines()
    {
        // Arrange
        var (invoice, _) = await SeedInvoiceWithLines();
        var firstLineId = await _context.InvoiceLines
            .Where(l => l.InvoiceId == invoice.Id)
            .Select(l => l.Id)
            .FirstAsync();

        var dto = new CreateCreditInvoiceDto
        {
            SelectedLineIds = [firstLineId],
        };

        // Act
        var result = await _service.CreateCreditInvoiceAsync(invoice.Id, dto, _userId);

        // Assert
        Assert.Single(result.Lines);
        Assert.Equal(-50m, result.Subtotal);
        Assert.Equal(-10.50m, result.VatAmount);
        Assert.Equal(-60.50m, result.Total);
    }

    [Fact]
    public async Task CreateCreditInvoiceAsync_PreservesVatRateAsPositive()
    {
        // Arrange
        var (invoice, _) = await SeedInvoiceWithLines();
        var lineIds = await _context.InvoiceLines
            .Where(l => l.InvoiceId == invoice.Id)
            .Select(l => l.Id)
            .ToListAsync();

        var dto = new CreateCreditInvoiceDto
        {
            SelectedLineIds = lineIds,
        };

        // Act
        var result = await _service.CreateCreditInvoiceAsync(invoice.Id, dto, _userId);

        // Assert
        foreach (var line in result.Lines)
        {
            Assert.Equal(21m, line.VatRate);
        }
    }

    [Fact]
    public async Task CreateCreditInvoiceAsync_SetsIsCreditInvoiceAndOriginalReference()
    {
        // Arrange
        var (invoice, _) = await SeedInvoiceWithLines();
        var lineIds = await _context.InvoiceLines
            .Where(l => l.InvoiceId == invoice.Id)
            .Select(l => l.Id)
            .ToListAsync();

        var dto = new CreateCreditInvoiceDto
        {
            SelectedLineIds = lineIds,
        };

        // Act
        var result = await _service.CreateCreditInvoiceAsync(invoice.Id, dto, _userId);

        // Assert
        Assert.True(result.IsCreditInvoice);
        Assert.Equal(invoice.Id, result.OriginalInvoiceId);
        Assert.Equal(invoice.InvoiceNumber, result.OriginalInvoiceNumber);
        Assert.Equal(InvoiceStatus.Draft, result.Status);
    }

    [Fact]
    public async Task CreateCreditInvoiceAsync_ForDraftInvoice_ThrowsInvalidOperation()
    {
        // Arrange
        var (invoice, _) = await SeedInvoiceWithLines();
        invoice.Status = InvoiceStatus.Draft;
        await _context.SaveChangesAsync();

        var dto = new CreateCreditInvoiceDto
        {
            SelectedLineIds = [1],
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateCreditInvoiceAsync(invoice.Id, dto, _userId));
    }

    [Fact]
    public async Task CreateCreditInvoiceAsync_ForCreditInvoice_ThrowsInvalidOperation()
    {
        // Arrange
        var (invoice, _) = await SeedInvoiceWithLines();
        invoice.IsCreditInvoice = true;
        await _context.SaveChangesAsync();

        var dto = new CreateCreditInvoiceDto
        {
            SelectedLineIds = [1],
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateCreditInvoiceAsync(invoice.Id, dto, _userId));
    }

    [Fact]
    public async Task CreateCreditInvoiceAsync_WithEmptyLineIds_ThrowsInvalidOperation()
    {
        // Arrange
        var (invoice, _) = await SeedInvoiceWithLines();

        var dto = new CreateCreditInvoiceDto
        {
            SelectedLineIds = [],
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateCreditInvoiceAsync(invoice.Id, dto, _userId));
    }

    #endregion

    #region ConfirmCreditInvoiceAsync Tests

    [Fact]
    public async Task ConfirmCreditInvoiceAsync_RecordsTransactionWithAbsoluteValue()
    {
        // Arrange
        var (invoice, _) = await SeedInvoiceWithLines();
        var lineIds = await _context.InvoiceLines
            .Where(l => l.InvoiceId == invoice.Id)
            .Select(l => l.Id)
            .ToListAsync();

        var creditDto = new CreateCreditInvoiceDto { SelectedLineIds = lineIds };
        var creditInvoice = await _service.CreateCreditInvoiceAsync(invoice.Id, creditDto, _userId);

        // Act
        var result = await _service.ConfirmCreditInvoiceAsync(creditInvoice.Id, _userId);

        // Assert
        Assert.Equal(InvoiceStatus.Sent, result.Status);

        // Verify the transaction service was called with the credit invoice
        _mockTransactionService.Verify(
            s => s.RecordCreditInvoiceAsync(
                It.Is<Invoice>(i => i.Id == creditInvoice.Id),
                It.Is<Invoice>(i => i.Id == invoice.Id),
                _userId,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region ApplyCreditInvoicesAsync Tests

    private async Task<Invoice> SeedCreditInvoiceAsync(Guid studentId, decimal absoluteTotal, string? invoiceNumber = null)
    {
        var creditInvoice = new Invoice
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = invoiceNumber ?? $"C-{Guid.NewGuid():N[..6]}",
            StudentId = studentId,
            IssueDate = new DateOnly(2026, 1, 10),
            DueDate = new DateOnly(2026, 1, 10),
            Status = InvoiceStatus.Sent,
            IsCreditInvoice = true,
            OriginalInvoiceId = Guid.NewGuid(),
            Subtotal = -absoluteTotal,
            VatAmount = 0m,
            Total = -absoluteTotal,
        };
        _context.Invoices.Add(creditInvoice);
        await _context.SaveChangesAsync();
        return creditInvoice;
    }

    [Fact]
    public async Task ApplyCreditInvoicesAsync_SingleCreditInvoice_FullyCoverInvoice_MarksInvoicePaid()
    {
        // Arrange — target invoice: €121, credit invoice: €121
        var (invoice, student) = await SeedInvoiceWithLines();
        await SeedCreditInvoiceAsync(student.Id, absoluteTotal: 121m, invoiceNumber: "C-202601");

        // Act
        var result = await _service.ApplyCreditInvoicesAsync(invoice.Id, _userId);

        // Assert
        Assert.Equal(InvoiceStatus.Paid, result.Status);
        Assert.Equal(0m, result.Balance);
        Assert.Equal(121m, result.AmountPaid);
    }

    [Fact]
    public async Task ApplyCreditInvoicesAsync_CreditLessThanInvoice_PartialApplication_InvoiceRemainsOpen()
    {
        // Arrange — target: €121, credit: €50 → remaining €71
        var (invoice, student) = await SeedInvoiceWithLines();
        await SeedCreditInvoiceAsync(student.Id, absoluteTotal: 50m, invoiceNumber: "C-202601");

        // Act
        var result = await _service.ApplyCreditInvoicesAsync(invoice.Id, _userId);

        // Assert
        Assert.Equal(InvoiceStatus.Sent, result.Status);
        Assert.Equal(50m, result.AmountPaid);
        Assert.Equal(71m, result.Balance);
    }

    [Fact]
    public async Task ApplyCreditInvoicesAsync_CreditExceedsInvoice_OnlyInvoiceAmountApplied()
    {
        // Arrange — target: €121, credit: €200 → only €121 applied
        var (invoice, student) = await SeedInvoiceWithLines();
        await SeedCreditInvoiceAsync(student.Id, absoluteTotal: 200m, invoiceNumber: "C-202601");

        // Act
        var result = await _service.ApplyCreditInvoicesAsync(invoice.Id, _userId);

        // Assert — invoice fully covered
        Assert.Equal(InvoiceStatus.Paid, result.Status);
        Assert.Equal(0m, result.Balance);
        Assert.Equal(121m, result.AmountPaid);

        // Verify RecordCreditAppliedAsync called with exactly invoice amount (121m)
        _mockTransactionService.Verify(
            s => s.RecordCreditAppliedAsync(
                It.IsAny<Invoice>(),
                It.Is<Invoice>(i => i.Id == invoice.Id),
                It.Is<Payment>(p => p.Amount == 121m && p.Method == PaymentMethod.CreditBalance && p.Reference != null),
                _userId,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ApplyCreditInvoicesAsync_MultipleCreditInvoices_AppliesToSmallestFirst()
    {
        // Arrange — CI1: €30 (smallest), CI2: €100 (larger)
        var (invoice, student) = await SeedInvoiceWithLines(); // total = 121
        var ci1 = await SeedCreditInvoiceAsync(student.Id, absoluteTotal: 30m, invoiceNumber: "C-202601");
        var ci2 = await SeedCreditInvoiceAsync(student.Id, absoluteTotal: 100m, invoiceNumber: "C-202602");

        // Act
        var result = await _service.ApplyCreditInvoicesAsync(invoice.Id, _userId);

        // Assert — smallest (CI1=30) applied first, then CI2 fills remainder
        Assert.Equal(InvoiceStatus.Paid, result.Status);

        // Verify CI1 was applied first (amount = 30)
        _mockTransactionService.Verify(
            s => s.RecordCreditAppliedAsync(
                It.Is<Invoice>(ci => ci.Id == ci1.Id),
                It.IsAny<Invoice>(),
                It.Is<Payment>(p => p.Amount == 30m),
                _userId,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ApplyCreditInvoicesAsync_MultipleCreditInvoices_ChainUntilPaid()
    {
        // Arrange — target: €121, CI1: €60, CI2: €80 → CI1 covers 60, CI2 covers 61
        var (invoice, student) = await SeedInvoiceWithLines(); // total = 121
        await SeedCreditInvoiceAsync(student.Id, absoluteTotal: 60m, invoiceNumber: "C-202601");
        await SeedCreditInvoiceAsync(student.Id, absoluteTotal: 80m, invoiceNumber: "C-202602");

        // Act
        var result = await _service.ApplyCreditInvoicesAsync(invoice.Id, _userId);

        // Assert
        Assert.Equal(InvoiceStatus.Paid, result.Status);
        Assert.Equal(0m, result.Balance);
        Assert.Equal(121m, result.AmountPaid);

        // Both credit invoices should have been used
        _mockTransactionService.Verify(
            s => s.RecordCreditAppliedAsync(
                It.IsAny<Invoice>(), It.IsAny<Invoice>(), It.IsAny<Payment>(),
                _userId, It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task ApplyCreditInvoicesAsync_NoAvailableCredit_Throws()
    {
        // Arrange — target invoice has no credit invoices
        var (invoice, _) = await SeedInvoiceWithLines();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ApplyCreditInvoicesAsync(invoice.Id, _userId));
        Assert.Contains("No credit invoices", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ApplyCreditInvoicesAsync_CancelledInvoice_Throws()
    {
        // Arrange
        var (invoice, student) = await SeedInvoiceWithLines();
        await SeedCreditInvoiceAsync(student.Id, absoluteTotal: 50m, invoiceNumber: "C-202601");
        invoice.Status = InvoiceStatus.Cancelled;
        await _context.SaveChangesAsync();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ApplyCreditInvoicesAsync(invoice.Id, _userId));
        Assert.Contains("Cancelled", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ApplyCreditInvoicesAsync_CreditInvoiceTarget_Throws()
    {
        // Arrange
        var (invoice, student) = await SeedInvoiceWithLines();
        await SeedCreditInvoiceAsync(student.Id, absoluteTotal: 50m, invoiceNumber: "C-202601");
        invoice.IsCreditInvoice = true;
        await _context.SaveChangesAsync();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ApplyCreditInvoicesAsync(invoice.Id, _userId));
        Assert.Contains("credit invoice", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ApplyCreditInvoicesAsync_PaidInvoice_Throws()
    {
        // Arrange
        var (invoice, student) = await SeedInvoiceWithLines();
        await SeedCreditInvoiceAsync(student.Id, absoluteTotal: 50m, invoiceNumber: "C-202601");
        invoice.Status = InvoiceStatus.Paid;
        await _context.SaveChangesAsync();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ApplyCreditInvoicesAsync(invoice.Id, _userId));
        Assert.Contains("already paid", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ApplyCreditInvoicesAsync_VerifiesTwoTransactionsPerApplication()
    {
        // Arrange — one credit invoice fully covers one target invoice
        var (invoice, student) = await SeedInvoiceWithLines(); // total = 121
        var creditInvoice = await SeedCreditInvoiceAsync(student.Id, absoluteTotal: 121m, invoiceNumber: "C-202601");

        // Act
        await _service.ApplyCreditInvoicesAsync(invoice.Id, _userId);

        // Assert: RecordCreditAppliedAsync called once with (creditInvoice, targetInvoice, payment)
        _mockTransactionService.Verify(
            s => s.RecordCreditAppliedAsync(
                It.Is<Invoice>(ci => ci.Id == creditInvoice.Id),
                It.Is<Invoice>(ti => ti.Id == invoice.Id),
                It.Is<Payment>(p => p.Amount == 121m && p.Method == PaymentMethod.CreditBalance),
                _userId,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ApplyCreditInvoicesAsync_PartiallyConsumedCreditInvoice_OnlyRemainingCreditApplied()
    {
        // Scenario: CI = -€50. Invoice B (€25) was already paid from CI, leaving €25 remaining.
        // Now Invoice C (total=121) is processed — only €25 (the remainder) should be applied.
        //
        // This test verifies that GetConfirmedCreditInvoicesWithRemainingCreditAsync and
        // GetAppliedCreditAmountAsync together correctly cap the applied amount at the
        // remaining credit, not the full CI total.

        var (invoice, student) = await SeedInvoiceWithLines(); // total = 121 — acts as "Invoice C"
        var creditInvoice = await SeedCreditInvoiceAsync(student.Id, absoluteTotal: 50m, invoiceNumber: "C-202601");

        // Simulate that €25 of the credit invoice was already consumed (applied to a prior Invoice B)
        var priorConsumption = new StudentTransaction
        {
            Id = Guid.NewGuid(),
            StudentId = student.Id,
            TransactionDate = new DateOnly(2026, 1, 20),
            Type = TransactionType.CreditOffset,
            Description = "Credit C-202601 applied to invoice 202601",
            ReferenceNumber = "C-202601",
            Debit = 25m,
            Credit = 0,
            InvoiceId = creditInvoice.Id,   // key: linked to the credit invoice, not the target
            CreatedById = _userId
        };
        _context.StudentTransactions.Add(priorConsumption);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ApplyCreditInvoicesAsync(invoice.Id, _userId);

        // Assert — only the remaining €25 (not the full €50) is applied
        Assert.Equal(InvoiceStatus.Sent, result.Status);   // 121 - 25 = 96 remaining → not paid
        Assert.Equal(25m, result.AmountPaid);
        Assert.Equal(96m, result.Balance);

        _mockTransactionService.Verify(
            s => s.RecordCreditAppliedAsync(
                It.Is<Invoice>(ci => ci.Id == creditInvoice.Id),
                It.Is<Invoice>(ti => ti.Id == invoice.Id),
                It.Is<Payment>(p => p.Amount == 25m),
                _userId,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ApplyCreditInvoicesAsync_FullyConsumedCreditInvoice_IsExcluded_ThrowsNoCredit()
    {
        // Scenario: CI = -€50, but all €50 was already applied to a prior invoice.
        // Calling apply-credit on a new invoice should throw "no credit available".

        var (invoice, student) = await SeedInvoiceWithLines(); // total = 121
        var creditInvoice = await SeedCreditInvoiceAsync(student.Id, absoluteTotal: 50m, invoiceNumber: "C-202601");

        // Consume the full €50
        var fullConsumption = new StudentTransaction
        {
            Id = Guid.NewGuid(),
            StudentId = student.Id,
            TransactionDate = new DateOnly(2026, 1, 20),
            Type = TransactionType.CreditOffset,
            Description = "Credit C-202601 applied",
            ReferenceNumber = "C-202601",
            Debit = 50m,
            Credit = 0,
            InvoiceId = creditInvoice.Id,
            CreatedById = _userId
        };
        _context.StudentTransactions.Add(fullConsumption);
        await _context.SaveChangesAsync();

        // Act & Assert — CI is fully consumed so it is excluded by the repository query
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ApplyCreditInvoicesAsync(invoice.Id, _userId));
        Assert.Contains("No credit invoices", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
