# 0017 — «Avslutt» på medisin lagres som et tidspunkt

**Status:** Vedtatt
**Dato:** 2026-09-27
**Gjelder:** plan kapittel 4 og 5 — tabellen `medisin`

## Kontekst

«Avslutt» satte `slutt_dato` til dagens dato. Men sluttdatoen er *til og med*: en
kur «til 30. september» skal gis hele den dagen. En medisin som ble avsluttet, var
derfor aktiv resten av dagen — med «Gi dose», neste dose og en rad i «Forfaller
snart» — og forsvant først ved midnatt.

To løsninger ble vurdert:

1. La «Avslutt» sette sluttdatoen til i går. Ingen skjemaendring, men perioden
   ville vist at kuren sluttet i går, også når det ble gitt en dose i dag. Startet
   medisinen i dag, ville sluttdatoen blitt liggende før startdatoen.
2. En egen kolonne for når medisinen ble avsluttet.

## Beslutning

Alternativ 2, valgt av Marius.

- Ny nullbar kolonne `medisin.avsluttet_tid` (`timestamptz`). Satt betyr avsluttet
  med en gang, uansett sluttdato.
- «Avslutt» setter tidspunktet, og setter `slutt_dato` til i dag når den mangler
  eller ligger lenger fram. Perioden viser da når kuren faktisk sluttet. En
  sluttdato som allerede har passert, beholdes.
- **Aktiv** betyr: ikke avsluttet, og sluttdatoen har ikke passert. Regelen står i
  `Medisinfilter.Aktiv` for spørringene og i `MedisinRad.ErAvsluttet` for rader i
  minnet. `MedisinredigeringTester.Regelen_i_sporringen_og_i_minnet_er_den_samme`
  feiler når de to spriker.
- `LoggDose` avviser en avsluttet medisin. Knappen er skjult, men en fane som har
  stått åpen, kan fortsatt poste.

Eksisterende rader får ingen verdi i migrasjonen. En rad kan ikke vite om den ble
avsluttet med knappen eller fikk en planlagt sluttdato, og for rader med sluttdato
i fortiden gir begge samme svar.

## Konsekvenser

- En medisin som ble avsluttet med den gamle knappen i dag, er aktiv til midnatt.
  Trykk «Avslutt» på nytt for å avslutte den med en gang.
- Rediger endrer ikke `avsluttet_tid`. En avsluttet medisin kan rettes, men ikke
  gjenopptas. Det finnes ingen knapp for det, og det er ikke bedt om.
