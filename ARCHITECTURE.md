# PRM Platform — Client / Server Architecture

Two **separate programs** communicating over **HTTP REST APIs** (JWT Bearer auth).

```
Project-Resource-Management/
│
├── Client/                         Console UI (thin HTTP client)
│   └── Prm.Console/
│
├── Server/                         REST API + Clean Architecture
│   ├── Prm.Domain/
│   ├── Prm.Application/
│   ├── Prm.Infrastructure/
│   └── Prm.Api/
│
├── tests/                          Server unit + integration tests
├── docs/
└── tools/
```

## Communication

| From | To | Protocol |
|------|-----|----------|
| `Client/Prm.Console` | `Server/Prm.Api` | HTTP REST + JSON |
| Server layers | Neon PostgreSQL | EF Core (client never touches DB) |

## Run server

```powershell
dotnet run --project Server/Prm.Api
```

Swagger: `http://localhost:5140/swagger` (see launch output).

## Run client

```powershell
dotnet run --project Client/Prm.Console
```

Configure API URL in `Client/Prm.Console/appsettings.json` (`ApiBaseUrl`). Manager AI features work offline (deterministic fallback) or with a Gemini/Groq key set in Admin settings.

## Logout

Handled on the **client** only (discard JWT). No server logout endpoint required.
