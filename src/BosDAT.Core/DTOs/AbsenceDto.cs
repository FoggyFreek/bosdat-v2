using BosDAT.Core.Enums;

namespace BosDAT.Core.DTOs;

public record AbsenceDto
{
    public Guid Id { get; init; }
    public Guid? StudentId { get; init; }
    public Guid? TeacherId { get; init; }
    public string? PersonName { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
    public AbsenceReason Reason { get; init; }
    public string? Notes { get; init; }
    public bool InvoiceLesson { get; init; }
    public int AffectedLessonsCount { get; init; }
}

public record CreateAbsenceDto
{
    public Guid? StudentId { get; init; }
    public Guid? TeacherId { get; init; }
    public required DateOnly StartDate { get; init; }
    public required DateOnly EndDate { get; init; }
    public required AbsenceReason Reason { get; init; }
    public string? Notes { get; init; }
    public bool InvoiceLesson { get; init; }
}

public record UpdateAbsenceDto
{
    public required DateOnly StartDate { get; init; }
    public required DateOnly EndDate { get; init; }
    public required AbsenceReason Reason { get; init; }
    public string? Notes { get; init; }
    public bool InvoiceLesson { get; init; }
}
