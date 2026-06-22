# Prm.Console

BRD menu-driven console UI. Screens call the REST API in `Server/Prm.Api`.

## Folder structure

```
Prm.Console/
├── Program.cs              Entry point, main loop
├── appsettings.json        ApiBaseUrl (default http://localhost:5140)
├── Api/                    HttpClient wrapper, JWT header, error parsing
├── Auth/                   SessionState, client-side logout
├── Models/                 JSON DTOs (mirror API responses; no server project refs)
├── Navigation/             Menu stack, [B] Back, Logout
├── Rendering/              Tables, prompts, screen headers
└── Screens/
    ├── WelcomeScreen.cs    Login / Exit
    ├── ChangePasswordScreen.cs
    ├── Admin/              Admin BRD flows
    ├── Manager/            Manager BRD flows
    └── Employee/           Employee BRD flows
```

## Conventions

- **Namespace:** `Prm.Client` (avoid `Prm.Console` — conflicts with `System.Console`)
- **Logout:** client-side only — clear JWT and return to welcome screen
- **AI screens:** deferred until Phase 8 backend
