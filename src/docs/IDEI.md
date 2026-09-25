# Idei pentru mai târziu

Lucruri hotărâte în timpul refactorului de logică, dar lăsate deoparte ca să nu
umfle etapele. Fiecare se face la rândul ei, ca etapă de sine stătătoare, cu
deciziile luate atunci, nu acum.

Refactorul de logică (S1–S12) s-a încheiat; de aici încolo se îmbunătățește.
Ordinea hotărâtă: zidurile sparte, sătenii și soldații, recrutarea instantă,
spionii, apoi mobilizarea și îngreunarea asaltului.

**Făcute:** zidurile rămân sparte; sătenii și soldații, două neamuri;
catapultele se pierd la retragere sau odată cu expediția nimicită. Ce a rămas e
mai jos.

Mobilizarea, spionii și îngreunarea asaltului au ieșit din S7 ca planul să nu se
contrazică; mobilizarea și spionii adaugă suprafață de interfață — un buton pe
ordinul de atac și o direcție nouă de alocare — deci panoul de ordine primește o
a doua trecere când le facem.

### Recrutarea instantă în timpul unui asalt

Azi, în timpul unui asalt, atacatorul poate suna retragerea, iar apărătorul e
doar spectator. Ideea îi dă și lui ceva de făcut: **când zidul cade, satul
poate lua arma pe loc.**

**Ce s-a hotărât**

- **Oamenii sunt sătenii tăi, nu recruți cumpărați.** Mecanismul e înarmarea de
  la etapa sătenilor, doar că se întâmplă în mijlocul asaltului.
- **Costă 2 aur de cap**, dublu față de înarmarea plănuită (1 aur). Motivul e
  analitic, nu de gust: la preț egal, înarmarea sub asediu o **domină strict**
  pe cea plănuită — același cost, dar cu informație perfectă, fiindcă nu
  convertești decât când chiar ești atacat. Prima e opțiune, a doua e
  angajament, iar opțiunea trebuie să coste. Doi aur e și prețul pe cap al
  recrutării (12 pentru 6), care însă sosește abia runda următoare.
- **Conversia e permanentă.** Cine ia arma la breșă rămâne soldat și după
  asediu; nu se întoarce la plug. Deci prețul adevărat nu e doar aurul, ci și
  capacitatea de negoț pierdută pentru tot restul meciului.
- **Intră în garnizoană**, nu pe zid. La breșă zidul oricum nu mai există.
- **Se poate o singură dată pe asalt**, fiindcă fereastra e una singură.
- **Fereastra se deschide și la o cetate fără ziduri**, unde asaltul intră
  direct. Altfel o cetate spartă o dată ar pierde pârghia asta pentru tot restul
  meciului, și s-ar aduna urât peste regula zidurilor care rămân sparte.
- **Atacatorul nu se poate retrage cât ține fereastra.** E o graniță, nu o fază
  de luptă; altfel apărătorul ar plăti pentru o bătălie care nu mai are loc.
- **Dacă asaltul e respins, nimic nu se întoarce.** Aurul e cheltuit, iar
  sătenii rămân soldați — din supraviețuitori oricum n-am avea de unde ști care
  a fost sătean.

**Cum se face, fără să rescriem cronologia**

Ăsta era motivul pentru care etapa părea grea: cronologia bătăliei se
calculează o dată, la început, iar oameni intrați la mijloc ar invalida-o din
acel moment. Pauza la breșă rezolvă asta — întăririle sosesc **pe granița
dintre faze**, nu în mijlocul unei faze:

1. se calculează faza zidurilor și se oprește acolo;
2. **cel mult 10 secunde** — apărătorul alege câți săteni înarmează, iar dacă
   se hotărăște mai devreme fereastra se închide pe loc și asaltul pornește;
   atacatorul vede doar numărătoarea;
3. se calculează faza din cetate, cu garnizoana deja mărită.

Fereastra e numai a apărătorului: la breșă atacatorul n-are ce decide, e deja
angajat. E oglinda ferestrei de după câmp, unde apărătorul e cel care privește
ceasul. Ca stare seamănă cu `MarchChoice`: un termen, o alegere, un rezultat
ținut deoparte până expiră.

**Atenție la calibrare:** prețul nu descurajează, ci taxează. Cetatea căzută
înseamnă meci pierdut pe loc, deci la limită orice preț finit merită plătit. Ce
face cu adevărat e să golească trezoreria — 10 apărători costă 20 de aur, adică
vreo două runde de caravană — și să concureze cu repararea zidului și cu
ospățul. Riscul de urmărit e același bulgăre ca la ziduri: apărătorul sărac
rămâne complet fără pârghia asta.

