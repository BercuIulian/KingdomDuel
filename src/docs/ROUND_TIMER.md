# Limită de timp per rundă — plan pe etape

Trei lucruri legate între ele:

1. **cronometru per rundă**, reglabil în lobby, implicit **45 s**;
2. **acțiunea „nu face nimic"** — alegere validă, fără beneficiu;
3. **abandon prin inactivitate** — 3 runde ratate încheie meciul.

Șapte etape, fiecare lăsând jocul într-o stare funcțională și livrabilă. La
finalul fiecăreia sunt **deciziile de luat înainte de a scrie codul acelei etape**,
cu recomandarea mea. Ordinea nu e arbitrară: partea riscantă (etapa 5) vine după
un refactor sigur (etapa 3) și după ce cronometrul a fost deja văzut în joc fără
să poată face rău (etapa 4).

---

## Cum funcționează cronometrul — decizia de fond

Aplicația n-are niciun planificator, iar containerul doarme (`--min-instances=0`),
deci un serviciu de fundal n-ar rula tocmai când ar conta.

În schimb: meciul ține momentul limită al rundei, iar **orice cerere care atinge
meciul verifică întâi dacă termenul a trecut** și, dacă da, completează acțiunile
lipsă cu „nu face nimic" și rezolvă runda înainte de a-și face treaba proprie.
Polling-ul de 2 s existent devine motorul care declanșează expirarea.

Consecință: **ceasul serverului e singurul care contează.** Cronometrul din browser
e decorativ; un client cu ceasul modificat nu câștigă nimic.

---

## Etapa 1 — Acțiunea „nu face nimic"

Fără cronometru încă. Doar a cincea acțiune, aleasă manual.

**De făcut:**
- `ActionType.Idle`, adăugat **la finalul** enum-ului.
- Verificat că motorul se comportă corect — vezi mai jos, probabil zero linii noi.
- Card nou în lista `Orders` din `Game.razor`, plus intrări în `OrderName` și
  `OrderVerb`.
- `RULES.md`, secțiunile 3 și 4.

**Ce am aflat citind motorul:** `ApplySelfAction` e un `switch` fără `default`,
deci `Idle` cade prin el fără efect. Iar `ResolveInteraction` tratează orice nu e
`Attack` sau `Defend` ca „nefortificat", deci intră pe ramura din
`ResolveOneSidedAttack` unde atacatorul fură 15 aur și 10 trupe. **Exact
comportamentul cerut, fără nicio modificare.** Enum-urile se persistă ca string
(`HasConversion<string>`), deci valoarea nouă nu strică rândurile existente și nu
cere migrare.

### De concretizat

**1.1 — Cum se numește în interfață?** `Idle` e numele intern. În română:
**„Răgaz"** (ton medieval, se potrivește cu „Fortificare" și „Diplomație") sau
**„Așteptare"** (mai clar, mai plat). Recomand „Răgaz".

**1.2 — Verbul pentru cronică.** Apare ca „*X a…*" lângă „a lansat un atac", „s-a
fortificat". Pentru „Răgaz" propun **„a stat deoparte"**.

**1.3 — `Idle` resetează seria de fortificare?** Recomand **da**. `DefenseStreak`
scumpește fortificarea repetată; dacă `Idle` ar păstra seria, ai putea alterna
fortificare–răgaz–fortificare ca să ocolești penalizarea — și atunci acțiunea
„fără beneficiu" devine un beneficiu.

**1.4 — Cum descriem riscul pe card?** Textul ar trebui să spună ce riști, nu doar
ce nu primești. Propun: *„Nu primești nimic. Dacă adversarul atacă, atacul reușește
complet."*

---

## Etapa 2 — Setarea din lobby

Valoarea se salvează și se afișează, dar încă nu face nimic.

**De făcut:**
- `GameConstants.DefaultRoundSeconds = 45`.
- `Match.RoundSeconds` + configurație EF + **migrare**.
- `UpdateLobbyRequest.RoundSeconds` + regulă în `UpdateLobbyValidator`.
- `MatchStateDto.RoundSeconds`, apoi **regenerarea clientului**
  (`dotnet run --generateclients true` — nu se scrie de mână).
