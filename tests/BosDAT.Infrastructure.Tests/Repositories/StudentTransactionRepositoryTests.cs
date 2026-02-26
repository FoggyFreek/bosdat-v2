using BosDAT.Core.Entities;
using BosDAT.Core.Enums;
using BosDAT.Infrastructure.Repositories;

namespace BosDAT.Infrastructure.Tests.Repositories;

public class StudentTransactionRepositoryTests : RepositoryTestBase
{
    private readonly StudentTransactionRepository _repository;
    private readonly Guid _studentId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    public StudentTransactionRepositoryTests()
    {
        _repository = new StudentTransactionRepository(Context);
        SeedStudent();
    }

    private void SeedStudent()
    {
        Context.Users.Add(new ApplicationUser
        {
            Id = _userId,
            UserName = "testuser",
            NormalizedUserName = "TESTUSER",
            Email = "testuser@test.com",
            NormalizedEmail = "TESTUSER@TEST.COM",
            SecurityStamp = Guid.NewGuid().ToString()
        });
        Context.Students.Add(new Student
        {
            Id = _studentId,
            FirstName = "Alice",
            LastName = "Tester",
            Email = "alice@test.com",
            Phone = "0611111111",
            Status = StudentStatus.Active
        });
        Context.SaveChanges();
    }

    #region GetByStudentAsync

    [Fact]
    public async Task GetByStudentAsync_WithNoTransactions_ReturnsEmpty()
    {
        // Act
        var result = await _repository.GetByStudentAsync(_studentId);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByStudentAsync_ReturnsOnlyTransactionsForStudent()
    {
        // Arrange
        var otherStudentId = Guid.NewGuid();
        Context.Students.Add(new Student
        {
            Id = otherStudentId, FirstName = "Other", LastName = "Student",
            Email = "other@test.com", Phone = "0622222222", Status = StudentStatus.Active
        });

        Context.StudentTransactions.AddRange(
            BuildTransaction(_studentId, new DateOnly(2026, 1, 5), 100m, 0),
            BuildTransaction(_studentId, new DateOnly(2026, 1, 10), 0, 50m),
            BuildTransaction(otherStudentId, new DateOnly(2026, 1, 6), 200m, 0));
        await Context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByStudentAsync(_studentId);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, t => Assert.Equal(_studentId, t.StudentId));
    }

    [Fact]
    public async Task GetByStudentAsync_ReturnsTransactionsOrderedByDateThenCreatedAt()
    {
        // Arrange
        Context.StudentTransactions.AddRange(
            BuildTransaction(_studentId, new DateOnly(2026, 1, 20), 30m, 0),
            BuildTransaction(_studentId, new DateOnly(2026, 1, 5), 10m, 0),
            BuildTransaction(_studentId, new DateOnly(2026, 1, 10), 20m, 0));
        await Context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByStudentAsync(_studentId);

        // Assert
        Assert.Equal(new DateOnly(2026, 1, 5), result[0].TransactionDate);
        Assert.Equal(new DateOnly(2026, 1, 10), result[1].TransactionDate);
        Assert.Equal(new DateOnly(2026, 1, 20), result[2].TransactionDate);
    }

    #endregion

    #region GetBalanceAsync

    [Fact]
    public async Task GetBalanceAsync_WithNoTransactions_ReturnsZero()
    {
        // Act
        var result = await _repository.GetBalanceAsync(_studentId);

        // Assert
        Assert.Equal(0m, result);
    }

    [Fact]
    public async Task GetBalanceAsync_CalculatesDebitMinusCredit()
    {
        // Arrange: debit 100 + 50 = 150, credit 40 → balance = 110
        Context.StudentTransactions.AddRange(
            BuildTransaction(_studentId, new DateOnly(2026, 1, 1), debit: 100m, credit: 0),
            BuildTransaction(_studentId, new DateOnly(2026, 1, 5), debit: 50m, credit: 0),
            BuildTransaction(_studentId, new DateOnly(2026, 1, 10), debit: 0, credit: 40m));
        await Context.SaveChangesAsync();

        // Act
        var result = await _repository.GetBalanceAsync(_studentId);

        // Assert
        Assert.Equal(110m, result);
    }

    [Fact]
    public async Task GetBalanceAsync_OnlyCountsTransactionsForStudent()
    {
        // Arrange
        var otherStudentId = Guid.NewGuid();
        Context.Students.Add(new Student
        {
            Id = otherStudentId, FirstName = "Bob", LastName = "Other",
            Email = "bob@test.com", Phone = "0633333333", Status = StudentStatus.Active
        });
        Context.StudentTransactions.AddRange(
            BuildTransaction(_studentId, new DateOnly(2026, 1, 1), debit: 100m, credit: 0),
            BuildTransaction(otherStudentId, new DateOnly(2026, 1, 1), debit: 9999m, credit: 0));
        await Context.SaveChangesAsync();

        // Act
        var result = await _repository.GetBalanceAsync(_studentId);

        // Assert
        Assert.Equal(100m, result);
    }

    #endregion

    #region GetByStudentFilteredAsync

