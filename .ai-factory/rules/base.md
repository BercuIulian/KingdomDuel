# Reguli de bază ale proiectului

> Convenții detectate automat din cod. Editează-le după nevoie.

## Convenții de denumire

- Fișiere: un tip per fișier, numele fișierului = numele tipului (PascalCase); componentele Blazor au `.razor` + `.razor.css` alăturat
- Variabile și parametri: camelCase; câmpurile private fără prefix `_` (constructori primari)
- Metode și proprietăți: PascalCase; metodele async se termină în `Async`
- Clase: PascalCase, `sealed` implicit; `internal` în `Application` (endpoint-uri, servicii)
- Endpoint-uri: `<Acțiune><Resursă>Endpoint` (ex. `JoinMatchEndpoint`), cu `Description(b => b.WithName("<Acțiune><Resursă>"))`
- Validatori: `<Request>Validator`, în `KingdomDuel.Shared/Validators/<Feature>`
- Configurări gazdă: `<Zonă>Configuration` cu metode de extensie `Add…Configuration` / `Use…Configuration`
- Testele: metode cu nume-propoziție în snake_case (`A_lone_army_marches_on_the_castle`), clase `<Subiect>Tests`

## Structura modulelor

- `KingdomDuel.Domain` — entități, enumuri, motorul de joc (`Rounds/`), `GameConstants`; fără dependențe externe
- `KingdomDuel.Application` — `Features/<Feature>/` cu endpoint-uri FastEndpoints, servicii, `*Errors`, `*Mappings`, `*Queries`, `*ServiceConfiguration`
- `KingdomDuel.Persistence` — `IEntityTypeConfiguration` per entitate + migrări EF
- `KingdomDuel.Shared` — `Requests/`, `Responses/`, `Dtos/`, `Validators/` pe feature
- `KingdomDuel.Api` — doar `Program.cs` și `Configurations/`; endpoint-urile **nu** stau aici
- `KingdomDuel.Client` — `Pages/`, `Components/`, `Layout/`, `Authentication/`, `ApiClients/`
- Dependențele merg spre interior: `Api → Application → Domain`
- Orice număr de joc nou merge în `GameConstants`, nu inline
- Timpul se ia din `TimeProvider`, aleatorul din `Random` injectat — niciodată `DateTime.UtcNow` sau `new Random()` în logică

## Contractul API și clientul

- `ApiClients/ApiClient.cs` se regenerează (`dotnet run --generateclients true` din `src/KingdomDuel.Api`), nu se editează de mână
- Un `POST` fără corp se scrie `EndpointWithoutRequest`, altfel NSwag trimite corp gol și răspunsul e `400`
- Identitatea utilizatorului se citește din token (`User.GetUserId()`), niciodată din body sau rută

## Tratarea erorilor

- Erori de business: `AddError("mesaj în română")` sau `AddError(<Feature>Errors.X(...))`, apoi `await Send.ErrorsAsync(cancellation: ct)` și `return`
- Resursă lipsă: `Send.NotFoundAsync(ct)`; utilizator inexistent: `Send.UnauthorizedAsync(ct)`
- Validarea de format stă în validatorii FluentValidation din `Shared`, cu `.WithMessage(...)` în română
- Excepțiile se aruncă doar pentru stări imposibile sau configurare greșită; handler-ul global (`ExceptionConfiguration`) le loghează și întoarce un mesaj generic în producție

## Flux de control

- Preferă un flux plat și ușor de citit în locul condițiilor imbricate adânc. Folosește guard clauses, `return`/`continue` timpuriu, metode ajutătoare mici cu nume clar sau o clasificare explicită când fac codul mai ușor de urmărit. Tratează din timp cazurile limită și ramurile irelevante, ca drumul principal să rămână vizibil.

## Logging

- `ILogger` / `ILoggerFactory` din ASP.NET Core, cu mesaje structurate (`{Method} {Path}`), în română
- Se loghează puțin: momentan doar erorile neprinse, în `ExceptionConfiguration`

## Comentarii

- Comentezi doar ce codul nu poate spune singur; raționamentul schimbării merge în mesajul de commit

## Teste

- xUnit v3 + Shouldly (`ShouldBe`), `Using` globale în `tests/Directory.Build.props`
- Unit: `tests/KingdomDuel.UnitTests/{Domain,Application,Shared}`, cu helper-e de construcție (`GameSetup`)
- Integrare: `tests/KingdomDuel.IntegrationTests`, Postgres real prin Testcontainers (Docker Desktop pornit)
- Client: `tests/KingdomDuel.Client.Tests`, bUnit pentru componente
- Pragurile de coverage din `build/coverage-gate.cs` trebuie să treacă (`./build/coverage.ps1`)
