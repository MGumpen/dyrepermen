# 0002 — CI kjører på alle brancher, ikke bare main

**Status:** Vedtatt
**Dato:** 2026-08-10
**Gjelder:** plan kapittel 14.5, CLAUDE.md «Arbeidsflyt»

## Kontekst

Planen begrenser `Bygg og test` til `push` mot `main`, og CLAUDE.md gjentar
det: «Push til `main` utløser `Bygg og test`».

Prosjektet bruker tre brancher: `main` for produksjon, `dev` for integrasjon
og `feature/*` for arbeid. Med planens utløser testes ingenting før det
allerede står i produksjonsbranchen.

Det gjør også grenbeskyttelsen i samme kapittel virkningsløs: «Require status
checks to pass» kan ikke kreve en sjekk som aldri har kjørt på kilden til
en pull request.

## Beslutning

`bygg.yml` utløses på `push` til alle brancher og på alle `pull_request`.
Sti-filteret fra planen beholdes på `push`, slik at endringer i `docs/` ikke
utløser bygg. På `pull_request` er det bevisst ikke sti-filter, slik at en
påkrevd statussjekk alltid rapporterer.

Utrulling forblir uendret: kun fra `main`, og kun etter grønt bygg.

`concurrency` avbryter et pågående bygg når det kommer ny push på samme
branch — men aldri på `main`, der hvert bygg kan utløse en utrulling.

## Konsekvens

- Rødt bygg oppdages mens arbeidet skrives, ikke etter at det er merget.
- Grenbeskyttelse på `main` med `Bygg og test` som påkrevd sjekk får mening.
- Noe høyere forbruk av CI-minutter. På et privat repo med to brukere og et
  bygg på under et minutt er det uten praktisk betydning.
