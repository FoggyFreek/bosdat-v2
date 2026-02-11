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

**i18n (react-i18next):**
```ts
// Usage in components
import { useTranslation } from 'react-i18next'

const { t } = useTranslation()
<button>{t('common.actions.save')}</button>  // "Opslaan" (nl) / "Save" (en)
<h1>{t('common.entities.students')}</h1>  // "Leerlingen" (nl) / "Students" (en)

//declaration for Typescript Types
export type CourseStatus = 'Active' | 'Paused' | 'Completed' | 'Cancelled'
export const courseStatusTranslations = {
    'Active': 'courses.status.active',
    'Paused': 'courses.status.paused',
    'Completed': 'courses.status.completed',
    'Cancelled': 'courses.status.cancelled',
  } as const satisfies Record<CourseStatus, string>;
//usage of Typescript Types
 {t(courseStatusTranslations[course.status])}

//declaration for Enums
export type DayOfWeek = keyof typeof DAY_NAME_TO_NUMBER
export const dayOfWeekTranslations = {
    'Sunday': 'common.time.days.sunday',
    'Monday': 'common.time.days.monday',
    'Tuesday': 'common.time.days.tuesday',
    'Wednesday': 'common.time.days.wednesday',
    'Thursday': 'common.time.days.thursday',
    'Friday': 'common.time.days.friday',
    'Saturday': 'common.time.days.saturday',
  } as const satisfies Record<DayOfWeek, string>;
// usage of Enums
{t(dayOfWeekTranslations[course.dayOfWeek])}

// With interpolation
<p>{t('dashboard.stats.totalStudents', { count: 25 })}</p>  // "25 totaal leerlingen"
```

**Structure:**
- Config: `src/i18n/config.ts` (auto-imported in `main.tsx`)
- Translations: `src/i18n/locales/{nl,en}.json` (namespace-based)
- Switcher: `<LanguageSwitcher />` in Layout top bar
- Default: Dutch (nl), fallback: Dutch, persisted in localStorage
- Docs: `src/i18n/README.md` (complete namespace documentation)

**Namespaces:**
- `common.*` - Shared library (actions, entities, status, states, form, time)
  - Use for terms used in 3+ places
  - `common.actions.*` - save, cancel, edit, delete, etc.
  - `common.entities.*` - student, teacher, course, lesson, etc.
  - `common.status.*` - active, inactive, trial, etc.
- `dashboard.*` - Dashboard page
- `students.*` - Student management
- `teachers.*` - Teacher management
- `courses.*` - Course management
- `lessons.*` - Lesson management
- `enrollments.*` - Enrollment process
- `invoices.*` - Invoice management
- `settings.*` - Settings page
- `auth.*` - Authentication
- `navigation.*` - Navigation items

**Adding translations:**
1. Choose namespace: common (shared) vs feature-specific
2. Add to both `locales/nl.json` AND `locales/en.json`
3. Use hierarchical keys: `students.form.firstName` not `student_first_name`
4. Run `npm run check:i18n` to verify consistency
5. See `src/i18n/README.md` for complete guide

**Testing:**
- Global mock in `src/test/setup.ts` handles `react-i18next` for all tests
- Mock returns translation keys as-is: `t('common.actions.save')` → `'common.actions.save'`
- **CRITICAL:** Do NOT import `@/i18n/config` in `src/test/utils.tsx` (breaks mock hoisting)
- For `TFunction` type: `import type { TFunction } from 'i18next'`
- Tests use mocked `useTranslation` - no actual i18n initialization needed

## Tech Stack

| Layer | Tech | Version |
|-------|------|---------|
| Backend | .NET, C#, EF Core | 8, 12, 8 |
| Frontend | React, TS, Vite | 19, 5, 7 |
| i18n | react-i18next | Latest |
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