- Câmp nou în panoul *Setări meci* din `Lobby.razor`, cu același
  `@bind:after="PersistLobbyAsync"`. Doar gazda îl schimbă, ca restul.

Meciurile existente primesc implicit 45 s, ceea ce e corect pentru ele.

### De concretizat

**2.1 — Ce interval permitem?** Recomand **15–180 secunde**. Sub 15 nu apuci să
citești evenimentul rundei; peste 180 cronometrul nu mai înseamnă nimic.

**2.2 — Există „fără limită"?** Recomand **da, `0` = dezactivat**. Până acum jocul
n-a avut cronometru, iar doi prieteni care joacă pe îndelete ar trebui să poată
reveni la comportamentul vechi. Costă puțin acum și e greu de adăugat mai târziu,
fiindcă atinge fiecare verificare din etapele 4–6.

**2.3 — Pasul din interfață.** Celelalte câmpuri au `step` (10 la aur, 5 la moral).
Propun **15**, cu 45 la mijloc.

---

## Etapa 3 — Refactor: un singur loc care avansează runda

**Fără nicio schimbare de comportament.** Etapă pur tehnică, dar fundamentul
etapelor 4–6.

Azi, logica de rezolvare stă înghesuită în `SubmitActionEndpoint.HandleAsync`. Din
etapa 5 vor exista **două** căi care rezolvă o rundă — submisie completă și
expirare. Dacă rămân duplicate, vor diverge la prima modificare.

**De făcut:**
- Serviciu nou, `RoundResolver`, în `Features/Matches/`.
- Se mută în el: verificarea „au trimis amândoi", apelul motorului, adăugarea în
  istoric, golirea acțiunilor în așteptare, incrementarea rundei, evenimentul nou,
  încheierea meciului.
- `SubmitActionEndpoint` doar înregistrează acțiunea și cheamă `RoundResolver`.

**Verificat separat înainte de a merge mai departe** — un meci jucat cap-coadă
trebuie să se comporte identic cu azi. De aici încolo totul se sprijină pe pasul
ăsta.

### De concretizat

Nimic de decis. Dacă vrei, aici e momentul potrivit pentru **primele teste
unitare** din proiect: `RoundResolver` și `GameEngine` sunt logică pură, fără
dependențe, iar acum ai și un motiv concret — să dovedești că refactorul n-a
schimbat nimic.

---

## Etapa 4 — Cronometrul, vizibil dar fără efect

Serverul ține termenul, clientul îl arată, **dar expirarea nu face încă nimic**.
Poți juca și simți ritmul înainte ca timpul să poată face rău cuiva.

**De făcut:**
- `Match.RoundDeadline` (`DateTimeOffset?`, absolut UTC) + migrare. Absolut, nu
  relativ — altfel repornirea containerului ar prelungi runda.
- Setat la pornirea meciului și la fiecare rezolvare, în `RoundResolver`.
- `MatchStateDto.SecondsRemaining` (`int?`), calculat de server.
- În `Game.razor`: al doilea ticker, la ~100 ms, care scade o valoare locală și
  redesenează. La fiecare poll valoarea se **rescrie** din `SecondsRemaining`, deci
  nu poate diverge de server. Se oprește la `Completed` și în `DisposeAsync`.

**`SecondsRemaining` e relativ, nu absolut, deliberat.** Dacă serverul ar trimite
momentul limită, clientul l-ar compara cu propriul ceas — iar un ceas decalat cu 30
de secunde ar arăta un cronometru complet greșit. Cu o valoare relativă,
resincronizată la fiecare 2 secunde, decalajul nu contează.

### De concretizat

**4.1 — Bară sau numărătoare inversă?** Recomand **bară care se golește**, cu
numărul apărând doar în ultimele 10 secunde, odată cu trecerea la o culoare de
avertizare. Un număr care ticăie tot timpul mută atenția de pe tablă pe ceas; bara
se citește dintr-o privire, iar cifra devine semnal de urgență tocmai fiindcă până
atunci n-a fost acolo.

