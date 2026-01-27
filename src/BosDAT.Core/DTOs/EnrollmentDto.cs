using BosDAT.Core.Entities;

namespace BosDAT.Core.DTOs;

public record EnrollmentDto
{
    public Guid Id { get; init; }
    public Guid StudentId { get; init; }
    public string StudentName { get; init; } = string.Empty;
    public Guid CourseId { get; init; }
    public DateTime EnrolledAt { get; init; }
    public decimal DiscountPercent { get; init; }
    public EnrollmentStatus Status { get; init; }
    public string? Notes { get; init; }
}

public record CreateEnrollmentDto
{
    public Guid StudentId { get; init; }
    public Guid CourseId { get; init; }
    public decimal DiscountPercent { get; init; }
    public string? Notes { get; init; }
}

public record UpdateEnrollmentDto
{
    public decimal DiscountPercent { get; init; }
    public EnrollmentStatus Status { get; init; }
    public string? Notes { get; init; }
}
