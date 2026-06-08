# Neon PostgreSQL Setup (Phase 2)

## 1. Configure connection string (choose one)

### Option A — User Secrets (recommended)

From the repo root:

```powershell
cd src/Prm.Api
dotnet user-secrets set "ConnectionStrings:Default" "Host=YOUR_HOST;Database=neondb;Username=YOUR_USER;Password=YOUR_PASSWORD;SSL Mode=Require;Trust Server Certificate=true"
```

### Option B — `appsettings.Development.json` (gitignored)

Edit `src/Prm.Api/appsettings.Development.json` and replace `REPLACE_WITH_YOUR_NEON_PASSWORD`.

Convert Neon URI to key-value form:

```
Host=ep-xxx-pooler.region.aws.neon.tech;Database=neondb;Username=...;Password=...;SSL Mode=Require;Trust Server Certificate=true
```

## 2. Apply migrations

```powershell
dotnet ef database update --project src/Prm.Infrastructure --startup-project src/Prm.Api
```

Or start the API — migrations and seed run automatically on startup.

## 3. Verify

```powershell
dotnet run --project src/Prm.Api
```

- `GET /health` — healthy
- `GET /api/database/status` — connected, counts, bootstrap admin

**Never commit real passwords to git.**
