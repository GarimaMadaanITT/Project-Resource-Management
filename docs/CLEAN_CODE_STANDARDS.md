# PRM Clean Code Standards

This document defines coding standards for the PRM Tool. All new code (including Phase 5 Manager APIs) must follow these rules.

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

Use `ILogger<T>`. Log security-relevant actions (user create/deactivate, password reset, project create/update, manager assignment, employee deactivation). Never log passwords, hashes, tokens, connection strings, or API keys.

## Audit trail

Entity timestamps (`AuditableEntity`) track `CreatedAt`/`UpdatedAt`. Structured logs provide operational audit entries. No separate audit log table in current schema.

## PR checklist

Before PR: meaningful names, no duplicated validation, SOLID followed, thin controllers, persistence-only repositories, proper exceptions/status codes, tests added, no dead code, Swagger tested.
