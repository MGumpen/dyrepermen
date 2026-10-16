# 0019 — Behandling over flere dager og flere ganger per dag

**Status:** Vedtatt
**Dato:** 2026-10-10
**Gjelder:** plan kapittel 4, 5, 8.3 og 10.3 — tabellene `behandling` og `behandling_dose`

## Kontekst

En behandling var én hendelse på én dato. Men mange behandlinger er kurer: ormekur
én gang daglig i tre dager, øyedråper to ganger daglig i en uke. Uten støtte for
det måtte brukeren registrere tre behandlinger, og ingenting på oversikten sa om
dagens dose var gitt — eller om noen andre i husstanden allerede hadde gitt den.

## Beslutning

**Datamodell**

- `behandling` får `antall_dager INT NOT NULL DEFAULT 1` og
  `ganger_per_dag INT NOT NULL DEFAULT 1`, begge med CHECK `>= 1`. Eksisterende
  rader får 1 og 1 i migrasjonen, og oppfører seg som før.
- `dato` er første dag. Kuren er aktiv fra og med `dato` til og med
  `dato + antall_dager - 1`.
- Ny tabell `behandling_dose (id, behandling_id, gitt_tid, gitt_av_bruker_id)`,
  med samme form som `dose` for medisin. `ON DELETE CASCADE` fra behandlingen,
  `ON DELETE SET NULL` fra brukeren, indeks på `(behandling_id, gitt_tid DESC)`.
  Query-filteret går gjennom to ledd: `d.Behandling.Dyr.HusstandId`.
- Domenet setter standardverdien 1 selv. EF sender da alltid verdien, og
  databasens `DEFAULT 1` gjelder bare rader som ikke kommer fra appen.

**Regler**

- En behandling med 1 dag og 1 gang er en vanlig behandling. Den regnes som gitt
  når den registreres, får ingen doser og ingen «Gitt»-knapp.
- Alt annet er en kur. En dose kan bare registreres når kuren er aktiv i dag, og
  høyst `ganger_per_dag` doser per kalenderdag.
- «I dag» er norsk dato (`Tidssone.Idag`), og dosene telles fra norsk midnatt
  (`Tidssone.DagStart`).
- Reglene håndheves i `IBehandlingService.LoggDose`. Tjenesten slår opp
  behandlingen gjennom query-filteret, låser raden med `SELECT ... FOR UPDATE` i
  en transaksjon, teller dagens doser og legger inn den nye. To som trykker
  samtidig, blir dermed satt i kø, og den andre teller dosen den første la inn.
- Regelen for «aktiv kur» står i `Behandlingsfilter.AktivKur` for spørringene og
  i `Behandlingskur` for minnet. En test feiler når de spriker.

**Oversikten**

- Aktive kurer står i «Forfaller snart» med fremdrift: «Ormekur – Milbemax – dag
  2 av 3 · 1/2 i dag», og en «Gitt»-knapp til dagens doser er gitt. Deretter står
  raden igjen som «fullført i dag», uten knapp. Raden regnes aldri som forfalt.
- Kurene hentes som en korrelert underspørring i dashbordets spørring 1, slik
  medisinene gjør. Dashbordet får ingen ny rundtur.
- Knappen poster til `POST /dashbord/kur/{dyrId}/{behandlingId}`. Med htmx byttes
  bare raden ut, og svaret viser raden slik den faktisk er — også når noen andre
  har gitt dagens siste dose. Uten skript er det et vanlig skjema som sender
  brukeren tilbake til oversikten.
- Samme rad og knapp står på dyrets side, under «Kommende behandlinger», over
  påminnelsene om neste gang. Kurene hentes i samme spørring som resten av
  sammendraget. Knappen sender `fraDyr=true`, så svaret er dyrets utgave av raden
  og en innsending uten skript lander på dyrets side.
- Historikken på behandlingssiden viser dosene i en kur, med tidspunkt og hvem
  som krysset av («i dag 08:12 – Kari»), i en `details`-blokk som åpnes uten
  skript. En slettet bruker vises som «slettet bruker».
- **Handlingen er åpen for gjester**, som «gi dose» på en medisin (ADR 0009).
  Passer du hunden mens kuren pågår, er det du som gir den. Den står på
  gjestelisten i `RolleTester`. Å registrere eller rette selve kuren krever
  fortsatt beboer.

**Valg som ikke var spesifisert**

- «Gitt i dag» på påminnelsen om neste gang kopierer kurlengden: neste ormekur er
  en tredagerskur den også. Dagens første dose logges med det samme — ellers ville
  oversikten sagt «0/1 i dag» rett etter at brukeren sa at den var gitt.
- Forslagene i skjemaet («Gitt før») tar også med kurlengden.
- Rettes en kur, blir doser som allerede er logget, stående.
- Skjemaet tillater 1–365 dager og 1–24 ganger per dag. Databasen krever bare
  `>= 1`. Taket fanger tastefeil og hindrer at en enkelt kur kan gi tusenvis av
  rader.

## Konsekvenser

- Påminnelsene på e-post (fase 4) er ikke bygd. Når `PaminnelseService` kommer,
  skal den ta med aktive kurer der dagens doser ikke er fullført. Bruk
  `Behandlingsfilter.AktivKur` og tell `behandling_dose` fra `Tidssone.DagStart`
  i samme spørring — ikke skriv vilkåret på nytt. `neste_dato` brukes som før til
  påminnelsen om neste kur.
- En kur som har en åpen påminnelse om neste gang innenfor varselvinduet, kan stå
  i «Forfaller snart» to ganger: én gang som kur og én gang som påminnelse. Med
  vanlige intervaller på måneder skjer det ikke i praksis.
