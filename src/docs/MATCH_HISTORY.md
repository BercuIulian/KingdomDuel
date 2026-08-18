# Istoricul meciurilor în profil — plan pe etape

Lista ultimelor meciuri jucate, cu un rezumat de statistici, în pagina de profil.

Patru etape, fiecare lăsând aplicația într-o stare funcțională. La finalul
fiecăreia sunt deciziile de luat **înainte** de a scrie codul acelei etape, cu
recomandarea mea.

## Stabilit deja

- **Conținut:** doar rezumatul — adversar, rezultat, motivul finalului, dată,
  runde. Fără cronica rundă cu rundă.
- **Statistici:** un rezumat simplu — meciuri jucate, victorii, înfrângeri,
  egaluri.
- **Volum:** ultimele 10, fără paginare.

---

## Constrângerea care dictează totul

**Nu există nicăieri o urmă durabilă a meciurilor jucate.**

`LeaveMatchEndpoint` chiar șterge din bază: când ambii jucători apasă „Înapoi la
meniu", dispar meciul, rundele și legătura dintre utilizatori și meci. Meciurile
terminate din care nimeni n-a ieșit rămân doar din inerție — sunt exact
creșterea nemărginită pe care o știm ca datorie tehnică.

Două consecințe:

1. Istoricul **nu se poate construi peste tabela de meciuri**. Are nevoie de o
   înregistrare proprie, scrisă în clipa în care meciul se încheie și
   independentă de ciclul de viață al meciului.
2. **Nu se poate recupera nimic retroactiv.** Istoricul începe de la livrare.

Partea bună: înregistrarea separată deschide și curățarea meciurilor terminate,
fiindcă rezultatul nu mai depinde de rândul din `matches`. Rămâne o funcționalitate
separată — vezi ultima secțiune.

---

## Etapa 1 — Înregistrarea rezultatului

Invizibilă pentru utilizator. De aici încolo, fiecare meci încheiat lasă o urmă.

**De făcut:**
- Entitate nouă, de exemplu `MatchRecord`, fără nicio legătură cu `Match`.
- Configurație EF + **migrare**.
- Scrierea înregistrării în fiecare loc unde un meci trece în `Completed`:
  `RoundResolver` (final pe teren și abandon prin inactivitate) și
  `LeaveMatchEndpoint` (abandon explicit). Ideal printr-un singur ajutor, ca la
  `RoundResolver` — trei locuri care scriu independent ar diverge.

### De concretizat

