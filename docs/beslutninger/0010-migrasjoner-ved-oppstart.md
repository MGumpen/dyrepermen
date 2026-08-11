# 0010 — Migrasjoner kjøres ved oppstart

**Status:** Vedtatt
**Dato:** 2026-08-11
**Gjelder:** plan kapittel 14.3, 14.5, CLAUDE.md «Datalag»

## Kontekst

Planen slår fast at migrasjoner aldri skal kjøres ved oppstart. Skjemaet
skulle i stedet legges inn av et eget steg i `.github/workflows/rull-ut.yml`,
før Render fikk beskjed om å rulle ut.

Begrunnelsen er reell, men gjelder et scenario prosjektet ikke er i: kjører
to instanser oppstart samtidig, migrerer de samtidig, og databasen havner i
en tilstand ingen har designet.

Driften er én web-tjeneste på Renders gratisnivå med én instans. Prisen for
å beskytte mot flere instanser var at appen ikke kunne rulles ut uten en
GitHub Actions-arbeidsflyt og fire hemmeligheter i tillegg — et helt
utrullingsapparat for et oppsett som ellers består av to bokser.

## Beslutning

**`DyrepermenDbContext.Database.MigrateAsync()` kalles i `Program.cs` rett
før `app.Run()`.**

`.github/workflows/rull-ut.yml` og `rull-ut-dev.yml` opprettes ikke.
Utrulling skjer fra Render, som bygger branchen den er koblet til.

`MigrateAsync` er idempotent — den leser `__EFMigrationsHistory` og kjører
bare det som mangler. En omstart uten nye migrasjoner gjør ingenting.

`tools/migrer.sh` beholdes. Det er fortsatt riktig verktøy når skjemaet skal
endres uten å rulle ut kode, eller når en migrasjon skal inspiseres før den
kjøres.

## Konsekvens

- Render alene er nok til å få appen i lufta. Ingen GitHub-hemmeligheter,
  ingen manuelle terminalkommandoer.
- Rekkefølgen migrer → rull ut er garantert, siden begge skjer i samme
  prosess. Faren for at ny kode møter et gammelt skjema er borte, ikke bare
  redusert.
- **En feilet migrasjon hindrer oppstart.** Det er ønsket: en app som kjører
  mot et halvferdig skjema er verre enn en som ikke starter. Render viser
  feilen i utrullingsloggen og beholder forrige versjon i drift.
- Første oppstart mot en sovende Neon-branch tar noen sekunder ekstra mens
  databasen vekkes. `GET /helse` treffer fortsatt ikke databasen.
- **Dette må reverseres før appen skaleres til flere instanser.** Render
  Free har én; en oppgradering med `numInstances > 1` gjør antakelsen
  usann uten at noe sier fra.

## Vurdert og forkastet

`EnableRetryOnFailure` for å tåle Neons oppvåkning ble vurdert. Den kan ikke
brukes her: `ForplanService`, `HusstandService` og `KontoService` åpner
egne transaksjoner med `BeginTransactionAsync`, og en retrying execution
strategy kaster på brukerstyrte transaksjoner.
