# BosDAT v2 - AI Assistant Guide

Music school management system. **Core:** Course blueprints → automated lesson generation → invoicing/salary.

**Repo:** `FoggyFreek/bosdat-v2` (branch: `main`) | **JIRA:** Cloud ID `e107cebe-73a2-4fb8-8fc8-7513953706dc`

## Architecture

**Backend:** Clean Architecture (.NET 8, C# 12, EF Core 8, PostgreSQL 16)
- `BosDAT.API` → Controllers, JWT, Swagger
- `BosDAT.Core` → Entities, Interfaces, DTOs, Enums (zero dependencies)
- `BosDAT.Infrastructure` → Repositories, Services, Migrations

**Frontend:** Bulletproof React (React 19, TS, Vite)
- `features/[domain]/` → components, types, context
- TanStack Query (5min cache) + React Context (memoized)
- shadcn/ui + Tailwind exclusively

## Critical Domain Logic

### 1. Course → Lesson Generation Algorithm

**Non-obvious:** Courses = templates. Lessons = scheduled instances.

**Location:** `src/BosDAT.API/Controllers/LessonsController.cs:GenerateLessonsAsync()`

**Steps:**
1. Find first date matching `DayOfWeek` + `WeekParity` (ISO 8601 weeks)
2. Loop: Weekly (+7d), Biweekly (+14d, respecting parity), Monthly (+1mo)
3. Skip holidays and duplicates
4. Create: Individual (1/student), Group/Workshop (1 total, StudentId=null)

**Tests:** `tests/BosDAT.API.Tests/Controllers/LessonsController/` + `TestHelpers.cs`

### 2. ISO 8601 Week Parity

**Gotcha:** Biweekly uses ISO week numbers, not date arithmetic.

**Utility:** `BosDAT.Core/Utilities/IsoWeekHelper.cs`
- `GetIso8601WeekOfYear(date)` → 1-53
- `GetWeekParity(date)` → Odd/Even
- `MatchesWeekParity(date, parity)` → bool

### 3. Student Ledger vs Invoices

**Two systems:**
- **Invoices:** Normal billing (lessons → lines)
- **Ledger:** Out-of-cycle corrections (refunds, credits, adjustments)

**Service:** `BosDAT.Infrastructure/Services/StudentLedgerService.cs`

### 4. Pricing Versioning

**Gotcha:** Course type pricing is versioned (`ValidFrom`), immutable post-invoice.

**Service:** `BosDAT.Infrastructure/Services/CourseTypePricingService.cs`

## Key Patterns

### Backend

**Repository + UoW:**
```csharp
public class Controller(IUnitOfWork uow) : ControllerBase
{
    var entity = await uow.Repository.GetByIdAsync(id);
    await uow.SaveChangesAsync(); // Single transaction
}
```

**Primary Constructors:**
```csharp
public class Service(IDep dep) : IService { }
```

**Naming:** snake_case (DB), PascalCase (C#). Config in `ApplicationDbContext.OnModelCreating()`.

**Gotchas:**
- `.AsNoTracking()` for read-only queries
- Audit/timestamps auto via `SaveChanges()` override

### Frontend

**Code Splitting:**
```ts
// ❌ Barrel: single chunk
lazy(() => import('./pages').then(m => ({ default: m.Page })))

// ✅ Direct: separate chunk
lazy(() => import('./pages/StudentsPage').then(m => ({ default: m.StudentsPage })))
```

**Query Keys:**
```ts
['students']              // List
['students', id]          // Detail
['students', id, 'ledger'] // Related
```

**Context Memoization (REQUIRED):**
```ts
const value = useMemo(() => ({ user, login }), [user, login])
```

**Immutability (ES2023):**
```ts
arr.toSorted() // ✅ New array
arr.sort()     // ❌ Mutation
```

**Gotchas:**
- Fresh `QueryClient` per test (`test/utils.tsx`)
- Route-level lazy only, never barrels
- `queryClient.invalidateQueries(['key'])` after mutations

## Tech Stack

| Layer | Tech | Version |
|-------|------|---------|
| Backend | .NET, C#, EF Core | 8, 12, 8 |
| Frontend | React, TS, Vite | 18, 5, 5 |
| DB | PostgreSQL | 16 |
| Testing | xUnit, Vitest+RTL | 2.6, 4.0 |
| Auth | Identity + JWT | 8.0 |

## Commands

```bash
# Backend (from bosdat-v2/)
dotnet build BosDAT.sln
dotnet run --project src/BosDAT.API  # :5000 /swagger
dotnet test

# EF Migrations
dotnet ef migrations add [Name] --project src/BosDAT.Infrastructure --startup-project src/BosDAT.API
dotnet ef database update --project src/BosDAT.Infrastructure --startup-project src/BosDAT.API

# Frontend (from src/BosDAT.Web/)
npm run dev    # :5173
npm run test
npm run test:coverage

# Docker
docker-compose up -d
```

## Defaults

**Credentials:** `admin@bosdat.nl` / `Admin@123456`
**Swagger:** http://localhost:5000/swagger
**Seeding:** `/api/admin/seeder/*` (admin-only)

## Rules

See `.claude/rules/`:
- `backend.md` - C# patterns
- `frontend.md` - React patterns
- `workflow.md` - TDD, security, git
