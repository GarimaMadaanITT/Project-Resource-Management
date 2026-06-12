# PRM Project — Complete Documentation

**Project:** Project & Resource Management (PRM) Tool  
**Course:** Learn & Code — Final Project  
**Stack:** .NET 8, Clean Architecture, ASP.NET Core Web API, EF Core, Neon PostgreSQL  
**Last updated:** June 2026  

This document consolidates the **Business Requirements**, **technical decisions**, **implementation progress**, and **guidance from our development conversations** through Phase 6.

---

## Table of Contents

1. [Project Overview](#1-project-overview)
2. [Conversation Summary & Key Decisions](#2-conversation-summary--key-decisions)
3. [Implementation Phases](#3-implementation-phases)
4. [Architecture](#4-architecture)
5. [UML Diagram Mapping](#5-uml-diagram-mapping)
6. [Database (Neon PostgreSQL)](#6-database-neon-postgresql)
7. [Configuration & Secrets](#7-configuration--secrets)
8. [Authentication](#8-authentication)
9. [API Reference](#9-api-reference)
10. [Seed Data & Test Credentials](#10-seed-data--test-credentials)
11. [Project Structure (Files & Folders)](#11-project-structure-files--folders)
12. [Design Patterns & SOLID (So Far)](#12-design-patterns--solid-so-far)
13. [How to Run](#13-how-to-run)
14. [Pending Work](#14-pending-work)
15. [Enterprise Validation (Phase 4.5)](#15-enterprise-validation-phase-45)
16. [Related Documents](#16-related-documents)

---

## 1. Project Overview

The PRM Tool replaces spreadsheet-driven resource planning for an IT services company. It tracks:

- **Employees**, skills, bench/allocated status  
- **Projects**, milestones, health  
- **Allocations** (utilisation %, date ranges)  
- **Timesheets** with activity tags  
- **AI** skill matching and project risk summaries (planned)  
- **Background scheduler** for utilisation, health, and missed-timesheet detection  

### User Roles (from BRD)

| Role | Responsibilities |
|------|------------------|
| **Admin** | Users, employees, skills, projects, company allocations, system settings |
| **Manager** | Team dashboard, allocation, project health, team timesheets, AI assistant |
| **Employee** | Submit timesheets, view own allocations/history |

### Deliverable Scope (Agreed)

- **Now:** REST API backend + console client — **Phases 1–9 complete**
- **BRD source:** [docs/BRD.md](BRD.md) (copied from `PRM_BRD_V4.md`)

---

## 2. Conversation Summary & Key Decisions

### Initial analysis

- Reviewed the full BRD and six UML sequence diagrams (Auth, Resource Allocation, Timesheet, AI Skill Match, AI Risk Summary, Scheduler).  
- Mapped generic diagram names to PRM-specific services (e.g. Scheduler jobs → utilisation, health, missed timesheets).  
- Noted BRD has **no self-registration** — only Admin creates users (Auth diagram’s register/email-verify flow does **not** apply).

### Technology choices

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Language | **C# / .NET 8** | User preference; aligns with assignment |
| Architecture | **Clean Architecture** (4 projects) | Separation of concerns; testable; assignment-ready |
| Database | **Neon PostgreSQL** | User chose Neon over SQLite/SQL Server; production-like, cloud-hosted |
| ORM | **EF Core 8 + Npgsql** | Standard .NET stack; migrations to Neon |
| Auth | **JWT Bearer** | Stateless REST; role claims Admin/Manager/Employee |
| Client | **Deferred** | Backend first; CLI vs web TBD after Phase 9 |

### Phase plan (agreed workflow)

We implement **one phase at a time**, verify, then proceed:

1. Environment & scaffold  
2. Neon DB connection, schema, seed  
3. Authentication  
4. Admin APIs ✅  
5. Manager APIs ✅  
6. Employee APIs ✅  
7. Background scheduler ✅  
8. AI features ✅  
9. Backend + console completion — tests, README, SOLID docs ✅  

No fixed timeline was enforced — focus is sequential quality.

### Logout discussion

- **BRD requires** Logout on all role menus.  
- **Not yet implemented** on the API.  
- **JWT behaviour:** Logout is primarily **client-side** (discard token). Optional server endpoint + token blacklist can be added later.  
- Console client (future) will clear JWT and return to login screen.

### Security note

- Neon connection string was shared in chat during setup — **rotate password** in Neon Console if needed.  
- Secrets live in **.NET User Secrets only** — never committed to git.

---

## 3. Implementation Phases

| Phase | Status | Summary |
|-------|--------|---------|
| **1** | ✅ Complete | Solution scaffold, Clean Architecture, Swagger, `/health`, git, BRD copy |
| **2** | ✅ Complete | Domain entities, EF Core, Neon migrations, seed data, exception middleware |
| **3** | ✅ Complete | Login, JWT, change-password, role policies, force-password middleware |
| **4** | ✅ Complete | All Admin BRD APIs (employees, users, projects, allocations, settings) |
| **4.5** | ✅ Complete | Enterprise validation layer, HTTP status codes, unit + integration tests |
| **5** | ✅ Complete | Manager dashboard, allocation, projects, team timesheets |
| **6** | ✅ Complete | Employee timesheets, allocations, reminders |
| **7** | ✅ Complete | Background scheduler + audit logging (utilisation, health, missed timesheets) |
| **8** | ✅ Complete | AI skill match + risk summary (Gemini/Groq + deterministic fallback) |
| **9** | ✅ Complete | Console client (all BRD screens), SOLID/docs, 159 tests |

---

## 4. Architecture

### Clean Architecture layers

```
┌─────────────────────────────────────────┐
│              Prm.Api                    │  Controllers, Middleware, Program.cs
├─────────────────────────────────────────┤
│         Prm.Infrastructure              │  EF Core, Repositories, JWT, BCrypt
├─────────────────────────────────────────┤
│         Prm.Application                 │  Services, DTOs, Interfaces
├─────────────────────────────────────────┤
│           Prm.Domain                    │  Entities, Enums, DomainException
└─────────────────────────────────────────┘
                    │
                    ▼
            Neon PostgreSQL (neondb)
```

**Dependency rule:** Domain ← Application ← Infrastructure ← Api  

Domain has **zero** references to EF Core, HTTP, or JWT.

### Request flow (example: Admin list employees)

```
Client → AdminEmployeesController
      → AdminEmployeeService (Application)
      → EmployeeRepository (Infrastructure)
      → PrmDbContext → Neon PostgreSQL
```

### Middleware pipeline

1. `GlobalExceptionMiddleware` — RFC 7807 ProblemDetails  
2. `UseAuthentication` — JWT validation  
3. `ForcePasswordChangeMiddleware` — blocks protected routes if `force_password_change=true`  
4. `UseAuthorization` — role policies  

---

## 5. UML Diagram Mapping

Your sequence diagrams were adapted as follows:

| UML / Diagram | PRM Implementation |
|---------------|-------------------|
| **User / Auth** | `AuthController` → `AuthService` → `UserRepository` (login + change password only) |
| **Resource Allocation** | Planned Phase 5: `AllocationService` with ≤100% validation |
| **Timesheet / Task Builder** | `EmployeeTimesheetService` + `TimesheetValidator` + `TimesheetBuilder` + `ActivityTagCatalog` |
| **AI Skill Match** | `ManagerAiController` → `ManagerAiService` → capacity pre-filter → `ILlmCompletionService` |
| **AI Risk Summary** | `ManagerAiService` collects milestone/timesheet facts → `ILlmCompletionService` |
| **Scheduler** | `PrmSchedulerHostedService` → `SchedulerOrchestrator` → utilisation recompute, project health recompute, missed-timesheet detection (audit-only) |

---

## 6. Database (Neon PostgreSQL)

### Connection

- **Provider:** Neon (serverless PostgreSQL)  
- **Database name:** `neondb`  
- **Use pooler endpoint** (`-pooler` in hostname) for app connections  
- **SSL:** Required  

### Tables (11)

| Table | Purpose |
|-------|---------|
| `users` | Login accounts, roles, password hash |
| `employees` | Profiles linked to users; `manager_id` for team hierarchy |
| `skills` | Master skill catalog |
| `employee_skills` | Skills + proficiency per employee |
| `projects` | Projects with manager, status, health |
| `milestones` | Project milestones |
| `allocations` | Employee ↔ project assignments |
| `timesheets` | Weekly timesheet headers |
| `timesheet_entries` | Hours + activity tags per project |
| `system_settings` | LLM key, provider, scheduler interval, max weekly hours |
| `audit_logs` | Change-only audit trail (entity, action, JSON snapshots, actor, source) |

### Migrations

- Migrations: `20260606103535_InitialCreate`, `20260610091632_AddAuditLogs`  
- Location: `Server/Prm.Infrastructure/Migrations/`  
- Applied automatically on API startup via `ApplyMigrationsAndSeedAsync()`

See also: [NEON_SETUP.md](NEON_SETUP.md)

---

## 7. Configuration & Secrets

### User Secrets (`Server/Prm.Api`, id: `prm-api-local-dev`)

**Never commit these values.**

```powershell
cd Server/Prm.Api

# Neon (convert URI to key-value form)
dotnet user-secrets set "ConnectionStrings:Default" "Host=YOUR_HOST;Database=neondb;Username=YOUR_USER;Password=YOUR_PASSWORD;SSL Mode=Require;Trust Server Certificate=true"

# JWT (minimum 32 characters for Key)
dotnet user-secrets set "Jwt:Key" "YourSecretKeyAtLeast32CharactersLong!"
dotnet user-secrets set "Jwt:Issuer" "PrmApi"
dotnet user-secrets set "Jwt:Audience" "PrmClient"
```

### appsettings.json

Contains **empty placeholders** only — real values come from User Secrets.

---

## 8. Authentication

### Flow

1. `POST /api/auth/login` — validate credentials (BCrypt), return JWT + `forcePasswordChange`  
2. If `forcePasswordChange: true` — call `POST /api/auth/change-password` before other APIs  
3. Use JWT in header: `Authorization: Bearer {token}`  
4. Swagger: click **Authorize** and paste `Bearer {token}`  

### JWT claims

| Claim | Content |
|-------|---------|
| `sub` | User ID |
| `unique_name` | Username |
| `role` | Admin / Manager / Employee |
| `force_password_change` | true / false |

### Role policies

- `AdminOnly` — Admin role  
- `ManagerOnly` — Manager role  
- `EmployeeOnly` — Employee role  

### Password rules (BRD)

- Minimum 8 characters  
- At least one uppercase letter  
- At least one number  

Implemented in `PasswordValidator` (`Prm.Application/Common/PasswordValidator.cs`).

### Logout

**Not implemented yet.** See [Pending Work](#14-pending-work).

---

## 9. API Reference

### Public / health

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/health` | Health check (includes DB) |
| GET | `/api/database/status` | Neon connection + row counts |

### Auth

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| POST | `/api/auth/login` | Public | Login → JWT |
| POST | `/api/auth/change-password` | JWT | Change password → new JWT |
| GET | `/api/auth/me` | JWT | Current user |
| GET | `/api/auth/admin-check` | Admin | Role test |
| GET | `/api/auth/manager-check` | Manager | Role test |
| GET | `/api/auth/employee-check` | Employee | Role test |

### Admin (Phase 4) — requires Admin JWT

#### Employees — `/api/admin/employees`

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/admin/employees` | List (`?department=&status=Bench\|Allocated`) |
| PUT | `/api/admin/employees/{id}` | Update department |
| POST | `/api/admin/employees/{id}/deactivate` | Deactivate; end allocations; block login |
| GET | `/api/admin/employees/{id}/skills` | List skills |
| POST | `/api/admin/employees/{id}/skills` | Add skill |
| PUT | `/api/admin/employees/{id}/skills/{skillId}` | Update proficiency |
| DELETE | `/api/admin/employees/{id}/skills/{skillId}` | Remove skill |
| PUT | `/api/admin/employees/{id}/assign-manager` | Assign manager by manager **user** ID |

#### Users — `/api/admin/users`

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/admin/users` | List all users |
| POST | `/api/admin/users` | Create user (Admin/Manager/Employee) |
| POST | `/api/admin/users/{id}/reset-password` | Reset temp password |
| POST | `/api/admin/users/{id}/deactivate` | Deactivate |
| POST | `/api/admin/users/{id}/reactivate` | Reactivate |

**Create user body example:**

```json
{
  "fullName": "New Manager",
  "email": "new.manager@techserve.com",
  "username": "new.manager",
  "temporaryPassword": "Manager@99",
  "role": "Manager",
  "department": "Delivery"
}
```

#### Projects — `/api/admin/projects`

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/admin/projects` | List projects |
| POST | `/api/admin/projects` | Create project |
| PUT | `/api/admin/projects/{id}` | Update project |
| GET | `/api/admin/projects/{id}/milestones` | List milestones |
| POST | `/api/admin/projects/{id}/milestones` | Add milestone |
| PUT | `/api/admin/projects/{id}/milestones/{milestoneId}/status` | Update milestone status |

#### Allocations — `/api/admin/allocations`

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/admin/allocations` | Active allocations (`?employeeId=&projectId=`) |

#### Settings — `/api/admin/settings`

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/admin/settings` | Get settings (API key masked) |
| PUT | `/api/admin/settings` | Update LLM provider/key, scheduler interval, max hours |

#### Audit logs — `/api/admin/audit-logs`

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/admin/audit-logs` | Paginated audit trail (`?page=&pageSize=&entityName=&action=&source=&from=&to=`) |

Write operations across Admin, Manager, and Employee APIs record change-only JSON snapshots (`OldValue`/`NewValue`). Scheduler jobs write audits with `Source=Scheduler` and `PerformedByUserId=null`.

### Employee (Phase 6) — requires Employee JWT

#### Timesheets — `/api/employee/timesheets`

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/employee/timesheets/activity-tags` | Predefined BRD activity tags + `AllowsCustomOther` |
| GET | `/api/employee/timesheets` | Last 12 weeks history (Submitted / Missed / Current) |
| GET | `/api/employee/timesheets/{weekStart}` | Week detail (`weekStart` must be Monday, `yyyy-MM-dd`) |
| POST | `/api/employee/timesheets` | Submit timesheet for a week (201 Created) |

**Submit timesheet body example:**

```json
{
  "weekStart": "2026-05-11",
  "entries": [
    {
      "projectId": 2,
      "hours": 18,
      "activityTags": ["Backend API Development", "Bug Fixing"]
    }
  ]
}
```

> Use **project ID** from `/api/employee/allocations` or Admin projects — not allocation ID.

**Phase 6 business rules:**

- Week start must be **Monday (UTC)**; cannot submit a **future** week.
- Duplicate submit for the same employee + week → **409 Conflict**.
- Hours capped per project from allocation utilisation % and `MaxWeeklyHours` setting.
- Activity tags required when hours > 0; predefined catalog + custom text when `"Other"` is selected.
- **Missed** weeks computed on read (no DB row + employee had allocations that week).
- History window: **12 weeks** (`ValidationConstants.TimesheetHistoryWeeks`).

#### Allocations — `/api/employee/allocations`

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/employee/allocations` | **Active** allocations only; status always `"Active"` |

#### Reminders — `/api/employee/reminders`

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/employee/reminders` | Reminder if **previous completed week** timesheet not submitted |

Previous completed week = `GetCurrentWeekStartUtc().AddDays(-7)`.

### AI (Phase 8) — requires Manager JWT

| Method | Route | Description |
|--------|-------|-------------|
| POST | `/api/ai/skill-match` | Natural-language team resource search |
| GET | `/api/ai/risk-summary/{projectId}` | AI project risk paragraph |

**Skill match body example:**

```json
{
  "projectId": 1,
  "requirement": "Need a Java developer with microservices experience for 20 hrs per week"
}
```

Capacity pre-filter runs before LLM. Without an API key, deterministic offline ranking is used.

---

## 10. Seed Data & Test Credentials

Seeded on first run when `users` table is empty (`DataSeeder.cs`).

### Bootstrap admin (BRD requirement)

| Field | Value |
|-------|-------|
| Username | `admin` |
| Password | `Admin@1234` (change on first login) |
| Role | Admin |
| forcePasswordChange | true |

> If you changed the password during testing, use your new password (e.g. `Admin@5678`).

### Other seeded accounts

| Username | Password | Role | Notes |
|----------|----------|------|-------|
| `ankit.shah` | `Manager@1234` | Manager | Has employee profile |
| `neha.joshi` | `Manager@1234` | Manager | |
| `rohan.verma` | `Manager@1234` | Manager | |
| `ravi.kumar` | `Employee@1234` | Employee | Under Ankit’s team |
| `anil.mehta` | `Employee@1234` | Employee | |
| `sara.khan` | `Employee@1234` | Employee | |
| `dev.patel` | `Employee@1234` | Employee | |
| `priya.sharma` | `Employee@1234` | Employee | **Inactive** |

### Seeded projects

Alpha Portal, Beta CRM, Gamma Rewrite, Delta Migrate — with milestones, allocations, and a sample timesheet for **ravi.kumar** (week starting **2026-05-04**, Monday).

### System settings defaults

- LLM Provider: Gemini  
- Scheduler interval: 4 hours  
- Max weekly hours: 40  

---

## 11. Project Structure (Files & Folders)

```
c:\lncFinalAssignment\
├── Prm.sln
├── README.md
├── .gitignore
├── docs/
│   ├── BRD.md                      Business requirements (full BRD)
│   ├── NEON_SETUP.md                 Neon connection guide
│   └── PROJECT_DOCUMENTATION.md      This file
├── src/
│   ├── Prm.Domain/
│   │   ├── Common/                   AuditableEntity
│   │   ├── Entities/                 User, Employee, Project, etc.
│   │   ├── Enums/                    UserRole, ProjectStatus, etc.
│   │   └── Exceptions/               DomainException
│   ├── Prm.Application/
│   │   ├── Common/                   AuthConstants, ValidationConstants, ErrorMessages, PasswordValidator
│   │   ├── Validation/               Guards and validators (StringGuard, EntityGuard, etc.)
│   │   ├── Services/Shared/          AccountDeactivationService
│   │   └── Services/Admin/           Facades + specialized employee/user services
│   ├── Prm.Infrastructure/
│   │   ├── Auth/                     JwtSettings, JwtTokenService
│   │   ├── Security/                 BcryptPasswordHasher
│   │   ├── Persistence/              PrmDbContext, Configurations
│   │   ├── Persistence/Seeding/      DataSeeder
│   │   ├── Persistence/Migrations/     EF Core migrations
│   │   ├── Repositories/             User, Employee, Project, etc.
│   │   └── DependencyInjection.cs
│   └── Prm.Api/
│       ├── Controllers/              Auth, Database
│       ├── Controllers/Admin/        Admin* controllers
│       ├── Middleware/               GlobalException, ForcePasswordChange
│       ├── Program.cs
│       ├── SwaggerConfiguration.cs
│       └── appsettings.json
└── tests/
    └── Prm.Tests/
        ├── DomainEntityTests.cs
        └── PasswordValidatorTests.cs
```

---

## 12. Design Patterns & SOLID (So Far)

See also [CLEAN_CODE_STANDARDS.md](./CLEAN_CODE_STANDARDS.md) for enforced coding rules and PR checklist.

Document these for assignment submission as phases complete.

| Pattern / Principle | Where |
|-------------------|-------|
| **Repository** | `IUserRepository`, `IEmployeeRepository`, etc. in Application; implementations in Infrastructure |
| **Dependency Inversion (DIP)** | Controllers → service interfaces; Infrastructure implements interfaces |
| **Single Responsibility (SRP)** | Separate admin services per domain area |
| **Separation of Concerns** | Domain has no infrastructure references |
| **Fail Fast** | `DomainException` + validators before DB writes |
| **Strategy** | `GeminiLlmProvider` / `GroqLlmProvider` via `LlmCompletionService` (Phase 8) |

---

## 13. How to Run

```powershell
# From repo root
dotnet restore
dotnet build
dotnet test
dotnet run --project Server/Prm.Api
```

- **Swagger:** URL shown in console (e.g. `https://localhost:7xxx/swagger`)  
- **Health:** `GET /health`  
- **DB status:** `GET /api/database/status`  

### Login test body

```json
POST /api/auth/login
{
  "username": "admin",
  "password": "Admin@1234"
}
```

---

## 14. Pending Work

All BRD phases are implemented. Optional future enhancements:

| Feature | Notes |
|---------|-------|
| **Logout API** | Client-side JWT discard satisfies BRD; optional token blacklist |
| **Web frontend** | Optional per BRD — not required |
| **Live LLM in dev** | Set Gemini/Groq API key in Admin → System Configuration |

---

## 15. Enterprise Validation (Phase 4.5)

Centralized validators live in `Server/Prm.Application/Validation/`. Services enforce BRD business rules before any write.

### HTTP status codes (RFC 7807 ProblemDetails)

| Code | Exception | Use |
|------|-----------|-----|
| **400** | `DomainException` | Validation / business rule violation |
| **401** | `UnauthorizedAccessException` | Invalid login, inactive account |
| **403** | `ForbiddenException` | Self-deactivation, last active Admin |
| **404** | `KeyNotFoundException` | Missing resource |
| **409** | `ConflictException` | Duplicate username, email, employee skill, or timesheet week |

### Business rules enforced

| Area | Rules |
|------|-------|
| **Employees** | Inactive employees cannot receive manager assignment or skill changes; employee list shows **active only**; status derived from current utilisation |
| **Managers** | Inactive managers cannot be assigned to projects or as reporting managers; deactivation blocked if active team members or active projects remain |
| **Deactivation** | Ends active allocations; preserves historical `manager_id`; reactivate via **users** endpoint only (`POST /api/admin/users/{id}/reactivate`) |
| **Projects** | Name required; manager must exist, be role Manager, and be active; `StartDate < EndDate`; `TotalStoryPoints >= 0` |
| **Milestones** | Title required; due date within project dates; sum of milestone SP ≤ project total SP |
| **Users** | All create fields mandatory + email format; duplicate username/email rejected; cannot deactivate self or last Admin |
| **Allocations** | `AllocationGuard` blocks inactive employees; employee view shows **active only** |
| **Timesheets** | Monday week start (UTC); no future weeks; no duplicate submit; allocation-scoped projects; per-project and total hour caps; activity tags when hours > 0; **Missed** status computed on read |

### ID conventions

| Route | ID type |
|-------|---------|
| `/api/admin/employees/{id}` | **Employee ID** |
| `/api/admin/users/{id}` | **User ID** |
| `PUT .../assign-manager` body `managerUserId` | **Manager user ID** |

### Tests

```powershell
dotnet test                              # 75 unit + 84 integration (159 total)
dotnet test tests/Prm.Tests              # Unit tests (validators, guards, mocked services)
dotnet test tests/Prm.Tests.Integration  # API integration tests (SQLite in-memory; stop running API first)
```

---

## 16. Related Documents

| Document | Location | Purpose |
|----------|----------|---------|
| Business Requirements | [docs/BRD.md](BRD.md) | Full BRD — screens, rules, roles |
| Neon setup | [docs/NEON_SETUP.md](NEON_SETUP.md) | Connection string & migrations |
| Quick start | [README.md](../README.md) | Build, run, phase status |
| UML diagrams | Provided in chat / Cursor assets | Sequence flows for Auth, Allocation, Timesheet, AI, Scheduler |
| API test cases | [docs/API_TEST_CASES.md](API_TEST_CASES.md) | Full automated + manual test catalog |

---

*This documentation reflects the project state after **Phase 9** (all BRD phases complete).*
