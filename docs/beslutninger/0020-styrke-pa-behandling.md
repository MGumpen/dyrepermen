# 0020 — Styrke på behandling

**Status:** Vedtatt
**Dato:** 2026-10-10
**Gjelder:** plan kapittel 4 og 5 — tabellen `behandling`

## Kontekst

En behandling hadde type og preparat, men ikke styrke. «Milbemax» finnes både for
små hunder (2,5 mg/25 mg) og store (12,5 mg/125 mg), og den som skal gi neste
ormekur, må vite hvilken som ble gitt sist.

## Beslutning

- Ny nullbar kolonne `behandling.styrke VARCHAR(40)`, fritekst. Samme grep som
  `medisin.dose`: styrker skrives på mange måter («12,5 mg/125 mg», «2,5 ml»,
  «1 pipette 10–20 kg»), og et tall med en enhet ville ikke dekket dem.
- Styrken vises etter preparatet overalt der behandlingen vises: «Ormekur –
  Milbemax 12,5 mg/125 mg». Formatet står ett sted, i `Behandlingsformat.MedPreparat`.
- «Gitt i dag» og forslagene i skjemaet («Gitt før») kopierer styrken fra forrige
  gang.
- **Styrken er ikke en del av nøkkelen som følger opp en påminnelse**
  (`Behandlingsfilter.ApenPaminnelse`, ADR 0016). En valp får sterkere tablett
  etter hvert som den vokser, men det er fortsatt samme ormekur. Med styrken i
  nøkkelen ville den gamle påminnelsen blitt stående som forfalt.
- Dataeksporten tar med styrken, og også kurfeltene og dosene fra ADR 0019, som
  manglet der.

## Konsekvenser

- Eksisterende behandlinger har ingen styrke. Den kan fylles inn med «Rediger».
- Har styrken endret seg siden sist, må den rettes etter «Gitt i dag». Dialogen
  har bare feltet for neste gang.
