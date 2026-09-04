# Regulile jocului — Duelul Regatelor

Acest document descrie **absolut toate** regulile jocului, așa cum sunt
implementate în `KingdomDuel.Domain`. Fiecare număr de aici există și în
`GameConstants.cs` — dacă modifici una, modific-o și pe cealaltă.

## 1. Resurse

Fiecare jucător controlează un regat cu trei resurse:

- **Aur** — finanțează Apărarea și Diplomația, se produce prin Comerț.
- **Trupe** — pleacă în expediții și se pierd în lupte; se câștigă doar
  luându-le adversarului.
- **Moral** — crește prin Apărare/Comerț/Diplomație, scade în ciocniri și
  rebeliuni, și e condiție de colaps dacă ajunge la 0.

Toate trei pornesc de la **50** pentru fiecare jucător și **se acumulează**
de-a lungul meciului — nu se resetează de la o rundă la alta.

**Puterea totală** a unui regat = Aur + Trupe + Moral. E folosită la
regula de stagnare și la decizia finală a meciului.

## 2. Runde

Meciul durează un număr de runde stabilit de gazdă în lobby (implicit
`GameConstants.DefaultTotalRounds`). La fiecare rundă, ambii jucători aleg
simultan una din cele 5 acțiuni de mai jos; nimeni nu vede alegerea
celuilalt înainte ca ambele să fie trimise.

Fiecare rundă are o **limită de timp**, stabilită de gazdă în lobby (implicit
`GameConstants.DefaultRoundSeconds`, între `MinRoundSeconds` și
`MaxRoundSeconds`). Nu există variantă fără limită. La expirare, cine n-a
trimis nimic primește automat *Fără ordin*, iar ordinul celui care a apucat să
trimită rămâne valabil. Peste termen se acordă o mică perioadă de grație
(`RoundGraceSeconds`), pentru latența rețelei — invizibilă în interfață.

## 3. Acțiunile

### Atac
Atacul nu are un cost fix. Atacatorul **alege câte trupe trimite** în
expediție, între **1** și **15** — și niciodată mai multe decât are regatul în
clipa rezolvării. La evenimentul Zvonuri de pace plafonul coboară la **8**.

Notăm cu `putere = trupe trimise ÷ 15` cât de tare e expediția față de una
completă. Ea decide două lucruri: **șansele** și **prada** (secțiunea 4).

Trupele trimise nu se pierd toate: pleacă și se întorc cele care scapă. Cât
nu se mai întoarce depinde de ce a găsit oastea acolo:

| Rezultat | Din expediție se pierde |
|---|---|
| Asalt frânt de o fortificație | **tot** |
| Alungat de un regat nefortificat | **60%** |
| Străpungere reușită | **45%** |
| Lovitură nestingherită | **35%** |
| Ciocnire cu altă expediție | **30–100%**, vezi secțiunea 4 |

Pierderile se rotunjesc **în sus**. De aici vine riscul: cine trimite toată
oastea și e respins rămâne fără ea, iar regatul se prăbușește.

Dacă un jucător alege Atac fără să aibă nicio trupă, ordinul nu produce
niciun atac; adversarul e tratat ca nefiind atacat.

### Apărare
- Cost de aur crescător pe fiecare rundă consecutivă de Apărare:
  runda 1 din streak costă **5 aur**, a doua **8**, a treia **11** ș.a.m.d.
  (+3 aur pentru fiecare rundă suplimentară la rând). Streak-ul se resetează
  la orice altă acțiune.
- Câștig de moral descrescător pe același streak: **+5** prima dată, **+3**
  a doua oară, **+1** a treia oară, apoi rămâne la minimum **+1**.
- **Regula de stagnare (anti-turtling):** dacă, în momentul în care alegi
  Apărare, puterea ta totală e deja **egală sau mai mare** decât a
  adversarului, mai pierzi **4 moral** suplimentar — poporul cere progres,
  nu doar ziduri. Acest penalty se aplică indiferent de streak, de fiecare
  dată când te aperi fiind deja în avantaj.

### Comerț
- Câștig de aur: **+15** normal, **+7** dacă evenimentul e Secetă, **+30**
  dacă evenimentul e Recoltă bogată.
- Moral: **+2**, mereu.

### Diplomație
- Cost: **5 aur**.
- Câștig de moral: **+10** normal, **+20** dacă evenimentul e Târg diplomatic.

### Fără ordin
- Cost: **nimic**. Câștig: **nimic**.
- Rupe streak-ul de Apărare, ca orice altă acțiune care nu e Apărare. Altfel
  alternarea Apărare–Fără ordin ar ocoli costul crescător de aur, iar o acțiune
  care nu aduce nimic ar deveni pe ascuns un avantaj.
- Nu oferă nicio protecție: în fața unui atac se comportă ca Comerț — vezi
  secțiunea 4.

## 4. Interacțiunea dintre ordine

**Prada se scalează cu puterea expediției.** Cifrele de mai jos sunt cele de
la angajament total; sub el se ia proporțional, dar niciodată mai puțin de 1.
Furtul nu poate scădea o resursă sub 0 — se limitează la ce are victima.

### Atac vs Atac (ciocnire în câmp deschis)
Cele două expediții se întâlnesc. Fiecare pierde o parte din **ce a trimis**,
după cum se împarte câmpul între cele două oști:

```
avantaj de moral = clamp(round((moralul meu − moralul lui) ÷ 10), −5, +5)
oastea mea pe câmp = max(1, trupe trimise + avantaj de moral)
partea mea de câmp = oastea mea ÷ (oastea mea + oastea lui)
ce pierd = 30% + 70% × (1 − partea mea de câmp)
```

