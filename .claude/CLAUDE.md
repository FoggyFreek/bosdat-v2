# BosDAT v2 - AI Assistant Guide

Music school management system. **Core:** Course blueprints → automated lesson generation → invoicing/salary.

**Repo:** `FoggyFreek/bosdat-v2` (branch: `main`) | **JIRA:** Cloud ID `e107cebe-73a2-4fb8-8fc8-7513953706dc`

## Architecture

**Backend:** Clean Architecture (.NET 10, C# 13, EF Core 10, PostgreSQL 16)
- `BosDAT.API` → Controllers, JWT, Scalar (OpenAPI UI at `/scalar/v1`)
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

### 3. Student financials: Transactions & Invoices

**Layered systems:**
- **Invoices:** Normal billing (lessons → invoice lines)
- **Transactions:** Central ledger — every financial event creates one or more `StudentTransaction` rows

**Services:** `BosDAT.Infrastructure/Services/InvoiceService.cs`, `StudentTransactionService.cs`, `CreditInvoiceService.cs`

**Key `TransactionType` values:**
| Value | Side | Description |
|-------|------|-------------|
| `InvoiceCharge` | Debit | New invoice issued to student |
| `Payment` | Credit | Cash/bank payment received |
| `CreditInvoice` | Credit | Credit note issued (negative invoice) |
| `CreditOffset` | Debit on credit invoice | Credit note being consumed — used to track remaining credit |
| `CreditApplied` | Credit on target invoice | Credit note applied to reduce an outstanding invoice |

**Credit application (double-entry pair):** When a credit invoice is applied to an outstanding invoice, two transactions are created simultaneously — `CreditOffset` (debits the credit invoice, tracking consumption) and `CreditApplied` (credits the target invoice, reducing the balance). Remaining credit is computed dynamically: `Abs(creditInvoice.Total) - SUM(CreditOffset debits for that invoice)`.

### 4. Pricing Versioning

**Gotcha:** Course type pricing is versioned (`ValidFrom`), immutable post-invoice.

**Service:** `BosDAT.Infrastructure/Services/CourseTypePricingService.cs`

## Tech Stack

| Layer | Tech | Version |
|-------|------|---------|
| Backend | .NET, C#, EF Core | 10, 13, 10 |
| Frontend | React, TS, Vite | 19, 5, 7 |
| i18n | react-i18next | Latest |
| DB | PostgreSQL | 16 |
| Testing | xUnit, Vitest+RTL | 2.6, 4.0 |
| Auth | Identity + JWT | 10.0 |

## Commands

```bash
# Backend (from bosdat-v2/)
dotnet build BosDAT.sln
dotnet run --project src/BosDAT.API  # :5000 /scalar/v1
dotnet test

# EF Migrations
dotnet ef migrations add [Name] --project src/BosDAT.Infrastructure --startup-project src/BosDAT.API
dotnet ef database update --project src/BosDAT.Infrastructure --startup-project src/BosDAT.API

# Frontend (from src/BosDAT.Web/)
npm run dev    # :5173
npm run build  # tsc + vite build (catches type errors)
npm run lint
npm run test
npm run test:coverage

# Docker
docker-compose up -d
```

## Defaults

**Credentials:** `admin@bosdat.nl` / `Admin@123456`
**Scalar UI:** http://localhost:5000/scalar/v1
**OpenAPI JSON:** http://localhost:5000/openapi/v1.json
**Seeding:** `/api/admin/seeder/*` (admin-only)

## Reference

**Rules** (auto-loaded constraints): `.claude/rules/backend.md`, `frontend.md`, `workflow.md`

**Skills** (detailed templates, loaded on demand):
- `backend-patterns` — Controller, Service, Repository, UoW, DI registration templates
- `efcore` — Query patterns, migrations, model configuration
- `testing-backend` — xUnit + Moq mock wiring and test structure
- `frontend-patterns` — Component, hook, context, page templates + TanStack Query
- `testing-frontend` — Vitest + RTL patterns, vi.mock, QueryClient, renderHook
- `i18n` — Translation hooks, enum maps, namespace guide, adding translations
