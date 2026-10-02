# AGENTS.md

> Hartă a proiectului pentru agenți AI. Actualizeaz-o când structura se schimbă semnificativ.

## Prezentare

Kingdom Duel — joc de duel pe runde pentru doi jucători, ASP.NET Core + Blazor
WebAssembly peste PostgreSQL. Detalii în [.ai-factory/DESCRIPTION.md](.ai-factory/DESCRIPTION.md).

## Stack

- **Limbaj:** C# / .NET 10
- **Framework:** ASP.NET Core + FastEndpoints; Blazor WebAssembly
- **Bază de date:** PostgreSQL 18
- **ORM:** EF Core 10 (Npgsql)

## Structura proiectului

```
KingdomDuel/
├── KingdomDuel.sln
├── global.json                 dotnet test pe Microsoft Testing Platform
├── dotnet-tools.json           unelte locale (ReportGenerator)
├── compose.yaml                Postgres local (port 5433)
├── Dockerfile                  imaginea de producție
├── .github/workflows/          deploy.yml: teste + coverage, apoi Cloud Run
├── build/                      coverage.ps1 + coverage-gate.cs (praguri)
├── tests/
│   ├── KingdomDuel.UnitTests/         motorul de joc, Application, Shared
│   ├── KingdomDuel.IntegrationTests/  API cap-coadă pe Postgres (Testcontainers)
│   └── KingdomDuel.Client.Tests/      logica clientului + componente (bUnit)
└── src/
    ├── docs/                   RULES.md (regulile jocului)
    ├── KingdomDuel.Domain/     entități, enumuri, motorul de joc (Rounds/), GameConstants
    ├── KingdomDuel.Application/ endpoint-uri FastEndpoints pe feature (Features/Matches, Features/Auth), AppDbContext
    ├── KingdomDuel.Persistence/ configurări EF + Migrations/
    ├── KingdomDuel.Shared/     Requests, Responses, Dtos, Validators — contractul server–client
    ├── KingdomDuel.Api/        gazda: Program.cs + Configurations/
    └── KingdomDuel.Client/     Blazor WASM: Pages, Components, Layout, Authentication, ApiClients
```

## Puncte de intrare

| Fișier | Rol |
|--------|-----|
| `src/KingdomDuel.Api/Program.cs` | pornirea gazdei: middleware, migrări, generarea clientului, fallback SPA |
| `src/KingdomDuel.Api/Configurations/` | configurarea pe zone (API, Auth, RateLimit, Database, Health, Exception, Proxy) |
| `src/KingdomDuel.Application/AppDbContext.cs` | contextul EF |
| `src/KingdomDuel.Domain/Rounds/GameEngine.cs` | rezolvarea rundei și a bătăliei |
| `src/KingdomDuel.Domain/GameConstants.cs` | toate numerele jocului |
| `src/KingdomDuel.Application/Features/Matches/RoundResolver.cs` | mersul meciului în timp (expirări, bătălii) |
| `src/KingdomDuel.Client/Program.cs` | pornirea clientului Blazor |
| `src/KingdomDuel.Client/ApiClients/ApiClient.cs` | client NSwag generat — nu se editează de mână |
| `src/KingdomDuel.Api/appsettings*.json` | configurare (connection string, JWT) |

## Documentație

| Document | Cale | Descriere |
|----------|------|-----------|
| README | README.md | structură, rulare, bază de date, API, teste, publicare |
| Regulile jocului | src/docs/RULES.md | regulile complete, sursa testelor de motor |

## Fișiere de context AI

| Fișier | Rol |
|--------|-----|
| AGENTS.md | harta proiectului (acest fișier) |
| .ai-factory/DESCRIPTION.md | specificația proiectului și stack-ul |
| .ai-factory/ARCHITECTURE.md | decizii și reguli de arhitectură |
| .ai-factory/rules/base.md | convențiile de cod detectate |
| .ai-factory/config.yaml | setările AI Factory (limbă, căi, git) |

## Reguli pentru agenți

- Nu combina comenzi shell cu `&&`; rulează-le separat, ca fiecare să poată fi aprobată și verificată.
  - Greșit: `git checkout main && git pull`
  - Corect: întâi `git checkout main`, apoi `git pull origin main`
- După orice schimbare de contract API, regenerează clientul (`dotnet run --generateclients true` din `src/KingdomDuel.Api`).
- Testele de integrare cer Docker Desktop pornit.
