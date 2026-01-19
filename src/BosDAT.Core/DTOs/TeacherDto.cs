using BosDAT.Core.Entities;

namespace BosDAT.Core.DTOs;

public record TeacherDto
{
    public Guid Id { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public string? Prefix { get; init; }
    public string FullName { get; init; } = string.Empty;
    public required string Email { get; init; }
    public string? Phone { get; init; }
    public string? Address { get; init; }
    public string? PostalCode { get; init; }
    public string? City { get; init; }
    public decimal HourlyRate { get; init; }
    public bool IsActive { get; init; }
    public TeacherRole Role { get; init; }
    public string? Notes { get; init; }
    public List<InstrumentDto> Instruments { get; init; } = new();
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public record CreateTeacherDto
{
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public string? Prefix { get; init; }
    public required string Email { get; init; }
    public string? Phone { get; init; }
    public string? Address { get; init; }
    public string? PostalCode { get; init; }
    public string? City { get; init; }
    public decimal HourlyRate { get; init; }
    public TeacherRole Role { get; init; } = TeacherRole.Teacher;
    public string? Notes { get; init; }
    public List<int> InstrumentIds { get; init; } = new();
}

public record UpdateTeacherDto
{
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public string? Prefix { get; init; }
    public required string Email { get; init; }
    public string? Phone { get; init; }
    public string? Address { get; init; }
    public string? PostalCode { get; init; }
    public string? City { get; init; }
    public decimal HourlyRate { get; init; }
    public bool IsActive { get; init; }
    public TeacherRole Role { get; init; }
    public string? Notes { get; init; }
    public List<int> InstrumentIds { get; init; } = new();
}

public record TeacherListDto
{
    public Guid Id { get; init; }
    public string FullName { get; init; } = string.Empty;
    public required string Email { get; init; }
    public string? Phone { get; init; }
    public bool IsActive { get; init; }
    public TeacherRole Role { get; init; }
    public List<string> Instruments { get; init; } = new();
}
