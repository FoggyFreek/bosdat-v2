using BosDAT.Core.Entities;

namespace BosDAT.Core.DTOs;

public record LessonTypeDto
{
    public int Id { get; init; }
    public int InstrumentId { get; init; }
    public string InstrumentName { get; init; } = string.Empty;
    public required string Name { get; init; }
    public int DurationMinutes { get; init; }
    public LessonTypeCategory Type { get; init; }
    public decimal PriceAdult { get; init; }
    public decimal PriceChild { get; init; }
    public int MaxStudents { get; init; }
    public bool IsActive { get; init; }
}

public record CreateLessonTypeDto
{
    public int InstrumentId { get; init; }
    public required string Name { get; init; }
    public int DurationMinutes { get; init; } = 30;
    public LessonTypeCategory Type { get; init; } = LessonTypeCategory.Individual;
    public decimal PriceAdult { get; init; }
    public decimal PriceChild { get; init; }
    public int MaxStudents { get; init; } = 1;
}

public record UpdateLessonTypeDto
{
    public int InstrumentId { get; init; }
    public required string Name { get; init; }
    public int DurationMinutes { get; init; }
    public LessonTypeCategory Type { get; init; }
    public decimal PriceAdult { get; init; }
    public decimal PriceChild { get; init; }
    public int MaxStudents { get; init; }
    public bool IsActive { get; init; }
}
