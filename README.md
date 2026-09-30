# Kingdom Duel

Joc de duel pentru doi jucători: două regate, un număr fix de runde, iar în
fiecare rundă fiecare jucător își împarte în secret oamenii și aurul. Soldații
atacă sau apără zidul, sătenii pleacă la negoț sau ca spioni, aurul merge pe
ospețe, recruți, fortificații și catapulte. Runda se rezolvă când amândoi au
trimis, sau când expiră timpul rundei; o bătălie se joacă apoi în timp, pe ecran,
cu retragere, marș mai departe după câmp și clopotul de la spărtură.

Regulile complete sunt în [RULES.md](src/docs/RULES.md).

Proiectul e un **ASP.NET Core** care servește și API-ul, și un client
**Blazor WebAssembly**, cu persistență în **PostgreSQL**. Contul e obligatoriu:
nu se joacă fără autentificare.

## Structură

```
KingdomDuel/
├── KingdomDuel.sln
├── global.json                           dotnet test rulează pe Microsoft Testing Platform
├── dotnet-tools.json                     unelte locale (ReportGenerator)
├── build/                                coverage.ps1 + pragurile de coverage
├── tests/                                proiectele de teste (vezi „Teste”)
└── src/
    ├── docs/                             documentația (vezi mai jos)
    │
    ├── KingdomDuel.Domain/               modelele + logica de joc, fără dependențe externe
    │   ├── GameConstants.cs              fiecare număr din joc, într-un singur loc
    │   ├── GameOutcome.cs                motivul finalului → cine a câștigat (server + client)
    │   ├── Contracts/Entity.cs           baza cu Guid Id
    │   ├── Entities/Matches/             Match, PlayerSlot, RoundResult, MatchRecord
    │   ├── Entities/Users/User.cs        contul, cu normalizarea numelui
    │   ├── Enums/                        ActionType, FeastTier, PurchaseType, SpyTier, HuntTier,
    │   │                                 MatchStatus, GameOverReason, MatchOutcome
    │   └── Rounds/                       motorul: GameEngine (rezolvarea rundei și a bătăliei),
    │                                     Battle, BattleReplay, OrderPlan, KingdomState, Spying,
    │                                     LiveBattle, MarchChoice, BreachCall
    │
    ├── KingdomDuel.Application/          endpoint-urile + regulile lor
    │   ├── AppDbContext.cs
    │   ├── RateLimitPolicies.cs          numele politicilor de rate limiting
    │   ├── Features/Matches/             un endpoint per operație + RoundResolver (mersul meciului
    │   │                                 în timp), MatchRecorder, MatchQueries, MatchMappings
    │   └── Features/Auth/                Endpoints/ + Services/
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
    │   └── Configurations/               Api, Auth, RateLimit, Database, Health, Exception,
    │                                     Proxy, ClientGeneration
    │
    └── KingdomDuel.Client/               Blazor WebAssembly
        ├── Pages/                        Landing, Login, Play, Lobby, Game, Profile
        ├── Components/                   OrderPanel + OrderDraft (ordinul rundei), BattleView,
        │                                 BreachView, MarchView, CastleView, RoundTimer, RulesBook, …
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
- **Docker Desktop** — Postgres rulează în container, nu instalat pe mașină

## Cum rulezi

```bash
docker compose up -d
dotnet build
dotnet run --project src/KingdomDuel.Api
```

Pornește pe `http://localhost:5080` (vezi `Properties/launchSettings.json`) și
servește acolo și clientul Blazor. În dezvoltare, Swagger UI e la
`/swagger`; în producție documentul OpenAPI nu se generează deloc.

Migrările EF se aplică automat la pornire, deci baza goală din container e
suficientă — nu trebuie pregătită în niciun fel.

## Baza de date

Local, Postgres vine din `compose.yaml`; în producție e pe **Neon**. Aplicația
nu știe diferența: primește doar un connection string.

