namespace BosDAT.Core.DTOs;

public record CourseTaskDto
{
    public Guid Id { get; init; }
    public Guid CourseId { get; init; }
    public string Title { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}

public record CreateCourseTaskDto
{
    public required string Title { get; init; }
}
