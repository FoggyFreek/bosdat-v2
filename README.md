# BosDAT v2 - Modern Music School Management System

> A comprehensive web-based application for managing music schools, built with .NET 10, React 19, and PostgreSQL 16.

## 🎯 Overview

BosDAT v2 is a complete rewrite of the legacy NMI Access database system, modernizing music school operations with automated lesson scheduling, intelligent pricing, financial tracking, and comprehensive reporting.

**Core Concept:** Course blueprints (templates) combined with Students, Teachers, Rooms, and Lesson Types feed an automated planning engine that generates scheduled lessons, invoices for students, and salary calculations for teachers.

## ✨ Key Features

### 🎓 Student & Teacher Management
- **Student Profiles** - Complete student records with billing details, contact info, enrollment history
- **Duplicate Detection** - Intelligent fuzzy matching prevents duplicate entries (Levenshtein distance algorithm)
- **Teacher Profiles** - Instrument assignments, availability, hourly rates, course schedules
- **Role-Based Access** - Admin, Teacher, Staff, User roles with granular permissions

### 📅 Automated Lesson Scheduling
- **Course Blueprints** - Define recurring lesson patterns (Weekly, Biweekly with ISO 8601 week parity, Monthly)
- **Automated Generation** - Single or bulk lesson creation from course templates
- **Holiday Management** - Automatic skipping of scheduled holidays
- **Conflict Detection** - Teacher/room/student scheduling conflict prevention
- **Course Types** - Individual (1-on-1), Group, Workshop formats

### 💰 Financial Management
- **Dynamic Pricing** - Version-controlled pricing with adult/child tiers
- **Discount System** - Family, multi-course, and custom enrollment discounts
- **Student Ledger** - Credit/debit tracking for refunds, corrections, overpayments
- **Invoice Generation** - Automated invoice creation with PDF export
- **Teacher Payments** - Salary calculation based on lessons taught
- **Payment Tracking** - Multiple payment methods (cash, bank transfer, card)

### 📊 Advanced Features
- **Audit Logging** - Comprehensive change tracking with JSONB storage (who, what, when, IP)
- **Calendar Views** - Week/day/month views for students, teachers, and rooms
- **Settings Management** - Configurable instruments, rooms, holidays, system settings
- **Database Seeding** - Development/demo data generation with realistic Dutch names
- **Registration Fees** - One-time fee tracking and ledger integration

## 🏗️ Architecture

### Clean Architecture (3-Layer Backend)

```
┌─────────────────────────────────────────────────────────┐
│ BosDAT.API (Presentation Layer)                         │
│ • RESTful Controllers                                   │
│ • JWT Authentication Middleware                         │
│ • OpenAPI / Scalar Documentation                        │
│ • CORS Configuration                                    │
└─────────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────┐
│ BosDAT.Core (Domain Layer)                              │
│ • Entities (Student, Teacher, Course, Lesson, etc.)     │
│ • Interfaces (IRepository<T>, IUnitOfWork, IServices)   │
│ • DTOs (API Contracts)                                  │
│ • Enums (WeekParity, CourseFrequency, Statuses)         │
│ • Domain Utilities (IsoWeekHelper)                      │
└─────────────────────────────────────────────────────────┘
                          ↑
┌─────────────────────────────────────────────────────────┐
│ BosDAT.Infrastructure (Data + Services Layer)           │
│ • EF Core 10 + PostgreSQL 16                            │
│ • Repository Pattern + Unit of Work                     │
│ • Domain Services (Pricing, Scheduling, Ledger, Email)  │
│ • Migrations & Seeding                                  │
│ • Automatic Audit Logging                               │
└─────────────────────────────────────────────────────────┘
```

### Bulletproof React Frontend

Feature-based structure under `src/BosDAT.Web/src/`:

- **`features/[domain]/`** — components, API calls, types per domain
- **`components/`** — shared UI (shadcn/ui primitives)
- **`pages/`** — lazy-loaded route components
- **`services/`** — Axios client + interceptors
- **`hooks/`** — shared custom hooks

Server state via TanStack Query, client state via memoized React Context, shadcn/ui + Tailwind for styling.

## 🛠️ Tech Stack

