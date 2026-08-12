# Kingdom Duel

Joc de duel pentru doi jucători: două regate, un număr fix de runde, iar în
fiecare rundă fiecare jucător alege în secret una din patru acțiuni — atac,
fortificare, comerț sau diplomație. Runda se rezolvă când amândoi au trimis
ordinul. Regulile complete sunt în [RULES.md](src/docs/RULES.md).

Proiectul e un **ASP.NET Core** care servește și API-ul, și un client
**Blazor WebAssembly**, cu persistență în **PostgreSQL**. Contul e obligatoriu:
nu se joacă fără autentificare.

## Structură

```
KingdomDuel/
├── KingdomDuel.sln
└── src/
    ├── docs/                             documentația (vezi mai jos)
    │
    ├── KingdomDuel.Domain/               modelele + logica de joc, fără dependențe externe
    │   ├── GameConstants.cs              fiecare număr din joc, într-un singur loc
    │   ├── GameEngine.cs                 rezolvă o rundă + verifică sfârșitul meciului
    │   ├── Contracts/Entity.cs           baza cu Guid Id
    │   ├── Entities/Matches/             Match, PlayerSlot, Kingdom, RoundResult
    │   ├── Entities/Users/User.cs        contul, cu normalizarea numelui
    │   └── Enums/                        ActionType, GameEventType, MatchStatus, GameOverReason
    │
    ├── KingdomDuel.Application/          endpoint-urile + regulile lor
    │   ├── AppDbContext.cs
    │   ├── RateLimitPolicies.cs          numele politicilor de rate limiting
    │   ├── Features/Matches/             un endpoint per operație + MatchQueries, MatchMappings
    │   └── Features/Auth/                Endpoints/ + Services/ (vezi AUTH.md)
    │
    ├── KingdomDuel.Persistence/          EF Core: configurări + migrări
    │   ├── Configurations/               un IEntityTypeConfiguration per entitate
    │   └── Migrations/
    │
    ├── KingdomDuel.Shared/               contractele împărțite între server și client
    │   ├── Requests/, Responses/, Dtos/  DTO-urile expuse de API
    │   ├── Validators/                   FluentValidation, rulate pe server
    │   └── Auth/AuthHeaders.cs
    │
    ├── KingdomDuel.Api/                  gazda: Program.cs + Configurations/
    │   └── Configurations/               Api, Auth, RateLimit, Database, ClientGeneration
    │
    └── KingdomDuel.Client/               Blazor WebAssembly
        ├── Pages/                        Home, Login, Lobby, Game, Profile
        ├── Components/                   UserBar, RulesBook, CastleView, …
        ├── Authentication/               TokenStore, BearerTokenHandler, …
        └── ApiClients/ApiClient.cs       generat cu NSwag — nu se scrie de mână
```

Dependențele merg spre interior: `Api → Application → Domain`, iar
`Persistence` și `Shared` sunt referite de unde e nevoie. `Domain` nu depinde
de nimic extern.

> **Endpoint-urile stau în `Application`, nu în `Api`.** `Api` conține doar
> gazda și configurările; `Program.cs` îi dă lui FastEndpoints assembly-ul
> `Application` prin `o.Assemblies`.

## Cerințe

- **.NET 10 SDK**
- **PostgreSQL 18** pe `localhost:5432`, cu o bază numită `KingdomDuel`

## Cum rulezi

```bash
dotnet build
dotnet run --project src/KingdomDuel.Api
```

Pornește pe `http://localhost:5080` (vezi `Properties/launchSettings.json`) și
servește acolo și clientul Blazor. În dezvoltare, Swagger UI e la
`/swagger`; în producție documentul OpenAPI nu se generează deloc.

Migrările EF se aplică automat la pornire.

## Baza de date

Connection string-ul e în `src/KingdomDuel.Api/appsettings.json`, cheia
`ConnectionStrings:KingdomDuelDb`. În producție se dă prin variabila de mediu
`ConnectionStrings__KingdomDuelDb`.

DbContext-ul e în `Application`, migrările în `Persistence`, startup project-ul
e `Api`:

```bash
dotnet ef migrations add <Nume> --project src/KingdomDuel.Persistence --startup-project src/KingdomDuel.Api
dotnet ef database update --project src/KingdomDuel.Persistence --startup-project src/KingdomDuel.Api
```

## Autentificare

Cont obligatoriu, **username + parolă**, email opțional. Fără pachetul
Identity — o entitate `User` proprie, din care se reutilizează doar
`PasswordHasher<User>`. Token JWT de 7 zile, ținut în `localStorage`, reînnoit
alunecător când mai are sub o zi, revocabil printr-o coloană de versiune
(`User.TokenVersion` + claim-ul `ver`), verificată la fiecare cerere.

Cheia de semnare vine din `Jwt:SigningKey`; pornirea eșuează explicit dacă
lipsește sau are sub 32 de octeți. În dezvoltare e fixă în
`appsettings.Development.json`, ca token-urile să supraviețuiască repornirilor;
în producție se dă prin `Jwt__SigningKey`.

Deciziile, cu alternativele respinse și motivele lor, sunt în [AUTH.md](src/docs/AUTH.md).

## Endpoint-uri

