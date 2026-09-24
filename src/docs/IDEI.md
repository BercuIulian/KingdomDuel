# Idei pentru mai târziu

Lucruri hotărâte în timpul refactorului de logică, dar lăsate deoparte ca să nu
umfle etapele. Fiecare se face la rândul ei, ca etapă de sine stătătoare, cu
deciziile luate atunci, nu acum.

Refactorul de logică (S1–S12) s-a încheiat; de aici încolo se îmbunătățește.
Ordinea hotărâtă: zidurile sparte, sătenii și soldații, recrutarea instantă,
spionii, apoi mobilizarea și îngreunarea asaltului. Recrutarea instantă vine
după săteni fiindcă s-ar putea să însemne tocmai înarmarea lor.
Mobilizarea, spionii și îngreunarea asaltului au ieșit din S7 ca planul să nu se
contrazică; mobilizarea și spionii adaugă suprafață de interfață — un buton pe
ordinul de atac și o direcție nouă de alocare — deci panoul de ordine primește o
a doua trecere când le facem.

### Zidurile rămân sparte

Azi, dacă atacatorul străpunge zidurile și apoi se retrage, apărătorul pierde
niște oameni și cu asta se încheie: runda următoare zidurile sunt iar întregi.
Deci un asalt dus pe jumătate e curat pierdere pentru atacator.

Ideea: **odată sparte, zidurile rămân sparte.** Refacerea costă aur — în jur de
**15**. Cât nu sunt refăcute, un asalt următor **sare peste faza zidurilor** și
începe direct cu lupta din cetate.

**Ce câștigă jocul din asta**, și e mai mult decât pare:

- **Un asalt parțial devine progres.** Slăbești acum, iei cetatea mai târziu.
  Retragerea de după străpungere capătă rost, nu mai e doar limitarea pierderilor.
- **Aurul capătă un consum defensiv obligatoriu.** Adversarul îți poate goli
  trezoreria spărgându-ți zidurile la răstimpuri. E un vector de atac economic,
  și concurează direct cu ospățul — deci alegi între ziduri și moral.
- **Cifra e deja măsurată.** O cetate fără ziduri, la armate egale, cade în ~48%
  din cazuri în loc de 13% (rândul „0 pe ziduri” din tabelul de la 7.2). Puternic,
  dar nu absurd: rămâne o monedă, nu o certitudine.

**Decizia care contează cel mai mult:** *când* se pierd zidurile. În mesaj apar
două citiri, iar diferența e uriașă:

- **La străpungere, orice se întâmple mai departe.** Străpungerea se produce la
  secunda 15 dacă atacatorul e încă în viață, iar taxa de la ziduri e doar ~11
  oameni — deci aproape orice atac serios ajunge acolo. Ar însemna că **și un
  asalt eșuat lasă cetatea fără ziduri**, adică zidurile cad aproape la fiecare
  atac. Poate fi prea fragil.
- **Doar dacă atacatorul se retrage după străpungere** (formularea inițială).
  Mult mai îngust: cine intră și e nimicit în cetate nu-ți dărâmă zidurile.

Recomand a doua ca punct de plecare, cu măsurare, fiindcă prima face zidurile
consumabile la fiecare rundă.

**Restul de decis:**

- Refacerea e un ordin nou sau un buton lângă ospăț? Costă aur și niciun soldat,
  deci seamănă cu ospățul, nu cu o alocare de trupe.
- Are efect în runda în care o ceri, sau din următoarea? Recomand în aceeași
  rundă: nu poți oricum reacționa la un atac pe care nu-l vezi.
- Ce se întâmplă cu alocarea de apărare când nu sunt ziduri? Recomand ca cei
  puși la apărare să intre pur și simplu în garnizoană.
- Ce face fortificația de la S8 peste asta: nivelul supraviețuiește spargerii și
  doar zidul se reface, sau se pierde și el?
- **Repararea zidului face parte din ideea asta**, nu din S8: fără starea „zid
  dărâmat” nu există ce repara. Preț propus: 15 aur.
- **Riscul de bulgăre:** cine n-are 15 aur rămâne fără ziduri, iar fiecare atac
  următor e monedă. Nu e spirală — starea nu se agravează — dar rămânerea în
  urmă economic devine dur pedepsită. De urmărit la calibrare.

