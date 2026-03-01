---
name: email-sending
description: "Email sending infrastructure: outbox pattern, IEmailSender (Console/Brevo), EmailOutboxMessage entity, background processor, configuration, DI registration. Use when working on email delivery, outbox processing, retry logic, or adding new email providers."
---

# Email Sending Infrastructure

BosDAT uses the **transactional outbox pattern**: emails are queued to the database, then a background worker renders templates and delivers via a provider (Console or Brevo).

## Architecture

```
Service code
  └── IEmailService.QueueEmailAsync()        ← writes to DB
        └── EmailOutboxMessage (Status=Pending)
              └── EmailOutboxProcessorBackgroundService (Worker)
                    ├── IEmailTemplateRenderer.RenderAsync()  ← Razor → HTML
                    └── IEmailSender.SendAsync()              ← Console or Brevo
```

## Key Files

| File | Purpose |
|------|---------|
| `Core/Entities/EmailOutboxMessage.cs` | Rich domain entity with state machine |
| `Core/Enums/EmailStatus.cs` | Pending, Processing, Sent, Failed, DeadLetter |
| `Core/Interfaces/Services/IEmailSender.cs` | Provider abstraction (single + batch) |
| `Core/Interfaces/Services/IEmailService.cs` | Queue entry-point |
| `Infrastructure/Email/ConsoleEmailSender.cs` | Dev provider — logs to console |
| `Infrastructure/Email/BrevoEmailSender.cs` | Prod provider — Brevo SMTP API |
| `Infrastructure/Email/EmailSettings.cs` | Configuration classes |
| `Infrastructure/Services/EmailService.cs` | Outbox queuing implementation |
| `Worker/Services/EmailOutboxProcessorBackgroundService.cs` | Background processor |
| `Worker/Configuration/WorkerSettings.cs` | Polling interval, batch size |

---

## EmailOutboxMessage Entity

Rich domain model with factory method and state transitions:

```csharp
public class EmailOutboxMessage : BaseEntity
{
    // Content
    public string To { get; private set; }
    public string Subject { get; private set; }
    public string TemplateName { get; private set; }       // e.g. "InvitationEmail"
    public string TemplateDataJson { get; private set; }   // Serialized model

    // Status tracking
    public EmailStatus Status { get; private set; }
    public int RetryCount { get; private set; }
    public DateTime? NextAttemptAtUtc { get; private set; }

    // Results
    public string? ProviderMessageId { get; private set; }
    public string? LastError { get; private set; }         // Max 2000 chars
    public DateTime? SentAtUtc { get; private set; }

    public uint ConcurrencyToken { get; set; }             // PostgreSQL xmin row version
}
```

### State Machine

```
Create() → Pending
  └── MarkProcessing() → Processing
        ├── MarkSent(messageId) → Sent  ✓
        └── MarkFailed(error) → Failed (retry scheduled)
              └── after 5 retries → DeadLetter  ✗
```

### Retry Strategy

Exponential backoff: `5^retryCount` minutes (5, 25, 125, 625, 3125 min). After 5 failures → DeadLetter.

### DB Configuration

- Table: `email_outbox_messages`
- Index: `ix_email_outbox_status_next_attempt` on (`Status`, `NextAttemptAtUtc`)
- Concurrency: PostgreSQL `xmin` row version prevents race conditions
- Constraints: To (255), Subject (500), TemplateName (100), LastError (2000)
- TemplateDataJson stored as `jsonb`

---

## IEmailSender Interface

```csharp
public interface IEmailSender
{
    Task<string> SendAsync(string to, string subject, string htmlBody,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> SendBatchAsync(IReadOnlyList<EmailMessage> messages,
        CancellationToken cancellationToken = default);
}

public record EmailMessage(string To, string Subject, string HtmlBody);
```

Returns provider message IDs for tracking.

---

## ConsoleEmailSender (Development)

Logs emails to console instead of sending. Generates `console-{guid}` message IDs.

- Registered as: `Singleton`
- Used when: `EmailSettings:Provider` is `"Console"` (default)

---

## BrevoEmailSender (Production)

