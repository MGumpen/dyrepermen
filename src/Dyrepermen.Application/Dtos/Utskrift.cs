namespace Dyrepermen.Application.Dtos;

/// <summary>
/// Alt om ett dyr, samlet for utskrift.
///
/// Veterinaerer, handleliste og forfallsvarsler er utelatt med vilje. Et
/// utskrevet ark er et oyeblikksbilde man tar med seg - "hva forfaller de
/// neste 14 dagene" er utdatert dagen etter, og handlelisten hoerer hjemme i
/// butikken, ikke i permen.
/// </summary>
public sealed record DyrUtskrift(
    DyrDetaljer Dyr,
    IReadOnlyList<VektRad> Vekter,

    /// <summary>Null ved faerre enn to malinger - da finnes det ingen graf.</summary>
    Vektgrafdata? Graf,

    IReadOnlyList<BehandlingRad> Behandlinger,
    IReadOnlyList<MedisinRad> Medisiner,

    /// <summary>Kun den aktive planen. Historiske planer hoerer ikke hjemme her.</summary>
    ForplanRad? Forplan,

    IReadOnlyList<ForsikringRad> Forsikringer,
    IReadOnlyList<InformasjonRad> Notater);

/// <summary>
/// Utvalget av husstanden, klar for utskrift. Listene for deler som ikke er
/// valgt, er tomme; visningen sporr <see cref="Valg"/> om hva som skal vises,
/// slik at "ingen vekt registrert" ikke star pa et ark der vekt ikke var valgt.
/// </summary>
public sealed record Utskrift(
    IReadOnlyList<DyrUtskrift> Dyr,

    /// <summary>Notater som ikke horer til et bestemt dyr.</summary>
    IReadOnlyList<InformasjonRad> FellesNotater,

    Utskriftsvalg Valg);
