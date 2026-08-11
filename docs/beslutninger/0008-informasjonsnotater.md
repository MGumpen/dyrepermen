# 0008 — Ny tabell `informasjon` for fritekstnotater

**Status:** Vedtatt
**Dato:** 2026-08-10
**Gjelder:** plan kapittel 5 (skjemaet), kapittel 9 (ruter)

## Kontekst

Planen har ingen plass til fri tekst om et dyr. `behandling.notat` og
`forplan.notat` henger på hver sin hendelse, og `vetbesok.diagnose` er
knyttet til ett besøk. Det finnes ingenting for kunnskap som gjelder
vedvarende:

- «Spiser ikke før klokka 07»
- «Er redd for torden — skal ha teppet i gangen»
- «Vet: Dyreklinikken Arendal, 37 00 00 00»

Dette er informasjonen som ellers bor i hodet til den ene av de to voksne,
og som den andre trenger nettopp når hun står der alene. Det er samme
begrunnelse som ligger bak fôringsloggen i kapittel 8.2: poenget med
applikasjonen er at to personer skal kunne dele ansvaret.

Marius ba om funksjonen etter å ha sett tilsvarende i en annen app, og
valgte «notater i tillegg til en oversiktsside».

**Dette er et bevisst tillegg til skjemaet i kapittel 5**, ikke en
tolkning av det. Kapittel 5 kaller seg selv fasiten, så avviket dokumenteres
her.

## Beslutning

```sql
CREATE TABLE informasjon (
    id                     INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    husstand_id            INT           NOT NULL REFERENCES husstand(id),
    dyr_id                 INT           REFERENCES dyr(id) ON DELETE CASCADE,
    tittel                 VARCHAR(80)   NOT NULL,
    tekst                  VARCHAR(2000) NOT NULL,
    opprettet_av_bruker_id INT,
    opprettet_dato         DATE          NOT NULL DEFAULT CURRENT_DATE
);
CREATE INDEX ix_informasjon_husstand ON informasjon(husstand_id);
```

Følger mønstrene som allerede gjelder:

- Implementerer `IHusstandsbundet` og har query-filter. Filterprøven fanger
  det om noen fjerner filteret senere.
- `dyr_id` er nullbar. Uten dyr er notatet husstandens felles, og vises som
  «Felles» — samme mønster som `handleliste`.
- **`ON DELETE CASCADE` fra `dyr`**, i motsetning til handlelisten som har
  `RESTRICT`. Et notat om Luna har ingen mening uten Luna. Et handlelistepunkt
  har det — «tørrfôr» skal fortsatt kjøpes.
- Ingen cascade fra `husstand`.
- `opprettet_av_bruker_id` med `ON DELETE SET NULL`, som alle andre
  `*_av_bruker_id`-kolonner.

Ny rute `/informasjon`, i tråd med navnekonvensjonen i kapittel 9.

## Konsekvens

- Migrasjonen `LeggTilInformasjon` legger til tabellen. Skjemaet i
  kapittel 5 er ikke lenger komplett — **les denne ADR-en sammen med det.**
- Dataeksporten i kapittel 12.5 må ta med notatene, ellers er den ikke
  lenger «alle data».
- `tekst` er begrenset til 2000 tegn. Det er et notatfelt, ikke et
  dokumentarkiv — filopplasting kommer i fase 5.
- Ingen versjonering av notater. Blir revisjonsspor et behov, er det en ny
  beslutning.
