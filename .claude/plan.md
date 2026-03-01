# Email Module Implementation Plan — Outbox Pattern

## Architectural Decision: Worker Database Access

The existing worker calls API endpoints via HTTP. The email outbox processor needs **direct database access** instead, because:
- Polling an API every 10s is wasteful and adds latency
- Atomic status transitions (Pending → Processing) require DB-level optimistic concurrency
- This is the standard outbox pattern — worker reads DB directly

**Impact:** Worker project gains a reference to `BosDAT.Infrastructure` and registers `ApplicationDbContext` + `IUnitOfWork`. Existing HTTP-based services remain unchanged.

---

## Phase 1: Core Layer — Entity, Enum, Interfaces

### 1.1 `src/BosDAT.Core/Enums/EmailStatus.cs`
```csharp
public enum EmailStatus { Pending, Processing, Sent, Failed, DeadLetter }
```

### 1.2 `src/BosDAT.Core/Entities/EmailOutboxMessage.cs`
- Extends `BaseEntity` (gets Id, CreatedAt, UpdatedAt)
- Properties: `To`, `Subject`, `TemplateName`, `TemplateDataJson`, `Status`, `RetryCount`, `NextAttemptAtUtc`, `ProviderMessageId`, `LastError`, `SentAtUtc`
- Domain methods: `MarkProcessing()`, `MarkSent(providerId)`, `MarkFailed(error)`, `MarkDeadLetter()`
- Static factory: `Create(to, subject, templateName, templateData)` — serializes data to JSON, sets Status=Pending
- `ScheduleRetry()` — exponential backoff: `5^retryCount` minutes, max 5 retries → DeadLetter
- `ConcurrencyToken` (byte[] / rowversion) for optimistic concurrency

### 1.3 `src/BosDAT.Core/Interfaces/Services/IEmailService.cs`
Application-level interface — used by business services to queue emails:
```csharp
public interface IEmailService
{
    Task QueueEmailAsync(string to, string subject, string templateName,
                         object templateData, CancellationToken ct = default);
}
```
Note: No `SaveChangesAsync` call inside — caller controls the transaction boundary (saves in same UoW transaction as business data).

### 1.4 `src/BosDAT.Core/Interfaces/Services/IEmailSender.cs`
Infrastructure-level interface — implemented by provider:
```csharp
public interface IEmailSender
{
    Task<string> SendAsync(string to, string subject, string htmlBody, CancellationToken ct);
}
```
Returns provider message ID.

### 1.5 `src/BosDAT.Core/Interfaces/Services/IEmailTemplateRenderer.cs`
```csharp
public interface IEmailTemplateRenderer
{
    Task<string> RenderAsync(string templateName, object model, CancellationToken ct = default);
}
```

---

## Phase 2: Infrastructure Layer — DbContext, Repository, Services

### 2.1 `ApplicationDbContext` — Add DbSet + Configuration
- Add `DbSet<EmailOutboxMessage> EmailOutboxMessages`
- `OnModelCreating`: table `email_outbox_messages`, snake_case columns, index on `(Status, NextAttemptAtUtc)`, concurrency token

### 2.2 EF Migration
```bash
dotnet ef migrations add AddEmailOutbox --project src/BosDAT.Infrastructure --startup-project src/BosDAT.API
```

### 2.3 `IUnitOfWork` + `UnitOfWork` — Add EmailOutboxMessages repository
- Add `IRepository<EmailOutboxMessage> EmailOutboxMessages { get; }` to interface
- Add lazy property in `UnitOfWork`

### 2.4 `src/BosDAT.Infrastructure/Services/EmailService.cs`
Implements `IEmailService`. Uses primary constructor with `IUnitOfWork`.
- `QueueEmailAsync` creates `EmailOutboxMessage.Create(...)` and adds via `uow.EmailOutboxMessages.AddAsync()`
- Does NOT call `SaveChangesAsync` — caller saves as part of their transaction

### 2.5 `src/BosDAT.Infrastructure/Email/EmailTemplateRenderer.cs`
Implements `IEmailTemplateRenderer` using RazorLight:
- NuGet: `RazorLight` (latest)
- Templates loaded from embedded resources or file path: `Infrastructure/Email/Templates/`
- Caches compiled templates

### 2.6 `src/BosDAT.Infrastructure/Email/Templates/InvitationEmail.cshtml`
First template — receives model with `DisplayName`, `InvitationUrl`, `ExpiresAt`.
Simple, clean HTML email with BosDAT branding.

### 2.7 `src/BosDAT.Infrastructure/Email/BrevoEmailSender.cs`
Implements `IEmailSender`:
- NuGet: `sib_api_v3_sdk` (Brevo official SDK)
- Configuration class: `BrevoSettings { ApiKey, FromEmail, FromName }`
- Sends transactional email via Brevo API
- Returns provider message ID

### 2.8 `src/BosDAT.Infrastructure/Email/ConsoleEmailSender.cs`
Development fallback — logs email to console/logger instead of sending. Used when no Brevo API key is configured.

### 2.9 DI Registration in `ServiceCollectionExtensions.cs`
```csharp
services.AddScoped<IEmailService, EmailService>();
services.AddSingleton<IEmailTemplateRenderer, EmailTemplateRenderer>();
// Provider registered conditionally based on config
services.AddSingleton<IEmailSender, BrevoEmailSender>(); // or ConsoleEmailSender
```