```bash
docker compose up -d          # pornește baza (urcă singură și la restart)
docker compose ps             # „healthy” = e gata de conexiuni
docker compose logs postgres
docker compose down           # oprește containerul, păstrează datele
docker compose down -v        # șterge și volumul: bază goală la următoarea pornire
```

Datele stau în volumul `kingdomduel-pgdata`, deci supraviețuiesc oprii
containerului. Portul publicat e **5433**, nu 5432: pe o mașină cu un serviciu
Postgres nativ pornit, containerul n-ar putea lega 5432, iar aplicația s-ar
conecta în tăcere la serverul nativ și și-ar face baza acolo. Un client extern
(pgAdmin, DataGrip) se conectează la `localhost:5433`, user și parolă `postgres`.

Connection string-ul se suprapune în trei straturi, în ordinea în care câștigă:

| Unde | Când se aplică |
|---|---|
| `appsettings.Development.json` | în dezvoltare, containerul local pe 5433 |
| user secrets (`kingdomduel-server`) | doar pe mașina ta, peste cel de sus |
| `ConnectionStrings__KingdomDuelDb` | în producție, din Secret Manager |

`appsettings.json` **nu** are o valoare implicită, deliberat: o revizie de
producție fără secret eșuează la pornire cu cauza în log, în loc să pornească
spre `localhost`. Dacă ai nevoie de altceva pe mașina ta — alt port, altă bază —
suprascrie local, fără să atingi repo-ul:

```bash
dotnet user-secrets set "ConnectionStrings:KingdomDuelDb" "Host=localhost;Port=5434;Database=KingdomDuel;Username=postgres;Password=postgres" --project src/KingdomDuel.Api
```

DbContext-ul e în `Application`, migrările în `Persistence`, startup project-ul
e `Api`:

```bash
dotnet ef migrations add <Nume> --project src/KingdomDuel.Persistence --startup-project src/KingdomDuel.Api
dotnet ef database update --project src/KingdomDuel.Persistence --startup-project src/KingdomDuel.Api
```

Comenzile `dotnet ef` rulează în environment `Development` (îl iau din
`launchSettings.json`), deci văd același connection string ca aplicația.

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
| `GET` | `/auth/profile/history` | Ultimele 10 meciuri încheiate + victorii, înfrângeri, egaluri pe tot istoricul. |
| `POST` | `/matches` | Creează un meci. Fără body — identitatea vine din token. Răspuns: cod + starea meciului. |
| `GET` | `/matches/mine` | Meciul în desfășurare al apelantului, dacă există. |
| `GET` | `/matches/{code}` | Starea meciului (polling). Doar pentru participanți — altfel `404`. |
| `POST` | `/matches/{code}/join` | Alăturare cu codul. Idempotent: dacă ești deja pe un slot, întoarce starea. |
| `PUT` | `/matches/{code}/lobby` | Setările meciului (runde, aur/trupe/săteni/moral de start, timp pe rundă) + starea *ready*. |
| `POST` | `/matches/{code}/start` | Gazda pornește meciul. |
| `POST` | `/matches/{code}/orders` | Ordinul rundei: oamenii pe direcții, ospăț, cumpărături, săteni cumpărați și înarmați, spioni, căutarea spionilor. Când au ordonat amândoi, runda se rezolvă. |
| `POST` | `/matches/{code}/retreat` | Sună retragerea din bătălia în desfășurare. |
| `POST` | `/matches/{code}/march` | După câmpul câștigat: `{ advance }` — mergi la cetate sau te întorci. |
| `POST` | `/matches/{code}/rally` | La spărtură, apărătorul își cheamă satul la arme: `{ villagers }`. |
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

Fișierele statice și ruta de fallback a clientului sunt **scoase** de sub
limitare. O încărcare proaspătă de Blazor WASM cere ~80 de resurse, iar
contorizate ar epuiza singure limita globală și pagina ar primi 429 în timp ce
se încarcă.

