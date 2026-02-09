using BosDAT.Core.Entities;
using BosDAT.Core.Enums;
using BosDAT.Infrastructure.Repositories;
using FluentAssertions;

namespace BosDAT.Infrastructure.Tests.Repositories;

public class StudentRepositoryTests : RepositoryTestBase
{
    private readonly StudentRepository _repository;

    public StudentRepositoryTests()
    {
        _repository = new StudentRepository(Context);
        SeedTestData();
    }

    [Fact]
    public async Task GetByEmailAsync_ShouldReturnStudentWhenEmailExists()
    {
        // Arrange
        var expectedEmail = "alice.johnson@example.com";

        // Act
        var result = await _repository.GetByEmailAsync(expectedEmail);

        // Assert
        result.Should().NotBeNull();
        result!.Email.Should().BeEquivalentTo(expectedEmail);
    }

    [Fact]
    public async Task GetByEmailAsync_ShouldBeCaseInsensitive()
    {
        // Arrange
        var expectedEmail = "ALICE.JOHNSON@EXAMPLE.COM";

        // Act
        var result = await _repository.GetByEmailAsync(expectedEmail);

        // Assert
        result.Should().NotBeNull();
        result!.Email.Should().BeEquivalentTo("alice.johnson@example.com");
    }

    [Fact]
    public async Task GetByEmailAsync_ShouldReturnNullWhenEmailDoesNotExist()
    {
        // Arrange
        var nonexistentEmail = "nonexistent@example.com";

        // Act
        var result = await _repository.GetByEmailAsync(nonexistentEmail);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetWithEnrollmentsAsync_ShouldReturnStudentWithEnrollments()
    {
        // Arrange
        var student = Context.Students.First(s => s.Email == "alice.johnson@example.com");

        // Act
        var result = await _repository.GetWithEnrollmentsAsync(student.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Enrollments.Should().NotBeEmpty();
        result.Enrollments.First().Course.Should().NotBeNull();
        result.Enrollments.First().Course.CourseType.Should().NotBeNull();
        result.Enrollments.First().Course.CourseType.Instrument.Should().NotBeNull();
        result.Enrollments.First().Course.Teacher.Should().NotBeNull();
    }

    [Fact]
    public async Task GetWithEnrollmentsAsync_ShouldReturnNullForNonexistentStudent()
    {
        // Arrange
        var nonexistentId = Guid.NewGuid();

        // Act
        var result = await _repository.GetWithEnrollmentsAsync(nonexistentId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetWithInvoicesAsync_ShouldReturnStudentWithInvoices()
    {
        // Arrange
        var student = Context.Students.First();

        // Create invoice for student
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            StudentId = student.Id,
            InvoiceNumber = "202401",
            IssueDate = new DateOnly(2024, 1, 15),
            DueDate = new DateOnly(2024, 1, 29),
            Status = InvoiceStatus.Draft,
            Subtotal = 100.00m,
            VatAmount = 21.00m,
            Total = 121.00m,
            PeriodStart = new DateOnly(2024, 1, 1),
            PeriodEnd = new DateOnly(2024, 1, 31)
        };

        var invoiceLine = new InvoiceLine
        {
            InvoiceId = invoice.Id,
            Description = "Piano lesson",
            Quantity = 4,
            UnitPrice = 25.00m,
            VatRate = 21.00m,
            LineTotal = 100.00m
        };

        Context.Invoices.Add(invoice);
        Context.InvoiceLines.Add(invoiceLine);
        await Context.SaveChangesAsync();

        // Act
        var result = await _repository.GetWithInvoicesAsync(student.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Invoices.Should().NotBeEmpty();
        result.Invoices.First().Lines.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetActiveStudentsAsync_ShouldReturnOnlyActiveStudents()
    {
        // Arrange
        var inactiveStudent = new Student
        {
            Id = Guid.NewGuid(),
            FirstName = "Inactive",
            LastName = "Student",
            Email = "inactive@example.com",
            Phone = "0600000000",
            DateOfBirth = new DateOnly(2010, 1, 1),
            Status = StudentStatus.Inactive
        };
        Context.Students.Add(inactiveStudent);
        await Context.SaveChangesAsync();

        // Act
        var result = await _repository.GetActiveStudentsAsync();

        // Assert
        result.Should().NotBeEmpty();
        result.Should().AllSatisfy(s => s.Status.Should().Be(StudentStatus.Active));
        result.Should().NotContain(s => s.Id == inactiveStudent.Id);
    }

    [Fact]
    public async Task GetActiveStudentsAsync_ShouldReturnStudentsOrderedByLastNameThenFirstName()
    {
        // Arrange
        var student1 = new Student
        {
            Id = Guid.NewGuid(),
            FirstName = "Zack",
            LastName = "Anderson",
            Email = "zack@example.com",
            Phone = "0600000001",
            DateOfBirth = new DateOnly(2010, 1, 1),
            Status = StudentStatus.Active
        };

        var student2 = new Student
        {
            Id = Guid.NewGuid(),
            FirstName = "Aaron",
            LastName = "Anderson",
            Email = "aaron@example.com",
            Phone = "0600000002",
            DateOfBirth = new DateOnly(2010, 1, 1),
            Status = StudentStatus.Active
        };

        Context.Students.AddRange(student1, student2);
        await Context.SaveChangesAsync();

        // Act
        var result = await _repository.GetActiveStudentsAsync();

        // Assert
        var andersons = result.Where(s => s.LastName == "Anderson").ToList();
        andersons.Should().HaveCountGreaterThanOrEqualTo(2);
        andersons.Should().BeInAscendingOrder(s => s.FirstName);
    }

    [Fact]
    public async Task SearchAsync_ShouldFindStudentByFirstName()
    {
        // Arrange
        var searchTerm = "alice";

        // Act
        var result = await _repository.SearchAsync(searchTerm);

        // Assert
        result.Should().NotBeEmpty();
        result.Should().Contain(s => s.FirstName.ToLower().Contains(searchTerm));
    }

    [Fact]
    public async Task SearchAsync_ShouldFindStudentByLastName()
    {
        // Arrange
        var searchTerm = "johnson";

        // Act
        var result = await _repository.SearchAsync(searchTerm);

        // Assert
        result.Should().NotBeEmpty();
        result.Should().Contain(s => s.LastName.ToLower().Contains(searchTerm));
    }

    [Fact]
    public async Task SearchAsync_ShouldFindStudentByEmail()
    {
        // Arrange
        var searchTerm = "alice.johnson";

        // Act
        var result = await _repository.SearchAsync(searchTerm);

        // Assert
        result.Should().NotBeEmpty();
        result.Should().Contain(s => s.Email.ToLower().Contains(searchTerm));
    }

    [Fact]
    public async Task SearchAsync_ShouldBeCaseInsensitive()
    {
        // Arrange
        var searchTerm = "ALICE";

        // Act
        var result = await _repository.SearchAsync(searchTerm);

        // Assert
        result.Should().NotBeEmpty();
    }

    [Fact]
    public async Task SearchAsync_ShouldReturnEmptyForNoMatches()
    {
        // Arrange
        var searchTerm = "nonexistent";

        // Act
        var result = await _repository.SearchAsync(searchTerm);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task HasActiveEnrollmentsAsync_ShouldReturnTrueWhenStudentHasActiveEnrollment()
    {
        // Arrange
        var student = Context.Students.First(s => s.Email == "alice.johnson@example.com");

        // Act
        var result = await _repository.HasActiveEnrollmentsAsync(student.Id);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasActiveEnrollmentsAsync_ShouldReturnTrueWhenStudentHasTrailEnrollment()
    {
        // Arrange
        var student = Context.Students.First(s => s.Email == "bob.williams@example.com");

        // Act
        var result = await _repository.HasActiveEnrollmentsAsync(student.Id);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasActiveEnrollmentsAsync_ShouldReturnFalseWhenStudentHasNoEnrollments()
    {
        // Arrange
        var studentWithNoEnrollments = new Student
        {
            Id = Guid.NewGuid(),
            FirstName = "No",
            LastName = "Enrollments",
            Email = "noenrollments@example.com",
            Phone = "0600000000",
            DateOfBirth = new DateOnly(2010, 1, 1),
            Status = StudentStatus.Active
        };
        Context.Students.Add(studentWithNoEnrollments);
        await Context.SaveChangesAsync();

        // Act
        var result = await _repository.HasActiveEnrollmentsAsync(studentWithNoEnrollments.Id);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasActiveEnrollmentsAsync_ShouldReturnFalseWhenEnrollmentsAreCompleted()
    {
        // Arrange
        var student = new Student
        {
            Id = Guid.NewGuid(),
            FirstName = "Completed",
            LastName = "Only",
            Email = "completed@example.com",
            Phone = "0600000000",
            DateOfBirth = new DateOnly(2010, 1, 1),
            Status = StudentStatus.Active
        };

        var course = Context.Courses.First();

        var completedEnrollment = new Enrollment
        {
            Id = Guid.NewGuid(),
            StudentId = student.Id,
            CourseId = course.Id,
            EnrolledAt = new DateTime(2024, 1, 1),
            Status = EnrollmentStatus.Completed
        };

        Context.Students.Add(student);
        Context.Enrollments.Add(completedEnrollment);
        await Context.SaveChangesAsync();

        // Act
        var result = await _repository.HasActiveEnrollmentsAsync(student.Id);

        // Assert
        result.Should().BeFalse();
    }
}
