using BosDAT.Core.Entities;
using BosDAT.Core.Enums;
using BosDAT.Infrastructure.Repositories;

namespace BosDAT.Infrastructure.Tests.Repositories;

public class InvoiceRepositoryTests : RepositoryTestBase
{
    private readonly InvoiceRepository _repository;

    public InvoiceRepositoryTests()
    {
        _repository = new InvoiceRepository(Context);
        SeedTestData();
        SeedInvoices();
    }

    private void SeedInvoices()
    {
        var student = Context.Students.First();
        var enrollment = Context.Enrollments.First();

        var invoice1 = new Invoice
        {
            Id = Guid.NewGuid(),
            StudentId = student.Id,
            EnrollmentId = enrollment.Id,
            InvoiceNumber = "202401",
            IssueDate = new DateOnly(2024, 1, 15),
            DueDate = new DateOnly(2024, 1, 29),
            Status = InvoiceStatus.Sent,
            Subtotal = 100.00m,
            VatAmount = 21.00m,
            Total = 121.00m,
            PeriodStart = new DateOnly(2024, 1, 1),
            PeriodEnd = new DateOnly(2024, 1, 31)
        };

        var invoice2 = new Invoice
        {
            Id = Guid.NewGuid(),
            StudentId = student.Id,
            EnrollmentId = enrollment.Id,
            InvoiceNumber = "202402",
            IssueDate = new DateOnly(2024, 2, 15),
            DueDate = new DateOnly(2024, 2, 29),
            Status = InvoiceStatus.Draft,
            Subtotal = 100.00m,
            VatAmount = 21.00m,
            Total = 121.00m,
            PeriodStart = new DateOnly(2024, 2, 1),
            PeriodEnd = new DateOnly(2024, 2, 28)
        };

        var invoice3 = new Invoice
        {
            Id = Guid.NewGuid(),
            StudentId = student.Id,
            EnrollmentId = enrollment.Id,
            InvoiceNumber = "202403",
            IssueDate = new DateOnly(2024, 1, 1),
            DueDate = new DateOnly(2024, 1, 10),
            Status = InvoiceStatus.Sent,
            Subtotal = 100.00m,
            VatAmount = 21.00m,
            Total = 121.00m,
            PeriodStart = new DateOnly(2024, 1, 1),
            PeriodEnd = new DateOnly(2024, 1, 31)
        };

        Context.Invoices.AddRange(invoice1, invoice2, invoice3);
        Context.SaveChanges();
    }

    [Fact]
    public async Task GetByInvoiceNumberAsync_ShouldReturnInvoiceWhenExists()
    {
        // Arrange
        var invoiceNumber = "202401";

        // Act
        var result = await _repository.GetByInvoiceNumberAsync(invoiceNumber);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(invoiceNumber, result!.InvoiceNumber);
        Assert.NotNull(result.Student);
        Assert.NotNull(result.Lines);
        Assert.NotNull(result.Payments);
    }

    [Fact]
    public async Task GetByInvoiceNumberAsync_ShouldReturnNullWhenNotExists()
    {
        // Arrange
        var nonexistentNumber = "999999";

        // Act
        var result = await _repository.GetByInvoiceNumberAsync(nonexistentNumber);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetWithLinesAsync_ShouldReturnInvoiceWithAllRelatedData()
    {
        // Arrange
        var invoice = Context.Invoices.First();

        var line = new InvoiceLine
        {
            InvoiceId = invoice.Id,
            Description = "Piano lesson",
            Quantity = 4,
            UnitPrice = 25.00m,
            VatRate = 21.00m,
            LineTotal = 100.00m
        };
        Context.InvoiceLines.Add(line);
        await Context.SaveChangesAsync();

        // Act
        var result = await _repository.GetWithLinesAsync(invoice.Id);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result!.Student);
        Assert.NotEmpty(result.Lines);
        Assert.NotNull(result.Payments);
    }

    [Fact]
    public async Task GetByStudentAsync_ShouldReturnInvoicesForStudent()
    {
        // Arrange
        var student = Context.Students.First();

        // Act
        var result = await _repository.GetByStudentAsync(student.Id);

        // Assert
        Assert.NotEmpty(result);
        Assert.All(result, i => Assert.Equal(student.Id, i.StudentId));
        Assert.NotNull(result[0].Lines);
        Assert.NotNull(result[0].Payments);
    }

    [Fact]
    public async Task GetByStudentAsync_ShouldReturnInvoicesOrderedByIssueDateDescending()
    {
        // Arrange
        var student = Context.Students.First();

        // Act
        var result = await _repository.GetByStudentAsync(student.Id);

        // Assert
        Assert.NotEmpty(result);
        var list = result.ToList();
        for (int i = 0; i < list.Count - 1; i++)
        {
            Assert.True(list[i].IssueDate >= list[i + 1].IssueDate);
        }
    }

