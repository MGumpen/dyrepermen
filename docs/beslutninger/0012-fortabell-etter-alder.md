# 0012 — Fôrtabell etter alder, med valgfri innblanding

**Status:** Vedtatt · **Utvidet 2026-09-06** (se «Tillegg» nederst)
**Dato:** 2026-09-06
**Gjelder:** plan kapittel 8.1, CLAUDE.md «Datalag»

## Kontekst

Fôrplanen hadde to metoder: prosent av kroppsvekt, som følger siste
vektregistrering, og fast mengde i gram. De dekker et dyr som spiser én ting.

En valp som trappes fra råfôr til tørrfôr spiser to. Råfôret måles fortsatt
som prosent av vekten. Tørrfôret måles i gram etter **alder**, ikke vekt —
det er slik fôrposene er merket. Og forholdet mellom dem flyttes gradvis over
noen uker.

Med bare de to gamle metodene måtte brukeren regne selv: slå opp alderen i
tabellen, trekke fra, gange med andelen, og gjøre det på nytt hver uke.
Nøyaktig den regningen appen finnes for å slippe.

Prinsippet i kapittel 8.1 står fast: **appen anbefaler ikke fôrmengde.** Alle
tallene her kommer fra brukeren — prosentsatsen, hele tørrfôrtabellen og
andelen. Appen slår opp og ganger. Den har ingen innebygde faktorer, ingen
kurve og ingen mening om hva som er riktig for dyret.

## Beslutning

**En tredje metode, `Formetode.Blanding`, lagret som `'B'`.**

Den bruker `prosent_tidels` som råfôrregel, en ny kolonne
`rafor_andel_prosent` som blandingsforhold, og en ny tabell `forplantrinn`
med alder i måneder mot gram per dag.

### Andelen skalerer hver fôrtype for seg

70 % råfôr betyr 70 % av det dyret ville fått på ren råfôring, **pluss** 30 %
av det tørrfôrposen oppgir for alderen.

Det er ikke det samme som å dele én dagsmengde 70/30. Råfôr og tørrfôr er
ikke sammenlignbare gram for gram — tørrfôr er tørket ned til om lag en
tredel av vekten sin. En felles dagsmengde delt på andelene ville gitt et dyr
som gikk ned i vekt gjennom overgangen, med et tall som så helt rimelig ut.

```
Råfôr:   8,20 kg × 5 %  = 410 g,  70 % av det = 287 g
Tørrfôr: tabellen ved 18 uker = 180 g,  30 % av det =  54 g
Sum                                                   341 g
```

Summen, 341 g, er altså **ikke** 410 g. Det er tilsiktet.

### Tørrfôrtabellen trappes jevnt, men bare ukentlig

Mellom to rader interpoleres det lineært. Utenfor tabellen ekstrapoleres det
ikke: under første rad gjelder første rad, over siste gjelder siste.

Alderen rundes ned til hele uker før oppslaget. Regnet per dag ville mengden
endret seg med noen gram hver morgen, og en tabell som aldri står stille er
umulig å måle etter. Med uker står tallet i sju dager og tar ett hopp.

Ukesrytmen gjør også dagsgrensen viktig: alderen regnes mot norsk dato
(`Tidssone.Idag`), ikke UTC. Ellers ville dashbordet og fôrplansiden vist
ulik mengde mellom midnatt og klokka to.

### Andelen flyttes for hånd

Ingen automatisk nedtrapping med start, slutt og varighet. Brukeren setter
ett tall og endrer det når magen tåler mer tørrfôr — som er når beslutningen
faktisk tas. En innebygd tidsplan ville måttet erstattes hver gang overgangen
måtte bremses, altså nettopp når den var i veien.

Til gjengjeld fylles skjemaet forhåndsutfylt fra den aktive planen. Å flytte
andelen fra 70 til 60 er ett tastetrykk, ikke en ny innskriving av hele
tabellen.

### Regnestykket samlet ett sted

Regelen lå i fire kopier — `ForplanService`, `DashbordService`, `DyrService`
og `Forplanformat.Sammendrag`. De var like, men bare fordi ingen hadde endret
noen av dem ennå. En tredje metode ville gjort fire kopier til fire
divergerende kopier.

Alt regnestykket ligger nå i `Application/Extensions/Forberegning`, som er
ren logikk uten databasetilgang. Tjenestene henter grunnlaget og spør den.

### Bryteren

Knappen «Avansert» på fôrplansiden setter metodevelgeren, den lagrer ingen
tilstand. Uten javascript står velgeren synlig og gjør samme jobb. Dette er
ikke en funksjonsbryter som `forplan_aktiv` — det er samme skjema med flere
felter, og en bryter til per dyr ville vært et valg brukeren måtte ta før hun
visste hva det gjaldt.

## Konsekvens

- Fem tilstander i grensesnittet i stedet for fire. Den nye er «mangler
  fødselsdato», som følger samme regel som «mangler vekt»: si fra, ikke vis
  0 gram.
