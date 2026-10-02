# Kingdom Duel

## Prezentare

Joc de duel pe runde pentru doi jucători, în browser. Fiecare jucător conduce
un regat și, în fiecare rundă, își împarte în secret oamenii și aurul: soldați
la atac sau la zid, săteni la negoț sau ca spioni, aur pe ospețe, recruți,
fortificații și catapulte. Runda se rezolvă când au trimis amândoi sau când
expiră timpul; bătăliile se joacă apoi în timp, pe ecran, cu retragere, marș
după câmpul câștigat și clopotul de la spărtură.

Contul e obligatoriu. Regulile complete: `src/docs/RULES.md`. Detaliile de
rulare, API și publicare: `README.md`.

## Funcționalități

- Conturi proprii (înregistrare, login, profil, schimbare parolă), JWT cu
  reînnoire și revocare prin versiunea tokenului — fără pachetul Identity
- Lobby pe cod de meci: setări (runde, resurse de start, timp pe rundă), *ready*, start
- Ordinul rundei, rezolvarea rundei de motorul de joc, expirare pe timp și abandon
- Bătălie live (retragere), marș după câmp, chemarea satului la spărtură
- Spionaj și vânătoarea spionilor; fiecare jucător vede doar ce are voie
- Istoric de meciuri și revanșă
- Rate limiting pe cont/IP, health checks (liveness fără bază, readiness cu bază)

## Stack

- **Limbaj:** C# pe .NET 10 (`Nullable` și `ImplicitUsings` activate)
- **Backend:** ASP.NET Core + FastEndpoints 8 (cu Swagger/OpenAPI și ClientGen)
- **Frontend:** Blazor WebAssembly, servit de același host; Bootstrap
- **Bază de date:** PostgreSQL 18 (local în Docker pe portul 5433, producție pe Neon)
- **ORM:** EF Core 10 + Npgsql; migrări aplicate automat la pornire
- **Validare:** FluentValidation, în `KingdomDuel.Shared`, rulată pe server
- **Client API:** generat cu NSwag din documentul OpenAPI
- **Teste:** xUnit v3 pe Microsoft Testing Platform, Shouldly, bUnit, Testcontainers (Postgres)
- **Coverage:** coverlet.MTP + ReportGenerator, praguri în `build/coverage-gate.cs`
- **Publicare:** Docker → Google Cloud Run, GitHub Actions cu Workload Identity Federation

## Note de arhitectură

- Layere cu dependențe spre interior: `Api → Application → Domain`;
  `Persistence` și `Shared` sunt referite unde e nevoie. `Domain` nu are
  dependențe externe.
- `Domain` ține motorul de joc ca funcții pure (`GameEngine`), cu `Random`
  injectat, și toate numerele jocului în `GameConstants`.
- `Application` conține endpoint-urile FastEndpoints organizate pe feature
  (`Features/Matches`, `Features/Auth`), plus serviciile lor. `Api` e doar
  gazda și configurările (`Configurations/*Configuration.cs`).
- `Shared` e contractul comun server–client: Requests, Responses, Dtos, Validators.
- Timpul vine prin `TimeProvider`, ca testele să folosească ceas fals.
- Identitatea se citește mereu din token, niciodată din body sau URL.

## Cerințe nefuncționale

- **Logging:** `ILogger` din ASP.NET Core; erorile neprinse se loghează în
  handler-ul global, care întoarce un mesaj generic în producție
- **Erori:** erori de validare și de business prin `AddError` + `Send.ErrorsAsync`
  (FastEndpoints), mesaje pentru utilizator în română
- **Securitate:** JWT, parole hash-uite, rate limiting, `ForwardedHeaders` în
  spatele proxy-ului, fără secrete în repo
- **UI:** fiecare pagină încape într-un singur viewport; panourile care cresc
  se derulează în interior

## Arhitectură

Ghidurile detaliate de arhitectură sunt în `.ai-factory/ARCHITECTURE.md`.
Pattern: Explicit Architecture (Flat Vertical Slice — simplificat)
