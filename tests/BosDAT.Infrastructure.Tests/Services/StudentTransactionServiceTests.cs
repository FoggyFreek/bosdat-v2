using Moq;
using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Enums;
using BosDAT.Core.Interfaces;
using BosDAT.Infrastructure.Services;

namespace BosDAT.Infrastructure.Tests.Services;

public class StudentTransactionServiceTests
{
    private readonly Mock<IStudentTransactionRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly StudentTransactionService _service;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _studentId = Guid.NewGuid();
    private readonly Guid _invoiceId = Guid.NewGuid();

    public StudentTransactionServiceTests()
    {
        _repoMock
            .Setup(r => r.AddAsync(It.IsAny<StudentTransaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((StudentTransaction t, CancellationToken _) => t);

        _uowMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _service = new StudentTransactionService(_repoMock.Object, _uowMock.Object);
    }

    #region RecordInvoiceChargeAsync

    [Fact]
    public async Task RecordInvoiceChargeAsync_CreatesDebitTransaction()
    {
        // Arrange
        var invoice = BuildInvoice(total: 121m, invoiceNumber: "202601");

        // Act
        await _service.RecordInvoiceChargeAsync(invoice, _userId);

        // Assert
        _repoMock.Verify(r => r.AddAsync(
            It.Is<StudentTransaction>(t =>
                t.StudentId == _studentId &&
                t.Type == TransactionType.InvoiceCharge &&
                t.Debit == 121m &&
                t.Credit == 0 &&
                t.InvoiceId == _invoiceId &&
                t.ReferenceNumber == "202601" &&
                t.Description.Contains("202601") &&
                t.CreatedById == _userId),
            It.IsAny<CancellationToken>()),
            Times.Once);

        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RecordInvoiceChargeAsync_UsesInvoiceIssueDateAsTransactionDate()
    {
        // Arrange
        var issueDate = new DateOnly(2026, 1, 15);
        var invoice = BuildInvoice(total: 50m, invoiceNumber: "202602", issueDate: issueDate);

        // Act
        await _service.RecordInvoiceChargeAsync(invoice, _userId);

        // Assert
        _repoMock.Verify(r => r.AddAsync(
            It.Is<StudentTransaction>(t => t.TransactionDate == issueDate),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region RecordPaymentAsync

    [Fact]
    public async Task RecordPaymentAsync_WithReference_UsePaymentReference()
    {
        // Arrange
        var invoice = BuildInvoice(total: 121m, invoiceNumber: "202601");
        var payment = BuildPayment(amount: 121m, reference: "BANK-REF-001");

        // Act
        await _service.RecordPaymentAsync(payment, invoice, _userId);

        // Assert
        _repoMock.Verify(r => r.AddAsync(
            It.Is<StudentTransaction>(t =>
                t.Type == TransactionType.Payment &&
                t.Credit == 121m &&
                t.Debit == 0 &&
                t.ReferenceNumber == "BANK-REF-001" &&
                t.PaymentId == payment.Id),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RecordPaymentAsync_WithNullReference_FallsBackToInvoiceNumber()
    {
        // Arrange
        var invoice = BuildInvoice(total: 121m, invoiceNumber: "202601");
        var payment = BuildPayment(amount: 121m, reference: null);

        // Act
        await _service.RecordPaymentAsync(payment, invoice, _userId);

        // Assert
        _repoMock.Verify(r => r.AddAsync(
            It.Is<StudentTransaction>(t => t.ReferenceNumber == "202601"),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RecordPaymentAsync_DescriptionContainsInvoiceNumber()
    {
        // Arrange
        var invoice = BuildInvoice(total: 50m, invoiceNumber: "202605");
        var payment = BuildPayment(amount: 50m);

        // Act
        await _service.RecordPaymentAsync(payment, invoice, _userId);

        // Assert
        _repoMock.Verify(r => r.AddAsync(
            It.Is<StudentTransaction>(t => t.Description.Contains("202605")),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region RecordInvoiceAdjustmentAsync

    [Fact]
    public async Task RecordInvoiceAdjustmentAsync_WhenDifferenceIsZero_DoesNothing()
    {
        // Arrange
        var invoice = BuildInvoice(total: 100m, invoiceNumber: "202601");

        // Act
        await _service.RecordInvoiceAdjustmentAsync(invoice, oldTotal: 100m, _userId);

        // Assert
        _repoMock.Verify(r => r.AddAsync(It.IsAny<StudentTransaction>(), It.IsAny<CancellationToken>()), Times.Never);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RecordInvoiceAdjustmentAsync_WhenIncrease_CreatesDebitTransaction()
    {
        // Arrange
        var invoice = BuildInvoice(total: 150m, invoiceNumber: "202601");

        // Act
        await _service.RecordInvoiceAdjustmentAsync(invoice, oldTotal: 100m, _userId);

        // Assert: difference = +50 → Debit
        _repoMock.Verify(r => r.AddAsync(
            It.Is<StudentTransaction>(t =>
                t.Type == TransactionType.InvoiceAdjustment &&
                t.Debit == 50m &&
                t.Credit == 0),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RecordInvoiceAdjustmentAsync_WhenDecrease_CreatesCreditTransaction()
    {
        // Arrange
        var invoice = BuildInvoice(total: 80m, invoiceNumber: "202601");

        // Act
        await _service.RecordInvoiceAdjustmentAsync(invoice, oldTotal: 100m, _userId);

        // Assert: difference = -20 → Credit
        _repoMock.Verify(r => r.AddAsync(
            It.Is<StudentTransaction>(t =>
                t.Type == TransactionType.InvoiceAdjustment &&
                t.Credit == 20m &&
                t.Debit == 0),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RecordInvoiceAdjustmentAsync_DescriptionIncludesSignedDifference()
    {
        // Arrange
        var invoice = BuildInvoice(total: 110m, invoiceNumber: "202601");

        // Act
        await _service.RecordInvoiceAdjustmentAsync(invoice, oldTotal: 100m, _userId);

        // Assert: description contains "+" for increase
        _repoMock.Verify(r => r.AddAsync(
            It.Is<StudentTransaction>(t => t.Description.Contains("+")),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region RecordInvoiceCancellationAsync

    [Fact]
    public async Task RecordInvoiceCancellationAsync_WhenOriginalTotalIsZero_DoesNothing()
    {
        // Arrange
        var invoice = BuildInvoice(total: 0m, invoiceNumber: "202601");

        // Act
        await _service.RecordInvoiceCancellationAsync(invoice, originalTotal: 0m, _userId);

        // Assert
        _repoMock.Verify(r => r.AddAsync(It.IsAny<StudentTransaction>(), It.IsAny<CancellationToken>()), Times.Never);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RecordInvoiceCancellationAsync_WhenOriginalTotalIsNegative_DoesNothing()
    {
        // Arrange
        var invoice = BuildInvoice(total: 0m, invoiceNumber: "202601");

        // Act
        await _service.RecordInvoiceCancellationAsync(invoice, originalTotal: -10m, _userId);

        // Assert
        _repoMock.Verify(r => r.AddAsync(It.IsAny<StudentTransaction>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RecordInvoiceCancellationAsync_WithPositiveTotal_CreatesCreditTransaction()
    {
        // Arrange
        var invoice = BuildInvoice(total: 0m, invoiceNumber: "202601");

        // Act
        await _service.RecordInvoiceCancellationAsync(invoice, originalTotal: 121m, _userId);

        // Assert
        _repoMock.Verify(r => r.AddAsync(
            It.Is<StudentTransaction>(t =>
                t.Type == TransactionType.InvoiceCancellation &&
                t.Credit == 121m &&
                t.Debit == 0 &&
                t.InvoiceId == _invoiceId &&
                t.ReferenceNumber == "202601" &&
                t.Description.Contains("cancelled")),
            It.IsAny<CancellationToken>()),
            Times.Once);

        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region GetTransactionsAsync

    [Fact]
    public async Task GetTransactionsAsync_EmptyList_ReturnsEmpty()
    {
        // Arrange
        _repoMock
            .Setup(r => r.GetByStudentAsync(_studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Act
        var result = await _service.GetTransactionsAsync(_studentId);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetTransactionsAsync_ComputesRunningBalanceCorrectly()
    {
        // Arrange: charge 100, payment 40 → balance after each: 100, 60
        var transactions = new List<StudentTransaction>
        {
            BuildTransaction(debit: 100m, credit: 0),
            BuildTransaction(debit: 0, credit: 40m),
        };

        _repoMock
            .Setup(r => r.GetByStudentAsync(_studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactions);

        // Act
        var result = await _service.GetTransactionsAsync(_studentId);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(100m, result[0].RunningBalance);
        Assert.Equal(60m, result[1].RunningBalance);
    }

    [Fact]
    public async Task GetTransactionsAsync_WithNamedUser_ResolvesFullName()
    {
        // Arrange
        var user = new ApplicationUser { FirstName = "Jan", LastName = "Smit" };
        var transaction = BuildTransaction(debit: 10m, credit: 0, createdBy: user);

        _repoMock
            .Setup(r => r.GetByStudentAsync(_studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([transaction]);

        // Act
        var result = await _service.GetTransactionsAsync(_studentId);

        // Assert
        Assert.Equal("Jan Smit", result[0].CreatedByName);
    }

    [Fact]
    public async Task GetTransactionsAsync_WithUserWithoutName_FallsBackToEmail()
    {
        // Arrange
        var user = new ApplicationUser { FirstName = null, LastName = null, Email = "user@example.com" };
        var transaction = BuildTransaction(debit: 10m, credit: 0, createdBy: user);

        _repoMock
            .Setup(r => r.GetByStudentAsync(_studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([transaction]);

        // Act
        var result = await _service.GetTransactionsAsync(_studentId);

        // Assert
        Assert.Equal("user@example.com", result[0].CreatedByName);
    }

    [Fact]
    public async Task GetTransactionsAsync_WithNullCreatedBy_ReturnsUnknown()
    {
        // Arrange
        var transaction = BuildTransaction(debit: 10m, credit: 0, createdBy: null);

        _repoMock
            .Setup(r => r.GetByStudentAsync(_studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([transaction]);

        // Act
        var result = await _service.GetTransactionsAsync(_studentId);

        // Assert
        Assert.Equal("Unknown", result[0].CreatedByName);
    }

    [Fact]
    public async Task GetTransactionsAsync_MapsDtoFieldsCorrectly()
    {
        // Arrange
        var paymentId = Guid.NewGuid();
        var transaction = BuildTransaction(debit: 50m, credit: 0);
        transaction.PaymentId = paymentId;
        transaction.InvoiceId = _invoiceId;

        _repoMock
            .Setup(r => r.GetByStudentAsync(_studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([transaction]);

        // Act
        var result = await _service.GetTransactionsAsync(_studentId);

        // Assert
        var dto = result[0];
        Assert.Equal(transaction.Id, dto.Id);
        Assert.Equal(_studentId, dto.StudentId);
        Assert.Equal(50m, dto.Debit);
        Assert.Equal(0m, dto.Credit);
        Assert.Equal(_invoiceId, dto.InvoiceId);
        Assert.Equal(paymentId, dto.PaymentId);
    }

    #endregion

    #region GetStudentBalanceAsync

    [Fact]
    public async Task GetStudentBalanceAsync_DelegatesToRepository()
    {
        // Arrange
        _repoMock
            .Setup(r => r.GetBalanceAsync(_studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(250m);

        // Act
        var result = await _service.GetStudentBalanceAsync(_studentId);

        // Assert
        Assert.Equal(250m, result);
        _repoMock.Verify(r => r.GetBalanceAsync(_studentId, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Helpers

    private Invoice BuildInvoice(decimal total, string invoiceNumber, DateOnly? issueDate = null)
    {
        return new Invoice
        {
            Id = _invoiceId,
            StudentId = _studentId,
            InvoiceNumber = invoiceNumber,
            IssueDate = issueDate ?? new DateOnly(2026, 1, 1),
            DueDate = new DateOnly(2026, 1, 31),
            Total = total
        };
    }

    private static Payment BuildPayment(decimal amount, string? reference = "REF-001")
    {
        return new Payment
        {
            Id = Guid.NewGuid(),
            InvoiceId = Guid.NewGuid(),
            Amount = amount,
            PaymentDate = new DateOnly(2026, 1, 15),
            Method = PaymentMethod.Bank,
            Reference = reference
        };
    }

    private StudentTransaction BuildTransaction(
        decimal debit, decimal credit, ApplicationUser? createdBy = null)
    {
        return new StudentTransaction
        {
            Id = Guid.NewGuid(),
            StudentId = _studentId,
            TransactionDate = new DateOnly(2026, 1, 1),
            Type = TransactionType.InvoiceCharge,
            Description = "Test transaction",
            ReferenceNumber = "202601",
            Debit = debit,
            Credit = credit,
            CreatedById = _userId,
            CreatedBy = createdBy!
        };
    }

    #endregion
}
