using Dyrepermen.Domain.Enums;

namespace Dyrepermen.Application.Extensions;

/// <summary>
/// Norske etiketter for behandlingstypene.
///
/// Etikettene ligger ikke som Display-attributter pa enumen - domenelaget
/// skal ikke baere presentasjonsmetadata. Men de skal heller ikke ligge i
/// fire kopier, slik de gjorde: i DyrService, i DashbordService, i
/// behandlingsvisningen og i utskriften.
///
/// Alle fire hadde "Behandling" som reserve for ukjent verdi. En ny type ville
/// derfor vist riktig navn de stedene noen husket a oppdatere, og "Behandling"
/// de andre - uten en eneste feilmelding.
/// </summary>
public static class Behandlingsformat
{
    public static string Navn(BehandlingType type) => type switch
    {
        BehandlingType.Vaksine => "Vaksine",
        BehandlingType.Ormekur => "Ormekur",
        BehandlingType.Flatt => "Flåttmiddel",
        BehandlingType.Kloklipp => "Kloklipp",
        BehandlingType.Tannrens => "Tannrens",
        BehandlingType.Annet => "Annet",
        _ => "Behandling"
    };

    /// <summary>
    /// Raden slik den vises i historikk og pa dashbordet:
    /// "Ormekur – Milbemax", eller bare "Ormekur" nar preparat mangler.
    /// </summary>
    public static string MedPreparat(BehandlingType type, string? preparat)
        => preparat is null ? Navn(type) : $"{Navn(type)} – {preparat}";
}
