# 0018 — Vedlegg lagres i databasen, ikke på disk

**Status:** Vedtatt
**Dato:** 2026-09-27
**Gjelder:** plan kapittel 13, 15 og fase 5b — opplastede dokumenter

## Kontekst

Et veterinærbesøk skal kunne få kvitteringen lagt ved: et bilde tatt med mobilen,
eller en PDF klinikken sendte på e-post. Planen la opp til et fillager på disk
(«fillager i v1», mappen `opplastinger/`).

Det virker ikke der appen skal kjøre. Render sin gratis webtjeneste har en midlertidig
disk: alt som er lagret lokalt, forsvinner ved hver utrulling, hver omstart og hver
gang tjenesten sovner etter 15 minutter uten trafikk. Gratistjenester kan ikke få
fast disk. Kvitteringene ville vært borte samme dag, uten en eneste feilmelding.

Neon på gratisnivået har 0,5 GB per prosjekt. Nås grensen, slettes ingenting, men
**alle skrivinger blokkeres** — hele appen slutter å kunne lagre, ikke bare vedleggene.

Vurdert:

1. **Databasen** (valgt av Marius). Ingen ny tjeneste eller nøkkel. Vedleggene
   isoleres av de samme query-filtrene, slettes av de samme kaskadene og kommer med
   i sikkerhetskopien.
2. **Objektlager** (Cloudflare R2, 10 GB gratis). Holder databasen liten, men krever
   en konto til, en nøkkel til, og sletting to steder.

## Beslutning

- `dokument` får `vetbesok_id` (valgfri, kaskade når timen slettes),
  `innholdstype` og `storrelse_byte`. `filnavn` fjernes — den var lagringsnavnet på
  disk og ville aldri fått en verdi.
- Selve filen ligger i `dokument_innhold` (`bytea`), én rad per dokument. Egen
  tabell, slik at en liste over vedlegg ikke leser megabytene.
- Begge tabellene er husstandsbundne med query-filter. Filer serveres av
  `DokumentController`, aldri fra `wwwroot`. En fremmed id gir 404.
- Reglene står i `Vedleggsregler`:
  - Kun jpeg, png og pdf, gjenkjent på de første bytene i filen — ikke på filnavn
    eller typen nettleseren oppgir.
  - Høyst 10 MB per fil (plan kapittel 15) og 25 MB per forespørsel.
  - Høyst **50 MB per husstand**.
  - Høyst **300 MB for alle til sammen**, slik at vedleggene aldri kan fylle Neon
    og stoppe resten av appen.
- Nettleseren skalerer bilder ned til 1600 px jpeg før opplasting
  (`wwwroot/js/bildeskalering.js`). En kvittering er da et par hundre KB, og 50 MB
  rommer flere hundre. Feiler skriptet, sendes originalen, og serveren håndhever
  grensene uansett.
- Filene sjekkes **før** timen lagres. En ugyldig fil gir en feilmelding og ingen
  time, ikke en time uten den kvitteringen brukeren trodde var med.
- Stengt i demo. Demoene deler databasen med ekte husstander, og 300 demoer med
  vedlegg ville fylt den.

`IVedleggService` er grensen. Flyttes filene til et objektlager senere, byttes
implementasjonen, og resten står.

## Konsekvenser

- Databasen vokser med vedleggene. Følg med på størrelsen i Neon-konsollen. Taket på
  300 MB gir god margin mot 0,5 GB, men sikkerhetskopien (plan kapittel 15) blir
  tilsvarende større.
- Treffes taket for hele databasen, får brukeren «ikke plass akkurat nå», og det
  logges en advarsel. Da er det tid for objektlager eller betalt Neon.
- Bilder skaleres ikke på serveren. Kommer et bilde uten at skriptet har kjørt, lagres
  det slik det er, innenfor grensene.
- Et senere steg — å lese kvitteringen med AI — bygger på dette: filen finnes
  allerede når tolkningen skal gjøres.
