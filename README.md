# BosDAT v2 - Modern Music School Management System

> A comprehensive web-based application for managing music schools, built with .NET 8, React 19, and PostgreSQL 16.

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
│ • Swagger/OpenAPI Documentation                         │
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
│ • EF Core 8 + PostgreSQL 16                             │
│ • Repository Pattern + Unit of Work                     │
│ • Domain Services (Pricing, Scheduling, Ledger)         │
│ • Migrations & Seeding                                  │
│ • Automatic Audit Logging                               │
└─────────────────────────────────────────────────────────┘
```

### Bulletproof React Frontend

```
src/
├── api/              # API client types (ApiError, PaginatedResponse)
├── components/       # Shared UI components
│   └── ui/          # shadcn/ui primitives (Button, Card, Dialog, etc.)
├── features/        # 🎯 Feature-based organization
│   ├── auth/        # Authentication (login, context, types)
│   ├── students/    # Student management
│   ├── teachers/    # Teacher management
│   ├── courses/     # Course management
│   ├── lessons/     # Lesson management
│   ├── enrollments/ # Enrollment management
│   ├── invoices/    # Invoice management
│   └── settings/    # Settings management
├── context/         # App-wide React context providers
├── pages/           # Route-level page components (lazy-loaded)
├── services/        # API service layer (axios + interceptors)
├── hooks/           # Shared custom React hooks
├── lib/             # Utility functions
└── test/            # Test setup and utilities
```

**Key Frontend Patterns:**
- **Server State:** TanStack Query (React Query) with 5-minute cache
- **Client State:** React Context with memoized values
- **Code Splitting:** Route-level lazy loading (direct imports, no barrel exports)
- **Vendor Chunks:** Optimized bundle splitting for long-term caching
- **Styling:** shadcn/ui + Tailwind CSS exclusively
- **Testing:** Vitest + React Testing Library

## 🛠️ Tech Stack

| Layer | Technology | Version |
|-------|------------|---------|
| Backend Framework | .NET / C# | 8.0 / 12.0 |
| ORM | Entity Framework Core | 8.0 |
| Database | PostgreSQL | 16 |
| Frontend Framework | React + TypeScript | 19.2 |
| Build Tool | Vite | 7.3 |
| State Management | TanStack Query (React Query) | 5.90 |
| HTTP Client | Axios | 1.13 |
| UI Components | shadcn/ui + Tailwind CSS | 4.1 |
| Forms | React Hook Form + Zod | 7.48 |
| Authentication | ASP.NET Core Identity + JWT | 8.0 |
| Backend Testing | xUnit | 2.6 |
| Frontend Testing | Vitest + React Testing Library | 4.0 |
| Containerization | Docker + Docker Compose | - |
| API Documentation | Swagger / OpenAPI | 3.0 |

## 🚀 Getting Started

### Prerequisites

- **.NET 8 SDK** - [Download](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Node.js 18+** - [Download](https://nodejs.org/)
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
# Swagger: http://localhost:5000/swagger
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
# Swagger UI: http://localhost:5000/swagger
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
│   ├── BosDAT.API/                    # ASP.NET Core Web API
│   │   ├── Controllers/               # REST API endpoints
│   │   │   ├── AuthController.cs      # Authentication (login, refresh, register)
│   │   │   ├── StudentsController.cs  # Student CRUD + duplicates + ledger
│   │   │   ├── TeachersController.cs  # Teacher CRUD
│   │   │   ├── CoursesController.cs   # Course CRUD + enrollments
│   │   │   ├── LessonsController.cs   # Lesson CRUD + generation (single/bulk)
│   │   │   ├── EnrollmentsController.cs # Enrollment + pricing
│   │   │   ├── InvoicesController.cs  # Invoice generation + PDF
│   │   │   ├── CalendarController.cs  # Schedule views
│   │   │   ├── SeederController.cs    # Development seeding
│   │   │   └── ...
│   │   ├── Program.cs                 # Application entry point & configuration
│   │   └── appsettings.json           # Configuration (DB, JWT, CORS)
│   │
│   ├── BosDAT.Core/                   # Domain Layer (no dependencies)
│   │   ├── Entities/                  # Domain models
│   │   │   ├── BaseEntity.cs          # Base with Id, CreatedAt, UpdatedAt
│   │   │   ├── Student.cs
│   │   │   ├── Teacher.cs
│   │   │   ├── Course.cs
│   │   │   ├── Lesson.cs
│   │   │   ├── Enrollment.cs
│   │   │   ├── Invoice.cs
│   │   │   └── ...
│   │   ├── Interfaces/                # Abstractions
│   │   │   ├── IRepository.cs         # Generic repository
│   │   │   ├── IUnitOfWork.cs         # Transaction coordinator
│   │   │   ├── IAuthService.cs
│   │   │   └── ...
│   │   ├── DTOs/                      # Data Transfer Objects
│   │   │   ├── AuthDtos.cs
│   │   │   ├── StudentDto.cs
│   │   │   └── ...
│   │   ├── Enums/                     # Domain enumerations
│   │   │   ├── WeekParity.cs          # Odd/Even/All (ISO 8601)
│   │   │   ├── CourseFrequency.cs     # Weekly/Biweekly/Monthly
│   │   │   ├── StudentStatus.cs
│   │   │   └── ...
│   │   └── Utilities/
│   │       └── IsoWeekHelper.cs       # ISO 8601 week calculations
│   │
│   ├── BosDAT.Infrastructure/         # Data Access Layer
│   │   ├── Data/
│   │   │   └── ApplicationDbContext.cs # EF Core DbContext + seeding
│   │   ├── Repositories/              # Repository implementations
│   │   │   ├── Repository.cs          # Generic implementation
│   │   │   ├── StudentRepository.cs
│   │   │   ├── CourseRepository.cs
│   │   │   └── ...
│   │   ├── Services/                  # Domain services
│   │   │   ├── AuthService.cs         # JWT generation, refresh tokens
│   │   │   ├── DuplicateDetectionService.cs # Fuzzy matching
│   │   │   ├── EnrollmentPricingService.cs # Dynamic pricing
│   │   │   └── ...
│   │   ├── Seeding/                   # Development data generation
│   │   │   ├── DatabaseSeeder.cs
│   │   │   ├── StudentDataGenerator.cs
│   │   │   └── ...
│   │   ├── Audit/
│   │   │   └── AuditEntry.cs          # Change tracking helpers
│   │   └── Migrations/                # EF Core migrations
│   │
│   └── BosDAT.Web/                    # React Frontend
│       ├── src/
│       │   ├── api/                   # API client types
│       │   │   └── types.ts
│       │   ├── components/            # Shared components
│       │   │   ├── ui/                # shadcn/ui primitives
│       │   │   ├── Layout.tsx
│       │   │   └── LoadingFallback.tsx
│       │   ├── features/              # Feature modules
│       │   │   ├── auth/
│       │   │   │   ├── components/    # Login forms
│       │   │   │   ├── context/       # AuthContext
│       │   │   │   └── types.ts
│       │   │   ├── students/
│       │   │   │   ├── components/    # StudentForm, StudentList, etc.
│       │   │   │   └── types.ts
│       │   │   └── ...
│       │   ├── pages/                 # Route components (lazy-loaded)
│       │   │   ├── LoginPage.tsx
│       │   │   ├── DashboardPage.tsx
│       │   │   ├── StudentsPage.tsx
│       │   │   └── ...
│       │   ├── context/               # App-wide providers
│       │   │   └── FormDirtyContext.tsx
│       │   ├── services/              # API service layer
│       │   │   ├── api.ts             # Axios client + interceptors
│       │   │   ├── studentsApi.ts
│       │   │   └── ...
│       │   ├── hooks/                 # Custom hooks
│       │   ├── lib/                   # Utils (cn, etc.)
│       │   ├── test/                  # Test utilities
│       │   │   ├── setup.ts
│       │   │   └── utils.tsx
│       │   ├── App.tsx                # Routes + providers
│       │   └── main.tsx               # Entry point
│       ├── package.json
│       ├── vite.config.ts             # Vite configuration
│       ├── tsconfig.json              # TypeScript configuration
│       └── tailwind.config.js         # Tailwind CSS configuration
│
├── tests/
│   ├── BosDAT.API.Tests/              # API integration tests
│   │   └── Controllers/
│   │       ├── LessonsController/     # Lesson generation tests
│   │       │   ├── TestHelpers.cs     # Shared test utilities
│   │       │   ├── FrequencyTests.cs
│   │       │   ├── HolidaySkippingTests.cs
│   │       │   ├── WeekParityTests.cs
│   │       │   └── ...
│   │       └── ...
│   └── BosDAT.Core.Tests/             # Domain logic tests
│
├── docker-compose.yml                  # Container orchestration
├── BosDAT.sln                          # Solution file
├── README.md                           # This file
└── CLAUDE.md                           # AI assistant guidance
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

## 🌍 Environment Configuration

### Backend (`appsettings.json`)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=bosdat;Username=bosdat;Password=your-password"
  },
  "JwtSettings": {
    "Secret": "your-minimum-32-character-secret-key-here",
    "Issuer": "BosDAT.API",
    "Audience": "BosDAT.Web",
    "AccessTokenExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 7
  },
  "Cors": {
    "AllowedOrigins": ["http://localhost:3000", "http://localhost:5173"]
  },
  "AdminSettings": {
    "DefaultPassword": "Admin@123456"
  }
}
```

