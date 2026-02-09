using BosDAT.Core.Entities;
using BosDAT.Infrastructure.Repositories;
using FluentAssertions;

namespace BosDAT.Infrastructure.Tests.Repositories;

public class UnitOfWorkTests : RepositoryTestBase
{
    private readonly UnitOfWork _unitOfWork;

    public UnitOfWorkTests()
    {
        _unitOfWork = new UnitOfWork(Context);
    }

    [Fact]
    public void Students_ShouldReturnSameInstanceOnMultipleCalls()
    {
        // Act
        var students1 = _unitOfWork.Students;
        var students2 = _unitOfWork.Students;

        // Assert
        students1.Should().BeSameAs(students2);
    }

    [Fact]
    public void Teachers_ShouldReturnSameInstanceOnMultipleCalls()
    {
        // Act
        var teachers1 = _unitOfWork.Teachers;
        var teachers2 = _unitOfWork.Teachers;

        // Assert
        teachers1.Should().BeSameAs(teachers2);
    }

    [Fact]
    public void Courses_ShouldReturnSameInstanceOnMultipleCalls()
    {
        // Act
        var courses1 = _unitOfWork.Courses;
        var courses2 = _unitOfWork.Courses;

        // Assert
        courses1.Should().BeSameAs(courses2);
    }

    [Fact]
    public void Enrollments_ShouldReturnSameInstanceOnMultipleCalls()
    {
        // Act
        var enrollments1 = _unitOfWork.Enrollments;
        var enrollments2 = _unitOfWork.Enrollments;

        // Assert
        enrollments1.Should().BeSameAs(enrollments2);
    }

    [Fact]
    public void Lessons_ShouldReturnSameInstanceOnMultipleCalls()
    {
        // Act
        var lessons1 = _unitOfWork.Lessons;
        var lessons2 = _unitOfWork.Lessons;

        // Assert
        lessons1.Should().BeSameAs(lessons2);
    }

    [Fact]
    public void Invoices_ShouldReturnSameInstanceOnMultipleCalls()
    {
        // Act
        var invoices1 = _unitOfWork.Invoices;
        var invoices2 = _unitOfWork.Invoices;

        // Assert
        invoices1.Should().BeSameAs(invoices2);
    }

    [Fact]
    public void StudentLedgerEntries_ShouldReturnSameInstanceOnMultipleCalls()
    {
        // Act
        var ledger1 = _unitOfWork.StudentLedgerEntries;
        var ledger2 = _unitOfWork.StudentLedgerEntries;

        // Assert
        ledger1.Should().BeSameAs(ledger2);
    }

    [Fact]
    public void Repository_ShouldReturnSameInstanceOnMultipleCalls()
    {
        // Act
        var repo1 = _unitOfWork.Repository<Room>();
        var repo2 = _unitOfWork.Repository<Room>();

        // Assert
        repo1.Should().BeSameAs(repo2);
    }

    [Fact]
    public void Repository_ShouldReturnDifferentInstancesForDifferentTypes()
    {
        // Act
        var roomRepo = _unitOfWork.Repository<Room>();
        var instrumentRepo = _unitOfWork.Repository<Instrument>();

        // Assert
        roomRepo.Should().NotBeSameAs(instrumentRepo);
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldPersistChanges()
    {
        // Arrange
        SeedTestData();
        var student = new Student
        {
            Id = Guid.NewGuid(),
            FirstName = "Test",
            LastName = "Student",
            Email = "test@example.com",
            Phone = "0600000000",
            DateOfBirth = new DateOnly(2010, 1, 1),
            Status = StudentStatus.Active
        };

        // Act
        await _unitOfWork.Students.AddAsync(student);
        var result = await _unitOfWork.SaveChangesAsync();

        // Assert
        result.Should().Be(1);
        var saved = await _unitOfWork.Students.GetByIdAsync(student.Id);
        saved.Should().NotBeNull();
        saved!.Email.Should().Be("test@example.com");
    }

    [Fact(Skip = "InMemory database provider doesn't support transactions")]
    public async Task BeginTransactionAsync_ShouldStartTransaction()
    {
        // Act
        await _unitOfWork.BeginTransactionAsync();

        // Assert - no exception thrown
        await _unitOfWork.RollbackTransactionAsync();
    }

    [Fact(Skip = "InMemory database provider doesn't support transactions")]
    public async Task CommitTransactionAsync_ShouldCommitChanges()
    {
        // Arrange
        SeedTestData();
        var student = new Student
        {
            Id = Guid.NewGuid(),
            FirstName = "Transaction",
            LastName = "Test",
            Email = "transaction@example.com",
            Phone = "0600000000",
            DateOfBirth = new DateOnly(2010, 1, 1),
            Status = StudentStatus.Active
        };

        // Act
        await _unitOfWork.BeginTransactionAsync();
        await _unitOfWork.Students.AddAsync(student);
        await _unitOfWork.SaveChangesAsync();
        await _unitOfWork.CommitTransactionAsync();

        // Assert
        var saved = await _unitOfWork.Students.GetByIdAsync(student.Id);
        saved.Should().NotBeNull();
    }

    [Fact(Skip = "InMemory database provider doesn't support transactions")]
    public async Task RollbackTransactionAsync_ShouldRevertChanges()
    {
        // Arrange
        SeedTestData();
        var studentId = Guid.NewGuid();
        var student = new Student
        {
            Id = studentId,
            FirstName = "Rollback",
            LastName = "Test",
            Email = "rollback@example.com",
            Phone = "0600000000",
            DateOfBirth = new DateOnly(2010, 1, 1),
            Status = StudentStatus.Active
        };

        // Act
        await _unitOfWork.BeginTransactionAsync();
        await _unitOfWork.Students.AddAsync(student);
        await _unitOfWork.SaveChangesAsync();
        await _unitOfWork.RollbackTransactionAsync();

        // Assert
        var saved = await _unitOfWork.Students.GetByIdAsync(studentId);
        saved.Should().BeNull();
    }

    [Fact(Skip = "Dispose calls on shared context cause issues in test cleanup")]
    public void Dispose_ShouldCleanUpResources()
    {
        // Arrange
        var uow = new UnitOfWork(Context);

        // Act
        uow.Dispose();

        // Assert - no exception thrown
        uow.Dispose(); // Should be safe to call twice
    }
}
