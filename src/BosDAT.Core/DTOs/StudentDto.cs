using BosDAT.Core.Entities;

namespace BosDAT.Core.DTOs;

public record StudentDto
{
    public Guid Id { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public string? Prefix { get; init; }
    public string FullName { get; init; } = string.Empty;
    public required string Email { get; init; }
    public string? Phone { get; init; }
    public string? PhoneAlt { get; init; }
    public string? Address { get; init; }
    public string? PostalCode { get; init; }
    public string? City { get; init; }
    public DateOnly? DateOfBirth { get; init; }
    public Gender? Gender { get; init; }
    public StudentStatus Status { get; init; }
    public DateTime? EnrolledAt { get; init; }
    public string? BillingAddress { get; init; }
    public string? BillingPostalCode { get; init; }
    public string? BillingCity { get; init; }
    public bool AutoDebit { get; init; }
    public string? Notes { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public record CreateStudentDto
{
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public string? Prefix { get; init; }
    public required string Email { get; init; }
    public string? Phone { get; init; }
    public string? PhoneAlt { get; init; }
    public string? Address { get; init; }
    public string? PostalCode { get; init; }
    public string? City { get; init; }
    public DateOnly? DateOfBirth { get; init; }
    public Gender? Gender { get; init; }
    public StudentStatus Status { get; init; } = StudentStatus.Active;
    public string? BillingAddress { get; init; }
    public string? BillingPostalCode { get; init; }
    public string? BillingCity { get; init; }
    public bool AutoDebit { get; init; }
    public string? Notes { get; init; }
}

public record UpdateStudentDto
{
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public string? Prefix { get; init; }
    public required string Email { get; init; }
    public string? Phone { get; init; }
    public string? PhoneAlt { get; init; }
    public string? Address { get; init; }
    public string? PostalCode { get; init; }
    public string? City { get; init; }
    public DateOnly? DateOfBirth { get; init; }
    public Gender? Gender { get; init; }
    public StudentStatus Status { get; init; }
    public string? BillingAddress { get; init; }
    public string? BillingPostalCode { get; init; }
    public string? BillingCity { get; init; }
    public bool AutoDebit { get; init; }
    public string? Notes { get; init; }
}

public record StudentListDto
{
    public Guid Id { get; init; }
    public string FullName { get; init; } = string.Empty;
    public required string Email { get; init; }
    public string? Phone { get; init; }
    public StudentStatus Status { get; init; }
    public DateTime? EnrolledAt { get; init; }
}
