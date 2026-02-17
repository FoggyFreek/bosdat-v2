---
name: backend-patterns
description: "Clean Architecture layer patterns: Controller (thin HTTP adapter), Service (business logic + Result<T>), Repository (generic + custom), UnitOfWork (transaction scope). Use when implementing new endpoints, services, or data access."
---

# Backend Patterns

BosDAT uses layered Clean Architecture. Each layer has a single responsibility and communicates only through interfaces.

| Layer | Role | Injects |
|-------|------|---------|
| **Controller** | HTTP in → DTO out | `IUnitOfWork`, domain services |
| **Service** | Business logic + mapping | `IUnitOfWork`, other services |
| **Repository** | EF Core data access | `ApplicationDbContext` |
| **UnitOfWork** | Transaction scope | All repositories |

---

## Controller Pattern

Controllers are thin HTTP adapters — routing, auth, validation only.

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StudentsController(IUnitOfWork uow) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<StudentDto>>> GetAll()
    {
        var students = await uow.Students.GetAllAsync();
        return Ok(students.Select(StudentDto.From));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<StudentDto>> GetById(int id)
    {
        var student = await uow.Students.GetByIdAsync(id);
        return student is null ? NotFound() : Ok(StudentDto.From(student));
    }

    [HttpPost]
    public async Task<ActionResult<StudentDto>> Create(CreateStudentDto dto)
    {
        var student = new Student { Name = dto.Name };
        await uow.Students.AddAsync(student);
        await uow.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = student.Id }, StudentDto.From(student));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var student = await uow.Students.GetByIdAsync(id);
        if (student is null) return NotFound();
        uow.Students.Remove(student);
        await uow.SaveChangesAsync();
        return NoContent();
    }
}
```

**Rules:**
- Inject `IUnitOfWork` for data access, domain services for business logic — never `ApplicationDbContext` or concrete repositories
- Use `[Authorize]` on every controller or action
- Prefer `ActionResult<T>` over `IActionResult` for typed Swagger docs
- Delegate multi-step operations to a service; use UoW directly only for simple CRUD
- Never catch general `Exception` — let global middleware handle it

---

## Service Pattern

Services own business logic, validation, and entity-to-DTO mapping.

```csharp
public interface IInvoiceService
{
    Task<Result<InvoiceDto>> GenerateForEnrollmentAsync(int enrollmentId);
}

public class InvoiceService(IUnitOfWork uow, IPricingService pricing) : IInvoiceService
{
    public async Task<Result<InvoiceDto>> GenerateForEnrollmentAsync(int enrollmentId)
    {
        var enrollment = await uow.Enrollments.GetByIdAsync(enrollmentId);
        if (enrollment is null)
            return Result<InvoiceDto>.Failure("Enrollment not found");

        if (enrollment.Status == EnrollmentStatus.Cancelled)
            return Result<InvoiceDto>.Failure("Cannot invoice a cancelled enrollment");

        var price = await pricing.GetCurrentPriceAsync(enrollment.CourseTypeId);
        var invoice = new Invoice { EnrollmentId = enrollmentId, Amount = price };

        await uow.Invoices.AddAsync(invoice);
        await uow.SaveChangesAsync();

        return Result<InvoiceDto>.Success(InvoiceDto.From(invoice));
    }
}
```

### Result\<T\> Pattern

```csharp
public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? Error { get; }

    private Result(bool isSuccess, T? value, string? error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    public static Result<T> Success(T value) => new(true, value, null);
    public static Result<T> Failure(string error) => new(false, default, error);
}
```

**Controller consuming Result:**
```csharp
var result = await invoiceService.GenerateForEnrollmentAsync(id);
return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
```

**Rules:**
- Register as `Scoped` (per-request lifetime)
- Never expose entities — always return DTOs or `Result<T>`
- The service that orchestrates the full operation owns `SaveChangesAsync`; sub-services must not save
- Use primary constructors (C# 12)

---

## Repository Pattern

Generic base covers CRUD; extend only for custom queries.

```csharp
// Interface (Core layer)
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task AddAsync(T entity);
    void Remove(T entity);
}

// Implementation (Infrastructure layer)
public class Repository<T>(ApplicationDbContext context) : IRepository<T> where T : class
{
    protected readonly DbSet<T> _set = context.Set<T>();

    public async Task<T?> GetByIdAsync(int id) => await _set.FindAsync(id);
    public async Task<IEnumerable<T>> GetAllAsync() => await _set.AsNoTracking().ToListAsync();
    public async Task AddAsync(T entity) => await _set.AddAsync(entity);
    public void Remove(T entity) => _set.Remove(entity);
}
```

### Custom Repository (extend only when needed)

```csharp
public interface IStudentRepository : IRepository<Student>
{
    Task<IEnumerable<Student>> GetActiveWithEnrollmentsAsync();
}

public class StudentRepository(ApplicationDbContext context)
    : Repository<Student>(context), IStudentRepository
{
    public async Task<IEnumerable<Student>> GetActiveWithEnrollmentsAsync() =>
        await _set
            .AsNoTracking()
            .Include(s => s.Enrollments)
            .Where(s => s.IsActive)
            .ToListAsync();
}
```

**Rules:**
- `.AsNoTracking()` on all read-only queries
- `GetByIdAsync` (via `FindAsync`) returns a tracked entity — use for update/delete; for read-only by-ID, project with `.AsNoTracking()` in a custom query
- Never call `SaveChanges` — UoW owns that
- Keep in `BosDAT.Infrastructure/Repositories/`

---

## Unit of Work Pattern

Groups all repositories under one transaction scope.

```csharp
// Interface (Core layer)
public interface IUnitOfWork : IDisposable
{
    IStudentRepository Students { get; }
    IEnrollmentRepository Enrollments { get; }
    IInvoiceRepository Invoices { get; }
    ICourseRepository Courses { get; }
    // add new repos here as domain grows

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

// Implementation (Infrastructure layer)
public class UnitOfWork(ApplicationDbContext context) : IUnitOfWork
{
    private IStudentRepository? _students;
    private IInvoiceRepository? _invoices;

    public IStudentRepository Students =>
        _students ??= new StudentRepository(context);

    public IInvoiceRepository Invoices =>
        _invoices ??= new InvoiceRepository(context);

    public async Task<int> SaveChangesAsync(CancellationToken ct = default) =>
        await context.SaveChangesAsync(ct);

    public void Dispose() => context.Dispose();
}
```

**Rules:**
- Registered as `Scoped` in DI (one instance per HTTP request)
- `SaveChangesAsync` called once per request — at the end of the controller action
- Never inject `ApplicationDbContext` directly into controllers
- Use lazy initialization (`??=`) for repository properties
