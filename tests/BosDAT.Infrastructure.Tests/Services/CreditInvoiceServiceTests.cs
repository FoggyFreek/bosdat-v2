using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
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
            Status = Core.Enums.StudentStatus.Active,
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

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
