# PRM — Project & Resource Management Tool

Learn & Code Final Project — REST API backend built with **.NET 8** and **Clean Architecture**.

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download) (or newer SDK that supports `net8.0`)
- [Neon](https://neon.tech) PostgreSQL account (configured in Phase 2)

## Solution structure

```
src/
  Prm.Domain/           Entities, enums, domain rules
  Prm.Application/      Services, DTOs, interfaces, validators
  Prm.Infrastructure/   EF Core, repositories, external services
  Prm.Api/              REST API, Swagger, DI wiring
tests/
  Prm.Tests/            Unit tests
docs/
  BRD.md                Business requirements
  PROJECT_DOCUMENTATION.md  Full project docs (architecture, phases, APIs, decisions)
  NEON_SETUP.md         Neon PostgreSQL setup
```

**Dependency flow:** Domain ← Application ← Infrastructure ← Api

## Quick start (Phase 1)

```bash
dotnet restore
dotnet build
dotnet run --project src/Prm.Api
```

- Swagger UI: `https://localhost:7xxx/swagger` (see console output for port)
- Health check: `GET https://localhost:7xxx/health`

## Configuration

Neon PostgreSQL connection string is stored in **User Secrets** (not in git). See [docs/NEON_SETUP.md](docs/NEON_SETUP.md).

```powershell
cd src/Prm.Api
dotnet user-secrets set "ConnectionStrings:Default" "Host=...;Database=neondb;..."
```

On startup the API applies pending migrations and runs seed data (bootstrap admin + demo data).

### Verify database (Phase 2)

- `GET /health` — includes EF Core database check
- `GET /api/database/status` — connection + row counts

### Authentication (Phase 3)

Configure JWT in User Secrets (minimum 32-char key):

```powershell
cd src/Prm.Api
dotnet user-secrets set "Jwt:Key" "YourSecretKeyAtLeast32CharactersLong!"
dotnet user-secrets set "Jwt:Issuer" "PrmApi"
dotnet user-secrets set "Jwt:Audience" "PrmClient"
```

**Endpoints:**

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| POST | `/api/auth/login` | Public | Returns JWT + `forcePasswordChange` flag |
| POST | `/api/auth/change-password` | JWT | Updates password; returns new JWT |
| GET | `/api/auth/me` | JWT | Current user profile |
| GET | `/api/auth/admin-check` | Admin | Role policy test |
| GET | `/api/auth/manager-check` | Manager | Role policy test |
| GET | `/api/auth/employee-check` | Employee | Role policy test |

**Bootstrap login:** `admin` / `Admin@1234` (must change password on first use).

Use Swagger **Authorize** button with `Bearer {token}`.

### Admin APIs (Phase 4) — requires Admin JWT

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/admin/employees` | List employees (`?department=&status=`) |
| PUT | `/api/admin/employees/{id}` | Update department |
| POST | `/api/admin/employees/{id}/deactivate` | Deactivate + end allocations |
| GET/POST/PUT/DELETE | `/api/admin/employees/{id}/skills` | Manage skills |
| PUT | `/api/admin/employees/{id}/assign-manager` | Assign manager |
| GET/POST | `/api/admin/users` | List / create users |
| POST | `/api/admin/users/{id}/reset-password` | Reset password |
| POST | `/api/admin/users/{id}/deactivate` | Deactivate user |
| POST | `/api/admin/users/{id}/reactivate` | Reactivate user |
| GET/POST/PUT | `/api/admin/projects` | Projects CRUD |
| GET/POST/PUT | `/api/admin/projects/{id}/milestones` | Milestones |
| GET | `/api/admin/allocations` | Company-wide active allocations |
| GET/PUT | `/api/admin/settings` | System configuration |

## Implementation phases

| Phase | Status |
|-------|--------|
| 1 — Environment & scaffold | Complete |
| 2 — Neon DB & schema | Complete |
| 3 — Authentication | Complete |
| 4 — Admin APIs | Complete |
| 5 — Manager APIs | Pending |
| 6 — Employee APIs | Pending |
| 7 — Scheduler | Pending |
| 8 — AI features | Pending |
| 9 — Backend completion | Pending |

## Assignment notes

SOLID principles, design patterns, and design principles will be documented here as each phase is completed.
