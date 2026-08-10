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

---

## Førstegangsoppsett

Gjøres én gang per maskin.

```bash
# 1. Lokale hemmeligheter for Docker Compose
cp infra/.env.example infra/.env

# 2. Tilkoblingsstrengen. Ligger utenfor repoet, aldri i appsettings.
dotnet user-secrets set "ConnectionStrings:Postgres" \
  "Host=localhost;Port=5434;Database=dyrepermen;Username=dyrepermen;Password=utvikling;Maximum Pool Size=10" \
  --project src/Dyrepermen.Web

# 3. Start databasen
docker compose -f infra/compose.yaml up -d db

# 4. Opprett skjemaet. Migrasjoner kjøres aldri ved oppstart.
dotnet ef database update \
  --project src/Dyrepermen.Infrastructure \
  --startup-project src/Dyrepermen.Web
```

Mangler steg 2, stopper appen ved oppstart med en melding som viser
kommandoen på nytt.

---

## Kjøre appen

**To ting må kjøre: databasen i Docker, og selve appen.** Compose starter
med vilje bare databasen — se «Hvorfor compose ikke starter appen» under.

```bash
# 1. Databasen (hvis den ikke alt kjører)
docker compose -f infra/compose.yaml up -d db

# 2. Appen
dotnet run --project src/Dyrepermen.Web
```

Appen ligger på **<https://localhost:7171>**.

**Bruk https-adressen.** Innloggingskapselen er satt med
`CookieSecurePolicy.Always`, så innlogging virker ikke over `http://`.
Derfor er `https` standardprofil i `launchSettings.json`.

### Hvorfor compose ikke starter appen

`docker compose up -d db` starter nøyaktig én tjeneste: `db`. `web`-tjenesten
i `infra/compose.yaml` er merket `profiles: ["full"]`, og Compose hopper over
tjenester med profil med mindre profilen er bedt om eksplisitt.

Det er bevisst, og er den anbefalte arbeidsmåten i plan kapittel 14.2: med
appen utenfor container beholder du hot reload, debugger og raske omstarter.
Å bygge et Docker-image på nytt for hver kodeendring tar titalls sekunder.

Vil du likevel kjøre **hele stakken** i container — den eneste måten å
verifisere at `Dockerfile` faktisk virker før Render prøver den:

```bash
docker compose -f infra/compose.yaml --profile full up --build
```

Da svarer appen på <http://localhost:8080>. Merk at migrasjoner ikke kjøres
automatisk der heller.

### Nyttige kommandoer

```bash
docker compose -f infra/compose.yaml ps       # hva kjører?
docker compose -f infra/compose.yaml logs db  # databaselogg
docker compose -f infra/compose.yaml down     # stopp, behold data
docker compose -f infra/compose.yaml down -v  # stopp, slett databasen
```

---

## Bygg og test

```bash
dotnet build   # advarsler er byggefeil
dotnet test
```

Integrasjonstestene starter sin egen PostgreSQL med Testcontainers og krever
at Docker kjører. De rører ikke utviklingsdatabasen.

---

## Databaseporten er 5434

Ikke 5432. En lokalt installert PostgreSQL binder `127.0.0.1:5432`, som er
mer spesifikt enn Dockers `*:5432` — da går `localhost:5432` til den lokale
serveren og ikke til containeren. Se ADR 0006.

Har du ingen lokal PostgreSQL, kan du sette `POSTGRES_PORT=5432` i
`infra/.env` og oppdatere tilkoblingsstrengen tilsvarende.

---

## Brancher

| Branch | Formål |
|---|---|
| `main` | Produksjon. Kun stabil, utgivelsesklar kode |
| `dev` | Integrasjon. Alt arbeid samles her før produksjon |
| `feature/mvp` | Første versjon av appen |

Arbeidsflyt: `feature/*` → `dev` → `main`. `Bygg og test` kjører på alle
brancher og pull requests; utrulling skjer kun fra `main`.

---

## Status

MVP er fase 1, 1b og 2 i `docs/plan.md` kapittel 16. Akseptansekriteriene i
samme kapittel er definisjonen av ferdig.

**Fase 1 er i arbeid.** På plass: monorepo-oppsett, hele databaseskjemaet med
query-filtre, isolasjonstest og filterprøve, Identity med 30 dagers vedvarende
innlogging og Data Protection-nøkler i database, innlogging som eneste inngang,
oppstartsskjerm og husstandsoppsett. Gjenstår: `Dyr`-CRUD.
