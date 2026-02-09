using BosDAT.Core.Entities;
using BosDAT.Core.Enums;

namespace BosDAT.Core.DTOs;

public record EnrollmentDto
{
    public Guid Id { get; init; }
    public Guid StudentId { get; init; }
    public string StudentName { get; init; } = string.Empty;
    public Guid CourseId { get; init; }
    public DateTime EnrolledAt { get; init; }
    public decimal DiscountPercent { get; init; }
    public DiscountType DiscountType { get; init; }
    public EnrollmentStatus Status { get; init; }
    public InvoicingPreference InvoicingPreference { get; init; }
    public string? Notes { get; init; }
}

public record CreateEnrollmentDto
{
    public Guid StudentId { get; init; }
    public Guid CourseId { get; init; }
    public decimal DiscountPercent { get; init; }
    public DiscountType DiscountType { get; init; }
    public InvoicingPreference InvoicingPreference { get; init; } = InvoicingPreference.Monthly;
    public string? Notes { get; init; }
}

public record UpdateEnrollmentDto
{
    public decimal DiscountPercent { get; init; }
    public EnrollmentStatus Status { get; init; }
    public InvoicingPreference InvoicingPreference { get; init; }
    public string? Notes { get; init; }
}

public record EnrollmentDetailDto
{
    public Guid Id { get; init; }
    public Guid StudentId { get; init; }
    public string StudentName { get; init; } = string.Empty;
    public Guid CourseId { get; init; }
    public string CourseName { get; init; } = string.Empty;
    public string InstrumentName { get; init; } = string.Empty;
    public string TeacherName { get; init; } = string.Empty;
    public string? RoomName { get; init; }
    public DayOfWeek DayOfWeek { get; init; }
    public TimeOnly StartTime { get; init; }
    public TimeOnly EndTime { get; init; }
    public DateTime EnrolledAt { get; init; }
    public decimal DiscountPercent { get; init; }
    public EnrollmentStatus Status { get; init; }
    public InvoicingPreference InvoicingPreference { get; init; }
    public string? Notes { get; init; }
}

public record StudentEnrollmentDto
{
    public Guid Id { get; init; }
    public Guid CourseId { get; init; }
    public string InstrumentName { get; init; } = string.Empty;
    public string CourseTypeName { get; init; } = string.Empty;
    public string TeacherName { get; init; } = string.Empty;
    public string? RoomName { get; init; }
    public DayOfWeek DayOfWeek { get; init; }
    public TimeOnly StartTime { get; init; }
    public TimeOnly EndTime { get; init; }
    public DateTime EnrolledAt { get; init; }
    public decimal DiscountPercent { get; init; }
    public EnrollmentStatus Status { get; init; }
    public InvoicingPreference InvoicingPreference { get; init; }
}