    [Fact]
    public async Task GetByStatusAsync_ShouldReturnInvoicesWithSpecificStatus()
    {
        // Arrange
        var status = InvoiceStatus.Draft;

        // Act
        var result = await _repository.GetByStatusAsync(status);

        // Assert
        Assert.NotEmpty(result);
        Assert.All(result, i => Assert.Equal(status, i.Status));
        Assert.NotNull(result[0].Student);
    }

    [Fact]
    public async Task GetByStatusAsync_ShouldReturnInvoicesOrderedByIssueDateDescending()
    {
        // Arrange
        var status = InvoiceStatus.Sent;

        // Act
        var result = await _repository.GetByStatusAsync(status);

        // Assert
        Assert.NotEmpty(result);
        var list = result.ToList();
        for (int i = 0; i < list.Count - 1; i++)
        {
            Assert.True(list[i].IssueDate >= list[i + 1].IssueDate);
        }
    }

    [Fact]
    public async Task GetOverdueInvoicesAsync_ShouldReturnOnlyOverdueInvoices()
    {
        // Arrange & Act
        var result = await _repository.GetOverdueInvoicesAsync();

        // Assert
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        Assert.NotEmpty(result);
        Assert.All(result, i =>
        {
            Assert.Equal(InvoiceStatus.Sent, i.Status);
            Assert.True(i.DueDate < today);
        });
        Assert.NotNull(result[0].Student);
    }

    [Fact]
    public async Task GetOverdueInvoicesAsync_ShouldReturnInvoicesOrderedByDueDateAscending()
    {
        // Arrange & Act
        var result = await _repository.GetOverdueInvoicesAsync();

        // Assert
        Assert.NotEmpty(result);
        var list = result.ToList();
        for (int i = 0; i < list.Count - 1; i++)
        {
            Assert.True(list[i].DueDate <= list[i + 1].DueDate);
        }
    }

    [Fact]
    public async Task GetOverdueInvoicesAsync_ShouldNotReturnDraftInvoices()
    {
        // Arrange & Act
        var result = await _repository.GetOverdueInvoicesAsync();

        // Assert
        Assert.DoesNotContain(result, i => i.Status == InvoiceStatus.Draft);
    }

    [Fact]
    public async Task GenerateInvoiceNumberAsync_ShouldGenerateCorrectFormatForFirstInvoice()
    {
        // Arrange
        Context.Invoices.RemoveRange(Context.Invoices);
        await Context.SaveChangesAsync();

        // Act
        var result = await _repository.GenerateInvoiceNumberAsync();

        // Assert
        var currentYear = DateTime.UtcNow.Year;
        Assert.Equal($"{currentYear}01", result);
        Assert.Equal(6, result.Length); // YYYYNN
    }

