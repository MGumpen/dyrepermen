# 0014 — Kontaktskjema på e-post

**Status:** Vedtatt
**Dato:** 2026-09-14
**Gjelder:** plan kapittel 14.4 og 17

## Kontekst

Brukerne skal kunne sende spørsmål og ønsker om endringer i appen. Planen
nevner ikke et kontaktskjema. Den har derimot SMTP-innstillinger for varsler i
kapittel 14.4 (`Epost__SmtpHost`, `Epost__Bruker`, `Epost__Passord`), og
e-postutsending i fase 4.

## Beslutning

**Henvendelsen sendes på e-post, og lagres ikke.** Ingen tabell og ingen
migrasjon. Svar-til er brukerens egen adresse, så et svar fra innboksen går
rett til henne.

**SMTP med `System.Net.Mail`, uten ny pakke.** Microsoft anbefaler MailKit for
ny kode, men for noen få meldinger i timen over STARTTLS på port 587 er
`SmtpClient` nok. Den støtter ikke implisitt TLS på port 465. Trengs det, er
det tiden for å bytte.

**Innstillingene** ligger i seksjonen `Epost`. De tre fra planen, og tre nye:

| Nøkkel | Innhold |
|---|---|
| `Epost__SmtpHost` | SMTP-vert |
| `Epost__Port` | Valgfri, 587 som standard |
| `Epost__Bruker`, `Epost__Passord` | Innlogging mot SMTP |
| `Epost__Avsender` | Fra-adressen. Egen nøkkel fordi brukernavnet hos flere tjenester er `apikey`, ikke en adresse |
| `Epost__Kontaktmottaker` | Hvor henvendelsene havner |

Hvor de settes, avhenger av hvordan appen kjører:

| Kjøring | Sted |
|---|---|
| `dotnet run` | `dotnet user-secrets set "Epost:SmtpHost" "…" --project src/Dyrepermen.Web` |
| `docker compose` | `infra/.env` (`EPOST_SMTPHOST` og så videre), som `infra/compose.yaml` sender inn |
| Render | Miljøvariabler i dashbordet |

Containeren ser ikke user-secrets. De ligger i hjemmemappen på maskinen, ikke
i imaget. Settes de bare der, svarer skjemaet «ikke satt opp» i Docker, og
etter fem forsøk slår grensen inn med 429.

**Mangler oppsettet, starter appen likevel.** Skjemaet svarer da «ikke satt
opp ennå» og beholder teksten. Å stoppe oppstarten, slik appen gjør når
tilkoblingsstrengen mangler, ville tatt ned produksjon for en funksjon ingen
er avhengig av.

**Fem innsendinger i timen per bruker.** Registreringen er åpen, så uten et
tak kan en enkelt bruker få appen til å sende ubegrenset e-post fra
SMTP-kontoen. Grensen telles per bruker, ikke per IP, og overskrides den, viser
feilsiden «For mange forsøk».

**Menyplassering:** over brukerkortet, ikke blant husstandens sider.
Henvendelsen gjelder brukeren og appen, ikke husstanden. Både beboere og
gjester kan sende. `KontaktController.Send` står derfor på gjestelisten i
`RolleTester`.

## Konsekvens

- Utsendingen testes ikke automatisk. `Appfabrikk` setter SMTP-verten tom, så
  ingen test sender ekte e-post. Den verifiseres manuelt, slik kapittel 17 sier
  om e-postutsending.
- Ingenting logges om innholdet eller adressene, bare bruker-ID og type. Når
  sendingen feiler, logges SMTP-statusen, ikke unntaket, fordi serverens svar
  ofte gjengir adressen.
- Emnet bygges kun av typen, aldri av fritekst, og meldingen sendes som ren
  tekst.
- Fase 4 trenger også e-post. Da flyttes SMTP-delen ut av `KontaktService` til
  en felles avsender. Den er ikke laget på forhånd.
