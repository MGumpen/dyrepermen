# 0005 — `Bruker` blir i Domain, med Identity-basetypen som eneste avhengighet

**Status:** Vedtatt
**Dato:** 2026-08-10
**Gjelder:** plan kapittel 3.4, 4.2, 6.1

## Kontekst

To krav i planen står mot hverandre:

- Kapittel 3.4: «Domain-laget refererer ingenting.»
- Kapittel 4.2: «**Bruker** — utvider `IdentityUser<int>`»

`IdentityUser<TKey>` ligger i pakken `Microsoft.Extensions.Identity.Stores`.
Skal `Bruker` arve fra den, må Domain referere noe.

Å flytte `Bruker` til Infrastructure løser ikke problemet, for kapittel 6.1
krever navigasjonsegenskaper **fra** domeneentiteter **til** `Bruker`:

> `Foring`, `Vekt`, `Dose` og `Handleliste` har en nullbar `Bruker? GittAv` /
> `RegistrertAv` / `OpprettetAv`

Og dashbordspørringen i kapittel 10.3 leser `f.GittAv.Visningsnavn` direkte.
Ligger `Bruker` i Infrastructure, må Domain referere Infrastructure — altså
motsatt vei av avhengighetsretningen. Det er et vesentlig verre brudd.

## Beslutning

`Bruker` blir i `Dyrepermen.Domain`, og Domain får én pakkereferanse:
`Microsoft.Extensions.Identity.Stores`.

Avgjørende for at dette er akseptabelt: pakken drar **ikke** med seg EF Core.
Den avhenger av `Microsoft.Extensions.Identity.Core` og logging-abstraksjonene,
ingenting som binder domenet til en database.

Det er også det den konkrete regelen i CLAUDE.md faktisk verner om:

> «Legger du en EF Core-avhengighet i Domain, er lagdelingen borte.»

EF Core kommer fra `Microsoft.AspNetCore.Identity.EntityFrameworkCore`, som
blir liggende i Infrastructure. Regelen holder.

## Konsekvens

- Navigasjonsegenskapene i kapittel 6.1 og dashbordspørringen i 10.3 fungerer
  som skrevet, uten omskriving.
- Avhengighetsretningen `Web → Application → Domain` og
  `Infrastructure → Application → Domain` er intakt.
- Domain er ikke lenger helt fri for pakker. **Grensen som gjelder videre:**
  ingen EF Core, ingen ASP.NET Core MVC, ingen Npgsql i Domain. Kommer det et
  behov som krever noe av dette, hører koden hjemme i et annet lag.
- Skal domenet en dag være helt rent, er veien å innføre en egen `Person`-type
  i Domain og la `Bruker` være en Identity-spesifikk type i Infrastructure med
  mapping mellom dem. Det koster en mappingklasse og en ekstra tabell-join for
  å vinne ren renhet, og er ikke verdt det for to brukere.
