# Arhitectură: Explicit Architecture (Flat Vertical Slice — simplificat)

## Prezentare

Domeniul e centrul aplicației: `KingdomDuel.Domain` ține entitățile, enumurile
și motorul de joc (`Rounds/GameEngine`) ca logică pură, fără nicio dependență
de framework, bază sau HTTP. Tot restul — FastEndpoints, EF Core, Blazor — e
periferie care se conectează la domeniu.

Cazurile de utilizare sunt organizate în **slice-uri plate pe feature**:
`Application/Features/<Feature>/` conține, unul lângă altul, câte un endpoint
pe operație, serviciile, interogările, mapările și erorile acelui feature.
Varianta e aleasă pentru că jocul are reguli multe (domeniu bogat), iar
operațiile sunt mici și independente; un singur fișier per operație e mai ușor
de urmărit decât straturi separate Application/Infrastructure/Presentation.

Documentul descrie **structura existentă**. Abaterea asumată față de varianta
strictă: nu există porturi și repository-uri — endpoint-urile folosesc
`AppDbContext` direct, cu interogări grupate în metode de extensie.

## Motivarea deciziei

- **Tip de proiect:** joc web pe runde, doi jucători, server autoritar
- **Stack:** C# / .NET 10, ASP.NET Core + FastEndpoints, EF Core + PostgreSQL, Blazor WASM
- **Factor principal:** regulile jocului trebuie testate izolat și determinist
  (`Random` și `TimeProvider` injectate), deci domeniul rămâne pur; restul
  rămâne subțire și pragmatic

## Structura folderelor

```
src/
├── KingdomDuel.Domain/              # DOMENIU PUR — zero dependențe externe
│   ├── GameConstants.cs             # fiecare număr din joc
│   ├── GameOutcome.cs
│   ├── Contracts/Entity.cs          # baza cu Guid Id
│   ├── Entities/{Matches,Users}/    # entitățile persistate
│   ├── Enums/
│   └── Rounds/                      # motorul: GameEngine, Battle, OrderPlan, KingdomState, …
│
├── KingdomDuel.Shared/              # CONTRACTUL server–client
│   ├── Requests/<Feature>/
│   ├── Responses/<Feature>/
│   ├── Dtos/<Feature>/
│   └── Validators/<Feature>/        # FluentValidation, rulat pe server
│
├── KingdomDuel.Application/         # CAZURI DE UTILIZARE
│   ├── AppDbContext.cs
│   ├── RateLimitPolicies.cs
│   └── Features/
│       ├── Matches/                 # slice plat: un endpoint per operație + ajutoare
│       │   ├── <Acțiune>MatchEndpoint.cs
│       │   ├── RoundResolver.cs     # mersul meciului în timp
│       │   ├── MatchQueries.cs      # extensii pe IQueryable<Match>
│       │   ├── MatchMappings.cs     # entitate → DTO
│       │   ├── MatchErrors.cs
│       │   └── MatchServiceConfiguration.cs
│       └── Auth/                    # slice cu Endpoints/ + Services/ (mai multe servicii)
│
├── KingdomDuel.Persistence/         # ADAPTOR DE IEȘIRE — EF Core
│   ├── Configurations/              # un IEntityTypeConfiguration per entitate
│   └── Migrations/
│
├── KingdomDuel.Api/                 # COMPOSITION ROOT
│   ├── Program.cs
│   └── Configurations/              # <Zonă>Configuration: Add…/Use…
│
└── KingdomDuel.Client/              # ADAPTOR DE INTRARE — Blazor WASM
    ├── Pages/, Components/, Layout/
    ├── Authentication/
    └── ApiClients/ApiClient.cs      # generat cu NSwag
```

## Reguli de dependență

- ✅ `Application → Domain, Shared`
- ✅ `Shared → Domain` (doar enumuri și tipuri simple)
- ✅ `Persistence → Application, Domain` (configurează `AppDbContext`, ține migrările)
- ✅ `Api → Application, Persistence, Client` (compunere și găzduire)
- ✅ `Client → Shared` și vorbește cu serverul doar prin `ApiClient`
- ❌ `Domain` nu referă nimic: nici EF Core, nici FastEndpoints, nici `Shared`
- ❌ `Client` nu referă `Domain` sau `Application` direct
- ❌ Endpoint-uri în `Api` — ele stau în `Application`, `Api` doar le înregistrează
- ❌ Un slice nu folosește tipurile `internal` ale altui slice; ce se împarte
  (ex. `User.GetUserId()` din `Auth`) se expune ca extensie publică/internă a assembly-ului

## Comunicarea între straturi