**Ce nu face parte din etapă:** sătenii **nu** se apără singuri. Ideea unui
ultim baraj automat (3 săteni = 1 soldat, când garnizoana e nimicită) a fost
cântărită și lăsată deoparte — o face automat și gratis pe cea pe care tocmai
am hotărât s-o plătești, și slăbește tăcut regula pe care stă tot echilibrul
despicării: **cetatea cade când rămâi fără soldați.** Rămâne variantă de
rezervă, de recântărit după ce se măsoară cum se joacă varianta plătită.

### Mobilizarea

Ca să nu fie corect din start să ataci în prima rundă cu toată oastea, **fără
mobilizare se poate trimite cel mult jumătate din soldați**. Cine vrea să atace
cu toți dă mai întâi un **ordin de mobilizare**, iar în runda următoare are voie
cu întreaga oaste.

Nu e o acțiune nouă: e un **buton pe ordinul de atac**, deci nu apare o direcție
în plus în alocare. Se poate cere în fiecare rundă. **Cât timp oastea e
mobilizată nu se poate da nici comerț, nici ospăț** — asta e prețul: renunți la
aur și la moral pentru o rundă ca să poți lovi cu tot.

Ce iese din asta, ca formă de joc: un atac total devine **telegrafiat cu o rundă
înainte**, iar adversarul are timp să reacționeze — dacă îl vede. De aceea
există ideea de mai jos, cu spionii.

**De decis la implementare:**

- Jumătatea se măsoară din ce? Oastea din cetate la începutul rundei, sau
  inclusiv caravana plecată și spionii? Recomand doar oastea prezentă.
- Plafonul se aplică numai pe `Attack`, sau pe tot ce pleacă de acasă (atac +
  comerț + spioni)? Recomand numai pe `Attack`, altfel regula se amestecă cu
  bugetul de alocare.
- Mobilizarea ține până e folosită, sau expiră după o rundă? Recomand să ținem o
  singură rundă — altfel se mobilizează o dată și se stă mobilizat la infinit.
- Interdicția de comerț/ospăț e în runda în care **ceri** mobilizarea, în cea în
  care oastea **e** mobilizată, sau în amândouă? Recomand în cea în care e
  mobilizată; ordinul de mobilizare în sine e gratuit.
- Se poate anula o mobilizare? Recomand da, dar fără să recuperezi runda.
- Jumătatea se rotunjește în sus sau în jos? Recomand în jos.

### Spionii

Un **ordin nou**: trimite sau cheamă spionii. Pleacă un număr fix de soldați
(probabil 2) și **rămân plecați rundă după rundă** până sunt chemați înapoi. Cât
sunt afară, în fiecare rundă se scade o sumă de aur — întreținerea lor departe de
cetate.

Ce raportează:

- dacă inamicul a trimis o **caravană** la comerț;
- dacă inamicul își **mobilizează** oastea pentru un atac cu toți soldații (vezi „Mobilizarea”).

Deci spionii sunt răspunsul la faptul că mobilizarea e telegrafiată: fără ei nu
vezi semnalul, cu ei plătești aur ca să-l vezi.

E și prima probă adevărată a promisiunii de la S2 — **o direcție nouă de alocare
fără să umblăm în șase fișiere**. Dacă adăugarea lui cere mai mult decât un
`ActionType`, o regulă în motor și o linie în panou, atunci segmentul nu era
scalabil și se vede aici.

**De decis la implementare:**

- Câți soldați pleacă și cât costă întreținerea pe rundă.
- Spionii se pierd? Îi poate prinde adversarul, sau sunt doar o cheltuială?
- Ce se întâmplă dacă nu mai ai aur de întreținere: se întorc singuri, sunt
  pierduți, sau intri pe datorie?
- Contează la puterea finală de departajare, ca și caravana?
- Raportul e din runda trecută sau din cea în curs? Contează: un raport despre
  runda în curs ar însemna să vezi ordinul adversarului **înainte** să-l dai pe
  al tău, ceea ce sparge simultaneitatea. Recomand raport despre runda
  **încheiată**.
- Se pot trimite spioni cât oastea e mobilizată?

### Îngreunarea asaltului

Grija de la care pornește: **un atac la cetate să nu fie ușor de câștigat.**

#### Ce spun cifrele deja măsurate

La armate egale apărătorul stă bine: 30 contra 30, cu zidul plin, cetatea cade
**13%**. Problema apare la un avantaj numeric modest: 40 contra 30 cade **89%**,
iar 50 contra 30 cade 87%. Deci nu asaltul e prea slab în general — e prea
sensibil la diferența de oaste.

Cauza e cea găsită la 7.1: **bătălia se încheie numai prin nimicire, deci
numerele sunt structural dominante.** Chiar și cu monedă cinstită la fiecare
tick, armata mai mare câștigă ~92%, fiindcă are nevoie de mai mulți morți ca să
piardă. Taxa de la ziduri e plafonată la ~11 oameni, deci nu poate compensa o
diferență mare.

