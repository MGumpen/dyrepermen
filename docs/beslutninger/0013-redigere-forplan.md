# 0013 — Fôrplanen kan redigeres, ikke bare erstattes

**Status:** Vedtatt
**Dato:** 2026-09-06
**Gjelder:** plan kapittel 8.1, [ADR 0012](0012-fortabell-etter-alder.md)

## Kontekst

Plan kapittel 9 lister `Rediger` på både `ForplanController` og
`BehandlingController`. Ingen av dem var bygd. Denne ADR-en beskriver ikke et
brudd med planen, men hvordan handlingen den allerede ber om er utformet — og
hvorfor den står ved siden av «lagre som ny» i stedet for å erstatte den.

Fôrplanen var uforanderlig. Den eneste måten å endre noe på var å lagre en
ny plan; den gamle ble stående med `aktiv = false` som revisjonsspor.

Det er riktig når regelen faktisk skifter — fra tørrfôr til råfôr, eller fra
en mengde veterinæren satte til en ny. Da er den gamle planen et faktum om
fortiden, og den skal bevares.

Men det er feil for en skrivefeil, og det ble direkte upraktisk med
overgangsplanen: andelen råfôr flyttes for hånd omtrent hver uke gjennom
overgangen. Ti justeringer ga ti «planer», og historikken sa mest om hvor
mange ganger noen hadde tastet et nytt tall.

## Beslutning

**To handlinger på samme skjema.**

| Knapp | Rute | Gjør |
|---|---|---|
| Lagre endringer | `POST /dyr/{id}/forplan/rediger` | Endrer den aktive raden |
| Lagre som ny plan | `POST /dyr/{id}/forplan` | Legger den gamle bort, oppretter ny |

Skjemaet posterer til `rediger` når det finnes en aktiv plan, og til `ny` når
det ikke gjør det. Den andre knappen bruker `formaction` — samme skjema, annen
adresse. Det gir to handlinger uten et skjema til med de samme atten feltene,
og uten javascript.

Skillet er brukerens, ikke appens: den som endrer vet om dette er en retting
eller en ny regel. Appen kan ikke gjette det fra at et tall gikk fra 400 til
450.

### `endret_dato`

Ny nullbar kolonne, satt ved hver redigering. Uten den ville «Planen ble lagt
inn 6. sep» blitt stående over et innhold fra en helt annen dag — en dato som
ser troverdig ut og er feil.

Opprettelsesdatoen røres ikke. Erstattes planen i stedet, står den riktige
datoen igjen på den gamle raden, som før.

### Tørrfôrtabellen erstattes i sin helhet

Å finne ut hvilke rader som er endret, lagt til og fjernet ville vært tre
spesialtilfeller der ett holder. Slettingen lagres i et eget
`SaveChanges` før innsettingen, inne i samme transaksjon: skjer begge i én,
bestemmer EF rekkefølgen, og en ny rad på samme måned som en gammel bryter da
`ux_forplantrinn_alder`. Det er nettopp den raden man endrer når en mengde
skal justeres.

## Konsekvens

- Historikken sier igjen noe. En rad per gang regelen skiftet, ikke en rad per
  gang noen tastet.
- **En redigering er ikke sporbar.** Den gamle verdien er borte. Det er
  prisen, og den er tatt bevisst: alternativet var en revisjonstabell for et
  problem ingen har hatt.
- `Forplaninnhold` (het `NyForplan`) brukes av begge handlingene. Feltene er
  de samme, og to nesten like typer ville drevet fra hverandre.
- Begge skrivehandlingene håndhever nå `forplan_aktiv`. Det gjorde ingen av
  dem før — bryteren ble bare sjekket når skjemaet ble tegnet, så en gammel
  faneside kunne skrive til en avslått funksjon. Se plan kapittel 8.2.
- Bytter man metode i en redigering, nulles feltene fra den gamle metoden ut,
  akkurat som ved opprettelse. Ellers blir det liggende igjen en andel på en
  plan som ikke blander noe, og `ck_forplan_verdi` slår inn som en
  `DbUpdateException`.

## Vurdert og forkastet

**Bare redigering.** Å fjerne «lagre som ny plan» og alltid endre i stedet.
Det ville gjort `aktiv`-kolonnen og hele revisjonssporet meningsløst, og
dataeksporten ville mistet historikken om hva dyret faktisk har spist.

**Bare erstatning, med en tydeligere melding om at det er greit.** Det var
tilstanden før. Problemet er ikke at brukeren misforstår, men at ti rader for
ti småjusteringer er dårlig data.

**En egen revisjonstabell for endringer.** Full sporbarhet på hver redigering.
Et helt lagringsapparat for et spørsmål ingen har stilt.
