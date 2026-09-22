# 0015 — Demomodus: én fersk demohusstand per besøkende

**Status:** Vedtatt, ikke påbegynt
**Dato:** 2026-09-14
**Gjelder:** plan kapittel 9, 11.2 og 12 — demomodus er ikke med i planen

> **Til den som tar opp arbeidet:** Planen ble godtatt med «ser bra ut» den
> 14.09, uten endringer i de seks beslutningene under. Ingenting er kodet ennå.
> Start på steg 1 i sjekklisten nederst. Demodataene i avsnitt 3 er et utkast —
> vis Marius det konkrete innholdet før steg 2 er ferdig.

## Kontekst

Besøkende skal kunne trykke **Prøv en demo** uten å lage konto eller logge inn,
og se alle funksjonene med realistiske data: vekt, fôring, fôrplan, medisin,
forfall, veterinær, forsikring, handleliste og informasjon.

Dataene skal være de samme hver gang, men datoene skal følge dagens dato, slik
at det aldri ser ut som det er lenge siden noe ble registrert.

## Beslutning

### 1. Én demohusstand per besøkende, ikke én felles

Klikket oppretter en demobruker og en husstand, fyller husstanden fra en fast
mal med datoer regnet ut fra i dag, og logger inn. Demoen slettes etter
24 timer.

Hvorfor ikke én felles demo:

- **Skrivebeskyttet** kan ikke vise det viktigste — registrere vekt, gi mat,
  krysse av på handlelisten.
- **Skrivbar** betyr at besøkende ser hverandres endringer, og at én person kan
  ødelegge den for alle.
- **Ferske datoer** ville krevd en jobb som skriver om felles data løpende.

Én husstand per besøkende gjenbruker det som allerede er bygget og testet:
husstandsisolasjonen, query-filtrene, rollene og alle tjenestene. Det finnes
ingen egne «demostier» i tjenestene. Alt virker, fordi det *er* den ekte appen.

### 2. Brukerflyt

- **Innloggingssiden** (`Views/Konto/LoggInn.cshtml`) er forsiden for
  uinnloggede. Under knappen «Opprett ny konto» kommer en diskret tekstknapp:
  **Prøv en demo**.
- **POST-skjema, ikke lenke.** Klikket oppretter data. En lenke ville latt
  søkemotorer og forhåndshenting i nettleseren lage demoer.
- Etter klikket lander brukeren på Oversikt (`/`).
- **Banner** øverst på alle sider i demo, i `_Layout.cshtml`: «Du prøver en
  demo. Alt slettes automatisk innen 24 timer.» med knappen **Opprett din egen
  konto**, som logger ut av demoen før den går til `/registrer`.
- **Logg ut** i en demo sletter demoen straks.
- Innloggingskapselen i demo er `isPersistent: false` — den overlever ikke at
  nettleseren lukkes. Ikke 30 dager slik «husk meg» gir.

### 3. Demodataene

Faste data, ingen tilfeldighet. Kun datoene flytter seg. Justert etter
Marius' innspill 15.09 — koden i `Demomal` er fasiten, tabellen er
oversikten.

| Funksjon | Innhold |
|---|---|
| Husstand | «Demohusstanden». Brukeren heter «Demobruker», rolle Beboer |
| Dyr | **Trixie**, hund, 4 år · **Rose**, valp, 22 uker · **Milo**, katt, 2 år, eneste kastrerte. Fødselsdato regnes fra i dag, så alderen står stille |
| Vekt | Trixie: 12 måneder rundt 25 kg · Rose: ukentlige veiinger fra 4,2 til 10,8 kg · Milo: 4,2 kg · siste veiing for 2 dager siden |
| Fôrplan | Trixie i gram · Rose 50/50: råfôr etter vekt og tørrfôr etter alderstabell, mellom to rader · Milo i prosent |
| Fôring | Trixie og Rose: i dag er frokost gitt og middag ikke — så «Gi mat» vises · i går: alt gitt, pluss én godbit til Trixie · Milo har fôringsloggen av, så Oversikt viser porsjonen uten knapper. Det finnes ingen godbitbryter per dyr, så Rose beholder godbitknappen |
| Medisin | Trixie har en pågående kur. Siste dose i går, neste i dag |
| Behandlinger | Vaksine **forfalt for 5 dager siden** · ormekur forfaller om 10 dager · historikk bakover |
| Veterinær | «Demoklinikken» (fast, 12345678) og «Demo døgnvakt» (vakt, 11223344) med åpningstider — numrene er valgt av Marius, og uten dem viser ikke Oversikt veterinærene · kommende time om 6 dager · tidligere besøk med kostnad og refusjon |
| Forsikring | Én polise for Trixie |
| Handleliste | Tre varer, én kjøpt |
| Informasjon | Notater om rutiner og allergier |
| Innstillinger | Alle funksjonsbrytere på |

Forfall regnes i `DashbordService` fra `Behandling.NesteDato` og
`Vetbesok.NesteKontrollDato`. Datoene i malen må treffe de vinduene, ellers
viser ikke Oversikt noe forfall.

