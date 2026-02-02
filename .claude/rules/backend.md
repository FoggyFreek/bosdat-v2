# Backend Rules (C# / .NET 8)

## Naming
- **Public:** PascalCase (classes, methods, properties)
- **Private:** _camelCase (fields with underscore prefix)
- **DB:** snake_case tables (configured in `ApplicationDbContext`)

## Architecture
- **Controllers:** Route-only, inject `IUnitOfWork` (NOT individual repos)
- **Services:** Business logic, use Primary Constructors
- **Repositories:** Generic `Repository<T>`, extend only when custom queries needed
- **Core Layer:** Zero external dependencies

## Patterns

**Primary Constructors (C# 12):**
```csharp
public class MyService(IDependency dep) : IMyService
{
    public void Method() => dep.DoSomething();
}
```

**Repository + UoW:**
```csharp
public class Controller(IUnitOfWork uow) : ControllerBase
{
    var entity = await uow.Students.GetByIdAsync(id);
    await uow.SaveChangesAsync(); // Single transaction
}
```

**Read-only queries:**
```csharp
await _context.Students.AsNoTracking().ToListAsync();
```

## Rules
- Use `IUnitOfWork` for transactions, not individual repos
- `.AsNoTracking()` for all read-only queries (performance)
- Audit/timestamps automatic via `SaveChanges()` override
- Validate inputs with DTOs + FluentValidation
- Never expose entities directly - use DTOs
- Use `Result<T>` pattern for service responses

## File Size
- 200-400 lines typical
- 800 max (split if larger)

## Security
- Parameterized queries only (EF does this)
- Validate all inputs
- Use `[Authorize]` / `[Authorize(Policy = "...")]`
- Never log sensitive data