Partiționarea pe IP folosește `Connection.RemoteIpAddress`, care în spatele unui
reverse proxy ar fi adresa proxy-ului — toți utilizatorii într-o singură
partiție. De aceea `ForwardedHeaders` e primul middleware din `Program.cs`.

## Teste

| Proiect | Ce testează |
|---|---|
| `tests/KingdomDuel.UnitTests` | motorul de joc (câte un fișier pe secțiune din `RULES.md`, plus meciuri întregi jucate pe ordine aleatoare), validatorii, `RoundResolver` cu ceas fals, ce vede fiecare jucător, istoricul |
| `tests/KingdomDuel.IntegrationTests` | API-ul cap-coadă pe un Postgres real, pornit în container prin Testcontainers: conturi, lobby, joc, bătălii |
| `tests/KingdomDuel.Client.Tests` | logica de autentificare din client, calculele din panoul de ordine (comparate cu motorul) și, cu bUnit, câteva componente |

xUnit v3 pe Microsoft Testing Platform, aserțiuni cu Shouldly. Testele de
integrare au nevoie de **Docker Desktop pornit**.

```bash
dotnet test --solution KingdomDuel.sln
```

Cu coverage, raport HTML și verificarea pragurilor, dintr-o singură comandă —
aceeași pe care o rulează CI-ul:

```powershell
./build/coverage.ps1 -Open
```

Raportul iese în `coverage/index.html`. Pragurile, pe proiect, stau în
`build/coverage-gate.cs`.

## Publicare

Jocul rulează pe **Google Cloud Run** (`us-east1`), cu Postgres pe **Neon**
(`us-east-2`) și deploy automat la fiecare push pe `main`, prin GitHub Actions.
Workflow-ul e în `.github/workflows/deploy.yml`: job-ul `test` rulează toate
testele și pragurile de coverage, iar `deploy` pornește doar dacă ele trec.

Autentificarea la Google se face prin **Workload Identity Federation**, deci nu
există nicio cheie de serviciu salvată în repo. Cele trei secrete din GitHub
sunt doar identificatori; siguranța stă în condiția care leagă federația de
acest repository.

Imaginea se construiește din `Dockerfile` — `sdk:10.0` pentru build,
`aspnet:10.0` pentru rulare, ca utilizator fără privilegii. Migrările EF se
aplică la pornirea containerului, iar serviciul rulează cu o singură instanță,
ca migrările să nu se poată suprapune.

Fișierul `compose.yaml` e strict pentru dezvoltare: e în `.dockerignore`, deci
nu ajunge nici în contextul de build, nici în imagine.

Secretele de producție — connection string-ul și cheia JWT — stau în Secret
Manager și se injectează ca variabile de mediu.

## Documentație

| Document | Ce conține |
|---|---|
| [RULES.md](src/docs/RULES.md) | regulile complete ale jocului |

Deciziile de arhitectură nu au document propriu: trăiesc în mesajele de commit,
unde stau lângă schimbarea pe care o explică.

## Ce nu e făcut, intenționat

1. **SignalR.** Clientul face polling la 2 s; push-ul în timp real vine mai
   târziu. Polling-ul e și motorul expirării rundelor — nu există planificator
   de fundal, iar containerul doarme când nu joacă nimeni.
2. **Curățarea meciurilor.** Un meci terminat din care n-a ieșit niciunul
   dintre cei doi rămâne în bază — nu există nimic care să-l șteargă. De când
   rezultatul se păstrează separat, în `match_records`, ștergerea nu mai pierde
   nimic; e prima datorie tehnică de plătit.
3. **Resetare de parolă și confirmare de email.** Blocate de lipsa SMTP; de
   aici și consecința că o parolă uitată înseamnă cont pierdut.
4. **Google OAuth.** Următoarea fază firească — nu cere SMTP și rezolvă exact
   consecința de mai sus.
