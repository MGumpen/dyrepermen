# 0009 — Flere husstander per bruker, med roller

**Status:** Vedtatt
**Dato:** 2026-08-11
**Gjelder:** plan kapittel 4.2, 5.2, 7, 12.3, 12.5, 12.6

## Kontekst

Modellen hadde én kolonne, `asp_net_users.husstand_id`. En bruker kunne
tilhøre nøyaktig én husstand.

Marius beskrev et tilfelle den ikke dekker: han passer farens hund ofte, men
de bor ikke sammen. Han trenger tilgang til opplysningene om den hunden —
uten å forlate sin egen husstand, og uten at faren mister sin.

Enkeltkolonnen gjorde det umulig, og den var dessuten kilden til prosjektets
skarpeste sikkerhetsproblem. Kapittel 12.3 beskriver det slik:

> uten den kan hvem som helst taste inn e-postadressen til en fremmed bruker
> og flytte dem ut av deres egen husstand

Den faren fantes **fordi** tilknytningen var en enkeltverdi. Å sette den
flyttet personen; det la ikke til noe.

## Beslutning

Ny tabell `husstandsmedlemskap`, og `asp_net_users.husstand_id` er fjernet.

```sql
CREATE TABLE husstandsmedlemskap (
    id             INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    husstand_id    INT     NOT NULL REFERENCES husstand(id) ON DELETE CASCADE,
    bruker_id      INT     NOT NULL REFERENCES asp_net_users(id) ON DELETE CASCADE,
    rolle          CHAR(1) NOT NULL,
    opprettet_dato DATE    NOT NULL DEFAULT CURRENT_DATE,
    CONSTRAINT ck_medlemskap_rolle CHECK (rolle IN ('E','G'))
);
CREATE UNIQUE INDEX ux_medlemskap_husstand_bruker
    ON husstandsmedlemskap(husstand_id, bruker_id);
```

**Ingen query-filter på denne tabellen.** Den er selve tenant-koblingen — et
filter her ville gjort det umulig å finne ut hvilke husstander du er med i,
som er nettopp det man trenger for å bytte mellom dem. Tilgangen håndheves
eksplisitt i tjenestene. Den implementerer derfor ikke `IHusstandsbundet`, og
filterprøven dekker den ikke.

### To roller

| Rolle | Kan |
|---|---|
| `Beboer` (B) | Alt: endre dyr, medlemmer, innstillinger, forsikring, notater |
| `Gjest` (G) | Se alt, og logge det daglige: vekt, fôring, medisindoser, handleliste |

Rollen het opprinnelig `Eier`, lagret som `'E'`. Marius formulerte skillet som
«bor der eller er gjest», og det treffer bedre: en husstand har ikke en eier,
den har noen som bor der. Omdøpt til `Beboer`/`'B'` i migrasjonen
`RolleBeboer`. I grensesnittet står det «Bor her» og «Gjest».

Gjesten kan skrive, ikke bare lese. Passer du hunden, må du kunne notere at
du ga mat og medisin — ellers får loggen hull nettopp de dagene noen andre
hadde ansvaret, og informasjonen må uansett formidles muntlig etterpå.

Rollen ligger på **medlemskapet**, ikke på brukeren. Du er eier hjemme og
gjest hos faren din.

### Aktiv husstand

Valget ligger i informasjonskapselen `dyrepermen_husstand`, og **valideres mot
medlemskapene ved hver eneste forespørsel** i `HusstandMiddleware`. En
redigert kapsel gir ingen tilgang — finnes ikke medlemskapet, faller den
tilbake på den første. Hele tenant-isolasjonen henger i den valideringen.

Kapselen er et *valg*, ikke en *rettighet*. Det skillet er verdt å holde fast
ved om noen senere vil legge mer i den.

### Håndheving

`[KreverEier]` er et autorisasjonsfilter, ikke en policy — rollen avhenger av
hvilken husstand du ser på, og er derfor ikke en claim.

Attributtet beskytter bare det noen husker å merke. Derfor finnes
`RolleTester`, som går gjennom **alle** POST-handlinger i alle controllere og
feiler hvis en mangler enten attributtet eller en plass på en eksplisitt
gjesteliste. Samme prinsipp som filterprøven i kapittel 17.3: du kan ikke
legge til en skrivehandling og glemme tilgangskontrollen — testen tvinger deg
til å ta stilling.

En andre test sjekker at gjestelisten bare viser til handlinger som faktisk
finnes, slik at et navn ikke blir stående igjen etter en omdøping.

## Konsekvens

- **Sikkerhetssjekken i kapittel 12.3 er fjernet, ikke glemt.** Å legge noen
  til i din husstand tar ingenting fra dem andre steder. `LeggTilResultat`
  har derfor ikke lenger `TilhorerAnnenHusstand`. Testen
  `Aa_legge_noen_til_tar_ikke_medlemskapet_deres_andre_steder` verifiserer at
  fjerningen var trygg.
- **Kapittel 12.6 er utdatert.** Den sier at alle medlemmer er likestilte og
  at det er en bevisst begrensning. Nå finnes roller.
- **En husstand må ha minst én beboer.** Den siste kan verken fjernes
  eller degraderes — uten beboer ville innstillingene vært låst for alle.
- **Kontosletting** rammer nå de husstandene der brukeren er eneste medlem,
  ikke «husstanden hennes». Er hun bare gjest hos noen, røres ikke den.
- **Migrasjonens rekkefølge var ikke valgfri.** EF genererte `DropColumn` før
  `CreateTable`, altså før tabellen som skulle overta dataene fantes. Det
  ville slettet hver eneste eksisterende tilknytning. Migrasjonen er skrevet
  om: opprett tabell, flytt data, slipp kolonne. Verifisert mot databasen at
  begge eksisterende brukere beholdt husstanden sin.
- Alle eksisterende tilknytninger ble migrert som beboer. De var eneste
  medlem, og noen må kunne endre innstillingene.
- **Invitasjonens rolle har ingen `HasDefaultValue`.** `Beboer` er CLR-standard
  for enumen, så EF ville utelatt kolonnen fra `INSERT` når rollen var
  `Beboer` — og databasens standard `'G'` ville slått inn. En invitert beboer
  ville blitt stille lagret som gjest. EF advarte om det, og advarselen var
  reell.
