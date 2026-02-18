---
name: efcore
description: "EF Core 8 patterns for PostgreSQL: query optimization (AsNoTracking, projections, includes), migration commands, model configuration (snake_case), data migrations. Use when writing queries or managing schema changes."
---

# EF Core

EF Core 8 with PostgreSQL 16. Configuration in `ApplicationDbContext`, queries through repositories.

**Key files:**
- `src/BosDAT.Infrastructure/Data/ApplicationDbContext.cs` — model config, audit hook
- `src/BosDAT.Infrastructure/Migrations/` — migration files
- `src/BosDAT.Infrastructure/Repositories/` — query implementations

---

## Query Patterns

### AsNoTracking (required for all reads)

```csharp
// Read-only — no change tracking overhead
var students = await context.Students.AsNoTracking().ToListAsync();
```

### Includes (eager loading)

```csharp
var enrollment = await context.Enrollments
    .AsNoTracking()
    .Include(e => e.Student)
    .Include(e => e.Course)
        .ThenInclude(c => c.CourseType)
    .FirstOrDefaultAsync(e => e.Id == id);
```

### Projection (prefer over loading full entities)

```csharp
var dtos = await context.Students
    .AsNoTracking()
    .Where(s => s.IsActive)
    .Select(s => new StudentSummaryDto
    {
        Id = s.Id,
        Name = s.Name,
        EnrollmentCount = s.Enrollments.Count
    })
    .ToListAsync();
```

### Existence Check (don't load entity just to check)

```csharp
// AnyAsync — does not materialize the entity
bool exists = await context.Enrollments
    .AnyAsync(e => e.StudentId == studentId && e.CourseId == courseId);
```

### Filtering & Ordering

```csharp
var invoices = await context.Invoices
    .AsNoTracking()
    .Where(i => i.StudentId == studentId && i.Status == InvoiceStatus.Pending)
    .OrderByDescending(i => i.CreatedAt)
    .Take(50)
    .ToListAsync();
```

### Tracked Write Pattern

```csharp
// Add
var entity = new Student { Name = dto.Name };
await context.Students.AddAsync(entity);
await context.SaveChangesAsync();  // entity.Id populated after this

// Update (tracked — no AsNoTracking)
var student = await context.Students.FindAsync(id);
student!.Name = dto.Name;
await context.SaveChangesAsync();

// Delete
context.Students.Remove(student!);
await context.SaveChangesAsync();
```

### Raw SQL (last resort)

```csharp
// EF8 FormattableString — type-safe, no SQL injection
var students = await context.Students
    .FromSql($"SELECT * FROM students WHERE name = {name}")
    .ToListAsync();
```

---

## Model Configuration

snake_case naming applied globally in `OnModelCreating`:

```csharp
protected override void OnModelCreating(ModelBuilder builder)
{
    foreach (var entity in builder.Model.GetEntityTypes())
    {
        entity.SetTableName(entity.GetTableName()!.ToSnakeCase());
        foreach (var prop in entity.GetProperties())
            prop.SetColumnName(prop.GetColumnName().ToSnakeCase());
    }
}
```

---

## Migrations

### Naming Convention

PascalCase descriptive names: `AddStudentTable`, `AddInvoiceStatusColumn`, `RemoveDeprecatedLedger`

### Data Migration Example

```csharp
public partial class BackfillInvoiceStatus : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
            UPDATE invoices SET status = 'Pending' WHERE status IS NULL
        ");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"UPDATE invoices SET status = NULL");
    }
}
```

### Rules
- Never edit `ApplicationDbContextModelSnapshot.cs` manually
- Never delete migration files applied to any environment
- Add migration after updating the entity model, not before
- Review generated `Up()` and `Down()` — EF sometimes misses rename vs drop+add
