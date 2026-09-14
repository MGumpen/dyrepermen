# 0007 — Tjenestegrensesnitt i Application, implementasjoner i Infrastructure

**Status:** Vedtatt
**Dato:** 2026-08-10
**Gjelder:** plan kapittel 3.2, 3.4, 6.6

## Kontekst

Tre utsagn i planen kan ikke alle stemme samtidig:

1. Kapittel 3.2 viser `Dyrepermen.Application/Services/` i trestrukturen.
2. Kapittel 3.4: «`Web → Application → Domain` og `Infrastructure → Application
   → Domain`.»
3. Kodeeksemplene i kapittel 8.1, 8.3 og 12.2 viser tjenester med
   `private readonly DyrepermenDbContext _db;`

`DyrepermenDbContext` ligger i `Dyrepermen.Infrastructure` (kapittel 3.2).
En tjeneste i Application kan derfor ikke se den, med mindre Application
refererer Infrastructure — motsatt vei av regelen i punkt 2.

## Beslutning

- **Grensesnittene fra kapittel 6.6 ligger i `Dyrepermen.Application/Interfaces/`.**
- **Implementasjonene som trenger database ligger i `Dyrepermen.Infrastructure/Services/`.**
- **Ren logikk uten database blir i Application.** `FordelPaMaltider`,
  kilo/gram-konvertering og `TomTilNull` hører hjemme der, og enhetstestes
  uten container.

Controllerne injiserer grensesnittene. `Web → Application` dekker det.
`Program.cs` registrerer implementasjonene, og Web refererer Infrastructure
kun der — nøyaktig unntaket kapittel 3.4 selv beskriver.

Alternativet — å definere et `IDyrepermenDb`-grensesnitt i Application som
DbContext-en implementerer — ble forkastet. Det ville krevd at Application
refererer EF Core for å kunne eksponere `DbSet<T>`, og dermed flyttet
avhengigheten i stedet for å fjerne den. Det gir også et abstraksjonslag som
ingen har bruk for, mot en database prosjektet ikke skal bytte.

## Konsekvens

- Avhengighetsretningen i kapittel 3.4 holder uten unntak.
- Application er fri for EF Core. Regelen fra ADR 0005 gjelder også her.
- Controllerne kan enhetstestes mot grensesnittene, som er begrunnelsen
  kapittel 6.6 selv oppgir for å ha dem.
- Splittes grensesnittet senere ut i `Dyrepermen.Api` (kapittel 2.1), deler
  API-et og MVC-appen de samme tjenestene uten endring.
- **Avvik fra trestrukturen i kapittel 3.2:** `Services/` ligger under
  Infrastructure, ikke Application. `Interfaces/` og `Dtos/` ligger som
  beskrevet.
