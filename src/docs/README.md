# Kingdom Duel — backend

API REST pentru a crea un meci, a te alătura cu un cod și a trimite acțiuni.
Organizat pe **Clean Architecture** (4 straturi), cu persistență în
**PostgreSQL**, use-case-uri prin **MediatR/CQRS** și endpoint-uri
**FastEndpoints**. Fără UI și fără SignalR încă — deliberat, ca să avansăm
secvențial.

## Structură (Clean Architecture)

```
KingdomDuel/
├── KingdomDuel.sln
├── RULES.md                              regulile complete ale jocului
├── src/
│   ├── KingdomDuel.Domain/               modelele + logica de joc, fără dependențe externe
│   │   ├── GameConstants.cs              fiecare număr din joc, într-un singur loc
│   │   ├── GameEngine.cs                 rezolvă o rundă + verifică sfârșitul meciului
│   │   ├── Enums/                        ActionType, GameEventType, MatchStatus, GameOverReason
│   │   └── Models/                       Kingdom, PlayerSlot, Match, RoundResult
│   │
│   ├── KingdomDuel.Application/          use-case-uri (CQRS prin MediatR)
│   │   ├── Abstractions/                 IMatchStore (port de persistență)
│   │   ├── Common/                       excepții + mapare la DTO-uri
│   │   ├── Matches/Commands/             CreateMatch, JoinMatch, SubmitAction (+ handlere)
│   │   ├── Matches/Queries/              GetMatch (+ handler)
│   │   ├── Matches/Dtos/                 DTO-urile expuse de API
│   │   └── DependencyInjection.cs        AddApplication()
│   │
│   ├── KingdomDuel.Infrastructure/       implementările tehnice
│   │   ├── Persistence/                  GameDbContext, MatchRecord, PostgresMatchStore, Migrations
│   │   └── DependencyInjection.cs        AddInfrastructure()
│   │
│   └── KingdomDuel.Api/                  ASP.NET Core + FastEndpoints
│       ├── Endpoints/Matches/            un endpoint + validator per operație
│       ├── ApplicationExceptionHandler.cs mapează excepțiile Application la coduri HTTP
│       └── Program.cs
```

Dependențele merg spre interior: `Api → Application → Domain` și
`Infrastructure → Application → Domain`. `Domain` nu depinde de nimic
extern — poate fi reutilizat și de o versiune desktop offline, fără server.

## Cerințe

- **.NET 10 SDK**
- **PostgreSQL 18** care rulează pe `localhost:5432`
- O bază de date numită `KingdomDuel` (vezi mai jos)

## Baza de date

Connection string-ul e în `src/KingdomDuel.Api/appsettings.json`, cheia
`ConnectionStrings:KingdomDuelDb`. Schema se creează/actualizează automat:
migrările EF Core sunt aplicate la pornirea aplicației.

Pentru a genera/aplica manual o migrare (DbContext-ul e în Infrastructure,
startup project-ul e Api):

```bash
dotnet ef migrations add <Nume> --project src/KingdomDuel.Infrastructure --startup-project src/KingdomDuel.Api --output-dir Persistence/Migrations
dotnet ef database update --project src/KingdomDuel.Infrastructure --startup-project src/KingdomDuel.Api
```

## Cum rulezi

```bash
cd KingdomDuel
dotnet build
dotnet run --project src/KingdomDuel.Api
```

Serverul pornește implicit pe `http://localhost:5080` (vezi
`Properties/launchSettings.json`). Testezi endpoint-urile cu
curl/Postman/Insomnia.

## Endpoint-uri disponibile

| Metodă | Rută | Ce face |
|---|---|---|
| `POST` | `/api/matches` | Creează un meci nou. Body: `{ "displayName": "..." }`. Răspuns: cod de meci + `playerId` + starea meciului. |
| `POST` | `/api/matches/{code}/join` | Al doilea jucător se alătură cu codul. Body: `{ "displayName": "..." }`. Răspuns: `playerId` + starea meciului. |
| `GET` | `/api/matches/{code}` | Starea curentă a meciului (pentru polling, până vine SignalR). |
| `POST` | `/api/matches/{code}/actions` | Trimite acțiunea unui jucător pentru runda curentă. Body: `{ "playerId": "...", "action": "Attack" \| "Defend" \| "Trade" \| "Diplomacy" }`. Runda se rezolvă automat când ambii jucători au trimis. |

Nu există autentificare — `playerId`-ul primit la creare/alăturare e
singurul „secret" care dovedește cine ești în meciul respectiv.

## Ce urmează (nu e făcut aici, intenționat)

1. UI (Blazor Server, cel mai probabil, discutat anterior).
2. SignalR pentru push în timp real în loc de polling.
3. Teste unitare pentru `GameEngine` și handlerele din Application.
4. Eventual Redis dacă serverul trebuie să scaleze pe mai multe instanțe.