| Layer | Technology | Version |
|-------|------------|---------|
| Backend Framework | .NET / C# | 10.0 / 13.0 |
| ORM | Entity Framework Core | 10.0 |
| Database | PostgreSQL | 16 |
| Frontend Framework | React + TypeScript | 19.2 |
| Build Tool | Vite | 7.3 |
| State Management | TanStack Query (React Query) | 5.90 |
| HTTP Client | Axios | 1.13 |
| UI Components | shadcn/ui + Tailwind CSS | 4.1 |
| Forms | React Hook Form + Zod | 7.48 |
| Authentication | ASP.NET Core Identity + JWT | 10.0 |
| Backend Testing | xUnit + Moq | 2.6 |
| Frontend Testing | Vitest + React Testing Library | 4.0 |
| Containerization | Docker + Docker Compose | - |
| API Documentation | OpenAPI / Scalar | 3.0 |

## 🚀 Getting Started

### Prerequisites

- **.NET 10 SDK** - [Download](https://dotnet.microsoft.com/download/dotnet/10.0)
- **Node.js 20+** - [Download](https://nodejs.org/)
- **Docker Desktop** - [Download](https://www.docker.com/products/docker-desktop) (optional, for containerized setup)
- **PostgreSQL 16** - (or use Docker)

### Quick Start with Docker

```bash
# Clone the repository
git clone https://github.com/FoggyFreek/bosdat-v2.git
cd bosdat-v2

# Start all services (PostgreSQL, API, Web)
docker-compose up -d

# Access the application
# Frontend: http://localhost:3000
# API: http://localhost:5000
# Scalar UI: http://localhost:5000/scalar/v1
```

### Local Development Setup

#### 1. Database Setup

```bash
# Start PostgreSQL with Docker
docker-compose up -d postgres

# Or install PostgreSQL 16 locally and create database:
createdb -U postgres bosdat
```

#### 2. Backend Setup

```bash
# Navigate to API project
cd src/BosDAT.API

# Restore dependencies
dotnet restore

# Apply database migrations
dotnet ef database update --project ../BosDAT.Infrastructure --startup-project .

# Run the API
dotnet run

# API available at: http://localhost:5000
# Scalar UI: http://localhost:5000/scalar/v1
```

#### 3. Frontend Setup

```bash
# Navigate to Web project
cd src/BosDAT.Web

# Install dependencies
npm install

# Start development server
npm run dev

# Frontend available at: http://localhost:5173
```

### Default Credentials

After initial setup:
- **Email:** `admin@bosdat.nl`
- **Password:** `Admin@123456`

**⚠️ Important:** Change this password immediately after first login!

## 📝 Development Commands

### Backend (from `bosdat-v2/` root)

```bash
# Build solution
dotnet build BosDAT.sln

# Run API (with hot reload)
dotnet run --project src/BosDAT.API

# Run all tests
dotnet test

# Create new migration
dotnet ef migrations add [MigrationName]  --project src/BosDAT.Infrastructure --startup-project src/BosDAT.API

# Apply migrations
dotnet ef database update --project src/BosDAT.Infrastructure --startup-project src/BosDAT.API

# Rollback to specific migration
dotnet ef database update [MigrationName] \
  --project src/BosDAT.Infrastructure \
  --startup-project src/BosDAT.API
```

### Frontend (from `src/BosDAT.Web/`)

```bash
# Install dependencies
npm install

# Start dev server with hot reload
npm run dev

# Build for production
npm run build

# Preview production build
npm run preview

# Run tests (watch mode)
npm run test

# Run tests (single run)
npm run test:run

# Run tests with coverage report
npm run test:coverage

# Lint code
npm run lint

# Format code
npm run format
```

### Docker Commands

```bash
# Start all services
docker-compose up -d

# Start with rebuild
docker-compose up --build -d

# Stop all services
docker-compose down

# View logs
docker-compose logs -f [service-name]

# Stop and remove volumes (⚠️ deletes database data)
docker-compose down -v
```

## 🗂️ Project Structure

```
bosdat-v2/
├── src/
│   ├── BosDAT.API/            # ASP.NET Core Web API (controllers, JWT, DI)
│   ├── BosDAT.Core/           # Domain layer — entities, interfaces, DTOs, enums
│   ├── BosDAT.Infrastructure/ # EF Core, repositories, services, email, migrations
│   ├── BosDAT.Worker/         # Background worker — email outbox, scheduled jobs
│   └── BosDAT.Web/            # React 19 frontend
│       └── src/
│           ├── features/      # Feature modules (auth, students, courses, …)
│           ├── components/    # Shared UI components (shadcn/ui)
│           ├── pages/         # Route-level components (lazy-loaded)
│           ├── services/      # Axios client + feature API modules
│           └── hooks/         # Custom React hooks
├── tests/
│   ├── BosDAT.API.Tests/      # Controller + service unit tests (xUnit + Moq)
│   ├── BosDAT.Core.Tests/     # Domain logic tests
│   └── BosDAT.Infrastructure.Tests/ # Repository integration tests
├── docker-compose.yml
└── BosDAT.sln
```

## 🔑 Core Concepts

### Course Blueprint → Lesson Generation

**The Heart of BosDAT v2:**

1. **Course = Template/Blueprint**
   - Defines: Schedule (day/time), Frequency pattern, Teacher, Students, Room
   - Example: "Piano Lessons - Every Tuesday 14:00 with John Smith"

2. **Automated Lesson Generation**
   - Algorithm creates scheduled lessons from course blueprints
   - Handles Weekly, Biweekly (with ISO 8601 week parity), Monthly frequencies
   - Automatically skips holidays
   - Prevents duplicate lessons

3. **Course Types**
   - **Individual:** Creates 1 lesson per enrolled student per date
   - **Group/Workshop:** Creates 1 lesson per date (all students attend together)

**Example:**

```
Course Blueprint:
├─ Name: "Piano Lessons"
├─ Teacher: John Smith
├─ Students: Alice, Bob (via Enrollments)
├─ Schedule: Tuesday 14:00-15:00
├─ Frequency: Weekly
├─ Type: Individual
└─ Date Range: Jan 1 - Mar 31, 2026

Generated Lessons:
├─ Jan 7 (Tue) 14:00 → Lesson with Alice + John
├─ Jan 7 (Tue) 14:00 → Lesson with Bob + John
├─ Jan 14 (Tue) 14:00 → Lesson with Alice + John
├─ Jan 14 (Tue) 14:00 → Lesson with Bob + John
└─ ... (continues weekly until Mar 31)
```

### Dynamic Pricing System

- **Version-Controlled Pricing:** Course types maintain pricing history
- **Age-Based Tiers:** Adult (18+) and child (<18) pricing
- **Cumulative Discounts:**
  - Family discount (multiple students from same billing contact)
  - Multi-course discount (student enrolled in multiple courses)
  - Custom enrollment discounts
- **Registration Fees:** One-time fee tracked via student ledger

### Student Ledger System

- **Purpose:** Track financial corrections outside normal invoicing
- **Entry Types:**
  - **Credit:** Amount owed to student (refunds, overpayments)
  - **Debit:** Amount owed by student (corrections, fees)
- **Statuses:** Open → PartiallyApplied → FullyApplied
- **Applications:** Link ledger entries to specific invoices for tracking

## 📊 Database Schema

PostgreSQL 16 with snake_case naming convention.

**Core Tables:**
- `students`, `teachers`, `courses`, `lessons`, `enrollments`
- `instruments`, `course_types`, `course_type_pricing_versions`
- `rooms`, `holidays`, `settings`
- `invoices`, `invoice_lines`, `payments`, `teacher_payments`
- `student_ledger_entries`, `student_ledger_applications`
- `audit_logs` (JSONB columns for change tracking)
- `email_outbox_messages` (transactional outbox for reliable email delivery)
- `refresh_tokens` (JWT token rotation)
- ASP.NET Identity tables (`asp_net_users`, `asp_net_roles`, etc.)

**Key Features:**
- UUID primary keys for entities
- Automatic timestamp management (CreatedAt, UpdatedAt)
- Automatic audit logging (all CRUD operations tracked)
- JSONB columns for flexible metadata
- Comprehensive foreign key relationships
- Strategic indexes for performance

## 🔐 Authentication & Authorization

### Flow

1. **Login:** `POST /api/auth/login` with email/password
2. **JWT Generation:**
   - Access token (1 hour expiration)
   - Refresh token (7 days, stored in database)
3. **Token Storage:** localStorage (client-side)
4. **Token Refresh:**
   - Automatic via axios interceptor on 401
   - Refresh endpoint creates new token pair
   - Old refresh token marked as revoked (rotation security)

### Roles & Policies

- **Roles:** Admin, Teacher, Staff, User
- **Policies:**
  - `AdminOnly` - Admin role required
  - `TeacherOrAdmin` - Teacher OR Admin role required
- **Controller Authorization:** `[Authorize]`, `[Authorize(Policy = "AdminOnly")]`

## 🧪 Testing

### Backend Testing (xUnit)

```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test tests/BosDAT.API.Tests

# Run with coverage
dotnet test /p:CollectCoverage=true
```

**Test Structure:**
- `BosDAT.API.Tests` - Integration tests for controllers
- `BosDAT.Core.Tests` - Unit tests for domain logic
- Specialized test helpers for lesson generation (`TestHelpers.cs`, `CourseBuilder`)

### Frontend Testing (Vitest + React Testing Library)

```bash
cd src/BosDAT.Web

# Watch mode (for development)
npm run test

# Single run (for CI)
npm run test:run

# Coverage report
npm run test:coverage
```

**Test Conventions:**
- Tests co-located with source in `__tests__/` folders
- Test files use `.test.tsx` extension
- Shared test utilities in `src/test/utils.tsx`
- Fresh `QueryClient` per test (prevents flaky tests)

## 🔑 Configuration & Secrets

Secrets are **never committed to git**. The two runtime environments each have their own mechanism:

| Runtime | Where secrets live | Loaded by |
|---------|-------------------|-----------|
| **Local dev** | `appsettings.Development.json` (gitignored) | .NET config pipeline automatically |
| **Docker** | `.env` (gitignored) | Docker Compose → environment variables |

---

### Local Development

`appsettings.Development.json` is gitignored in both `src/BosDAT.API/` and `src/BosDAT.Worker/`. These files are created on first run from the defaults already present in `appsettings.json`; you only need to fill in secrets.

**`src/BosDAT.API/appsettings.Development.json`**

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=bosdat;Username=bosdat;Password=bosdat_dev_password"
  },
  "JwtSettings": {
    "Secret": "YourSuperSecretKeyThatIsAtLeast32CharactersLong!"
  },
  "EmailSettings": {
    "Provider": "Console",
    "FromEmail": "noreply@bosdat.nl",
    "FromName": "BosDAT (dev)",
    "Brevo": {
      "ApiKey": ""
    }
  }
}
```

**`src/BosDAT.Worker/appsettings.Development.json`**

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=bosdat;Username=bosdat;Password=bosdat_dev_password"
  },
  "EmailSettings": {
    "Provider": "Console",
    "FromEmail": "noreply@bosdat.nl",
    "FromName": "BosDAT (dev)",
    "Brevo": {
      "ApiKey": ""
    }
  },
  "WorkerSettings": {
    "Credentials": {
      "Email": "worker@bosdat.nl",
      "Password": "Worker@123456"
    }
  }
}
```

> **Email in local dev:** `Provider` defaults to `"Console"` — emails are logged to stdout instead of sent. Set `Provider` to `"Brevo"` and fill in `ApiKey` to send real emails locally.

---

### Docker Setup

Copy `.env.example` to `.env` and fill in the required values:

```bash
cp .env.example .env
```

**`.env` reference:**

```bash
# ── Database ──────────────────────────────────────────────────────────────────
# Npgsql connection string
DB_CONNECTION_STRING=Host=postgres;Database=bosdat;Username=bosdat;Password=bosdat_dev_password

# ── JWT ───────────────────────────────────────────────────────────────────────
# Minimum 32 characters. Generate with: openssl rand -base64 32
JWT_SECRET=YourSuperSecretKeyThatIsAtLeast32CharactersLong!

# ── Worker ────────────────────────────────────────────────────────────────────
# Credentials used by BosDAT.Worker to authenticate against the API
WORKER_EMAIL=worker@bosdat.nl
WORKER_PASSWORD=YourWorkerPassword

# ── Email (Brevo) ─────────────────────────────────────────────────────────────
# "Console" logs emails to stdout (safe default). "Brevo" sends real emails.
EMAIL_PROVIDER=Console
EMAIL_FROM_ADDRESS=noreply@bosdat.nl
EMAIL_FROM_NAME=BosDAT

# Brevo transactional API key — required when EMAIL_PROVIDER=Brevo
# Get yours at: https://app.brevo.com/settings/keys/api
BREVO_API_KEY=your-brevo-api-key-here
```

Docker Compose maps these to .NET configuration automatically using the `__` separator convention (e.g. `EmailSettings__Brevo__ApiKey`). No code changes are needed to switch providers — only the `.env` values change.

---

### Enabling Brevo (real email sending)

1. Create a free account at [brevo.com](https://www.brevo.com)
2. Go to **Settings → API Keys → Create a new API key** (v3)
3. Copy the key
4. Set it in your config:
   - **Local dev:** add `"ApiKey": "your-key"` under `EmailSettings.Brevo` in `appsettings.Development.json`, and set `"Provider": "Brevo"`
   - **Docker:** set `BREVO_API_KEY=your-key` and `EMAIL_PROVIDER=Brevo` in `.env`

> **Note:** Brevo's free tier allows 300 emails/day which is sufficient for development and small deployments.

---

### Frontend (`.env.local`)

```bash
# Optional – Vite dev server proxies /api to localhost:5000 by default
VITE_API_URL=http://localhost:5000
```

## 🎨 UI Components

Built exclusively with **shadcn/ui** + **Tailwind CSS**.

**Available Components:**
- Forms: Input, Select, Checkbox, Radio, Textarea, DatePicker
- Feedback: Dialog, Alert, Toast, Badge, Progress
- Layout: Card, Separator, Tabs, Sheet (sidebar)
- Navigation: Button, DropdownMenu, NavigationMenu
- Data: Table, DataTable (with sorting/filtering/pagination)

## 📚 API Documentation

The API is self-documented via OpenAPI. When running locally:

| Interface | URL |
|-----------|-----|
| Scalar UI | http://localhost:5000/scalar/v1 |
| OpenAPI JSON | http://localhost:5000/openapi/v1.json |

## 🚧 Development Seeding

**Admin-only endpoints** for generating test data:

- `GET /api/admin/seeder/status` - Check seeding status
- `POST /api/admin/seeder/seed` - Generate demo data
- `POST /api/admin/seeder/reset` - Delete all seeded data
- `POST /api/admin/seeder/reseed` - Reset + Seed

**Generated Data:**
- Students with realistic Dutch names and addresses
- Teachers with instrument assignments
- Course types (Individual, Group, Workshop variations)
- Courses with realistic schedules
- Enrollments and generated lessons
- Invoices and student ledger entries

## 📈 Roadmap

### ✅ Completed
- Authentication & authorization (JWT, refresh token rotation)
- Account management & user invitations (email-based onboarding)
- Student & teacher management
- Course & enrollment management
- Automated lesson generation (with ISO 8601 week parity)
- Calendar views (week/day/month)
- Settings management (instruments, rooms, holidays)
- Audit logging
- Database seeding
- Duplicate detection (Levenshtein distance)
- Student ledger & transaction system
- Dynamic pricing with discounts
- Email infrastructure (outbox pattern, Brevo provider, Razor templates)

### 🚧 In Progress
- Invoice generation & PDF export
- Teacher payment calculations

### 📋 Planned
- Cancellation workflow
- Batch operations (bulk enrollment, bulk invoice generation)
- Data export (CSV, Excel)
- Dashboard analytics
- Student/parent portal

## 🤝 Contributing

This is a private project. For development guidelines, see:
- `.claude/rules/backend.md` - Backend coding standards
- `.claude/rules/frontend.md` - Frontend coding standards
- `.claude/rules/workflow.md` - TDD workflow & security checklist
- `.claude/CLAUDE.md` - AI assistant guidance

**Development Workflow:**
1. Create feature branch from `main`
2. Follow TDD (write tests first)
3. Implement feature (maintain 80%+ coverage)
4. Run code review (use code-reviewer agent)
5. Create pull request
6. Merge after approval

## 📄 License

Proprietary - All rights reserved

## 🔗 Links

- **Repository:** https://github.com/FoggyFreek/bosdat-v2
- **JIRA:** Cloud ID `e107cebe-73a2-4fb8-8fc8-7513953706dc`
- **Main Branch:** `main`

---

**Built with ❤️ for music schools**
