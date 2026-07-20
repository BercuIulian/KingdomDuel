# Regulile jocului — Duelul Regatelor

Acest document descrie **absolut toate** regulile jocului, așa cum sunt
implementate în `KingdomDuel.Domain`. Fiecare număr de aici există și în
`GameConstants.cs` — dacă modifici una, modific-o și pe cealaltă.

## 1. Resurse

Fiecare jucător controlează un regat cu trei resurse:

- **Aur** — finanțează Apărarea și Diplomația, se produce prin Comerț.
- **Trupe** — se cheltuiesc la Atac, se pierd în luptă.
- **Moral** — crește prin Apărare/Comerț/Diplomație, scade în ciocniri și
  rebeliuni, și e condiție de colaps dacă ajunge la 0.

Toate trei pornesc de la **50** pentru fiecare jucător și **se acumulează**
de-a lungul meciului — nu se resetează de la o rundă la alta.

**Puterea totală** a unui regat = Aur + Trupe + Moral. E folosită la
regula de stagnare și la decizia finală a meciului.

## 2. Runde

Meciul durează maximum **8 runde**. La fiecare rundă, ambii jucători aleg
simultan una din cele 4 acțiuni de mai jos; nimeni nu vede alegerea
celuilalt înainte ca ambele să fie trimise.

## 3. Acțiunile

### Atac
- Cost: **10 trupe** (**20 trupe** dacă evenimentul rundei e Zvonuri de pace).
- Rezultatul depinde de ce a ales adversarul — vezi secțiunea 4.

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

## 4. Interacțiunea dintre acțiuni

### Atac vs Atac (ciocnire directă)
Pierderea de bază e **15 trupe** pentru fiecare parte, dar diferența de
moral dintre cei doi jucători redistribuie pierderile: cine are moral mai
mare pierde mai puține trupe, celălalt mai multe. Formula: fiecare 10
puncte de diferență de moral mută o trupă din pierdere, plafonat la
**±5 trupe** (deci pierderile variază între 10 și 20 de trupe). Ambele
tabere pierd, în plus, **5 moral**.

### Atac vs Apărare — aurul cumpără putere militară
Aurul acumulat se traduce în **nivel de echipament/fortificație**: fiecare
20 de aur = 1 nivel, plafonat la **nivel 5** (deci maximum 100+ aur contează).

Șansa ca atacatorul să **străpungă** apărarea:

```
șansă = 10% + 8% × (nivelul atacatorului − nivelul apărătorului)
șansă finală limitată între 5% și 75%
```

- **Dacă străpunge:** atacul reușește ca un atac obișnuit reușit, dar fură
  puțin mai puțin decât un atac fără apărare în față — **12 aur** și
  **8 trupe** — plus **+5 moral** pentru atacator.
- **Dacă eșuează** (apărarea ține): atacatorul mai pierde trupe suplimentare
  peste costul de bază, dar echipamentul lui reduce paguba — pierdere
  suplimentară = `max(3, 10 − nivelul de echipament al atacatorului)`.
  Apărătorul primește un bonus de moral egal cu `5 + nivelul lui de
  fortificație`.

Practic: un regat sărac care doar se apără nu mai e un zid sigur dacă
adversarul lui are mult mai mult aur decât el — poate fi străpuns.

### Atac vs Comerț / Atac vs Diplomație
Atacul reușește direct, fără nicio șansă implicată:
- Dacă victima era la Comerț: atacatorul fură **15 aur** și **10 trupe**.
- Dacă victima era la Diplomație: atacatorul fură **20 aur** și **15 trupe**
  (penalizare mai mare — diplomația nu oferă nicio protecție militară).
- Atacatorul primește **+5 moral** în ambele cazuri.
- Furtul nu poate scădea o resursă sub 0 (se limitează la ce are victima).

### Diplomație vs Diplomație
Alianță: ambii jucători primesc **+10 aur**.

### Orice altă combinație fără atac
(Apărare vs Comerț, Apărare vs Diplomație, Comerț vs Comerț, Comerț vs
Diplomație etc.) — nu există interacțiune directă; fiecare parte primește
doar efectele proprii ale acțiunii alese (secțiunea 3).

## 5. Evenimente aleatorii

La începutul fiecărei runde se trage aleatoriu unul din următoarele
evenimente (toate cu șanse egale, inclusiv „nimic special”):

| Eveniment | Efect |
|---|---|
| Zi obișnuită | Niciun efect special. |
| Secetă | Comerțul dă doar **7 aur** în loc de 15. |
| Recoltă bogată | Comerțul dă **30 aur** în loc de 15. |
| Rebeliune internă | Orice tabără cu moral sub **30** pierde **10 trupe** automat, înainte de acțiuni. |
| Zvonuri de pace | Atacul costă **20 trupe** în loc de 10. |
| Târg diplomatic | Diplomația dă **20 moral** în loc de 10. |

## 6. Sfârșitul meciului

Meciul se termină imediat, indiferent de rundă, dacă:
- **Trupele** sau **Moralul** unui regat ajung la **0** → acel regat se
  prăbușește și pierde. Dacă ambele se prăbușesc în aceeași rundă → remiză.

Dacă niciun regat nu s-a prăbușit, meciul se termină automat după
**runda 8**: câștigă regatul cu **puterea totală** (Aur+Trupe+Moral) mai
mare. Egalitate exactă → remiză.

## 7. Ce nu e încă implementat (intenționat, pentru pașii următori)

- Sincronizare în timp real (SignalR) — momentan clientul trebuie să
  interogheze (poll) `GET /api/matches/{code}` ca să vadă starea curentă.
- Persistență — stările meciurilor există doar în memoria procesului.
- Autentificare — un jucător e identificat doar prin `playerId`-ul primit
  la creare/alăturare, fără cont sau parolă.
- Interfață grafică.