De aici o distincție care hotărăște ce merită implementat:

- **Un bonus fix** (țărani +10, un salt de moral) **mută pragul**, nu schimbă
  forma. Atacatorul are nevoie de mai mulți oameni ca să fie sigur de victorie,
  dar de la un punct înainte tot devine sigur.
- **Un multiplicator sau o limită de timp schimbă forma** — rămân valabile la
  orice mărime de oaste.

Merită și de reținut că **mobilizarea atacă deja o parte din grijă**: fără
ea nu se poate trimite mai mult de jumătate din oaste, iar un atac total e
telegrafiat cu o rundă înainte. Poate ajunge; de aceea îngreunarea asaltului
se măsoară **după** mobilizare, nu înainte.

#### Ideile propuse

**1. Salt de moral la străpungere.** Se potrivește cu regula existentă — moralul
e singurul multiplicator în luptele dintre soldați. Dar la 7.1 s-a măsurat că
moralul singur e slab contra unei diferențe numerice: la 30 contra 20, cel mic cu
moral 100 contra 50 câștigă tot doar 26%. Ca să conteze, saltul ar trebui să fie
mare. **Depinde de S10**, unde moralul capătă efect în luptă.

**2. Țăranii sar la ajutor: +10 apărători, doar pe durata asaltului.** ~~Simplu,
fără dependențe, ușor de explicat.~~ **Acoperită de recrutarea instantă**, în
formă mai bună: acolo sătenii chiar sar la ajutor, dar plătit și ales, nu
automat și gratis. Ce rămâne din ea e avertismentul — dacă vreodată se face un
baraj automat, oamenii aceia trebuie să **dispară** la sfârșitul bătăliei,
altfel un atac slab devine o metodă de a-ți crește oastea.

**3. Timp de penetrare mai lung, ca apărătorul să se pregătească.** E pârghia pe
care o avem deja (`WallBreakingSeconds`). Atenție: **timpul singur saturează.**
Măsurat la 7.2, dincolo de ~35 de secunde arcașii s-au consumat oricum, iar
secundele în plus nu mai cumpărau nimic. Ca timpul mai lung să conteargă,
trebuie urcată și `WallCapacity` — adică exact ce va face fortificația. Deci
ideea 3 merge **împreună cu fortificația**, nu singură.

#### Ce aș adăuga eu

**4. Atacatorul intră obosit.** Un handicap de forță în lupta din cetate, cu atât
mai mare cu cât a stat mai mult sub săgeți la ziduri. E un multiplicator, deci
schimbă forma, și mai are un câștig: **leagă faza zidurilor de lupta din
cetate.** Azi cele două sunt independente, iar arcașii contează doar prin cei
~11 morți; aici ar conta și prin cât timp au ținut. Fortificația și arcașii ar
începe să se compună.

**5. Apărătorii luptă mai bine în propria cetate.** Un multiplicator pe apărare
în faza `Keep` — coridoare și curți pe care le cunosc. Matematic e ruda ideii 4,
dar se explică diferit și e mai ușor de calibrat, fiind un singur număr.

**6. Atacatorul lasă oameni la spărtură.** O parte din supraviețuitorii care
străpung nu intră în cetate, ci țin breșa. Scalează cu mărimea oastei și e
oglinda pierderii de 30% a apărătorului.

**7. Asaltul are limită de timp: dacă cetatea n-a căzut până atunci, atacul e
respins.** Asta e răspunsul cel mai direct la cauza de fond — atacatorul trebuie
să câștige **repede**, nu doar în cele din urmă, deci numerele încetează să fie
decisive. E și o schimbare mică: o limită pe bătălia din cetate, exact ca
plafonul de la ziduri. **Bonus: rezolvă și problema duratei** notată la 7.3 — un
asalt măsurat a ținut 67 de secunde. Două probleme, o singură pârghie.

#### Avertisment de calibrare

**Nu se stivuiesc toate.** Fiecare mută echilibrul; moral + țărani + timp mai
lung + atacator obosit ar face atacul imposibil, și n-am mai ști care regulă a
făcut ce. Se ia una sau două, se măsoară cu 30 contra 30, 40 contra 30 și 60
contra 30, apoi se decide. Recomandarea mea de pornire: **7 (limită de timp)**,
fiindcă schimbă forma și taie durata, plus **2 (țăranii)** dacă mai e nevoie de
un prag de intrare — e cea mai ușor de explicat unui jucător.

### Probele într-un proiect de test adevărat

Probele de motor au trăit tot refactorul într-un banc din scratchpad, pe care o
comandă greșită l-a și șters odată. Locul lor e în repo: `tests/KingdomDuel.Tests`,
xUnit, rulate la fiecare build. De rescris pe măsură ce se atinge fiecare zonă,
nu toate deodată.
