# 0004 — Pakkeversjoner pinnes, ikke flytende `9.0.*`

**Status:** Vedtatt
**Dato:** 2026-08-10
**Gjelder:** plan kapittel 3.3

## Kontekst

Planens `Directory.Packages.props` bruker flytende versjoner:

```xml
<PackageVersion Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="9.0.*" />
```

Planen begrunner selv hvorfor sentral pakkestyring er «den viktigste
enkeltfilen i et .NET-monorepo»: uten den ender prosjektene med ulike
EF Core-versjoner, og feilen viser seg som uforståelige kjøretidsfeil i
migrasjoner.

Flytende versjoner undergraver nettopp det. `9.0.*` løses på nytt ved hver
`restore`, så CI kan bygge mot en annen versjon enn utviklermaskinen, uten
at noe i repoet endret seg. En EF Core-oppdatering kan endre hvordan
migrasjoner genereres, og da er byggeloggen eneste spor.

## Beslutning

Alle versjoner pinnes eksakt. Oppgradering er en egen, synlig commit.

## Konsekvens

- Samme versjoner lokalt, i CI og i containeren. Bygget er reproduserbart.
- Sikkerhetsoppdateringer kommer ikke av seg selv. De må hentes bevisst —
  `dotnet list package --outdated` viser hva som ligger bak.
- Låser vi oss til noe med en kjent feil, er det synlig i én fil.

## Oppgraderingsrutine

```bash
dotnet list package --outdated
# endre Directory.Packages.props, deretter:
dotnet restore && dotnet build && dotnet test
```

Én commit per oppgradering, slik at en regresjon kan reverteres alene.
