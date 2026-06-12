# PRM — Project & Resource Management Tool

Learn & Code Final Project — REST API backend built with **.NET 8** and **Clean Architecture**.

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download) (or newer SDK that supports `net8.0`)
- [Neon](https://neon.tech) PostgreSQL account (configured in Phase 2)

## Solution structure

```
Client/                 Console UI (HTTP REST client — Prm.Console)
Server/
  Prm.Domain/           Entities, enums, domain rules
  Prm.Application/      Services, DTOs, interfaces, validators
  Prm.Infrastructure/   EF Core, repositories, external services
  Prm.Api/              REST API, Swagger, DI wiring
tests/
  Prm.Tests/            Unit tests
  Prm.Tests.Integration/
docs/
  BRD.md                Business requirements
  PROJECT_DOCUMENTATION.md  Full project docs (architecture, phases, APIs, decisions)
  NEON_SETUP.md         Neon PostgreSQL setup
tools/
  Prm.DbReset/          Database reset utility
```

**Dependency flow:** Domain ← Application ← Infrastructure ← Api

## Quick start (Phase 1)

```bash
dotnet restore
dotnet build
dotnet run --project Server/Prm.Api
```

- Swagger UI: `https://localhost:7xxx/swagger` (see console output for port)
- Health check: `GET https://localhost:7xxx/health`

### Console client

```bash
dotnet run --project Client/Prm.Console
```

Start the **server first**. See [Client/README.md](Client/README.md).

## Configuration

Neon PostgreSQL connection string is stored in **User Secrets** (not in git). See [docs/NEON_SETUP.md](docs/NEON_SETUP.md).

```powershell
cd Server/Prm.Api
dotnet user-secrets set "ConnectionStrings:Default" "Host=...;Database=neondb;..."
```

On startup the API applies pending migrations and runs seed data (bootstrap admin + demo data).

### Verify database (Phase 2)

- `GET /health` — includes EF Core database check
- `GET /api/database/status` — connection + row counts

### Authentication (Phase 3)

Configure JWT in User Secrets (minimum 32-char key):

```powershell
cd Server/Prm.Api
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
| GET/PUT | `/api/admin/settings` | System configuration (LLM provider/key) |
| GET | `/api/admin/audit-logs` | Paginated audit trail |

### Manager APIs — requires Manager JWT

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/manager/dashboard` | Team bench / partial / full view |
| GET | `/api/manager/dashboard/employees/{id}` | Employee drill-down |
| POST | `/api/manager/allocations` | Create allocation |
| POST | `/api/manager/allocations/{id}/end` | End allocation |
| GET | `/api/manager/projects` | Manager's projects |
| GET | `/api/manager/projects/{id}` | Project detail + risk flags |
| GET | `/api/manager/timesheets` | Team timesheets by week |
| GET | `/api/manager/timesheets/employees/{id}` | Employee timesheet detail |

### AI APIs (Phase 8) — requires Manager JWT

| Method | Route | Description |
|--------|-------|-------------|
| POST | `/api/ai/skill-match` | Natural-language resource search (capacity pre-filter + LLM) |
| GET | `/api/ai/risk-summary/{projectId}` | Plain-English project risk paragraph |

Configure **Gemini** or **Groq** via Admin → System Configuration. Without an API key, the server uses a deterministic offline fallback so the feature remains testable.

### Employee APIs — requires Employee JWT

| Method | Route | Description |
|--------|-------|-------------|
| GET/POST | `/api/employee/timesheets` | History + submit timesheet |
| GET | `/api/employee/allocations` | Active allocations |
| GET | `/api/employee/reminders` | Missed timesheet reminder |

## Implementation phases

| Phase | Status |
|-------|--------|
| 1 — Environment & scaffold | Complete |
| 2 — Neon DB & schema | Complete |
| 3 — Authentication | Complete |
| 4 — Admin APIs | Complete |
| 5 — Manager APIs | Complete |
| 6 — Employee APIs | Complete |
| 7 — Scheduler + audit logs | Complete |
| 8 — AI features | Complete |
| 9 — Tests, console client, docs | Complete |

## Assignment notes — SOLID & design patterns

| Principle / Pattern | Where in PRM |
|---------------------|--------------|
| **Single Responsibility (S)** | Separate services per domain (`ManagerAiService`, `AdminEmployeeService`, `EmployeeTimesheetService`) — each class owns one use case area |
| **Open/Closed (O)** | Validators/guards (`AllocationValidator`, `ManagerScopeGuard`) extend behaviour without modifying entities |
| **Liskov Substitution (L)** | Repository interfaces (`IEmployeeRepository`, etc.) — Infrastructure implementations are interchangeable in tests via Moq |
| **Interface Segregation (I)** | Focused interfaces (`IManagerAiService`, `ILlmCompletionService`) instead of one giant service contract |
| **Dependency Inversion (D)** | API controllers depend on Application interfaces; Infrastructure implements them (`DependencyInjection.cs`) |
| **Repository** | `IUserRepository`, `IProjectRepository`, … — persistence isolated from business logic |
| **Strategy** | `GeminiLlmProvider` / `GroqLlmProvider` selected by `LlmCompletionService` from admin settings |
| **Factory (hosted)** | `LlmCompletionService` resolves the correct LLM provider at runtime |

See also [docs/PROJECT_DOCUMENTATION.md](docs/PROJECT_DOCUMENTATION.md) and [docs/CLEAN_CODE_STANDARDS.md](docs/CLEAN_CODE_STANDARDS.md).