- Grunnlaget kreves bare for den fôrtypen som er i bruk. 100 % råfôr trenger
  ingen fødselsdato, 0 % råfôr trenger ingen vekt — så planen kan legges inn
  før overgangen begynner, og stå igjen etter at den er ferdig.
- Måltidene fordeles per fôrtype, ikke bare som sum. Summen per måltid kan
  derfor avvike ett gram fra dagsmengden delt på antall måltider, siden hver
  fôrtype avrundes for seg. Summen over dagen er eksakt for begge.
- `forplantrinn` er husstandsbundet gjennom to ledd
  (`t.Forplan.Dyr.HusstandId`) og er med i isolasjonstesten.
- Tabellen hører til planen, ikke til dyret. Erstattes planen, følger en ny
  tabell med, og den gamle blir stående som revisjonsspor.
- `rafor_andel_prosent` lagres i **hele** prosent, ikke tidels som
  `prosent_tidels`. Andelen justeres for hånd gjennom noen uker, og et
  desimaltegn i det feltet er presisjon ingen har bruk for. Avviket fra
  regelen i CLAUDE.md er bevisst og begrenset til denne ene kolonnen.
- Skjemaet har åtte faste rader i tørrfôrtabellen. En fôrpose har fire–fem
  trinn, og et skjema som virker uten javascript er verdt mer enn radene
  ingen fyller ut.

## Vurdert og forkastet

**Energibasert beregning (RER/MER).** `70 × kg^0,75` ganget med en
livsfasefaktor, delt på fôrets kcal per 100 g. Den er standarden i
faglitteraturen, men krever at appen har en mening om aktivitetsfaktoren for
å være nyttig — og da anbefaler den fôrmengde. Det bryter med kapittel 8.1.
Brukeren kan fortsatt gjøre den regningen selv og legge inn svaret som fast
mengde.

**Trinn i stedet for jevn opptrapping.** Fôrposene er skrevet som trinn
(«3–4 mnd: 160 g»). Men da endrer mengden seg med 20 gram på én natt fire
ganger i året, og står bom stille imellom. Jevn opptrapping med ukentlig hopp
gir samme sum over tid og en overgang som følger dyret.

**Automatisk nedtrapping av andelen.** Se over — den måtte erstattes nettopp
når den var i veien.

**En egen funksjonsbryter per dyr.** `forplan_aktiv` finnes allerede, og en
bryter til ville tvunget brukeren til å velge før hun visste hva valget
gjaldt.

---

## Tillegg — metoden er en tabellmetode, ikke en overgangsmetode

Første utgave het `Blanding` og krevde alltid en råfôrregel: andelen skalerte
et råfôr mot et tørrfôr. Det var for smalt på to måter.

**Tabellen er nyttig alene.** Den vanligste bruken er ikke en overgang, men
en fôrpose med en alderstabell: skriv den av, og mengden følger valpen
oppover uten at noen justerer noe. Det er nå metodens hovedbruk, og
innblandingen er tilvalget.

**Retningen er ikke gitt.** En overgang går like gjerne fra tørrfôr til råfôr
som motsatt, og kan gå mellom to tørrfôr der bare det ene har en
alderstabell. Å bake «råfôr» og «tørrfôr» inn i modellen låste en retning
virkeligheten ikke har.

### Hva som endret seg

| Før | Nå |
|---|---|
| `Formetode.Blanding`, lagret `'B'` | `Formetode.Tabell`, lagret `'T'` |
| `rafor_andel_prosent` | `vektdel_andel_prosent` |
| `fornavn_torr` | `fornavn_alder` |
| Krevde alltid `prosent_tidels` | Krever den bare når andelen er over 0 |

Delene heter nå det de **er**: en *vektdel* måles i prosent av kroppsvekten,
en *aldersdel* slås opp i tabellen. Hva fôret er, sier modellen ingenting om
— det står i navnefeltene, som brukerens egne ord.

Andelen 0 og 100 er ikke spesialtilfeller å beklage. De er de to endepunktene
i en overgang: 0 er ren tabell, 100 er ren vekt, og veien mellom dem går
begge veier.

### Utenfor tabellen

Oppslaget sier nå fra når dyret er yngre enn første rad eller eldre enn
siste. Mengden holdes på nærmeste rad — det er riktig oppførsel — men et
flatt tall som *ser ut som* en beregning er verre enn et tall med en merknad.
Siden ber om en rad til i stedet.

### Mindre tekst

Kortet viste regelen, begge regnestykkene, ukesrytmen, hele tabellen og en
forklaring på måltidsfordelingen — alt samtidig. Det er riktig informasjon og
feil mengde av den.

Nå står tallet, måltidene og **én** linje om hvor tallet kommer fra.
Regnestykket og tabellen ligger bak «Vis regnestykket». Grunnlaget er
fortsatt synlig, slik kapittel 8.1 krever — det fyller bare ikke kortet for
den som ikke spurte.
