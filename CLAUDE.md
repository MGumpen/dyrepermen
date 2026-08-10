# Dyrepermen — arbeidsinstruks

Webapplikasjon for oppfølging av husstandens kjæledyr. ASP.NET Core MVC (.NET 9), EF Core, PostgreSQL, monorepo.

**Full spesifikasjon: `docs/plan.md`.** Dette dokumentet dekker hvordan du jobber. Planen dekker hva som skal bygges. Ved motstrid vinner planen på innhold, dette dokumentet på arbeidsmåte.

---

## Les dette først

- **MVP er fase 1, 1b og 2** (plan kapittel 16). Bygg ikke funksjoner fra senere faser fordi tabellen finnes i skjemaet.
- **Akseptansekriteriene i kapittel 16 er definisjonen av ferdig.** Kod ikke videre til neste fase før de er oppfylt og testene er grønne.
- **Isolasjonstesten skrives først** (kapittel 17.3), før første funksjon.

---

## Språk

| Hva | Språk |
|---|---|
| Domeneklasser, egenskaper, tabeller, kolonner | Norsk: `Dyr`, `VektGram`, `husstand_id` |
| Tjenester, grensesnitt, metoder | Norsk: `IForplanService.BeregnAktiv` |
| Rammeverkstyper og mønstre | Engelsk som rammeverket bruker: `DbContext`, `Controller`, `IAsyncLifetime` |
| Grensesnittekst, feilmeldinger, e-post | Norsk bokmål |
| Kodekommentarer | Norsk |
| Commit-meldinger | Norsk |
| Ruter | Norsk: `/dyr`, `/handleliste`, `/logg-inn` |

Ikke bruk æ, ø eller å i klassenavn, filnavn, tabellnavn eller ruter. Skriv `Foring`, ikke `Fôring`. `Vetbesok`, ikke `Vetbesøk`. Æøå brukes kun i tekst som vises til brukeren.

## Kodekonvensjoner

- `nullable enable`, `ImplicitUsings`, `TreatWarningsAsErrors` — satt i `Directory.Build.props`. **Advarsler er byggefeil.** Undertrykk ikke med `#pragma`; fiks årsaken.
- File-scoped namespaces: `namespace Dyrepermen.Domain.Entities;`
- `sealed` som standard på klasser som ikke arves fra
- Records for DTO-er og resultattyper, klasser for entiteter
- Fire mellomrom, LF, UTF-8 (`.editorconfig`)
- Konstruktørinjeksjon, ingen service locator, ingen statisk tilstand
- Alle asynkrone metoder tar `CancellationToken` og videresender den
- Ett offentlig type per fil, filnavn lik typenavn

## Arkitektur

`Web → Application → Domain` og `Infrastructure → Application → Domain`. Domain refererer ingenting.

- **Controllere er tynne.** De mapper mellom ViewModel og tjeneste. Ingen forretningsregler, ingen `DbContext` direkte.
- **Forretningslogikk i `Dyrepermen.Application`**, bak grensesnittene i plan kapittel 6.6.
- **Legger du en EF Core-avhengighet i Domain, er lagdelingen borte.** Ingenting feiler — du må passe på selv.

## Datalag

- Alle husstandsbundne entiteter implementerer `IHusstandsbundet` **og** får query-filter i `DyrepermenDbContext`. Filterprøven fanger glemte filtre, men bare hvis markørgrensesnittet er satt.
- `IHusstandContext` leser fra database, ikke fra claim. Se plan 7.2 og 12.3.1.
- Enkle datatyper: `INT`, `VARCHAR`, `CHAR`. Enums lagres som `char(1)` med eksplisitt `HasConversion`.
- Vekt lagres i gram som `INT`. Prosent lagres i tidels prosent som `INT`.
- Skriv aldri `Include` etterfulgt av `.Last()` i C# — projiser i spørringen.
- Migrasjoner kjøres aldri ved oppstart.

## Sikkerhet — ufravikelig

- `[ValidateAntiForgeryToken]` på alle POST-handlinger
- Feilmeldinger avslører ikke om en e-postadresse finnes eller hvilken husstand noe tilhører
- Logg aldri passord, jobbnøkkel, tilkoblingsstreng eller e-postadresser. Logg bruker-ID
- `SetApplicationName("dyrepermen")` er en intern nøkkelringidentifikator og skal **aldri** endres, heller ikke ved navnebytte
- Funksjonsbrytere styrer visning *og* skal håndheves på serveren
- Hemmeligheter i `infra/.env` lokalt, miljøvariabler i produksjon. Aldri i `appsettings.json`

## Kommandoer

```bash
# Lokal database
docker compose -f infra/compose.yaml up -d db

# Kjør appen
dotnet run --project src/Dyrepermen.Web

# Hele stakken i container
docker compose -f infra/compose.yaml --profile full up --build

# Migrasjon
dotnet ef migrations add <navn> \
  --project src/Dyrepermen.Infrastructure \
  --startup-project src/Dyrepermen.Web \
  --output-dir Persistence/Migrations

dotnet ef database update \
  --project src/Dyrepermen.Infrastructure \
  --startup-project src/Dyrepermen.Web

# Bygg og test
dotnet build
dotnet test
```

## Pakker

Sentral pakkestyring i `Directory.Packages.props`. **Legg aldri `Version` i en `.csproj`.** Ny pakke: legg `PackageVersion` i rotfilen, `PackageReference` uten versjon i prosjektet.

## Testing

- Enhetstester for ren logikk, integrasjonstester mot ekte PostgreSQL via Testcontainers
- **Bruk aldri EF Core InMemory.** Den håndhever ikke constraints og gir grønne tester på kode som feiler i produksjon
- Hver testklasse er uavhengig av rekkefølge og av andre testers data
- Ny entitet med husstandstilknytning → legg til i isolasjonstesten samme commit

## Arbeidsflyt

1. Les akseptansekriteriene for gjeldende fase i plan kapittel 16
2. Skriv testen først der det er mulig
3. Implementer
4. `dotnet build` med null advarsler, `dotnet test` grønt
5. Commit på norsk, imperativ form: «Legg til vektregistrering»
6. Push til `main` utløser `Bygg og test`. Grønt bygg utløser utrulling

Rødt bygg blokkerer utrulling. Ikke omgå det.

## Når spesifikasjonen er uklar

Planen har et kapittel 18 med åpne spørsmål. Er noe uspesifisert:

1. Velg det enkleste alternativet som oppfyller akseptansekriteriet
2. Skriv en kort ADR i `docs/beslutninger/` med hva du valgte og hvorfor
3. Ikke bygg ut en generell løsning for et problem som ikke finnes ennå

Ikke gjett på ting som er sikkerhetsrelatert eller påvirker skjemaet. Spør.

## Fallgruver som allerede har kostet tid

- **Data Protection-nøkler på filsystem** logger ut alle ved hver utrulling. De skal i database.
- **`RefreshSignInAsync` glemt** gir tom app i opptil 30 dager. Løst ved å lese husstand fra database, men ikke legg claim-oppslag tilbake.
- **`Directory.*.props` ikke kopiert inn i Dockerfile før `restore`** gir «pakke mangler versjon».
- **`context: .` i compose** i stedet for `context: ..` gjør at bygget ikke finner `.csproj`-filene.
- **`InvariantGlobalization=true`** får tidssonekonvertering til å kaste i containeren.
- **`char(15)` i PostgreSQL** blank-padder. Bruk `varchar` med lengde-CHECK.
- **Manglende sjekk på at e-postadressen tilhører en annen husstand** lar hvem som helst tømme en fremmed husstand.
