# Dyrepermen

Webapplikasjon for oppfølging av husstandens kjæledyr.

ASP.NET Core MVC (.NET 9) · Entity Framework Core · PostgreSQL · Monorepo

## Dokumentasjon

| Fil | Innhold |
|---|---|
| `CLAUDE.md` | Arbeidsinstruks: kodekonvensjoner, kommandoer, fallgruver |
| `docs/plan.md` | Full teknisk spesifikasjon |
| `docs/plan.pdf` | Samme dokument for lesing |
| `docs/beslutninger/` | ADR-er — én fil per beslutning tatt underveis |

## Kom i gang

```bash
# 1. Lokale hemmeligheter
cp infra/.env.example infra/.env

# 2. Database. Vertsport er 5434, ikke 5432 — se ADR 0006
docker compose -f infra/compose.yaml up -d db

# 3. Skjema. Migrasjoner kjøres aldri ved oppstart
dotnet ef database update \
  --project src/Dyrepermen.Infrastructure \
  --startup-project src/Dyrepermen.Web

# 4. Kjør appen på https://localhost:7171
dotnet run --project src/Dyrepermen.Web
```

**Bruk HTTPS-adressen.** Innloggingskapselen er satt med
`CookieSecurePolicy.Always`, så innlogging virker ikke over `http://`.
`https` er standardprofil i `launchSettings.json` nettopp derfor.

```bash
# Bygg og test. Advarsler er byggefeil
dotnet build
dotnet test
```

Integrasjonstestene starter sin egen PostgreSQL med Testcontainers og krever
at Docker kjører. De rører ikke utviklingsdatabasen.

## Brancher

| Branch | Formål |
|---|---|
| `main` | Produksjon. Kun stabil, utgivelsesklar kode |
| `dev` | Integrasjon. Alt arbeid samles her før produksjon |
| `feature/mvp` | Første versjon av appen |

Arbeidsflyt: `feature/*` → `dev` → `main`. `Bygg og test` kjører på alle
brancher og pull requests; utrulling skjer kun fra `main`.

## Status

MVP er fase 1, 1b og 2 i `docs/plan.md` kapittel 16. Akseptansekriteriene i
samme kapittel er definisjonen av ferdig.

**Fase 1 er i arbeid.** På plass: monorepo-oppsett, hele databaseskjemaet med
query-filtre, isolasjonstest og filterprøve, Identity med 30 dagers vedvarende
innlogging og Data Protection-nøkler i database, innlogging som eneste inngang,
oppstartsskjerm og husstandsoppsett. Gjenstår: `Dyr`-CRUD.
