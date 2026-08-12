# Dyrepermen — arbeidsinstruks

Webapplikasjon for oppfølging av husstandens kjæledyr. ASP.NET Core MVC (.NET 9), EF Core, PostgreSQL, monorepo.

**Full spesifikasjon: `docs/plan.md`.** Dette dokumentet dekker hvordan du jobber. Planen dekker hva som skal bygges. Ved motstrid vinner planen på innhold, dette dokumentet på arbeidsmåte.

---

## Les dette først

- **Hele funksjonsomfanget skal bygges før utrulling.** Det finnes ingen MVP-avgrensning lenger — planens opprinnelige «MVP er fase 1, 1b og 2» er opphevet. Appen kjøres lokalt i Docker gjennom hele løpet, og hosting er fase 8, helt til slutt.
- **Akseptansekriteriene i kapittel 16 er definisjonen av ferdig.** Kod ikke videre til neste fase før de er oppfylt og testene er grønne.
- **Bygg én fase om gangen likevel.** At alt skal med, betyr ikke at alt skal bygges samtidig. En halvferdig fase er verre enn en fase som ikke er påbegynt.
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
- Migrasjoner kjøres ved oppstart, i `Program.cs` rett før `app.Run()`. Dette opphever planens motsatte regel og forutsetter **én instans** — se ADR 0010. Skaleres appen ut, må kallet flyttes ut igjen.

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
6. Push utløser `Bygg og test` på alle brancher

Rødt bygg blokkerer. Ikke omgå det.

Utrulling skjer fra Render, som bygger branchen tjenesten er koblet til — ikke fra GitHub Actions. Skjemaet legges inn ved oppstart, se ADR 0010.

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
- **`HasDefaultValue` på en `bool`** gjør at bryteren aldri kan skrus av. EF bruker CLR-standarden (`false`) som sentinel, utelater kolonnen fra `INSERT`, og databasens `true` slår inn. Sett standardverdien på egenskapen i Domain i stedet, og fyll eksisterende rader i migrasjonen. Samme familie som `HusstandInvitasjon.Rolle`.
- **`AddColumn<char>` uten `defaultValue`** gir `'\0'`. Legger du til et CHECK-vilkår i samme migrasjon, avvises hver eneste eksisterende rad. Sett alltid den verdien de gamle radene faktisk hadde.
- **`OrderBy` på en enum med `HasConversion`** sorterer på det lagrede tegnet, ikke på enumverdien. `'A','F','S','V'` er sjelden rekkefølgen du mente. Sorter etter materialisering, og si i en kommentar hvorfor.
- **Npgsql tar ikke imot `postgresql://`-URI-er.** Neon og Render oppgir databasen på nettopp den formen, og `psql` godtar den — men `NpgsqlConnection`-konstruktøren kaster *før* den forsøker å kontakte noe, så feilen ser ut som et nettverksproblem. `Tilkoblingsstreng.Normaliser` oversetter. Dette stoppet en utrulling.
- **`GetEnumSelectList` skriver ut tallverdien, ikke navnet.** `<option value="0">Hund</option>`. Et skript som sammenligner mot `'Hund'` er alltid usant — og feilet skjuler seg selv, for feltet forsvinner uten en eneste feilmelding. La markupen bære verdien: `data-kun-hund="@((int)Art.Hund)"`.
- **Identitys `RequireUppercase` er ASCII-basert** (`c >= 'A' && c <= 'Z'`), ikke `char.IsUpper`. «Ørnulf7» avvises med «mangler stor bokstav» mens brukeren ser rett på en. Bruk `StorBokstavValidator`.
- **Identity krever tall, små og store bokstaver og spesialtegn som standard.** Setter du bare `RequiredLength`, står de fire andre igjen — usynlig for brukeren og for den som leser koden. Overstyr alle eksplisitt.
- **En regel som står to steder, spriker.** Passordkravet sto i Identity, i en DataAnnotation og i en hjelpetekst. Skjemaet lovet noe annet enn serveren krevde, og brukeren fikk en feil hun ikke kunne forutse. Legg regelen i én konstant, og skriv en test som feiler når de to kommer i utakt.
- **Dagsgrenser i UTC** flytter kveldsaktivitet til «i morgen». Bruk `Tidssone.DagStart`, som henter forskyvningen på midnatt — ikke på nåtidspunktet.