### Săteni și soldați, două neamuri

Astăzi o singură oaste face tot: negustorește, apără și atacă. Ideea e s-o
despicăm în două — **săteni** și **soldați** — fiecare cu treaba lui. Sătenii
pleacă la negoț și la orice muncă vom mai adăuga; soldații urcă pe ziduri și
pornesc la atac. Amândoi se cumpără cu aur, iar alegerea „pe ce dau aurul, pe
economie sau pe armată” ia locul alegerii sărace de azi, „trimit oastea la negoț
sau o țin acasă”.

Pornirea propusă: 20 de săteni și 10 soldați.

**Ce trebuie ținut minte când o facem:** azi negoțul e echilibrat tocmai fiindcă
te lasă descoperit — jumătate din echilibrul măsurat stă pe perechea
negustor-cu-zid contra asediator. Dacă negustoresc sătenii, riscul ăla dispare.
Tensiunea care îl înlocuiește e că aurul dat pe săteni nu s-a dat pe soldați,
iar **cetatea cade când rămâi fără soldați** — cine cumpără numai săteni moare
la prima năvală, cine cumpără numai soldați rămâne sărac. Ține, dar numai cât
timp regula asta rămâne adevărată.

**De hotărât la etapa ei:** dacă sătenii intră în putere la departajare (eu aș
zice da, 1 punct, ca aurul și ca soldatul); dacă pot fi pierduți altfel decât
odată cu cetatea (eu aș zice nu, fără pradă, ca până acum); și dacă ospățul
ridică moralul tuturor celor de acasă sau doar al soldaților.

### Recrutarea instantă în timpul unui asalt

Regula normală (S8): recruții sosesc din runda următoare. Excepția propusă: dacă
**ești atacat**, apărătorul poate recruta **pe loc**, iar oamenii intră imediat în
apărare. Scopul declarat e să facă atacul la cetate mai greu.

E o simetrie bună: azi, în timpul unui asalt, atacatorul poate să se retragă, iar
apărătorul e doar spectator. Asta i-ar da și lui ceva de făcut.

**De ce e un pas separat, nu parte din S8:** nu e o funcție de economie, e una de
bătălie, și e arhitectural mai grea decât retragerea. Retragerea doar **taie**
cronologia — viitorul se aruncă, deci e ieftină. Întăririle **adaugă oameni în
mijlocul luptei**, deci cronologia calculată la început nu mai e validă din acel
moment: trebuie recalculat restul bătăliei cu numerele noi și lipit la ce s-a
întâmplat deja. Merită aceeași grijă ca 7.4a, la etapa ei.

**De decis atunci:** în ce faze se poate (numai la ziduri, sau și în cetate); dacă
recruții sosiți astfel apar pe zid sau în garnizoană; dacă se poate de mai multe
ori într-un asalt; și ce se întâmplă cu o recrutare normală dată în aceeași rundă.

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

**2. Țăranii sar la ajutor: +10 apărători, doar pe durata asaltului.** Simplu,
fără dependențe, ușor de explicat. Fiind un număr absolut, contează mult la oști
mici și devine neglijabil la oști mari — bun ca prag de intrare, slab ca soluție
de fond. **De decis obligatoriu:** țăranii care supraviețuiesc **dispar** la
sfârșitul bătăliei, altfel un atac slab devine o metodă de a-ți crește oastea.

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

### Catapultele nu se pierd niciodată

Odată cu zidurile care rămân sparte, apare o asimetrie: nivelul zidului poate
fi dărâmat și răscumpărat, dar cele două catapulte, odată cumpărate, rămân ale
tale până la capătul meciului. Una e o investiție care se poate pierde, cealaltă
nu. De discutat ce le-ar putea consuma — un asalt eșuat, o retragere, uzura pe
rundă — sau dacă asimetria e de fapt în regulă, fiindcă zidul apără iar
catapulta doar atacă.

### Probele într-un proiect de test adevărat

Probele de motor au trăit tot refactorul într-un banc din scratchpad, pe care o
comandă greșită l-a și șters odată. Locul lor e în repo: `tests/KingdomDuel.Tests`,
xUnit, rulate la fiecare build. De rescris pe măsură ce se atinge fiecare zonă,
nu toate deodată.
