# 0001 — Husstandskontekst uten sirkulær avhengighet

**Status:** Vedtatt
**Dato:** 2026-08-10
**Gjelder:** plan kapittel 7.1, 7.2, 12.1 og 12.3.1

## Kontekst

Planen beskriver to typer som krever hverandre i konstruktøren:

- `DyrepermenDbContext(DbContextOptions, IHusstandContext)` — kapittel 7.1
- `DbHusstandContext(IHttpContextAccessor, DyrepermenDbContext)` — kapittel 7.2

DI-containeren løser ikke dette. Første forespørsel kaster
«A circular dependency was detected for the service of type `IHusstandContext`».
Feilen rammer alt, siden hvert query-filter leser `IHusstandContext`.

I tillegg leser `KreverHusstandMiddleware` i kapittel 12.1 en claim:

```csharp
ctx.User.FindFirst("husstand_id") is null
```

Kapittel 12.3.1 slår eksplisitt fast at `husstand_id` **ikke** skal leses fra
claim, fordi den blir foreldet når et medlem legges til av noen andre. Med
middlewaren som den står, blir en person som er lagt til i en husstand sendt
til `/husstand/oppsett` i opptil 30 dager — akkurat den feilen 12.3.1 er
skrevet for å hindre, gjeninnført ett kapittel tidligere.

## Beslutning

`IHusstandContext` implementeres som et scoped objekt **uten avhengigheter**:

```csharp
public sealed class Husstandskontekst : IHusstandContext
{
    public int HusstandId { get; set; }   // 0 = ikke satt, gir tomt resultatsett
}
```

Én middleware, plassert etter `UseAuthentication` og før `UseAuthorization`,
gjør oppslaget mot databasen og fyller verdien. Samme middleware håndterer
omdirigeringen til `/husstand/oppsett`, siden den allerede har svaret.

Alternativene ble forkastet:

- **Eget oppslags-DbContext** gir to EF-modeller mot de samme Identity-tabellene,
  der kun én kan eie migrasjonene. Mer bevegelige deler enn problemet krever.
- **Lat oppslag via `IServiceProvider`** fungerer, men er service locator, som
  CLAUDE.md forbyr eksplisitt i kodekonvensjonene.

## Konsekvens

- Sykelen finnes ikke lenger, fordi holderen ikke har avhengigheter.
- Husstanden leses fra database én gang per forespørsel, som planen forutsetter.
  Ett indeksert primærnøkkeloppslag.
- Claim-lesingen i 12.1 er borte. 12.3.1 er dermed oppfylt begge steder.
- **Fail closed:** kjører middlewaren ikke, står `HusstandId` på 0, og alle
  query-filtre gir tomt resultatsett. Ingen lekkasje ved feilkonfigurasjon.
- Tester setter holderen direkte. `FastHusstandContext` fra kapittel 17.2 blir
  den samme typen, uten en egen testdobbel.
- **Vedlikehold:** middlewaren må kjøre før noe leser `IHusstandContext`.
  Rekkefølgen i `Program.cs` er derfor ikke kosmetisk, og er kommentert der.