### 2.10 Configuration in `appsettings.json`
```json
"EmailSettings": {
    "Provider": "Brevo",
    "FromEmail": "noreply@bosdat.nl",
    "FromName": "BosDAT",
    "Brevo": {
        "ApiKey": ""  // Set via env var / user secrets
    }
}
```

---

## Phase 3: Wire Up Invitation Emails

### 3.1 Modify `UserManagementService`
- Inject `IEmailService` via primary constructor
- In `GenerateAndStoreTokenAsync()`: after creating the token, call `emailService.QueueEmailAsync()` with template `"InvitationEmail"` and model `{ DisplayName, InvitationUrl, ExpiresAt }`
- The outbox message is saved in the **same** `SaveChangesAsync` call as the invitation token — atomic
- `ResendInvitationAsync()` also queues a new email

### 3.2 Keep existing behavior
- The API still returns the `InvitationResponseDto` with the URL (admin can still copy-paste)
- Email sending is now **in addition to** the URL response, not replacing it

---

## Phase 4: Background Worker — Outbox Processor

### 4.1 Worker project changes
- Add project reference: `BosDAT.Infrastructure`
- Add NuGet: `Npgsql.EntityFrameworkCore.PostgreSQL`, `RazorLight`, `sib_api_v3_sdk`
- Register in `Program.cs`: `ApplicationDbContext`, `IEmailSender`, `IEmailTemplateRenderer`

### 4.2 `src/BosDAT.Worker/Configuration/WorkerSettings.cs`
Add `EmailOutboxJobSettings`:
```csharp
public class EmailOutboxJobSettings
{
    public bool Enabled { get; set; } = true;
    public int PollingIntervalSeconds { get; set; } = 10;
    public int BatchSize { get; set; } = 20;
    public int MaxRetries { get; set; } = 5;
}
```

### 4.3 `src/BosDAT.Worker/Services/EmailOutboxProcessorBackgroundService.cs`
- Follows existing BackgroundService pattern (primary constructor, scoped services)
- **Different scheduling**: Polls every N seconds (not daily like other jobs)
- Loop:
  1. Query outbox: `Status == Pending && (NextAttemptAtUtc == null || <= UtcNow)`, ordered by CreatedAt, take BatchSize
  2. For each message:
     - `MarkProcessing()` + save (optimistic concurrency check)
     - Render template via `IEmailTemplateRenderer`
     - Send via `IEmailSender`
     - `MarkSent(providerId)` or `MarkFailed(error)` + save
  3. Delay PollingIntervalSeconds

### 4.4 Worker `appsettings.json`
Add `EmailOutboxJob` settings + `EmailSettings` (same structure as API) + `ConnectionStrings`.

---

## Phase 5: Tests

### 5.1 Backend Tests
- **`EmailOutboxMessageTests.cs`** — Entity domain logic: Create, MarkProcessing, MarkSent, MarkFailed, retry scheduling, DeadLetter after max retries
- **`EmailServiceTests.cs`** — QueueEmailAsync creates correct outbox message via mocked UoW
- **`UserManagementServiceTests.cs`** — Update existing tests: verify email is queued when invitation is created/resent
- **`EmailOutboxProcessorTests.cs`** — Worker processes pending messages, handles failures, retries correctly

### 5.2 Skip DB-dependent tests
Tests that touch EF directly (template rendering integration, actual DB queries) get `[Fact(Skip = "Requires PostgreSQL")]` per backend rules.

---

## File Summary

| Action | File | Layer |
|--------|------|-------|
| Create | `Core/Enums/EmailStatus.cs` | Core |
| Create | `Core/Entities/EmailOutboxMessage.cs` | Core |
| Create | `Core/Interfaces/Services/IEmailService.cs` | Core |
| Create | `Core/Interfaces/Services/IEmailSender.cs` | Core |
| Create | `Core/Interfaces/Services/IEmailTemplateRenderer.cs` | Core |
| Modify | `Infrastructure/Data/ApplicationDbContext.cs` | Infra |
| Modify | `Core/Interfaces/IUnitOfWork.cs` | Core |
| Modify | `Infrastructure/Repositories/UnitOfWork.cs` | Infra |
| Create | `Infrastructure/Services/EmailService.cs` | Infra |
| Create | `Infrastructure/Email/EmailTemplateRenderer.cs` | Infra |
| Create | `Infrastructure/Email/BrevoEmailSender.cs` | Infra |
| Create | `Infrastructure/Email/ConsoleEmailSender.cs` | Infra |
| Create | `Infrastructure/Email/Templates/InvitationEmail.cshtml` | Infra |
| Create | EF Migration | Infra |
| Modify | `API/Extensions/ServiceCollectionExtensions.cs` | API |
| Modify | `API/appsettings.json` | API |
| Modify | `Infrastructure/Services/UserManagementService.cs` | Infra |
| Modify | `Worker/Program.cs` | Worker |
| Modify | `Worker/Configuration/WorkerSettings.cs` | Worker |
| Modify | `Worker/appsettings.json` | Worker |
| Create | `Worker/Services/EmailOutboxProcessorBackgroundService.cs` | Worker |
| Create | Tests (4+ files) | Tests |

## NuGet Packages to Add
- `RazorLight` → Infrastructure + Worker
- `sib_api_v3_sdk` (Brevo) → Infrastructure (or Worker if sender lives there)

## Implementation Order
Phases 1–5 are sequential. Within each phase, files are independent and can be created in any order (except migration depends on DbContext changes).