    [Fact]
    public async Task GetByStudentFilteredAsync_WithNoFilters_ReturnsAllForStudent()
    {
        // Arrange
        Context.StudentTransactions.AddRange(
            BuildTransaction(_studentId, new DateOnly(2026, 1, 1), 50m, 0, TransactionType.InvoiceCharge),
            BuildTransaction(_studentId, new DateOnly(2026, 1, 5), 0, 50m, TransactionType.Payment));
        await Context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByStudentFilteredAsync(_studentId);

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetByStudentFilteredAsync_FilteredByType_ReturnsOnlyMatchingType()
    {
        // Arrange
        Context.StudentTransactions.AddRange(
            BuildTransaction(_studentId, new DateOnly(2026, 1, 1), 100m, 0, TransactionType.InvoiceCharge),
            BuildTransaction(_studentId, new DateOnly(2026, 1, 5), 0, 100m, TransactionType.Payment));
        await Context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByStudentFilteredAsync(_studentId, type: TransactionType.Payment);

        // Assert
        Assert.Single(result);
        Assert.Equal(TransactionType.Payment, result[0].Type);
    }

    [Fact]
    public async Task GetByStudentFilteredAsync_FilteredByFromDate_ReturnsOnlyFromDate()
    {
        // Arrange
        Context.StudentTransactions.AddRange(
            BuildTransaction(_studentId, new DateOnly(2026, 1, 1), 10m, 0),
            BuildTransaction(_studentId, new DateOnly(2026, 2, 1), 20m, 0),
            BuildTransaction(_studentId, new DateOnly(2026, 3, 1), 30m, 0));
        await Context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByStudentFilteredAsync(
            _studentId, from: new DateOnly(2026, 2, 1));

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, t => Assert.True(t.TransactionDate >= new DateOnly(2026, 2, 1)));
    }

    [Fact]
    public async Task GetByStudentFilteredAsync_FilteredByToDate_ReturnsOnlyUpToDate()
    {
        // Arrange
        Context.StudentTransactions.AddRange(
            BuildTransaction(_studentId, new DateOnly(2026, 1, 1), 10m, 0),
            BuildTransaction(_studentId, new DateOnly(2026, 2, 1), 20m, 0),
            BuildTransaction(_studentId, new DateOnly(2026, 3, 1), 30m, 0));
        await Context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByStudentFilteredAsync(
            _studentId, to: new DateOnly(2026, 2, 1));

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, t => Assert.True(t.TransactionDate <= new DateOnly(2026, 2, 1)));
    }

    [Fact]
    public async Task GetByStudentFilteredAsync_FilteredByDateRange_ReturnsOnlyInRange()
    {
        // Arrange
        Context.StudentTransactions.AddRange(
            BuildTransaction(_studentId, new DateOnly(2026, 1, 1), 10m, 0),
            BuildTransaction(_studentId, new DateOnly(2026, 2, 15), 20m, 0),
            BuildTransaction(_studentId, new DateOnly(2026, 3, 31), 30m, 0));
        await Context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByStudentFilteredAsync(
            _studentId,
            from: new DateOnly(2026, 2, 1),
            to: new DateOnly(2026, 3, 1));

        // Assert
        Assert.Single(result);
        Assert.Equal(new DateOnly(2026, 2, 15), result[0].TransactionDate);
    }

    #endregion

    #region GetAppliedCreditAmountAsync

    [Fact]
    public async Task GetAppliedCreditAmountAsync_NoTransactions_ReturnsZero()
    {
        // Arrange
        var creditInvoiceId = Guid.NewGuid();

        // Act
        var result = await _repository.GetAppliedCreditAmountAsync(creditInvoiceId);

        // Assert
        Assert.Equal(0m, result);
    }

    [Fact]
    public async Task GetAppliedCreditAmountAsync_SumsCreditOffsetDebitsForInvoice()
    {
        // Arrange
        var creditInvoiceId = Guid.NewGuid();
        var otherInvoiceId = Guid.NewGuid();

        var t1 = BuildTransaction(_studentId, new DateOnly(2026, 1, 5), debit: 30m, credit: 0, TransactionType.CreditOffset);
        t1.InvoiceId = creditInvoiceId;

        var t2 = BuildTransaction(_studentId, new DateOnly(2026, 1, 10), debit: 20m, credit: 0, TransactionType.CreditOffset);
        t2.InvoiceId = creditInvoiceId;

        // Different invoice — should not be included
        var t3 = BuildTransaction(_studentId, new DateOnly(2026, 1, 15), debit: 100m, credit: 0, TransactionType.CreditOffset);
        t3.InvoiceId = otherInvoiceId;

        Context.StudentTransactions.AddRange(t1, t2, t3);
        await Context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAppliedCreditAmountAsync(creditInvoiceId);

        // Assert
        Assert.Equal(50m, result);
    }

    [Fact]
    public async Task GetAppliedCreditAmountAsync_IgnoresNonCreditOffsetTypes()
    {
        // Arrange
        var creditInvoiceId = Guid.NewGuid();

        var creditOffset = BuildTransaction(_studentId, new DateOnly(2026, 1, 5), debit: 40m, credit: 0, TransactionType.CreditOffset);
        creditOffset.InvoiceId = creditInvoiceId;

        // Payment type with same invoiceId — should NOT be summed
        var payment = BuildTransaction(_studentId, new DateOnly(2026, 1, 6), debit: 99m, credit: 0, TransactionType.Payment);
        payment.InvoiceId = creditInvoiceId;

        Context.StudentTransactions.AddRange(creditOffset, payment);
        await Context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAppliedCreditAmountAsync(creditInvoiceId);

        // Assert — only the CreditOffset transaction counts
        Assert.Equal(40m, result);
    }

    #endregion

    #region Helpers

    private StudentTransaction BuildTransaction(
        Guid studentId,
        DateOnly date,
        decimal debit,
        decimal credit,
        TransactionType type = TransactionType.InvoiceCharge)
    {
        return new StudentTransaction
        {
            Id = Guid.NewGuid(),
            StudentId = studentId,
            TransactionDate = date,
            Type = type,
            Description = "Test transaction",
            ReferenceNumber = "REF-001",
            Debit = debit,
            Credit = credit,
            CreatedById = _userId
        };
    }

    #endregion
}