### Frontend (`.env`)

```bash
# Optional - defaults to empty (proxy handles it in dev)
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

**Swagger UI available at:** `http://localhost:5000/swagger`

### Key Endpoints

**Authentication:**
- `POST /api/auth/login` - User login
- `POST /api/auth/refresh` - Refresh access token
- `POST /api/auth/register` - User registration
- `POST /api/auth/logout` - Logout (revoke refresh token)

**Students:**
- `GET /api/students` - List all students (with filters)
- `POST /api/students` - Create student
- `GET /api/students/{id}` - Get student details
- `PUT /api/students/{id}` - Update student
- `DELETE /api/students/{id}` - Delete student
- `POST /api/students/check-duplicates` - Duplicate detection
- `GET /api/students/{id}/ledger` - Student ledger entries

**Courses:**
- `GET /api/courses` - List courses
- `POST /api/courses` - Create course
- `GET /api/courses/{id}` - Get course with enrollments
- `PUT /api/courses/{id}` - Update course
- `POST /api/courses/{id}/enroll` - Enroll student

**Lessons:**
- `GET /api/lessons` - List lessons (with filters)
- `POST /api/lessons/generate` - Generate lessons for single course
- `POST /api/lessons/generate-bulk` - Generate for all active courses
- `PUT /api/lessons/{id}` - Update lesson (status, notes, etc.)

