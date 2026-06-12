# PRM Clean Code Standards

This document defines coding standards for the PRM Tool. All new code (including Phase 5 Manager and Phase 6 Employee APIs) must follow these rules.

## Principles

- Meaningful names; no abbreviations or single-letter variables
- One responsibility per method (target under 40 lines)
- Guard clauses over deep nesting
- No magic numbers or hardcoded business values
- Validation in Application layer only
- Controllers receive → call service → return
- Repositories query and save only

## Constants (Application layer)

| File | Purpose |
|------|---------|
| `AuthConstants` | JWT claim, policy names, role parsing |
| `ValidationConstants` | Password length, admin count, JWT key length |
| `DomainDefaults` | Default statuses and flags |
| `ErrorMessages` | Shared not-found and conflict messages |
| `MaskingConstants` / `ApiKeyMasker` | API key masking |

## Validation (Application layer)

Use guards and validators instead of inline checks:

- `StringGuard`, `EmailValidator`, `PasswordGuard`, `PasswordValidator`
- `EntityGuard`, `EnumGuard`, `UserGuard`, `UserAvailabilityGuard`
- `EmployeeGuard`, `SkillGuard`, `ManagerDeactivationGuard`, `AllocationGuard`
- `ProjectValidator`, `MilestoneValidator`, `SettingsValidator`
- `ActiveDateHelper` for UTC date/allocation logic

## Service structure

Admin services use specialized internal services with thin facades:

- **Employees:** `AdminEmployeeQueryService`, `AdminEmployeeCommandService`, `AdminEmployeeSkillService` → `AdminEmployeeService`
- **Users:** `AdminUserProvisioningService`, `AdminUserCredentialService`, `AdminUserLifecycleService` → `AdminUserService`
- **Shared:** `AccountDeactivationService` for user/employee deactivation rules

## Logging

Use `ILogger<T>`. Log security-relevant and operational actions. Never log passwords, hashes, tokens, connection strings, or API keys.

**Admin (existing):** user create/deactivate, password reset, project create/update, manager assignment, employee deactivation.

**Manager (Phase 5+):**
- Allocation created — `Information` with AllocationId, ProjectId, EmployeeId, Utilisation, ManagerUserId
- Allocation ended — `Information` with AllocationId, EndDate, ManagerUserId
- Employee status updated after allocation change — `Information`
- Manager scope denied — `Warning` with ManagerUserId, resource type, resource id
- Allocation validation rejected (overlap) — `Warning` with EmployeeId, date, utilisation totals

**Employee (Phase 6+):**
- Timesheet submitted — `Information` with EmployeeId, WeekStart, TotalHours, ProjectCount
- Timesheet validation rejected — `Warning` with WeekStart, ProjectId, or Reason
- Timesheet reminder returned — `Information` with EmployeeId, WeekStart

## Audit trail

Phase 7 adds an `audit_logs` table and `IAuditLogService`:

- **Change-only:** skips writes when serialized `OldValue` equals `NewValue`
- **Snapshots:** `AuditSnapshotBuilder` produces camelCase JSON for entities (User, Employee, Project, etc.)
- **Sources:** `User` (API writes), `Scheduler` (background jobs), `System` (reserved)
- **Query:** Admin-only `GET /api/admin/audit-logs` with pagination and filters

Entity timestamps (`AuditableEntity`) still track `CreatedAt`/`UpdatedAt`. Structured logs complement the DB audit trail for operations.

## PR checklist

Before PR: meaningful names, no duplicated validation, SOLID followed, thin controllers, persistence-only repositories, proper exceptions/status codes, tests added, no dead code, Swagger tested.