**«I dag»-regelen:** Starter noen demoen kl. 06:00, kan frokosten ikke være
«gitt 07:30 i dag» — da ligger den i framtiden. Alt som skal ha skjedd i dag,
plasseres før nåtidspunktet, og flyttes til i går hvis det ikke går. Ren
funksjon med enhetstester, også for dagen sommertiden starter og slutter. Bruk
`Tidssone.Idag` og `Tidssone.TilLokal` — aldri UTC-dato.

### 4. Skjema

Én ny kolonne: **`bruker.demo_utloper timestamptz NULL`**. Ekte brukere har
`NULL`. Indeks på kolonnen, fordi oppryddingen søker på den.

- Nullbar og uten standardverdi — unngår fella med `HasDefaultValue` i
  CLAUDE.md.
- På brukeren, ikke husstanden: restriksjonene og oppryddingen tar
  utgangspunkt i hvem som er innlogget. Husstanden følger med gjennom
  slettingen av «husstander der hun er eneste medlem».
- Ingen ny husstandsbundet entitet, så isolasjonstesten trenger ingen ny type.

### 5. Nye filer, i samme lagdeling som ellers

| Fil | Innhold |
|---|---|
| `Application/Extensions/Demomal.cs` | Ren funksjon: `Bygg(bruker, naa)` gir en `Demodata` — husstanden med alt som henger på den, pluss veterinærene og notatene, som `Husstand` ikke har samlinger for. `idag` regnes fra `naa`, så de to kan ikke spri. Ingen databasetilgang. Enhetstestes |
| `Application/Interfaces/IDemoService.cs` | `Start(ct)` rydder utløpte demoer, sjekker taket og gir bruker-ID — eller `null` når taket er nådd. Oppryddingen er privat i `DemoService`, fordi ingen andre kaller den |
| `Infrastructure/Services/DemoService.cs` | Oppretter bruker uten passord med `UserManager.CreateAsync(bruker)`, e-post `demo-{guid:N}@demo.invalid` (`.invalid` er et reservert toppdomene og kan aldri motta e-post). Husstand, medlemskap som Beboer, og malen — alt i én transaksjon |
| `Web/Controllers/DemoController.cs` | `POST /demo`, `[AllowAnonymous]`, `[ValidateAntiForgeryToken]`, `[EnableRateLimiting]`. Er brukeren allerede innlogget: send til `/`. Ellers `Rydd`, `Start`, `SignInAsync(bruker, isPersistent: false)`, send til `/` |
| `Web/Filtre/StengtIDemoAttribute.cs` | Avviser handlingen når `IGjeldendeBruker.ErDemo` er sann |

Registrering: `IDemoService` i `LeggTilInfrastruktur()`
(`Infrastructure/TjenesteRegistrering.cs`), grense for antall forespørsler i
`Program.cs` ved siden av `KontaktController.Grense`.

`IGjeldendeBruker` får `bool ErDemo`. `HusstandMiddleware` fyller den i samme
spørring som henter `Visningsnavn` og `Email` — ikke en ekstra tur til
databasen.

### 6. Sletting skal ligge ett sted

`KontoService.SlettBruker` (`Infrastructure/Services/KontoService.cs`) sletter
i dag i denne rekkefølgen, innenfor en transaksjon:

1. Brukeren via `UserManager.DeleteAsync` — `SET NULL` avidentifiserer vekt,
   doser og fôringer
2. `Dyr` i husstander der hun var eneste medlem — **må** slettes eksplisitt,
   fordi `Dyr` har `DeleteBehavior.Restrict`
3. `Handleliste` og `Informasjon` i de samme husstandene
4. Husstandene

Den krever passord, og demobrukere har ikke passord. **Trekk ut steg 1–4 i én
felles metode** som både kontosletting og demoopprydding bruker. To kopier
sprikte første gang noen la til en ny tabell.

### 7. Opprydding

- Kjøres ved hver nye demo: slett demoer der `demo_utloper < nå`, i porsjoner.
  Ingen planlagt jobb trengs.
- Kjøres også ved «Logg ut» fra en demo, for den ene demoen.
- **Tak på aktive demoer: 300.** Er taket nådd etter oppryddingen, vises en
  vennlig melding i stedet for en ny demo. Beskytter lagringen på Neons
  gratisnivå.

### 8. Hull som må tettes først

Slettes en bruker mens innloggingskapselen fortsatt gjelder, finner
`HusstandMiddleware` ingen brukerrad, setter `Visningsnavn = ""` og null
medlemskap — og sender brukeren til `/husstand/oppsett`.
`SecurityStampValidator` sjekker bare hver 12. time (`Program.cs`). Prøver hun
å opprette en husstand der, feiler det med en fremmednøkkelfeil.

**Rettelse:** Finnes ikke brukerraden, logg ut og send til `/logg-inn`.
Demoer slettes hver dag, så dette skjer ofte. Rettingen hjelper også ekte
brukere som slettet kontoen fra en annen enhet.

### 9. Stengt i demo

Håndheves på serveren med `[StengtIDemo]`, og skjules i grensesnittet.