    [Fact]
    public async Task GenerateInvoiceNumberAsync_ShouldIncrementSequenceNumber()
    {
        // Arrange
        var currentYear = DateTime.UtcNow.Year;
        var lastNumber = "10";

        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            StudentId = Context.Students.First().Id,
            InvoiceNumber = $"{currentYear}{lastNumber}",
            IssueDate = DateOnly.FromDateTime(DateTime.UtcNow),
            DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14)),
            Status = InvoiceStatus.Draft,
            Subtotal = 100.00m,
            VatAmount = 21.00m,
            Total = 121.00m,
            PeriodStart = new DateOnly(currentYear, 1, 1),
            PeriodEnd = new DateOnly(currentYear, 1, 31)
        };

        Context.Invoices.Add(invoice);
        await Context.SaveChangesAsync();

        // Act
        var result = await _repository.GenerateInvoiceNumberAsync();

        // Assert
        Assert.Equal($"{currentYear}11", result);
    }

    [Fact]
    public async Task GetByEnrollmentAsync_ShouldReturnInvoicesForSpecificEnrollment()
    {
        // Arrange
        var enrollment = Context.Enrollments.First();

        // Act
        var result = await _repository.GetByEnrollmentAsync(enrollment.Id);

        // Assert
        Assert.NotEmpty(result);
        Assert.All(result, i => Assert.Equal(enrollment.Id, i.EnrollmentId));
        Assert.NotNull(result[0].Lines);
        Assert.NotNull(result[0].Payments);
    }

    [Fact]
    public async Task GetByEnrollmentAsync_ShouldReturnInvoicesOrderedByIssueDateDescending()
    {
        // Arrange
        var enrollment = Context.Enrollments.First();

        // Act
        var result = await _repository.GetByEnrollmentAsync(enrollment.Id);

        // Assert
        Assert.NotEmpty(result);
        var list = result.ToList();
        for (int i = 0; i < list.Count - 1; i++)
        {
            Assert.True(list[i].IssueDate >= list[i + 1].IssueDate);
        }
    }

    [Fact]
    public async Task GetByPeriodAsync_ShouldReturnInvoiceForSpecificPeriod()
    {
        // Arrange
        var student = Context.Students.First();
        var enrollment = Context.Enrollments.First();
        var periodStart = new DateOnly(2024, 1, 1);
        var periodEnd = new DateOnly(2024, 1, 31);

        // Act
        var result = await _repository.GetByPeriodAsync(student.Id, enrollment.Id, periodStart, periodEnd);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(student.Id, result!.StudentId);
        Assert.Equal(enrollment.Id, result.EnrollmentId);
        Assert.Equal(periodStart, result.PeriodStart);
        Assert.Equal(periodEnd, result.PeriodEnd);
        Assert.NotNull(result.Lines);
        Assert.NotNull(result.Payments);
    }

    [Fact]
    public async Task GetByPeriodAsync_ShouldReturnNullWhenNoMatchingInvoice()
    {
        // Arrange
        var student = Context.Students.First();
        var enrollment = Context.Enrollments.First();
        var periodStart = new DateOnly(2025, 1, 1);
        var periodEnd = new DateOnly(2025, 1, 31);

        // Act
        var result = await _repository.GetByPeriodAsync(student.Id, enrollment.Id, periodStart, periodEnd);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetUnpaidInvoicesAsync_ShouldReturnUnpaidInvoices()
    {
        // Arrange
        var student = Context.Students.First();

        // Act
        var result = await _repository.GetUnpaidInvoicesAsync(student.Id);

        // Assert
        Assert.NotEmpty(result);
        Assert.All(result, i =>
        {
            Assert.Equal(student.Id, i.StudentId);
            Assert.Contains(i.Status, new[] {
                InvoiceStatus.Draft,
                InvoiceStatus.Sent,
                InvoiceStatus.Overdue
            });
        });
        Assert.NotNull(result[0].Lines);
        Assert.NotNull(result[0].Payments);
    }

    [Fact]
    public async Task GetUnpaidInvoicesAsync_ShouldNotReturnPaidInvoices()
    {
        // Arrange
        var student = Context.Students.First();

        var paidInvoice = new Invoice
        {
            Id = Guid.NewGuid(),
            StudentId = student.Id,
            InvoiceNumber = "202499",
            IssueDate = new DateOnly(2024, 1, 15),
            DueDate = new DateOnly(2024, 1, 29),
            Status = InvoiceStatus.Paid,
            Subtotal = 100.00m,
            VatAmount = 21.00m,
            Total = 121.00m,
            PeriodStart = new DateOnly(2024, 1, 1),
            PeriodEnd = new DateOnly(2024, 1, 31)
        };

        Context.Invoices.Add(paidInvoice);
        await Context.SaveChangesAsync();

        // Act
        var result = await _repository.GetUnpaidInvoicesAsync(student.Id);

        // Assert
        Assert.DoesNotContain(result, i => i.Id == paidInvoice.Id);
    }

    [Fact]
    public async Task GetUnpaidInvoicesAsync_ShouldReturnInvoicesOrderedByIssueDateAscending()
    {
        // Arrange
        var student = Context.Students.First();

        // Act
        var result = await _repository.GetUnpaidInvoicesAsync(student.Id);

        // Assert
        Assert.NotEmpty(result);
        var list = result.ToList();
        for (int i = 0; i < list.Count - 1; i++)
        {
            Assert.True(list[i].IssueDate <= list[i + 1].IssueDate);
        }
    }

    [Fact]
    public async Task GetUnpaidInvoicesAsync_ShouldReturnEmptyForStudentWithNoUnpaidInvoices()
    {
        // Arrange
        var studentWithNoPaidInvoices = new Student
        {
            Id = Guid.NewGuid(),
            FirstName = "No",
            LastName = "Invoices",
            Email = "noinvoices@example.com",
            Phone = "0600000000",
            DateOfBirth = new DateOnly(2010, 1, 1),
            Status = StudentStatus.Active
        };
        Context.Students.Add(studentWithNoPaidInvoices);
        await Context.SaveChangesAsync();

        // Act
        var result = await _repository.GetUnpaidInvoicesAsync(studentWithNoPaidInvoices.Id);

        // Assert
        Assert.Empty(result);
    }
}
