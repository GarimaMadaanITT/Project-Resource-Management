# PRM API — Test Case Catalog

Automated tests live in:

- `tests/Prm.Tests` — unit tests (validators, guards, mocked services)
- `tests/Prm.Tests.Integration` — full HTTP API tests (SQLite in-memory + seed)

Run all tests:

```powershell
dotnet test
```

---

## 1. Health & Database

| ID | Endpoint | Scenario | Expected |
|----|----------|----------|----------|
| H-01 | `GET /health` | Health check | 200 OK |
| H-02 | `GET /api/database/status` | DB connected after seed | 200, users ≥ 9, bootstrap admin |

---

## 2. Authentication

| ID | Endpoint | Scenario | Expected |
|----|----------|----------|----------|
| A-01 | `POST /api/auth/login` | Empty username | 400 |
| A-02 | `POST /api/auth/login` | Wrong password | 401 |
| A-03 | `POST /api/auth/login` | Valid manager credentials | 200 + JWT |
| A-04 | `POST /api/auth/login` | Inactive user (priya.sharma) | 401 |
| A-05 | `GET /api/admin/users` | Admin token with force password change | 403 |
| A-06 | `POST /api/auth/change-password` | Valid new password | 200 + new token |
| A-07 | `GET /api/auth/me` | Authenticated admin | 200, username admin |
| A-08 | `GET /api/auth/admin-check` | Admin role | 200 |
| A-09 | `GET /api/auth/manager-check` | Admin token | 403 |

---

## 3. Admin — Users

| ID | Endpoint | Scenario | Expected |
|----|----------|----------|----------|
| U-01 | `GET /api/admin/users` | List all users | 200, includes active + inactive |
| U-02 | `POST /api/admin/users` | Create valid admin | 200 |
| U-03 | `POST /api/admin/users` | Invalid email format | 400 |
| U-04 | `POST /api/admin/users` | Manager without department | 400 |
| U-05 | `POST /api/admin/users` | Duplicate username | 409 |
| U-06 | `POST .../reset-password` | Weak password (digits only) | 400 |
| U-07 | `POST .../reset-password` | Valid temporary password | 200 |
| U-08 | `POST .../deactivate` | Self-deactivation | 403 |
| U-09 | `POST .../deactivate` | Manager with team + projects | 400 |
| U-10 | `POST .../reactivate` | Deactivate then reactivate user | 200 |

---

## 4. Admin — Employees

| ID | Endpoint | Scenario | Expected |
|----|----------|----------|----------|
| E-01 | `GET /api/admin/employees` | Active employees only | 200, no inactive Priya |
| E-02 | `GET /api/admin/employees` | Sara Khan utilisation | Status Allocated, 75% |
| E-03 | `PUT /api/admin/employees/{id}` | Update department | 200 |
| E-04 | `PUT /api/admin/employees/{id}` | Empty department | 400 |
| E-05 | `POST .../skills` | Skill on inactive employee | 400 |
| E-06 | `POST .../skills` | Add new skill | 200 |
| E-07 | `POST .../skills` | Duplicate skill | 409 |
| E-08 | `GET .../skills` | List employee skills | 200, not empty |
| E-09 | `PUT .../assign-manager` | Assign active manager | 200 |
| E-10 | `POST .../deactivate` | Employee with allocations | 200, allocations ended |
| E-11 | `POST .../deactivate` | Manager with active team | 400 |

---

## 5. Admin — Projects & Milestones

| ID | Endpoint | Scenario | Expected |
|----|----------|----------|----------|
| P-01 | `GET /api/admin/projects` | List projects | 200, ≥ 4 projects |
| P-02 | `POST /api/admin/projects` | Valid project | 200 |
| P-03 | `POST /api/admin/projects` | Empty name | 400 |
| P-04 | `POST /api/admin/projects` | Employee as manager | 400 |
| P-05 | `GET .../milestones` | List milestones | 200 |
| P-06 | `POST .../milestones` | Valid milestone | 200 |
| P-07 | `POST .../milestones` | Due date outside project | 400 |
| P-08 | `POST .../milestones` | Story points exceed budget | 400 |
| P-09 | `PUT .../milestones/{id}/status` | Valid status update | 200 |

---

## 6. Admin — Allocations

| ID | Endpoint | Scenario | Expected |
|----|----------|----------|----------|
| AL-01 | `GET /api/admin/allocations` | All active allocations | 200, not empty |
| AL-02 | `GET ...?employeeId=` | Filter by employee | 200, filtered rows |
| AL-03 | `GET ...?projectId=` | Filter by project | 200, filtered rows |

---

## 7. Admin — Settings

| ID | Endpoint | Scenario | Expected |
|----|----------|----------|----------|
| S-01 | `GET /api/admin/settings` | Default settings | 200, Gemini, 4h, 40h |
| S-02 | `PUT /api/admin/settings` | Update scheduler interval | 200 |
| S-03 | `PUT /api/admin/settings` | Zero scheduler interval | 400 |

---

## 8. Authorization

| ID | Endpoint | Scenario | Expected |
|----|----------|----------|----------|
| Z-01 | `GET /api/admin/users` | No token | 401 |
| Z-02 | `GET /api/admin/users` | Manager token | 403 |
| Z-03 | `GET /api/admin/projects` | Employee token | 403 |

---

## 9. Unit Tests — Validation Layer

| Area | Tests |
|------|-------|
| StringGuard | null, empty, whitespace, trim |
| EmailValidator | valid / invalid formats |
| PasswordValidator | length, uppercase, number, digits-only message |
| EmployeeGuard | inactive employee, inactive manager |
| UserGuard | self-deactivate, last admin |
| AllocationGuard | inactive employee allocation block |
| MilestoneValidator | due date range, SP budget |
| EmployeeStatusResolver | utilisation → Allocated/Bench |
| AdminUserService | department before save, duplicate username, self-deactivate |
| AdminEmployeeService | inactive assign manager, manager deactivate block |
| AdminProjectService | inactive manager, milestone SP overflow |

---

## Manual Swagger checklist (Neon)

After `dotnet run --project src/Prm.Api`:

1. Login `admin` / `Admin@1234` → change password → authorize Swagger  
2. Repeat U-03 through P-08 scenarios against live Neon DB  
3. Confirm ProblemDetails body includes `detail` and correct status codes (400/403/409)

Reset Neon seed data:

```powershell
dotnet run --project tools/Prm.DbReset/Prm.DbReset.csproj
```