| Handling | Hvorfor |
|---|---|
| `InnstillingController.LeggTilMedlem`, `AngreInvitasjon`, `FjernMedlem` | **Viktigst.** Ellers kan en anonym besøkende legge en ekte persons e-postadresse inn i demohusstanden, og personen ser plutselig en fremmed husstand i menyen sin |
| `KontaktController.Send` | Svar-til ville vært en falsk adresse, og det åpner for spam gjennom SMTP-kontoen |
| `MinKontoController.Slett` | Krever passord. Erstattes av «Avslutt demo» |
| `HusstandController.Opprett` | Ellers kan én demo lage ubegrenset mange husstander |

Alle andre POST-handlinger er åpne i demo — det er poenget. Per 14.09 finnes
ingen filopplasting (`IFormFile` brukes ikke), så `Dokument` er ikke et
lagringsproblem ennå.

**Fase 4:** Påminnelsesjobben finnes ikke ennå. Når den bygges, **må** den
hoppe over brukere med `demo_utloper IS NOT NULL`. Det samme gjelder all annen
kode som sender e-post.

### 10. Misbruk og sikkerhet

- Bare `POST /demo` blir anonym. `FallbackPolicy` er uendret.
- **5 demoer i timen per IP.** `ForwardedHeaders` gir den ekte IP-en bak
  Render. Partisjonér på `HttpContext.Connection.RemoteIpAddress`.
- Taket på 300 aktive stopper massegenerering fra mange IP-er.
- Demobrukere har ikke passord — ingen innlogging å stjele.
- Logg bruker-ID, aldri e-post. Demoadressene er falske, men regelen gjelder
  likevel.

## Tester

**Enhetstester** (`tests/Dyrepermen.Application.Tests/DemomalTester.cs`):

- Samme `idag`/`naa` gir identisk mal
- Vaksinen er forfalt og timen er kommende uansett hvilken dato demoen startes
- Ingen «i dag»-hendelse ligger i framtiden — kl. 00:30, 06:00, 23:59, og på
  dagene sommertiden starter og slutter

**Integrasjonstester** (`tests/Dyrepermen.Integration.Tests/DemoTester.cs`):

- `POST /demo` gir 302 til `/`, og `/` viser dyrenavnene og forfallet
- `POST /demo` uten antiforgery-token avvises
- To demoer ser ikke hverandres data
- De stengte handlingene avvises for en demobruker
- **Opprydding:** en utløpt demo etterlater null rader i alle husstandsbundne
  tabeller. Gå gjennom alle `IHusstandsbundet`-typene i EF-modellen, slik
  `FilterTester` gjør — da feiler testen av seg selv når noen legger til en
  tabell som ikke ryddes
- En slettet bruker med gyldig kapsel blir logget ut (hullet i avsnitt 8)
- `DemoController.Start` legges bevisst på gjestelisten i `RolleTester`
- En test som bekrefter at de fire handlingene i avsnitt 9 har `[StengtIDemo]`

## Sjekkliste — én del om gangen, grønt mellom hver

- [x] **1.** Migrasjon for `bruker.demo_utloper` med indeks · rettelsen i
      `HusstandMiddleware` (avsnitt 8) med test. Kolonnen ligger i
      `asp_net_users` — Identity-tabellen `Bruker` er mappet til. Testen er
      `SikkerhetTester.Slettet_bruker_med_gyldig_kapsel_blir_logget_ut`
- [x] **2.** `Demomal` med enhetstester · vis Marius demodataene. Godkjent
      15.09 etter justering av navn, vekt og Roses blandingsplan
- [x] **3.** `DemoService`: oppretting, felles slettemetode (avsnitt 6),
      opprydding og tak · integrasjonstester. Den felles metoden er
      `Brukersletting.SlettBrukere`. Underveis kom en feil i kontoslettingen
      fram: dyrene ble slettet før handlelisten, som peker på dem med
      RESTRICT, så en husstand med en vare knyttet til et dyr kunne ikke
      slettes. Ingen test dekket kontosletting. `KontoslettingTester` ble
      sett rød på `fk_handleliste_dyr_dyr_id` før rettelsen
- [x] **4.** `DemoController`, knappen på innloggingssiden, banneret,
      `[StengtIDemo]` · resten av testene. «Opprett din egen konto» poster
      til `/logg-ut` med `registrer=true` — en bool, ikke en adresse, så den
      kan ikke brukes til å sende noen videre. «Avslutt demo» på Min konto
      er utlogging. `IDemoService.Avslutt` sletter bare brukere med
      `demo_utloper` satt
- [ ] **5.** `dotnet build` uten advarsler, `dotnet test` grønt · manuell sjekk
      i nettleseren på dev før `main`

Git gjøres av Marius — endre filer, ikke commit.

## Konsekvens

- Databasen får rader for anonyme besøkende i opptil 24 timer.
- Kontoslettingen endres (felles slettemetode), og dekkes av de eksisterende
  testene for sletting i tillegg til de nye.
- Ny anonym inngang til appen. Alt bak den er den vanlige, innloggede appen,
  med de samme filtrene og rollene.
