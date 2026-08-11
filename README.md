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

# 3. Start databasen (eller "docker compose up -d" for hele appen)
docker compose up -d db

# 4. Opprett skjemaet. Migrasjoner kjøres aldri ved oppstart.
dotnet ef database update \
  --project src/Dyrepermen.Infrastructure \
  --startup-project src/Dyrepermen.Web
```

Mangler steg 2, stopper appen ved oppstart med en melding som viser
kommandoen på nytt.

---

## Kjøre appen

Vil du bare kjøre appen, hopp til «Hele appen i Docker» under — der er det
én kommando. Avsnittet her er for **daglig utvikling**, der appen kjøres
utenfor container så du beholder hot reload, debugger og raske omstarter.
Å bygge et Docker-image på nytt for hver kodeendring tar titalls sekunder.

```bash
# 1. Kun databasen. Tjenesten navngis, ellers starter web-containeren også.
docker compose up -d db

# 2. Appen
dotnet run --project src/Dyrepermen.Web
```

Appen ligger på **<https://localhost:7171>**.

**Bruk https-adressen.** Innloggingskapselen er satt med
`CookieSecurePolicy.Always`, så innlogging virker ikke over `http://`.
Derfor er `https` standardprofil i `launchSettings.json`.

## Hele appen i Docker

Kjører app og database i hver sin container. Dette er eneste måte å
verifisere at `Dockerfile` faktisk virker før Render prøver den.

```bash
docker compose up -d            # start alt
docker compose up -d --build    # bygg web-imaget på nytt etter kodeendring
docker compose logs -f web      # følg loggen
docker compose down             # stopp, behold data
docker compose down -v          # stopp og slett databasen
```

Rotfila `compose.yaml` peker på `infra/compose.yaml` med `include`, så du
slipper flagg. Det finnes bare én definisjon — rotfila dupliserer ingenting.

Vil du bare ha databasen, navngir du tjenesten: `docker compose up -d db`.

Appen ligger på **<http://localhost:8080>** — http, ikke https.

**Skjemaet må finnes fra før.** Appen migrerer ikke ved oppstart, verken her
eller lokalt. Har du kjørt `down -v` og slettet databasen, må du starte `db`
alene og kjøre `dotnet ef database update` fra maskinen først.

### Hvorfor http virker her, men ikke lokalt

Innloggingskapselen krever normalt https. Containeren serverer http på 8080
uten noen TLS-terminator foran, slik Render har — så `web`-tjenesten i
`infra/compose.yaml` setter `Sikkerhet__KrevSikkerKapsel=false`.

Standarden er sikker, avviket står ett sted du ser det, appen logger en
advarsel ved oppstart, **og den nekter å starte med dette avslått i
Production.** Det er ikke mulig å rulle ut med kapselen i klartekst.

### Nyttige kommandoer

```bash
docker compose ps        # hva kjører?
docker compose logs db   # databaselogg
docker compose down      # stopp, behold data
docker compose down -v   # stopp, slett databasen
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

**Hele funksjonsomfanget skal bygges før utrulling.** Det finnes ingen
MVP-avgrensning — utrulling er fase 8, helt til slutt. Akseptansekriteriene i
`docs/plan.md` kapittel 16 er definisjonen av ferdig.

### Ferdig

| Fase | Innhold |
|---|---|
| 1 og 1b | Monorepo, hele skjemaet med query-filtre, isolasjonstest og filterprøve, Identity med 30 dagers innlogging, Data Protection-nøkler i database, `Dyr`-CRUD, dashbord |
| 2 | Vekt og behandling, med vektgraf |
| 3 | Medisiner og doser |
| 5a | Forsikring med selskap, premie, egenandeler og forsikringsbeløp |
| 5c | Veterinær: steder med telefon som ringes med ett trykk, kommende og gjennomførte timer med pris og refusjon |
| 6 | Handleliste |
| 6b | Fôringslogg bak funksjonsbryter |
| 6c | Husstand og konto, dataeksport, kontosletting |
| 6d | Handlinger direkte på dashbordet: porsjon for neste måltid, gi mat, godbit, avkryssing av handleliste |
| — | Flere husstander per bruker med gjesterolle, informasjonssider, designgjennomgang |

Dashbordet gjør **seks** databasespørringer uansett antall dyr. Kravet i
kapittel 16 er at ingenting skal vokse med antall dyr; nye kilder slås sammen
med de eksisterende framfor å legges til per rad.

**144 tester grønne.** Enhetstester for ren logikk, integrasjonstester mot
ekte PostgreSQL via Testcontainers. Aldri EF Core InMemory.

To fail-closed prøver holder sikkerheten på plass av seg selv:
`Modellfullstendighet` sammenligner `IHusstandsbundet`-typene i Domain mot
EF-modellen, og `RolleTester` går gjennom hver eneste `POST`-handling og
feiler hvis en mangler `[KreverEier]` uten å stå på den bevisste gjestelisten.

### Gjenstår

- **Fase 5b** — dokumenter med filopplasting
- **Fase 4** — påminnelser på e-post
- **Fase 7** — resten av poleringen, sikkerhetskopi-jobb
- **Fase 8** — utrulling til Render mot Neon