**1.1 — Un rând per meci sau per participant?** Recomand **per participant**,
adică două rânduri pe meci. Interogarea devine `WHERE UserId = @me`, cu un singur
index, iar rezultatul se stochează *din perspectiva jucătorului* („victorie"), nu
se reinterpretează la fiecare citire. Costul e dublarea rândurilor — irelevant la
~200 de octeți pe rând. Varianta per meci economisește spațiu și ar fi mai bună
dacă am adăuga cândva cronica, dar cere `WHERE Player1Id = @me OR Player2Id = @me`
și interpretare la citire.

**1.2 — Numele adversarului: instantaneu sau referință?** Numele se pot schimba
din profil. Recomand **instantaneu**, plus id-ul adversarului păstrat separat
pentru mai târziu. Un istoric care se rescrie când altcineva își schimbă numele e
derutant: ai jucat cu cineva anume, la un moment anume.

**1.3 — Ce câmpuri intră.** Proprietarul rândului, numele adversarului, rezultatul,
motivul finalului, runda la care s-a oprit și numărul de runde stabilit (afișate
combinat, „6/8”), puterea finală a ambelor tabere, și momentul începerii meciului.
Fără codul meciului — nu e relevant pentru cineva care își recitește istoricul.

**1.4 — Cum evităm scrierea dublă.** Prima apărare e deja acolo: istoricul se
inserează în aceeași tranzacție cu trecerea meciului în `Completed`, iar aceea e
un UPDATE cu verificare pe `xmin`. Dacă doi jucători ating finalul simultan, unul
dintre UPDATE-uri eșuează și inserturile lui nu se fac deloc.

A doua apărare e un **indice unic pe (proprietar, momentul începerii)**. Un jucător
nu poate fi în două meciuri deodată, deci nu poate avea două care încep în aceeași
fracțiune de secundă. Aceeași coloană servește și la ordonarea listei și la
afișarea datei — spre deosebire de un id de meci, care n-ar fi citit niciodată.

Asta a cerut un câmp nou pe `Match`: momentul în care gazda apasă Start. Lipsea
oricum — `CreatedAt` e crearea lobby-ului, nu începerea jocului — și va fi util și
la curățarea meciurilor vechi.

**1.5 — Ce se înregistrează?** Recomand **doar meciurile care au început** — un
lobby părăsit înainte de start nu e un meci jucat. Cele încheiate prin abandon sau
inactivitate **da**: contează pentru cel care a rămas.

---

## Etapa 2 — Endpoint-ul

**De făcut:**
- Endpoint nou care întoarce ultimele 10 intrări plus cifrele agregate.
- DTO-uri în `Shared`.
- **Regenerarea clientului**: `dotnet run --generateclients true`. Aici chiar e
  necesară, spre deosebire de un câmp nou pe un DTO existent — e endpoint nou.

Statisticile se calculează cu un `COUNT` pe bază, nu prin încărcarea tuturor
rândurilor: lista e limitată la 10, dar cifrele se referă la tot istoricul.

### De concretizat

**2.1 — Un endpoint sau două?** Recomand **unul singur**, care întoarce și lista,
și cifrele. Profilul le afișează împreună, deci două cereri ar însemna două
drumuri pentru un singur ecran.

**2.2 — Unde stă ruta?** Recomand `/auth/profile/history`, lângă celelalte rute de
profil, fiindcă e despre contul tău, nu despre un meci anume.

**2.3 — Numărul de intrări e fix sau parametru?** Recomand **fix, constantă pe
server**. Un parametru de la client ar fi o rotiță în plus fără cerere reală, iar
nelimitat ar deschide un răspuns care crește la nesfârșit.

**2.4 — Limitare de rată?** Ruta intră sub limita globală, ca restul. Nu propun
politică proprie: se cheamă o dată la deschiderea profilului.

---

## Etapa 3 — Interfața din profil

**De făcut:**
- Panou nou în `Profile.razor`, sub cel cu datele contului.
- Un rând de cifre deasupra listei.
- Stare goală pentru cine n-a jucat încă.

### De concretizat

**3.1 — Ce se vede pe un rând?** Propunerea mea: rezultatul ca accent vizual
(victorie/înfrângere/egalitate), numele adversarului, motivul pe scurt, runda și
data. Verdictele lungi din ecranul de final nu încap; îmi trebuie formulări
scurte, de listă.

**3.2 — Cum arată abandonul și inactivitatea?** Sunt victorii și înfrângeri care
n-au fost câștigate pe teren. Recomand să se distingă — „prin abandon", „prin
inactivitate" — altfel istoricul spune că ai câștigat un meci pe care adversarul
l-a părăsit la runda 1.

**3.3 — Regula de un singur viewport.** Profilul e o pagină scurtă acum. Cu 10
rânduri plus cifre, s-ar putea să nu mai încapă. Recomand **panoul care se
derulează pe dinăuntru**, ca istoricul rundelor din meci.

**3.4 — Formatul datei.** Recomand relativ pentru recent („acum 2 ore", „ieri") și
absolut mai departe. Dacă preferi simplu, doar data scurtă.

---

## Etapa 4 — Verificare

Scenariile de acoperit:

| Scenariu | Ce verifică |
|---|---|
| Meci dus la capăt pe runde | intrare corectă la ambii, cu rezultate opuse |
| Meci încheiat prin prăbușire | motivul corect |
| Abandon explicit | apare la amândoi, cu motivul de abandon |
| Încheiere prin inactivitate | apare la amândoi |
| Ambii apasă „Înapoi la meniu" | meciul se șterge, **istoricul rămâne** |
| Egalitate | numărată ca egal, nu ca înfrângere |
| Adversarul își schimbă numele | intrarea veche păstrează numele de atunci |
| Profil fără meciuri | stare goală, fără cifre derutante |

Al cincilea e cel mai important: dovedește că înregistrarea chiar e independentă
de ciclul de viață al meciului, adică exact motivul pentru care există.

Fluxul cere doi jucători, iar clientul ține token-ul în `localStorage`, deci două
taburi ar împărți același cont: **un jucător în browser, celălalt prin API**.

---

## Ce deschide asta mai departe

**Curățarea meciurilor terminate** devine în sfârșit ieftină. Azi un meci
`Completed` din care niciunul n-a apăsat *Înapoi la meniu* rămâne la nesfârșit,
fiindcă ștergerea lui ar pierde singura urmă a jocului. După etapa 1, urma e în
altă parte, iar meciul poate fi șters fără regrete.

Nu intră în planul ăsta, dar e prima funcționalitate care merită după el.
