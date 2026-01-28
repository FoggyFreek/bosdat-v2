using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using BosDAT.Core.Attributes;
using BosDAT.Core.Entities;

namespace BosDAT.Infrastructure.Audit;

public class AuditEntry
{
    public EntityEntry Entry { get; }
    public string EntityName { get; set; } 
    public AuditAction Action { get; set; }
    public Dictionary<string, object?> OldValues { get; } = new();
    public Dictionary<string, object?> NewValues { get; } = new();
    public List<string> ChangedProperties { get; } = new();
    public List<PropertyEntry> TemporaryProperties { get; } = new();

    public bool HasTemporaryProperties => TemporaryProperties.Count > 0;

    public AuditEntry(EntityEntry entry)
    {
        Entry = entry;
        EntityName = entry.Entity.GetType().Name;

        SetAction(entry.State);
        CapturePropertyValues(entry);
    }

    private void SetAction(EntityState state)
    {
        Action = state switch
        {
            EntityState.Added => AuditAction.Created,
            EntityState.Modified => AuditAction.Updated,
            EntityState.Deleted => AuditAction.Deleted,
            _ => AuditAction.Updated
        };
    }

    private void CapturePropertyValues(EntityEntry entry)
    {
        foreach (var property in entry.Properties)
        {
            var propertyName = property.Metadata.Name;

            // Skip properties marked as sensitive
            var propertyInfo = entry.Entity.GetType().GetProperty(propertyName);
            if (propertyInfo?.GetCustomAttributes(typeof(SensitiveDataAttribute), true).Length > 0)
            {
                continue;
            }

            // Handle temporary values (e.g., auto-generated IDs)
            if (property.IsTemporary)
            {
                TemporaryProperties.Add(property);
                continue;
            }

            switch (entry.State)
            {
                case EntityState.Added:
                    NewValues[propertyName] = property.CurrentValue;
                    break;

                case EntityState.Deleted:
                    OldValues[propertyName] = property.OriginalValue;
                    break;

                case EntityState.Modified:
                    if (property.IsModified && !Equals(property.OriginalValue, property.CurrentValue))
                    {
                        ChangedProperties.Add(propertyName);
                        OldValues[propertyName] = property.OriginalValue;
                        NewValues[propertyName] = property.CurrentValue;
                    }
                    break;
            }
        }
    }

    public AuditLog ToAuditLog(Guid? userId, string? userEmail, string? ipAddress)
    {
        // For Added entries, capture the temporary property values that are now set
        foreach (var prop in TemporaryProperties)
        {
            NewValues[prop.Metadata.Name] = prop.CurrentValue;
        }

        // Get the entity ID
        var entityId = GetEntityId();

        return new AuditLog
        {
            Id = Guid.NewGuid(),
            EntityName = EntityName,
            EntityId = entityId,
            Action = Action,
            OldValues = OldValues.Count == 0 ? null : JsonSerializer.Serialize(OldValues),
            NewValues = NewValues.Count == 0 ? null : JsonSerializer.Serialize(NewValues),
            ChangedProperties = ChangedProperties.Count == 0 ? null : JsonSerializer.Serialize(ChangedProperties),
            UserId = userId,
            UserEmail = userEmail,
            IpAddress = ipAddress,
            Timestamp = DateTime.UtcNow
        };
    }

    private string GetEntityId()
    {
        var keyValues = Entry.Properties
            .Where(p => p.Metadata.IsPrimaryKey())
            .Select(p => p.CurrentValue?.ToString() ?? string.Empty)
            .ToList();

        return string.Join(",", keyValues);
    }
}