Calls the [Brevo transactional email API](https://developers.brevo.com/docs/send-a-transactional-email).

```
POST https://api.brevo.com/v3/smtp/email
Headers: api-key, accept: application/json
```

### Single Send

Request body: `{ sender, to, subject, htmlContent }`
Response: `{ messageId }`

### Batch Send

Uses [messageVersions](https://developers.brevo.com/docs/batch-send-transactional-emails) — up to 1000 per request.

Request body: `{ sender, messageVersions: [{ to, subject, htmlContent }, ...] }`
Response: `{ messageIds: [...] }`

**Important:** No outer `to` field in batch mode — all recipients go inside `messageVersions`.

### Registration

```csharp
services.AddHttpClient<IEmailSender, BrevoEmailSender>();
```

Registered via `AddHttpClient` for proper `HttpClient` lifecycle management.

### Internal API Models

| Class | JSON root | Fields |
|-------|-----------|--------|
| `BrevoSendRequest` | single send | `sender`, `to`, `subject`, `htmlContent` |
| `BrevoBatchRequest` | batch send | `sender`, `messageVersions` |
| `BrevoMessageVersion` | batch item | `to`, `subject`, `htmlContent` |
| `BrevoContact` | sender/recipient | `email`, `name` |
| `BrevoSendResponse` | single response | `messageId` |
| `BrevoBatchResponse` | batch response | `messageIds` |

All use `[JsonPropertyName]` for camelCase serialization.

---

## EmailService (Outbox Queuing)

```csharp
public class EmailService(IUnitOfWork uow) : IEmailService
{
    public async Task QueueEmailAsync(string to, string subject, string templateName,
        object templateData, CancellationToken cancellationToken = default)
    {
        var message = EmailOutboxMessage.Create(to, subject, templateName, templateData);
        await uow.EmailOutboxMessages.AddAsync(message, cancellationToken);
        // Caller must call uow.SaveChangesAsync()
    }
}
```

**Key:** Does NOT call `SaveChangesAsync` — participates in the caller's transaction.

---

## Background Processor (Worker)

`EmailOutboxProcessorBackgroundService` runs in `BosDAT.Worker`:

### Processing Loop

1. Poll every N seconds (configurable, default 10)
2. Fetch batch of Pending emails where `NextAttemptAtUtc <= UtcNow`
3. Order by `CreatedAt` (FIFO)
4. For each email:
   - `MarkProcessing()` + `SaveChanges` (claim it)
   - Deserialize `TemplateDataJson` → `Dictionary<string, object>`
   - Render template via `IEmailTemplateRenderer`
   - Send via `IEmailSender`
   - `MarkSent()` or `MarkFailed()` + `SaveChanges`
5. `DbUpdateConcurrencyException` → another processor claimed it (safe to skip)

### Error Handling

- Individual email failures don't stop the batch
- Processing errors → 30s cooldown before next poll
- `OperationCanceledException` → graceful shutdown

### Configuration

```json
// Worker/appsettings.json
{
  "WorkerSettings": {
    "EmailOutboxJob": {
      "Enabled": true,
      "PollingIntervalSeconds": 10,
      "BatchSize": 20
    }
  }
}
```

---

## DI Registration

### API (`ServiceCollectionExtensions.cs`)

```csharp
public static IServiceCollection AddEmailServices(
    this IServiceCollection services, IConfiguration configuration)
{
    services.Configure<EmailSettings>(
        configuration.GetSection(EmailSettings.SectionName));
    services.AddScoped<IEmailService, EmailService>();
    services.AddSingleton<IEmailTemplateRenderer, EmailTemplateRenderer>();

    var provider = configuration[$"{EmailSettings.SectionName}:Provider"] ?? "Console";
    if (provider.Equals("Brevo", StringComparison.OrdinalIgnoreCase))
        services.AddHttpClient<IEmailSender, BrevoEmailSender>();
    else
        services.AddSingleton<IEmailSender, ConsoleEmailSender>();

    return services;
}
```

### Worker (`Program.cs`)

Same registrations plus:
```csharp
builder.Services.AddHostedService<EmailOutboxProcessorBackgroundService>();
```

### UnitOfWork

```csharp
// IUnitOfWork
IRepository<EmailOutboxMessage> EmailOutboxMessages { get; }
```

---

## Configuration

```json
// appsettings.json (both API and Worker)
{
  "EmailSettings": {
    "Provider": "Console",          // "Console" or "Brevo"
    "FromEmail": "noreply@bosdat.nl",
    "FromName": "BosDAT",
    "Brevo": {
      "ApiKey": ""                  // Set via environment variable in production
    }
  }
}
```

```csharp
public class EmailSettings
{
    public const string SectionName = "EmailSettings";
    public string Provider { get; set; } = "Console";
    public required string FromEmail { get; set; }
    public required string FromName { get; set; }
    public BrevoSettings Brevo { get; set; } = new();
}

public class BrevoSettings
{
    public string ApiKey { get; set; } = string.Empty;
}
```

---

## Adding a New Email Provider

1. Implement `IEmailSender` in `Infrastructure/Email/NewProviderEmailSender.cs`
2. Add provider-specific settings to `EmailSettings` if needed
3. Add registration branch in `AddEmailServices()` and Worker `Program.cs`:
   ```csharp
   else if (provider.Equals("NewProvider", StringComparison.OrdinalIgnoreCase))
       services.AddHttpClient<IEmailSender, NewProviderEmailSender>();
   ```
4. Update `appsettings.json` with provider config

---

## Testing

### Entity tests (`Core.Tests/Entities/EmailOutboxMessageTests.cs`)

Test state machine transitions:
- `Create()` sets correct initial state
- `MarkProcessing/Sent/Failed` transitions
- Retry scheduling with exponential backoff
- DeadLetter after max retries
- Error message truncation

### EmailService tests (`API.Tests/Services/EmailServiceTests.cs`)

Mock `IUnitOfWork`, verify:
- Creates `EmailOutboxMessage` with correct properties
- Does NOT call `SaveChangesAsync`

### BrevoEmailSender tests (`Infrastructure.Tests/Email/`)

Mock `HttpMessageHandler` to test:
- Correct API URL and headers
- Request body serialization
- Error handling for non-success status codes
- Batch size validation
- Message ID extraction from response
