using System.Text.Json.Serialization;

namespace BosDAT.Worker.Models;

public record AuthResponse
{
    [JsonPropertyName("accessToken")]
    public required string AccessToken { get; init; }

    [JsonPropertyName("refreshToken")]
    public required string RefreshToken { get; init; }

    [JsonPropertyName("expiresAt")]
    public DateTime ExpiresAt { get; init; }
}

public record LoginRequest
{
    [JsonPropertyName("email")]
    public required string Email { get; init; }

    [JsonPropertyName("password")]
    public required string Password { get; init; }
}

public record RefreshRequest
{
    [JsonPropertyName("refreshToken")]
    public required string RefreshToken { get; init; }
}

public record BulkGenerateLessonsRequest
{
    [JsonPropertyName("startDate")]
    public required string StartDate { get; init; }

    [JsonPropertyName("endDate")]
    public required string EndDate { get; init; }

    [JsonPropertyName("skipHolidays")]
    public bool SkipHolidays { get; init; }
}

public record BulkGenerateLessonsResult
{
    [JsonPropertyName("startDate")]
    public string StartDate { get; init; } = string.Empty;

    [JsonPropertyName("endDate")]
    public string EndDate { get; init; } = string.Empty;

    [JsonPropertyName("totalCoursesProcessed")]
    public int TotalCoursesProcessed { get; init; }

    [JsonPropertyName("totalLessonsCreated")]
    public int TotalLessonsCreated { get; init; }

    [JsonPropertyName("totalLessonsSkipped")]
    public int TotalLessonsSkipped { get; init; }
}

public record LessonDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("courseId")]
    public Guid CourseId { get; init; }

    [JsonPropertyName("scheduledDate")]
    public string ScheduledDate { get; init; } = string.Empty;

    [JsonPropertyName("startTime")]
    public string StartTime { get; init; } = string.Empty;

    [JsonPropertyName("endTime")]
    public string EndTime { get; init; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    [JsonPropertyName("studentId")]
    public Guid? StudentId { get; init; }

    [JsonPropertyName("teacherId")]
    public Guid TeacherId { get; init; }
}

public record UpdateLessonStatusRequest
{
    [JsonPropertyName("status")]
    public required string Status { get; init; }

    [JsonPropertyName("cancellationReason")]
    public string? CancellationReason { get; init; }
}

public record InvoiceRunRequest
{
    [JsonPropertyName("month")]
    public int Month { get; init; }

    [JsonPropertyName("year")]
    public int Year { get; init; }
}

public record InvoiceRunResult
{
    [JsonPropertyName("invoicesGenerated")]
    public int InvoicesGenerated { get; init; }

    [JsonPropertyName("totalAmount")]
    public decimal TotalAmount { get; init; }

    [JsonPropertyName("month")]
    public int Month { get; init; }

    [JsonPropertyName("year")]
    public int Year { get; init; }
}
