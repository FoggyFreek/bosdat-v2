namespace BosDAT.Core.Entities;

public class AuditLog
{
    public Guid Id { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public AuditAction Action { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? ChangedProperties { get; set; }
    public Guid? UserId { get; set; }
    public string? UserEmail { get; set; }
    public string? IpAddress { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
