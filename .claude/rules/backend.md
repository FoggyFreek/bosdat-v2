# Backend Rules (C# / .NET 8)

## Naming
- **Public:** PascalCase (classes, methods, properties)
- **Private:** _camelCase (fields with underscore prefix)
- **DB:** snake_case tables (configured in `ApplicationDbContext`)

## Architecture
- use the relevant skill when writing code for controllers, services, repositories or the core layer.
- **Controllers:** Route-only, inject `IUnitOfWork` when required. (NO business logic, and NO individual repos)
- **Services:** Business logic, use Primary Constructors
- **Repositories:** Generic `Repository<T>`, extend only when custom queries needed
- **Core Layer:** Zero external dependencies

## Rules
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
