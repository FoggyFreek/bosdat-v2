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

### 3. Student Transactions & Invoices

**Two systems:**
- **Invoices:** Normal billing (lessons → invoice lines)
- **Transactions:** Out-of-cycle financial records (refunds, credits, adjustments)

**Services:** `BosDAT.Infrastructure/Services/InvoiceService.cs`, `StudentTransactionService.cs`

### 4. Pricing Versioning

**Gotcha:** Course type pricing is versioned (`ValidFrom`), immutable post-invoice.

**Service:** `BosDAT.Infrastructure/Services/CourseTypePricingService.cs`

## Frontend Patterns

**Query Keys (domain convention):**
```ts
['students']              // List
['students', id]          // Detail
['students', id, 'transactions'] // Related
```

**Gotchas:**
- Fresh `QueryClient` per test (`test/utils.tsx`)
- `queryClient.invalidateQueries(['key'])` after mutations

### i18n (react-i18next)

```ts
import { useTranslation } from 'react-i18next'

const { t } = useTranslation()
<button>{t('common.actions.save')}</button>
<h1>{t('common.entities.students')}</h1>
```

**Translation map pattern (for enums/status types):**
```ts
export type CourseStatus = 'Active' | 'Paused' | 'Completed' | 'Cancelled'
export const courseStatusTranslations = {
    'Active': 'courses.status.active',
    'Paused': 'courses.status.paused',
    'Completed': 'courses.status.completed',
    'Cancelled': 'courses.status.cancelled',
  } as const satisfies Record<CourseStatus, string>;

// Usage
{t(courseStatusTranslations[course.status])}
```

**Structure:**
- Config: `src/i18n/config.ts` (auto-imported in `main.tsx`)
- Translations: `src/i18n/locales/{nl,en}.json` (namespace-based)
- Default: Dutch (nl), fallback: Dutch, persisted in localStorage
- Docs: `src/i18n/README.md` (complete namespace documentation)

**Namespaces:** `common.*` for shared terms (3+ places), then feature-specific: `students.*`, `teachers.*`, `courses.*`, `lessons.*`, `enrollments.*`, `invoices.*`, `dashboard.*`, `settings.*`, `auth.*`, `navigation.*`

**Adding translations:**
1. Choose namespace: common (shared) vs feature-specific
2. Add to both `locales/nl.json` AND `locales/en.json`
3. Use hierarchical keys: `students.form.firstName` not `student_first_name`

**Testing:**
- Global mock in `src/test/setup.ts` — translation keys returned as-is
- **CRITICAL:** Do NOT import `@/i18n/config` in `src/test/utils.tsx` (breaks mock hoisting)
- For `TFunction` type: `import type { TFunction } from 'i18next'`

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

## Reference

**Rules** (auto-loaded constraints): `.claude/rules/backend.md`, `frontend.md`, `workflow.md`

**Skills** (detailed templates, loaded on demand):
- `backend-patterns` — Controller, Service, Repository, UoW full templates
- `efcore` — Query patterns, migrations, model configuration
- `testing-backend` — xUnit + Moq mock wiring and test structure
- `dependency-injection` — Service registration, lifetimes, captive dependency prevention
