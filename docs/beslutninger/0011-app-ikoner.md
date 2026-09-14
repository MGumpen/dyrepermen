# 0011 — App-ikoner for hjem-skjerm

**Status:** Vedtatt
**Dato:** 2026-08-12
**Gjelder:** plan kapittel 10.2

## Kontekst

Legger man appen til på hjem-skjermen på iPhone, kom det ingen ikon — bare en
bokstav på grå bakgrunn.

Årsaken er at `<link rel="icon">` pekte på `logo.svg`, og **iOS støtter ikke
SVG som `apple-touch-icon`**. Den finner ingen brukbar PNG, og faller tilbake
på et skjermbilde eller en bokstav fra sidetittelen.

## Beslutning

**PNG-ikoner i tillegg til SVG-faviconet.** SVG i fanen, PNG på hjem-skjermen.

`app-ikon.svg` er kilden, og er samme pote som `logo.svg` med én forskjell:
grønnfargen fyller hele flaten i stedet for å ligge i en sirkel.

Grunnen er at iOS og Android legger sin *egen* maske over ikonet — en avrundet
firkant. Da gir de to alternativene:

| Kilde | Resultat på hjem-skjermen |
|---|---|
| Sirkel med gjennomsiktige hjørner | svarte hjørner, eller en liten logo med mye luft |
| Full flate | avrundet firkant i merkefargen, med potet like stor som i fanen |

Full flate er derfor det som faktisk ser ut som faviconet etter maskering,
selv om fila ser annerledes ut.

Ikonene er ugjennomsiktige. En gjennomsiktig piksel komponeres mot **svart**
på iOS, ikke mot hvitt.

`site.webmanifest` gir Android samme ikon, og `display: standalone` gjør at
appen åpnes uten nettleserramme når den startes fra hjem-skjermen.

## Slik lages PNG-ene på nytt

Ingen ekstra verktøy — begge følger med macOS:

```bash
cd src/Dyrepermen.Web/wwwroot
qlmanage -t -s 512 -o /tmp/ikon img/app-ikon.svg
sips -z 180 180 /tmp/ikon/app-ikon.svg.png -o apple-touch-icon.png
sips -z 192 192 /tmp/ikon/app-ikon.svg.png -o img/ikon-192.png
sips -z 512 512 /tmp/ikon/app-ikon.svg.png -o img/ikon-512.png
```

Kontroller resultatet etterpå. `qlmanage` feiler ikke med feilkode når
SVG-en er ugyldig — den *rendrer feilmeldingen som bilde*, og du sitter igjen
med en PNG som ser ut som en hvit nettside. Åpne fila og se på den.

## Konsekvens

- `apple-touch-icon.png` ligger i rota av `wwwroot`, ikke bare i `img/`. Det
  er der iOS leter når lenken mangler, for eksempel fra en bokmerket
  underside.
- Endres merkefargen, må både `app-ikon.svg`, `theme_color` i manifestet og
  `theme-color` i `_Layout.cshtml` endres. Tre steder, fordi ingen av dem kan
  lese en CSS-variabel.

## Vurdert og forkastet

**Bare øke størrelsen på SVG-faviconet.** Virker ikke: begrensningen er
formatet, ikke oppløsningen.

**Generere PNG i byggesteget.** Ville krevd en rasteriserer i Dockerfilen for
et ikon som endres omtrent aldri. Filene sjekkes inn i stedet, med oppskriften
over.