Toate au prefixul `/api`. Anonime sunt **doar** `register` și `login`; restul
cer `Authorization: Bearer <token>`.

| Metodă | Rută | Ce face |
|---|---|---|
| `POST` | `/auth/register` | Creează un cont. Body: `{ userName, password, email? }`. Răspuns: token + profil. |
| `POST` | `/auth/login` | Intră în cont. Body: `{ userName, password }`. Răspuns: token + profil. |
| `POST` | `/auth/logout` | Revocă toate token-urile contului. `204`. |
| `GET` | `/auth/profile` | Profilul apelantului. |
| `PUT` | `/auth/profile` | Schimbă numele și/sau emailul. Body: `{ userName, email? }`. Răspuns: profilul + `requiresSignIn`. |
| `PUT` | `/auth/profile/password` | Schimbă parola. Body: `{ currentPassword, newPassword }`. `204`, cu revocarea tuturor token-urilor. |
| `POST` | `/matches` | Creează un meci. Fără body — identitatea vine din token. Răspuns: cod + starea meciului. |
| `GET` | `/matches/mine` | Meciul în desfășurare al apelantului, dacă există. |
| `GET` | `/matches/{code}` | Starea meciului (polling). Doar pentru participanți — altfel `404`. |
| `POST` | `/matches/{code}/join` | Alăturare cu codul. Idempotent: dacă ești deja pe un slot, întoarce starea. |
| `PUT` | `/matches/{code}/lobby` | Setările meciului (runde, aur/trupe/moral de start) + starea *ready*. |
| `POST` | `/matches/{code}/start` | Gazda pornește meciul. |
| `POST` | `/matches/{code}/actions` | Trimite acțiunea rundei. Runda se rezolvă când ambii au trimis. |
| `POST` | `/matches/{code}/leave` | Părăsește meciul. Într-un meci în desfășurare, îl încheie prin abandon. |

Identitatea nu circulă niciodată prin body sau prin URL: fiecare endpoint o
citește din token.

În afara prefixului `/api`, anonime și scoase de sub rate limiting:

| Rută | Ce face |
|---|---|
| `/health` | liveness — doar „procesul răspunde", **fără** atingerea bazei |
| `/health/ready` | readiness — verifică și conexiunea la bază |

Liveness-ul nu atinge baza intenționat: baza managed se suspendă la
inactivitate, iar o sondă care ar da unhealthy la trezirea ei ar reporni degeaba
un container sănătos.

## Clientul API se regenerează, nu se scrie de mână

`ApiClients/ApiClient.cs` e generat cu NSwag din documentul OpenAPI. După orice
schimbare de contract:

```bash
cd src/KingdomDuel.Api
dotnet run --generateclients true
```

Comanda pornește aplicația, scrie clientul și iese; nu are nevoie de bază de
date. **Capcană:** `KingdomDuel.Api` referă `KingdomDuel.Client`, deci un
contract schimbat rupe clientul, clientul rupe build-ul API-ului și generatorul
nu mai poate porni. Ieșirea e să se construiască API-ul fără referința la
client pentru pasul de generare.

O rută `POST` fără corp propriu se scrie `EndpointWithoutRequest`. Altfel apare
în OpenAPI cu corp JSON obligatoriu, NSwag trimite un corp gol și FastEndpoints
răspunde `400`.

## Limite de trafic

`AddRateLimiter` din framework, fereastră fixă, fără coadă. Partiționarea e pe
id-ul contului când cererea e autentificată și pe IP altfel.

| Politică | Limită |
|---|---|
| globală | 90/min |
| `create-match` | 20/oră |
| `login` | 10/min per IP |
| `register` | 5/oră per IP |
| `change-password` | 10/oră per cont |

În plus, un cont poate avea **un singur meci neterminat**.

> La publicare: partiționarea pe IP folosește `Connection.RemoteIpAddress`,
> care în spatele unui reverse proxy e adresa proxy-ului — toți utilizatorii
> ajung în aceeași partiție. Se rezolvă cu `ForwardedHeaders`. Vezi
> [DEPLOYMENT.md](src/docs/DEPLOYMENT.md).

## Documentație

| Document | Ce conține |
|---|---|
| [RULES.md](src/docs/RULES.md) | regulile complete ale jocului |
| [AUTH.md](src/docs/AUTH.md) | planul de autentificare pe etape, cu deciziile luate |
| [DEPLOYMENT.md](src/docs/DEPLOYMENT.md) | ce trebuie rezolvat pentru expunerea publică |

## Ce nu e făcut, intenționat

1. **Teste.** Nu există niciun proiect de teste; `GameEngine` și serviciile de
   autentificare sunt primele candidate.
2. **SignalR.** Clientul face polling; push-ul în timp real vine mai târziu.
3. **Curățarea meciurilor.** Un meci terminat din care n-a ieșit niciunul
   dintre cei doi rămâne în bază — nu există nimic care să-l șteargă.
4. **Resetare de parolă și confirmare de email.** Blocate de lipsa SMTP; de
   aici și consecința că o parolă uitată înseamnă cont pierdut.
5. **Google OAuth.** Următoarea fază firească — nu cere SMTP și rezolvă exact
   consecința de mai sus.
