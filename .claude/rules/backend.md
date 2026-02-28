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

## Database Queries
- **All EF queries belong exclusively in repositories** — never in services or controllers
- **`EF.Functions.ILike` belongs exclusively in repositories** — never in services or controllers
- Services that need case-insensitive search must call a named repository method (e.g. `GetFilteredAsync`, `ExistsByNameAsync`)
- `EF.Functions.ILike` does not work with the EF in-memory provider — tests that hit ILike-based repository methods must be skipped (`[Fact(Skip = "Requires PostgreSQL")]`) or use a mocked repository
- **Services must never compose `IQueryable` or call `.Where()` / `.Include()` / `.Select()` directly** — if combined filters are needed, add a `GetFilteredAsync(...)` method to the repository
- **Services must never inject `ApplicationDbContext`** — all data access (reads and writes) goes through `IUnitOfWork` and named repository methods

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
