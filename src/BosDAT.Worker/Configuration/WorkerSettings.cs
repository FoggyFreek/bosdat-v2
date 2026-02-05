namespace BosDAT.Worker.Configuration;

public class WorkerSettings
{
    public const string SectionName = "WorkerSettings";

    public required ApiSettings Api { get; set; }
    public required WorkerCredentials Credentials { get; set; }
    public required InvoiceJobSettings InvoiceJob { get; set; }
    public required LessonGenerationJobSettings LessonGenerationJob { get; set; }
    public required LessonStatusUpdateJobSettings LessonStatusUpdateJob { get; set; }
}

public class ApiSettings
{
    public required string BaseUrl { get; set; }
    public int TimeoutSeconds { get; set; } = 30;
    public int RetryCount { get; set; } = 3;
}

public class WorkerCredentials
{
    public required string Email { get; set; }
    public required string Password { get; set; }
}

public class InvoiceJobSettings
{
    public bool Enabled { get; set; } = true;
    public int DayOfMonth { get; set; } = 1;
    public TimeOnly ExecutionTime { get; set; } = new(8, 0);
}

public class LessonGenerationJobSettings
{
    public bool Enabled { get; set; } = true;
    public int DaysAhead { get; set; } = 90;
    public TimeOnly ExecutionTime { get; set; } = new(2, 0);
    public bool SkipHolidays { get; set; } = true;
}

public class LessonStatusUpdateJobSettings
{
    public bool Enabled { get; set; } = true;
    public TimeOnly ExecutionTime { get; set; } = new(0, 0);
}
