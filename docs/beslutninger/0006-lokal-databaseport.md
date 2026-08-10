# 0006 — Lokal database bruker vertsport 5434, ikke 5432

**Status:** Vedtatt
**Dato:** 2026-08-10
**Gjelder:** plan kapittel 14.2

## Kontekst

Planens `compose.yaml` publiserer databasen på vertsport 5432. På
utviklingsmaskinen viste det seg å ikke fungere, og feilmåten er verdt å
dokumentere fordi den er lumsk.

Maskinen hadde allerede to PostgreSQL-servere installert lokalt — én på 5432
og én på 5433. Disse binder `127.0.0.1:5432`, mens Docker binder `*:5432`.
En bind til en konkret adresse er mer spesifikk enn en wildcard-bind, så
`localhost:5432` går til den lokale serveren. Containeren kjørte helt fint
ved siden av og svarte på `docker exec`.

Symptomet var:

```
28000: role "dyrepermen" does not exist
```

Altså en feilmelding som peker mot brukeroppsett i containeren, mens årsaken
er at forbindelsen aldri nådde containeren.

**Den farlige varianten er den som ikke feiler.** Hadde den lokale serveren
tilfeldigvis hatt en `dyrepermen`-rolle og en database med samme navn, ville
`dotnet ef database update` kjørt hele skjemaet inn i feil database uten et
eneste varsel. Alt ville sett riktig ut, og avviket ville dukket opp først
når noen lurte på hvorfor dataene forsvant ved `docker compose down -v`.

## Beslutning

Vertsporten settes til **5434** som standard, og gjøres overstyrbar:

```yaml
ports:
  - "${POSTGRES_PORT:-5434}:5432"
```

Containerporten er fortsatt 5432 — det er kun vertssiden som flyttes.
`appsettings.Development.json` og reservestrengen i
`DyrepermenDbContextFactory` peker på 5434.

Begrunnelsen står som kommentar i `compose.yaml` selv, med symptomet nevnt,
slik at neste person som møter feilmeldingen finner svaret der de leter.

## Konsekvens

- Prosjektet kolliderer ikke med en lokalt installert PostgreSQL. Det er en
  vanlig situasjon, ikke et særtilfelle for denne maskinen.
- Tilkoblingsstrengen finnes nå to steder lokalt: `appsettings.Development.json`
  og reservestrengen i fabrikken. Endres porten, må begge endres.
  **Dette bør ryddes** når `Program.cs` registrerer `AddDbContext` — da kan
  design-time-fabrikken fjernes, og `dotnet ef` leser appens konfigurasjon.
- Ingen påvirkning på Docker-nettverket, CI eller produksjon. Der brukes
  tjenestenavnet `db:5432` og `ConnectionStrings__Postgres` som miljøvariabel.
- Har du ingen lokal PostgreSQL, kan du sette `POSTGRES_PORT=5432` i
  `infra/.env` og endre tilkoblingsstrengen tilsvarende.
