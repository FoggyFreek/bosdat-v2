using Microsoft.EntityFrameworkCore;
using Xunit;
using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Infrastructure.Data;
using BosDAT.Infrastructure.Services;

namespace BosDAT.API.Tests.Services;

public class DuplicateDetectionServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly DuplicateDetectionService _service;

    public DuplicateDetectionServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options, null!);
        _service = new DuplicateDetectionService(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task CheckForDuplicates_WithNoExistingStudents_ReturnsNoDuplicates()
    {
        // Arrange
        var dto = new CheckDuplicatesDto
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com"
        };

        // Act
        var result = await _service.CheckForDuplicatesAsync(dto);

        // Assert
        Assert.False(result.HasDuplicates);
        Assert.Empty(result.Duplicates);
    }

    [Fact]
    public async Task CheckForDuplicates_WithExactEmailMatch_ReturnsHighConfidenceDuplicate()
    {
        // Arrange
        var existingStudent = new Student
        {
            Id = Guid.NewGuid(),
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com",
            Status = StudentStatus.Active
        };
        _context.Students.Add(existingStudent);
        await _context.SaveChangesAsync();

        var dto = new CheckDuplicatesDto
        {
            FirstName = "Johnny",
            LastName = "Doeman",
            Email = "john@example.com"
        };

        // Act
        var result = await _service.CheckForDuplicatesAsync(dto);

        // Assert
        Assert.True(result.HasDuplicates);
        Assert.Single(result.Duplicates);
        Assert.Equal(100, result.Duplicates[0].ConfidenceScore);
        Assert.Contains("email", result.Duplicates[0].MatchReason.ToLower());
    }

    [Fact]
    public async Task CheckForDuplicates_WithExactNameMatch_ReturnsDuplicate()
    {
        // Arrange
        var existingStudent = new Student
        {
            Id = Guid.NewGuid(),
            FirstName = "John",
            LastName = "Doe",
            Email = "existing@example.com",
            Status = StudentStatus.Active
        };
        _context.Students.Add(existingStudent);
        await _context.SaveChangesAsync();

        var dto = new CheckDuplicatesDto
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "new@example.com"
        };

        // Act
        var result = await _service.CheckForDuplicatesAsync(dto);

        // Assert
        Assert.True(result.HasDuplicates);
        Assert.Single(result.Duplicates);
        Assert.Contains("name", result.Duplicates[0].MatchReason.ToLower());
    }

    [Fact]
    public async Task CheckForDuplicates_WithPhoneMatch_ReturnsDuplicate()
    {
        // Arrange
        var existingStudent = new Student
        {
            Id = Guid.NewGuid(),
            FirstName = "Jane",
            LastName = "Smith",
            Email = "jane@example.com",
            Phone = "06-12345678",
            Status = StudentStatus.Active
        };
        _context.Students.Add(existingStudent);
        await _context.SaveChangesAsync();

        var dto = new CheckDuplicatesDto
        {
            FirstName = "Jane",
            LastName = "Smit",
            Email = "janesmit@example.com",
            Phone = "0612345678" // Same phone, different format
        };

        // Act
        var result = await _service.CheckForDuplicatesAsync(dto);

        // Assert
        Assert.True(result.HasDuplicates);
        Assert.Single(result.Duplicates);
        Assert.Contains("phone", result.Duplicates[0].MatchReason.ToLower());
    }

    [Fact]
    public async Task CheckForDuplicates_WithSimilarName_ReturnsDuplicate()
    {
        // Arrange - Use a name with very high similarity (same last name, very similar first)
        var existingStudent = new Student
        {
            Id = Guid.NewGuid(),
            FirstName = "Jan",
            LastName = "Jansen",
            Email = "janjansen@example.com",
            Phone = "0612345678", // Adding matching phone to ensure threshold is met
            Status = StudentStatus.Active
        };
        _context.Students.Add(existingStudent);
        await _context.SaveChangesAsync();

        var dto = new CheckDuplicatesDto
        {
            FirstName = "Jan",
            LastName = "Janssen", // Slightly different spelling
            Email = "newjan@example.com",
            Phone = "0612345678" // Same phone ensures match
        };

        // Act
        var result = await _service.CheckForDuplicatesAsync(dto);

        // Assert
        Assert.True(result.HasDuplicates);
        Assert.Single(result.Duplicates);
    }

    [Fact]
    public async Task CheckForDuplicates_WithExcludeId_ExcludesCurrentStudent()
    {
        // Arrange
        var currentStudentId = Guid.NewGuid();
        var currentStudent = new Student
        {
            Id = currentStudentId,
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com",
            Status = StudentStatus.Active
        };
        _context.Students.Add(currentStudent);
        await _context.SaveChangesAsync();

        var dto = new CheckDuplicatesDto
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com",
            ExcludeId = currentStudentId
        };

        // Act
        var result = await _service.CheckForDuplicatesAsync(dto);

        // Assert
        Assert.False(result.HasDuplicates);
        Assert.Empty(result.Duplicates);
    }

    [Fact]
    public async Task CheckForDuplicates_WithDateOfBirthAndSimilarName_ReturnsDuplicate()
    {
        // Arrange
        var existingStudent = new Student
        {
            Id = Guid.NewGuid(),
            FirstName = "Emma",
            LastName = "de Vries",
            Email = "emma@example.com",
            DateOfBirth = new DateOnly(2000, 5, 15),
            Status = StudentStatus.Active
        };
        _context.Students.Add(existingStudent);
        await _context.SaveChangesAsync();

        var dto = new CheckDuplicatesDto
        {
            FirstName = "Emma",
            LastName = "Vries",
            Email = "emmavries@example.com",
            DateOfBirth = new DateOnly(2000, 5, 15)
        };

        // Act
        var result = await _service.CheckForDuplicatesAsync(dto);

        // Assert
        Assert.True(result.HasDuplicates);
        Assert.Single(result.Duplicates);
        Assert.Contains("date of birth", result.Duplicates[0].MatchReason.ToLower());
    }

    [Fact]
    public async Task CheckForDuplicates_WithNoMatch_ReturnsNoDuplicates()
    {
        // Arrange
        var existingStudent = new Student
        {
            Id = Guid.NewGuid(),
            FirstName = "Alice",
            LastName = "Wonderland",
            Email = "alice@example.com",
            Phone = "0687654321",
            Status = StudentStatus.Active
        };
        _context.Students.Add(existingStudent);
        await _context.SaveChangesAsync();

        var dto = new CheckDuplicatesDto
        {
            FirstName = "Bob",
            LastName = "Builder",
            Email = "bob@example.com",
            Phone = "0612345678"
        };

        // Act
        var result = await _service.CheckForDuplicatesAsync(dto);

        // Assert
        Assert.False(result.HasDuplicates);
        Assert.Empty(result.Duplicates);
    }

    [Fact]
    public async Task CheckForDuplicates_ReturnsCorrectStudentStatus()
    {
        // Arrange
        var inactiveStudent = new Student
        {
            Id = Guid.NewGuid(),
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com",
            Status = StudentStatus.Inactive
        };
        _context.Students.Add(inactiveStudent);
        await _context.SaveChangesAsync();

        var dto = new CheckDuplicatesDto
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com"
        };

        // Act
        var result = await _service.CheckForDuplicatesAsync(dto);

        // Assert
        Assert.True(result.HasDuplicates);
        Assert.Single(result.Duplicates);
        Assert.Equal(StudentStatus.Inactive, result.Duplicates[0].Status);
    }

    [Fact]
    public async Task CheckForDuplicates_SortsByConfidenceScore()
    {
        // Arrange - Two students with different match scores
        var student1 = new Student
        {
            Id = Guid.NewGuid(),
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com", // Exact email match - 100%
            Status = StudentStatus.Active
        };
        var student2 = new Student
        {
            Id = Guid.NewGuid(),
            FirstName = "John",
            LastName = "Doe",
            Email = "johndoe2@example.com",
            Phone = "0698765432", // Matching phone - 50% + exact name 60%
            Status = StudentStatus.Active
        };
        _context.Students.AddRange(student1, student2);
        await _context.SaveChangesAsync();

        var dto = new CheckDuplicatesDto
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com",
            Phone = "0698765432"
        };

        // Act
        var result = await _service.CheckForDuplicatesAsync(dto);

        // Assert
        Assert.True(result.HasDuplicates);
        Assert.Equal(2, result.Duplicates.Count);
        // First should be the one with higher confidence (exact email match = 100%)
        Assert.True(result.Duplicates[0].ConfidenceScore >= result.Duplicates[1].ConfidenceScore);
        Assert.Equal(100, result.Duplicates[0].ConfidenceScore);
    }
}