- Cererea intră printr-un endpoint FastEndpoints, care primește request-ul din
  `Shared`, citește identitatea din token și încarcă entitatea prin `AppDbContext`
- Regulile jocului se aplică apelând domeniul (`GameEngine`, metodele entităților);
  endpoint-ul doar orchestrează: încarcă → apelează domeniul → salvează → mapează
- Răspunsul pleacă ca DTO din `Shared`, produs de `<Feature>Mappings`, filtrat
  pe ce are voie să vadă jucătorul care cere
- Interogările refolosite stau în `<Feature>Queries` ca extensii pe `IQueryable<T>`
- Logica legată de timp (expirări, bătălii live) stă în servicii scoped
  (`RoundResolver`) cu `TimeProvider` injectat
- Clientul folosește exclusiv `ApiClient` generat; contractul se schimbă în
  `Shared`, apoi clientul se regenerează

## Principii

1. **Domeniul e pur și determinist.** Fără I/O, fără ceas de sistem, fără
   `new Random()`: aleatorul și timpul se primesc ca parametri sau prin DI.
2. **Un endpoint per operație.** Fișierul endpoint-ului e cazul de utilizare;
   când orchestrarea crește, se extrage un serviciu în același slice.
3. **Contractul e în `Shared`, validarea de format tot acolo.** Regulile de
   business (stare, permisiuni) se verifică în endpoint, prin `AddError`.
4. **Serverul e autoritar.** Clientul nu calculează rezultate de joc; poate doar
   previzualiza calcule, verificate în teste contra motorului.
5. **Persistența e un detaliu.** Configurările EF și migrările stau în
   `Persistence`; entitățile din `Domain` nu poartă atribute EF.

## Notă despre organizarea codului

- **Feature-uri noi:** urmează structura din acest document — un folder sub
  `Features/`, un endpoint per operație, contractul în `Shared/<Feature>`.
- **Cod existent:** structura actuală e documentată ca atare. La modificări,
  preferă convențiile de aici, dar nu rescrie cod fără legătură cu sarcina.
- **Interoperabilitate:** când cod nou folosește cod existent, preferă
  interfețe curate, fără refactorizări făcute doar pentru aliniere structurală.

## Exemple de cod

### Endpoint ca orchestrator subțire

```csharp
namespace KingdomDuel.Application.Features.Matches;

internal sealed class JoinMatchEndpoint(AppDbContext dbContext, TimeProvider time)
    : EndpointWithoutRequest<JoinMatchResponse>
{
    public override void Configure()
    {
        Post("/matches/{code}/join");
        Description(b => b.WithName("JoinMatch"));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = User.GetUserId();
        var match = await dbContext.Matches.FindByCodeAsync(Route<string>("code")!, ct);

        if (match is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        if (match.Player2 is not null)
        {
            AddError("Meciul are deja doi jucători.");
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        // … modificarea stării, apoi:
        await dbContext.SaveChangesAsync(ct);
        await Send.OkAsync(new JoinMatchResponse { State = match.ToStateDto(userId, time.GetUtcNow()) }, ct);
    }
}
```

### Interogare refolosită ca extensie în slice

```csharp
internal static class MatchQueries
{
    public static Task<string?> FindActiveCodeAsync(
        this IQueryable<Match> matches,
        Guid userId,
        CancellationToken ct)
        => matches
            .Where(m => m.Status == MatchStatus.InProgress
                        && (m.Player1!.UserId == userId || m.Player2!.UserId == userId))
            .Select(m => m.Code)
            .FirstOrDefaultAsync(ct);
}
```

### Domeniul primește aleatorul, nu-l creează

```csharp
// ✅ în Domain
public static RoundOutcome Resolve(RoundInput input, Random random) { … }

// ✅ în Application — DI dă implementarea reală, testele dau una fixă
services.TryAddSingleton(TimeProvider.System);
services.TryAddSingleton(Random.Shared);
services.AddScoped<RoundResolver>();
```

## Anti-pattern-uri

- ❌ Reguli de joc în endpoint-uri sau în client — ele stau în `Domain/Rounds`
- ❌ `DateTime.UtcNow` / `new Random()` în logică — folosește `TimeProvider` și `Random` injectate
- ❌ Numere magice în motor — orice constantă de joc merge în `GameConstants`
- ❌ Editarea de mână a `ApiClient.cs` — se regenerează din OpenAPI
- ❌ Identitatea luată din body sau rută — doar din token
- ❌ Un `POST` fără corp scris ca `Endpoint<TRequest>` — folosește `EndpointWithoutRequest`
- ❌ Repository-uri generice peste `AppDbContext` — interogările refolosite devin extensii în slice
- ❌ Atribute EF sau tipuri FastEndpoints în `Domain`
