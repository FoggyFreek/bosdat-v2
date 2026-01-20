# bosDATv2 - Modern Music School Management System

## Overview
A modern web-based replacement for the NMI Access database, built with .NET 8 backend, React frontend, and PostgreSQL database.

## Current Implementation Status

### Completed Features
- **Authentication**: Login/logout with JWT tokens
- **Students Module**: List, detail view, create/edit forms with validation
- **Teachers Module**: List, detail view
- **Courses Module**: List view, enrollments
- **Schedule Module**: Weekly calendar view, lesson management
- **Testing Infrastructure**: Vitest + React Testing Library setup
- **Audit Logging**: Automatic tracking of all entity CRUD operations with user context

### In Progress
- Invoicing module
- Teacher payments
- Reports

## Technology Stack

| Layer | Technology |
|-------|------------|
| Backend | .NET 8 Web API (C#) |
| Frontend | React 18 + TypeScript + Vite |
| Database | PostgreSQL 16 |
| ORM | Entity Framework Core 8 |
| Auth | ASP.NET Core Identity + JWT |
| UI Components | Tailwind CSS + shadcn/ui |
| API Docs | Swagger/OpenAPI |
| Testing | Vitest + React Testing Library |
| Containerization | Docker + Docker Compose |

## Project Structure

```
bosdat-v2/
├── src/
│   ├── BosDAT.API/              # .NET Web API project
│   │   ├── Controllers/         # API endpoints
│   │   ├── Program.cs
│   │   └── appsettings.json
│   ├── BosDAT.Core/             # Domain models & interfaces
│   │   ├── Attributes/          # Custom attributes (e.g., SensitiveData)
│   │   ├── Entities/
│   │   ├── Interfaces/
│   │   └── DTOs/
│   ├── BosDAT.Infrastructure/   # EF Core, repositories
│   │   ├── Audit/               # Audit logging helpers
│   │   ├── Data/
│   │   ├── Repositories/
│   │   └── Services/
│   └── BosDAT.Web/              # React frontend
│       ├── src/
│       │   ├── components/       # Reusable UI components
│       │   │   ├── __tests__/    # Component tests
│       │   │   ├── ui/           # shadcn/ui components
│       │   │   └── *.tsx
│       │   ├── pages/            # Page components
│       │   │   ├── __tests__/    # Page tests
│       │   │   └── *.tsx
│       │   ├── context/          # React context providers
│       │   ├── services/         # API service layer
│       │   ├── test/             # Test utilities & setup
│       │   │   ├── setup.ts      # Global test setup
│       │   │   └── utils.tsx     # Test helpers & providers
│       │   ├── types/            # TypeScript type definitions
│       │   └── lib/              # Utility functions
│       ├── package.json
│       └── vite.config.ts
├── tests/
│   ├── BosDAT.API.Tests/
│   └── BosDAT.Core.Tests/
├── docker-compose.yml
└── README.md
```

## Database Schema (Modernized)

### Core Entities

```
students (was: Klanten)
├── id (UUID)
├── first_name, last_name, prefix
├── email (unique, required)
├── phone, phone_alt
├── address, postal_code, city
├── date_of_birth
├── gender
├── status (active, inactive, trial)
├── enrolled_at
├── billing_* (separate billing address)
├── auto_debit (boolean)
├── created_at, updated_at

teachers (was: Docenten)
├── id (UUID)
├── first_name, last_name, prefix
├── email (unique, login)
├── phone
├── address, postal_code, city
├── hourly_rate
├── is_active
├── role (teacher, admin, staff)
├── created_at, updated_at

instruments (was: Lessoorten - normalized)
├── id
├── name (Piano, Guitar, Drums, etc.)
├── category (string, percussion, vocal, keyboard)

lesson_types (was: Lessoorten)
├── id
├── instrument_id (FK)
├── name
├── duration_minutes (30, 40, 45, 60)
├── type (individual, group, workshop)
├── price_adult
├── price_child
├── max_students (for group lessons)
├── is_active

teacher_instruments (was: DocentLessoort)
├── teacher_id (FK)
├── instrument_id (FK)

rooms (was: Lokalen)
├── id
├── name
├── capacity
├── has_piano, has_drums, etc. (equipment flags)

courses (was: Cursussen)
├── id
├── teacher_id (FK)
├── lesson_type_id (FK)
├── room_id (FK)
├── day_of_week (0-6)
├── start_time (TIME)
├── end_time (TIME)
├── frequency (weekly, biweekly)
├── start_date, end_date
├── status (active, paused, completed)
├── is_workshop
├── is_trial

enrollments (was: Groepen)
├── id
├── student_id (FK)
├── course_id (FK)
├── enrolled_at
├── discount_percent
├── status (active, withdrawn)

lessons (was: Planning)
├── id
├── course_id (FK)
├── student_id (FK)
├── teacher_id (FK)
├── room_id (FK)
├── scheduled_date
├── start_time, end_time
├── status (scheduled, completed, cancelled, no_show)
├── cancellation_reason
├── is_invoiced
├── is_paid_to_teacher
├── notes

invoices (was: Nota)
├── id
├── invoice_number (formatted: NMI-2024-00001)
├── student_id (FK)
├── issue_date
├── due_date
├── subtotal, vat_amount, total
├── discount_amount
├── status (draft, sent, paid, overdue, cancelled)
├── paid_at
├── payment_method

invoice_lines (was: Notasamenstelling)
├── id
├── invoice_id (FK)
├── lesson_id (FK, optional)
├── description
├── quantity
├── unit_price
├── vat_rate
├── line_total

payments (was: KlantGeldVerkeer)
├── id
├── invoice_id (FK)
├── amount
├── payment_date
├── method (cash, bank, card)
├── reference
├── recorded_by (FK to users)

teacher_payments (was: Salaris)
├── id
├── teacher_id (FK)
├── period_month, period_year
├── lesson_count
├── total_minutes
├── hourly_rate
├── gross_amount
├── is_paid
├── paid_at

cancellations (was: Afmelding)
├── id
├── student_id (FK)
├── start_date, end_date
├── reason
├── status (pending, approved, rejected)

holidays (was: Vakanties)
├── id
├── name
├── start_date, end_date

settings (was: Konstanten)
├── key, value, type
├── Examples: vat_rate, child_age_limit, registration_fee, etc.

audit_logs (new - automatic audit trail)
├── id (UUID)
├── entity_name (Student, Invoice, etc.)
├── entity_id
├── action (Created, Updated, Deleted)
├── old_values (JSONB - previous state)
├── new_values (JSONB - new state)
├── changed_properties (JSONB - list of modified fields)
├── user_id, user_email (who made the change)
├── ip_address (client IP)
├── timestamp
```

## API Endpoints (RESTful)

### Authentication
- POST /api/auth/register
- POST /api/auth/login
- POST /api/auth/refresh
- POST /api/auth/logout

### Students
- GET/POST /api/students
- GET/PUT/DELETE /api/students/{id}
- GET /api/students/{id}/enrollments
- GET /api/students/{id}/invoices
- GET /api/students/{id}/lessons

### Teachers
- GET/POST /api/teachers
- GET/PUT/DELETE /api/teachers/{id}
- GET /api/teachers/{id}/availability
- GET /api/teachers/{id}/courses
- GET /api/teachers/{id}/payments

### Courses & Scheduling
- GET/POST /api/courses
- GET/PUT/DELETE /api/courses/{id}
- POST /api/courses/{id}/enroll
- GET /api/lessons
- GET/PUT /api/lessons/{id}
- POST /api/lessons/generate (bulk generate from courses)

### Invoicing
- GET/POST /api/invoices
- GET/PUT /api/invoices/{id}
- POST /api/invoices/{id}/send
- POST /api/invoices/{id}/pay
- GET /api/invoices/{id}/pdf

### Calendar & Schedule
- GET /api/calendar/week?date=2024-01-15
- GET /api/calendar/teacher/{id}
- GET /api/calendar/room/{id}

### Reports
- GET /api/reports/revenue?from=&to=
- GET /api/reports/teacher-hours?month=&year=
- GET /api/reports/student-attendance

## Frontend Pages

### Public
- Login page

### Dashboard
- Overview with key metrics (active students, upcoming lessons, unpaid invoices)
- Quick actions

### Students Module
- Student list with search/filter ✓
- Student detail page (info, enrollments, invoices, lesson history) ✓
- Add/edit student form ✓
  - Reusable `StudentForm` component for create/edit
  - Client-side validation (required fields, email format)
  - Routes: `/students/new`, `/students/:id/edit`

### Teachers Module
- Teacher list
- Teacher detail (info, courses, availability, payments)
- Availability calendar editor

### Schedule Module
- Weekly calendar view (by room, by teacher)
- Lesson detail modal
- Drag-and-drop rescheduling

### Courses Module
- Course list
- Course detail with enrolled students
- Add/edit course form

### Invoicing Module
- Invoice list with status filters
- Invoice detail/PDF preview
- Batch invoice generation
- Payment recording

### Settings
- Business info
- Pricing configuration
- Lesson types management
- Rooms management
- Holidays management
- User management

## Implementation Phases

### Phase 1: Foundation (Week 1-2)
1. Initialize .NET solution structure
2. Set up PostgreSQL with Docker
3. Create Entity Framework Core models and migrations
4. Implement basic CRUD for core entities (Students, Teachers, Instruments)
5. Set up React project with Vite + TypeScript
6. Implement authentication (ASP.NET Identity + JWT)
7. Create basic layout and navigation

### Phase 2: Core Features (Week 3-4)
1. Lesson types and rooms management
2. Course management (CRUD, scheduling)
3. Student enrollment system
4. Teacher assignment and availability
5. Weekly calendar view
6. Lesson generation from recurring courses

### Phase 3: Financial (Week 5-6)
1. Invoice generation system
2. Invoice PDF generation
3. Payment recording
4. Student balance tracking
5. Teacher payment/salary calculation
6. Basic financial reports

### Phase 4: Polish & Advanced (Week 7-8)
1. Cancellation handling
2. Holiday management
3. Email notifications
4. Dashboard with analytics
5. Bulk operations
6. Data export (CSV, Excel)
7. Testing and bug fixes

## Development Commands

### Prerequisites
- .NET 8 SDK
- Node.js 18+
- Docker & Docker Compose
- PostgreSQL 16 (or use Docker)

### Database Setup
```bash
# Start PostgreSQL with Docker
docker-compose up -d postgres

# Run migrations (from solution root)
cd src/BosDAT.API
dotnet ef migrations add InitialCreate --project ../BosDAT.Infrastructure
dotnet ef database update
```

### Backend Development
```bash
cd src/BosDAT.API
dotnet run
```
API will be available at: http://localhost:5000
Swagger UI: http://localhost:5000/swagger

### Frontend Development
```bash
cd src/BosDAT.Web
npm install
npm run dev
```
Frontend will be available at: http://localhost:5173

### Testing
```bash
# Backend tests
dotnet test

# Frontend tests (watch mode)
cd src/BosDAT.Web
npm run test

# Frontend tests (single run)
npm run test:run

# Frontend tests with coverage
npm run test:coverage
```

#### Frontend Testing Stack
- **Vitest** - Fast unit test runner (Vite-native)
- **React Testing Library** - Component testing utilities
- **@testing-library/user-event** - User interaction simulation
- **jsdom** - DOM environment for tests

#### Test File Conventions
- Tests are co-located with source code in `__tests__/` folders
- Test files use the `.test.tsx` extension
- Example: `src/components/__tests__/StudentForm.test.tsx`

### Production Build
```bash
# Build and run all services
docker-compose up --build
```

## Default Credentials

After initial setup, the following admin account is created:
- Email: admin@bosdat.nl
- Password: Admin@123456

**Important:** Change this password after first login!

## Environment Variables

### Backend (appsettings.json)
- `ConnectionStrings__DefaultConnection`: PostgreSQL connection string
- `JwtSettings__Secret`: JWT signing secret (min 32 characters)
- `JwtSettings__Issuer`: JWT issuer
- `JwtSettings__Audience`: JWT audience
- `Cors__AllowedOrigins`: Allowed frontend origins

### Frontend (.env)
- `VITE_API_URL`: Backend API URL

## Verification Plan
1. Create a test student, teacher, and course
2. Enroll student in course
3. Generate lessons for a month
4. Create and send invoice
5. Record payment
6. Verify calendar displays correctly
7. Run teacher payment report

## License
Proprietary - All rights reserved
