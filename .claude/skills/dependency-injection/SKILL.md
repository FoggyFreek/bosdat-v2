---
name: dependency-injection
description: ".NET DI patterns: ServiceCollectionExtensions registration, service lifetimes (Scoped/Singleton/Transient), captive dependency prevention, new service checklist. Use when adding or modifying service registrations."
---

# Dependency Injection

All registrations centralized in `src/BosDAT.API/Extensions/ServiceCollectionExtensions.cs`.

---

## Extension Method Pattern

```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Default")));

        // Unit of Work + Repositories
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Domain Services
        services.AddScoped<IInvoiceService, InvoiceService>();
        services.AddScoped<IEnrollmentService, EnrollmentService>();
        services.AddScoped<IRegistrationFeeService, RegistrationFeeService>();
        services.AddScoped<IStudentTransactionService, StudentTransactionService>();

        return services;
    }
}
```

**In `Program.cs`:** `builder.Services.AddInfrastructure(builder.Configuration);`

---

## Adding a New Service (checklist)

1. Define interface in `BosDAT.Core/Interfaces/IMyService.cs`
2. Implement in `BosDAT.Infrastructure/Services/MyService.cs`
3. Register in `ServiceCollectionExtensions.cs`:
   ```csharp
   services.AddScoped<IMyService, MyService>();
   ```
4. Inject via primary constructor wherever needed

---

## Service Lifetimes

| Lifetime | When | Example |
|----------|------|---------|
| `Scoped` | Per HTTP request (most services) | `IUnitOfWork`, domain services, repositories |
| `Singleton` | Shared state / expensive init | `IConfiguration`, caches |
| `Transient` | Stateless utilities | Formatters, converters |

```csharp
services.AddScoped<IInvoiceService, InvoiceService>();       // per request
services.AddSingleton<ICacheService, MemoryCacheService>();   // app lifetime
services.AddTransient<IEmailFormatter, HtmlEmailFormatter>(); // per injection
```

---

## Captive Dependency Warning

Never inject a `Scoped` service into a `Singleton` — the scoped service becomes effectively singleton:

```csharp
// Bug: IUnitOfWork (Scoped) captured inside Singleton
services.AddSingleton<IBackgroundJob, MyJob>();  // MyJob injects IUnitOfWork

// Fix: Use IServiceScopeFactory to create a scope manually
public class MyJob(IServiceScopeFactory scopeFactory)
{
    public async Task RunAsync()
    {
        using var scope = scopeFactory.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        // ...
    }
}
```

---

## Primary Constructors (C# 12) — Always preferred

```csharp
// Preferred — concise, idiomatic
public class InvoiceService(IUnitOfWork uow, IPricingService pricing) : IInvoiceService
{
    public async Task<InvoiceDto?> GetAsync(int id)
    {
        var invoice = await uow.Invoices.GetByIdAsync(id);
        return invoice is null ? null : InvoiceDto.From(invoice);
    }
}
```

Always inject **interfaces**, never concrete classes.