Moralul mai bun valorează, deci, trupe în plus la socoteală, fără să schimbe
câte au plecat de fapt. La oști egale amândoi pierd cam **65%**; cine trimite
mult peste celălalt scapă cu pierderi mici, iar cel mic e aproape șters.
Ambele tabere pierd, în plus, **5 moral**.

### Atac vs Apărare — aurul cumpără putere militară
Aurul acumulat se traduce în **nivel de echipament/fortificație**: fiecare
20 de aur = 1 nivel, plafonat la **nivel 5** (deci maximum 100+ aur contează).

Șansa ca atacatorul să **străpungă** apărarea:

```
șansă = 5% + 55% × putere + 8% × (nivelul atacatorului − nivelul apărătorului)
șansă finală limitată între 5% și 80%
```

O expediție completă cu echipament egal are deci ~60% șanse; o împunsătură de
3 trupe are ~16%.

- **Dacă străpunge:** fură **12 aur** și **8 trupe** la angajament total, plus
  moral pentru atacator. Din expediție se pierd **45%**.
- **Dacă eșuează** (apărarea ține): asaltul se frânge pe ziduri și **toată
  expediția se pierde**. Apărătorul primește un bonus de moral egal cu
  `5 + nivelul lui de fortificație`.

Practic: un regat sărac care doar se apără nu mai e un zid sigur dacă
adversarul lui are mult mai mult aur decât el — poate fi străpuns. Dar nici
atacatorul nu mai poate lovi fără să riște: un asalt respins îl lasă fără
oastea trimisă.

### Atac vs Comerț / Atac vs Diplomație / Atac vs Fără ordin
Chiar fără fortificație, un regat alungă o oaste prea mică:

```
șansă de reușită = 35% + 65% × putere
```

O expediție completă trece sigur (100%); una de 3 trupe reușește în ~48% din
cazuri.

- **Dacă reușește** și victima era la Comerț sau Fără ordin: atacatorul fură
  **15 aur** și **10 trupe** la angajament total.
- **Dacă reușește** și victima era la Diplomație: fură **20 aur** și
  **15 trupe** (penalizare mai mare — diplomația nu oferă nicio protecție
  militară).
- Atacatorul primește până la **+5 moral**, proporțional cu cât a trimis. Din
  expediție se pierd **35%**.
- **Dacă e alungat:** atacatorul pierde **60%** din oastea trimisă și
  apărătorul câștigă **2 moral**.

### Diplomație vs Diplomație
Alianță: ambii jucători primesc **+10 aur**.

### Orice altă combinație fără atac
(Apărare vs Comerț, Apărare vs Diplomație, Comerț vs Comerț, Comerț vs
Diplomație, orice vs Fără ordin etc.) — nu există interacțiune directă;
fiecare parte primește doar efectele proprii ale acțiunii alese (secțiunea 3).
Dacă amândoi aleg Fără ordin, runda trece fără nicio schimbare.

## 5. Evenimente aleatorii

La începutul fiecărei runde se trage aleatoriu unul din următoarele
evenimente (toate cu șanse egale, inclusiv „nimic special”):

| Eveniment | Efect |
|---|---|
| Zi obișnuită | Niciun efect special. |
| Secetă | Comerțul dă doar **7 aur** în loc de 15. |
| Recoltă bogată | Comerțul dă **30 aur** în loc de 15. |
| Rebeliune internă | Orice tabără cu moral sub **30** pierde **10 trupe** automat, înainte de acțiuni. |
| Zvonuri de pace | Într-o expediție pot pleca cel mult **8 trupe** în loc de 15. |
| Târg diplomatic | Diplomația dă **20 moral** în loc de 10. |

## 6. Sfârșitul meciului

Meciul se termină imediat, indiferent de rundă, dacă:
- **Trupele** sau **Moralul** unui regat ajung la **0** → acel regat se
  prăbușește și pierde. Dacă ambele se prăbușesc în aceeași rundă → remiză.
- Un jucător **părăsește meciul** → adversarul câștigă prin abandon
  (`GameOverReason.Player1Left` / `Player2Left`).
- Un jucător **ratează 3 runde** → adversarul câștigă prin inactivitate
  (`GameOverReason.Player1Disconnected` / `Player2Disconnected`). Dacă amândoi
  ating pragul, meciul se încheie fără învingător (`BothDisconnected`).
  Ratările sunt **cumulate pe tot meciul**, nu consecutive, și numără doar
  rundele în care jucătorul **n-a trimis nimic**. Alegerea deliberată a
  acțiunii *Fără ordin* nu intră în contor.

Dacă runda în care s-a atins pragul se încheie oricum pe teren — un regat
prăbușit, de pildă — verdictul de acolo rămâne; deconectarea se aplică doar
când runda nu produce singură un final.

Dacă niciun regat nu s-a prăbușit, meciul se termină automat după ultima
rundă stabilită în lobby: câștigă regatul cu **puterea totală**
(Aur+Trupe+Moral) mai mare. Egalitate exactă → remiză.

## 7. Ce nu e încă implementat (intenționat, pentru pașii următori)

- Sincronizare în timp real (SignalR) — momentan clientul trebuie să
  interogheze (poll) `GET /api/matches/{code}` ca să vadă starea curentă.
- Persistență — stările meciurilor există doar în memoria procesului.
- (fost) Autentificare — un jucător e identificat doar prin `playerId`-ul primit
  la creare/alăturare, fără cont sau parolă.
- Interfață grafică.
