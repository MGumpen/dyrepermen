# 0016 — Påminnelser følges opp der de står, uten statuskolonne

**Status:** Vedtatt
**Dato:** 2026-09-27
**Gjelder:** plan kapittel 8 og 10.3 — «Forfaller snart» på dashbordet

## Kontekst

Påminnelsene i «Forfaller snart» kunne bare leses. Når en ormekur var gitt, måtte
den registreres på nytt på behandlingssiden, med type, preparat og ny dato for neste
gang. Medisindoser sto ikke på dashbordet i det hele tatt, og en avtalt kontroll
hos veterinæren måtte legges inn som en helt ny time.

Det fantes også en feil: forrige behandling ble liggende som «forfalt» for alltid.
Ingenting koblet den nye behandlingen til den gamle, så den gamle påminnelsen
forsvant bare hvis noen tømte «neste gang» for hånd.

## Beslutning

### 1. En behandling er fulgt opp når en nyere av samme slag finnes

En påminnelse er åpen så lenge dyret ikke har fått en nyere behandling med samme
type og samme preparat. Store og små bokstaver teller ikke. Regelen står ett sted,
i `Behandlingsfilter.ApenPaminnelse`, og brukes av dashbordet, dyrets side og
behandlingssiden.

Regelen gjelder uansett hvordan den nye behandlingen kom inn — med «Gitt» eller
med vanlig registrering. Det retter feilen over uten skjemaendring, og eksisterende
data rettes av seg selv.

**Preparatet er med i nøkkelen.** En hund kan ha to vaksiner med hver sin syklus.
Var nøkkelen bare typen, ville grunnvaksinen skjult kennelhoste-påminnelsen uten at
noen merket det. Prisen er at et nytt preparat for samme ormekur lar den gamle
påminnelsen stå. Det er synlig og kan rettes — en påminnelse som forsvinner i
stillhet er det ikke.

**Hvorfor ingen kolonne som `fulgt_opp_av`:** den måtte vært fylt ut ved hver
registrering, og vanlig registrering ville ikke visst hvilken rad den fulgte opp.
Regelen over trenger ingen slik kobling.

### 2. «Gitt» registrerer samme behandling i dag, med samme intervall

Ett trykk. Den nye raden får samme type og preparat, dagens dato, og neste gang
med samme intervall som forrige gang. Intervallet gjenkjennes som hele måneder når
det er det (`Behandlingsintervall`), slik at «hver tredje måned» ikke kryper en dag
for hver gang. Datoen står i bekreftelsen og kan rettes.

Er raden allerede fulgt opp, blir det ingen ny behandling. Det hindrer at et
dobbelttrykk, eller to i husstanden som krysser av samtidig, gir to rader.

`Gitt` krever beboer, som resten av behandlingssiden.

### 3. Medisindoser som forfaller i dag, står på dashbordet

Aktive medisiner med fast intervall, der neste dose forfaller i løpet av dagen i
norsk tid (`Dosevarsel`). «Gi dose» bruker den eksisterende handlingen, med
dobbeltdoseringssjekken. Den er åpen for gjester, som på medisinsiden. Dosene hentes
i samme spørring som dyrene, så dashbordet har fortsatt åtte spørringer.

### 4. Veterinærtimer: datoen styrer fortsatt

Valgt av Marius: ingen statuskolonne. En time regnes fortsatt som gjennomført når
datoen har passert. «Gjennomført» på dashbordet åpner timen i skjemaet, så diagnose
og pris kan fylles inn uten at noe skrives på nytt.

En avtalt kontroll følges opp med «Bestill time». Den åpner et utfylt timeskjema,
og når timen lagres, tømmes `neste_kontroll_dato` på besøket den kom fra, i samme
lagring. Kontrollen lever videre som den nye timen. Hører besøket til et annet dyr,
lagres timen, men ingenting annet endres.

### 5. Tidligere behandlinger kan velges igjen

Behandlingsskjemaet viser det husstanden har gitt før, én gang per type og
preparat, på tvers av dyrene. Et trykk fyller ut skjemaet på serveren, med samme
intervall til neste gang. Preparatfeltet har de samme forslagene i en `datalist`.

## Konsekvenser

- Utskriften bruker samme regel: «neste» vises bare på behandlinger som ikke er
  fulgt opp.
- En behandling registrert med et annet preparat enn sist, følger ikke opp den
  gamle. Påminnelsen må da tømmes på den gamle raden.