**Calendar:**
- `GET /api/calendar/week?date=2024-01-15` - Weekly view
- `GET /api/calendar/teacher/{id}` - Teacher schedule
- `GET /api/calendar/room/{id}` - Room schedule

**Settings:**
- `GET /api/settings` - Get all settings
- `PUT /api/settings/{key}` - Update setting

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
- Authentication & authorization
- Student & teacher management
- Course & enrollment management
- Automated lesson generation (with ISO 8601 week parity)
- Calendar views (week/day/month)
- Settings management
- Audit logging
- Database seeding
- Duplicate detection
- Student ledger system
- Dynamic pricing with discounts

### 🚧 In Progress
- Invoice generation & PDF export
- Teacher payment calculations
- Advanced reporting

### 📋 Planned
- Email notifications (lesson reminders, invoice sent)
- Cancellation workflow
- Batch operations (bulk enrollment, bulk invoice generation)
- Data export (CSV, Excel)
- Dashboard analytics
- Teacher availability calendar editor
- Student/parent portal

## 🤝 Contributing

This is a private project. For development guidelines, see:
- `.claude/rules/coding.md` - Coding standards
- `.claude/rules/testing.md` - Testing requirements
- `.claude/rules/security.md` - Security checklist
- `CLAUDE.md` - AI assistant guidance

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