**4.2 — Pauză de citire între runde?** Ăsta e efectul secundar la care mă aștept
cel mai puțin să te fi gândit. Azi, când amândoi trimit ordinul, runda se rezolvă
instant și următoarea începe imediat. Cu cronometru, cele 45 de secunde ale rundei
noi ar curge **în timp ce jucătorul încă citește cronica** — partea cea mai frumoasă
a jocului. Variante: **(a)** accepți, timpul e generos oricum; **(b)** pauză fixă de
~5 s după rezolvare, în care cronometrul nu curge; **(c)** prima rundă primește timp
dublu. Recomand **(b)**. E decizie de design, nu tehnică — dar dacă alegi (b),
`RoundDeadline` se setează cu decalaj, deci e mai ieftin acum decât după etapa 5.

**4.3 — Ce se vede când timpul a ajuns la zero?** Clientul nu decide nimic — doar
afișează. Propun un text discret, *„Timpul a expirat, se așteaptă rezolvarea"*,
maximum 2 secunde până la următorul poll.

---

## Etapa 5 — Expirarea

Aici cronometrul începe să conteze. **Etapa cu cel mai mare risc din tot planul.**

**De făcut:**
- `RoundResolver.ExpireIfDue(match)`: dacă meciul e în desfășurare, termenul a
  trecut și lipsesc acțiuni — se completează cu `Idle` și se rezolvă runda.
- Apelat la începutul a două endpoint-uri:

| Endpoint | De ce |
|---|---|
| `GetMatchEndpoint` | **motorul principal** — polling-ul adversarului declanșează expirarea |
| `SubmitActionEndpoint` | ca o submisie întârziată să nu „învie" o rundă deja expirată |

- **Protecție împotriva rezolvării duble.**

**Riscul, explicit:** doi jucători pot face poll în aceeași milisecundă, ambii pot
vedea termenul depășit și ambii pot rezolva runda — rezultatul ar fi două runde
avansate dintr-o dată și o rundă fantomă în istoric. `--max-instances=1` **nu**
protejează: cererile ajung pe același proces, dar nu sunt serializate. E nevoie de
token de concurență optimistă pe `Match` (pe Postgres, `xmin`:
`builder.Property<uint>("xmin").IsRowVersion()`) și reîncercare pe
`DbUpdateConcurrencyException`.

Dacă amândoi jucătorii închid pagina, meciul rămâne blocat — aceeași situație ca
azi, ține de curățarea meciurilor, problemă separată.

### De concretizat

**5.1 — Perioadă de grație pentru latență?** Un ordin trimis la secunda 44,8 poate
ajunge la server la 45,3 — corect din perspectiva jucătorului, expirat din a
serverului. Recomand **2 secunde**, invizibile în interfață: jucătorul vede zero,
dar ordinul lui tot intră.

**5.2 — Cum tratăm conflictul de concurență?** Recomand **`xmin` + o reîncercare**
prin recitirea stării. Alternativa mai simplă — verificarea că `Round` n-a fost
schimbat între citire și salvare — merge, dar acoperă mai puțin.

**5.3 — Expirarea se aplică și când adversarul a trimis deja?** Da, evident — dar
merită spus: dacă unul a trimis și celălalt nu, la expirare runda se rezolvă cu
ordinul primului și `Idle` pentru al doilea. Nu se anulează nimic.

---

## Etapa 6 — Abandon prin inactivitate

**De făcut:**
- `PlayerSlot.MissedRounds` + migrare.
- `GameOverReason.Player1Disconnected` / `Player2Disconnected` — separate de
  `Player1Left`/`Player2Left`, care înseamnă „a apăsat *Părăsește*". Textul diferă,
  și e bine să se distingă în istoric.
- Incrementare în `ExpireIfDue`, verificarea pragului după rezolvare.
- `MatchStateDto.Player1MissedRounds` / `Player2MissedRounds` + regenerare client.
- Textul de final în `VerdictText` și `ResultEyebrow` din `Game.razor`.
- `RULES.md`, secțiunea 6.

### De concretizat

**6.1 — „Nu face nimic" ales deliberat intră în contor?** Recomand **nu**. Dacă
intră, o alegere tactică legitimă — să te ferești într-o rundă în care orice
acțiune te-ar costa — devine un pas către pierderea meciului, iar acțiunea de la
etapa 1 își pierde rostul. Contorul ar trebui să numere doar *absența* jucătorului.

