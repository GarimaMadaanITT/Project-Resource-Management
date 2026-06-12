# PRM Client

Console application for **Admin**, **Manager**, and **Employee** roles (BRD Section 4).

## Run

**Terminal 1 — Server:**
```powershell
dotnet run --project Server/Prm.Api
```

**Terminal 2 — Client:**
```powershell
dotnet run --project Client/Prm.Console
```

API URL: `http://localhost:5140` (see `Client/Prm.Console/appsettings.json` or env `PRM_API_URL`).

## Test credentials

| Role | Username | Password |
|------|----------|----------|
| Admin | `admin` | `Admin@1234` |
| Manager | `ankit.shah` | `Manager@1234` |
| Employee | `dev.patel` | `Employee@1234` |

Logout is client-side (clears JWT). Manager **AI Assistant** (skill match + risk summary) is wired to `/api/ai/*`.