**6.2 — 3 ratări consecutive sau cumulate pe meci?** Ai scris „de 3 ori în timpul
meciului", ceea ce sună a cumulat. Recomand **consecutive**: cineva cu internet
prost care ratează o rundă la început și una la final nu e plecat de la calculator,
dar ar fi tratat ca atare. Consecutiv măsoară ce vrei de fapt.

**6.3 — Se vede contorul?** Recomand **da, pentru amândoi**, lângă numele
jucătorului, apărând doar de la prima ratare. Dacă meciul se încheie brusc la a
treia fără avertisment, pare arbitrar. Contra-argument: dezvăluie adversarului că
ai probleme de conexiune.

**6.4 — Ce se întâmplă dacă amândoi ating pragul în aceeași rundă?** Caz de
margine, dar trebuie decis ca să nu apară un meci fără învingător declarat.
Recomand **egalitate**, cu motiv propriu, tratat ca `BothCollapsed` în interfață.

**6.5 — Pragul rămâne 3, fix?** Recomand **da, constantă în `GameConstants`**, nu
setare de lobby. Încă un câmp în lobby pentru ceva ce nimeni nu va regla e cost
fără câștig.

---

## Etapa 7 — Documentație și verificare

**De făcut:**
- `RULES.md`: secțiunea 2 (limita de timp), 3 (a cincea acțiune), 4 (`Idle` față de
  atac), 6 (abandonul prin inactivitate). Comentariul din capul lui `GameEngine`
  spune explicit că regulile și codul se schimbă împreună.
- Scenariile de verificare de mai jos.

Fluxul cere doi jucători, iar clientul ține token-ul în `localStorage` — două taburi
ar împărți același cont. Ca la testul de deploy: **un jucător în browser, celălalt
prin API cu `curl`**, ceea ce se potrivește bine aici, fiindcă scenariile
interesante înseamnă tocmai *absența* unei acțiuni.

| Scenariu | Ce verifică |
|---|---|
| Expirare cu un jucător inactiv | runda se rezolvă, el primește `Idle` |
| Expirare cu ambii inactivi | runda se rezolvă, nimic nu se întâmplă |
| `Idle` contra `Attack` | atacatorul fură integral |
| Submisie la limita termenului | perioada de grație (5.1) |
| 3 ratări | meci încheiat, motiv corect, adversarul câștigă |
| Reîncărcarea paginii la mijlocul rundei | cronometrul continuă corect |
| **Doi jucători care fac poll simultan la expirare** | **o singură rundă avansată** |

Ultimul e cel care ar scăpa cel mai ușor și ar fi cel mai greu de diagnosticat în
producție.

---

## Rezumatul deciziilor

Cele care schimbă jocul sunt **1.3**, **4.2**, **6.1** și **6.2**. Restul sunt
detalii pe care merg cu varianta recomandată dacă nu spui altceva.

| # | Decizie | Recomandarea mea |
|---|---|---|
| 1.1 | Numele acțiunii | „Răgaz" |
| 1.2 | Verbul din cronică | „a stat deoparte" |
| 1.3 | `Idle` resetează seria de fortificare | da |
| 1.4 | Textul de risc pe card | „…dacă adversarul atacă, atacul reușește complet" |
| 2.1 | Intervalul din lobby | 15–180 s |
| 2.2 | Opțiunea „fără limită" | da, `0` |
| 2.3 | Pasul din interfață | 15 |
| 4.1 | Bară sau numărătoare | bară, cifră în ultimele 10 s |
| 4.2 | Pauză de citire între runde | da, ~5 s |
| 4.3 | Afișaj la zero | text discret |
| 5.1 | Grație pentru latență | 2 s |
| 5.2 | Protecție la concurență | `xmin` + o reîncercare |
| 6.1 | `Idle` deliberat intră în contor | nu |
| 6.2 | Ratări consecutive sau cumulate | consecutive |
| 6.3 | Contorul e vizibil | da, ambii |
| 6.4 | Ambii ating pragul simultan | egalitate |
| 6.5 | Pragul e reglabil | nu, constantă |
