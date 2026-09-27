using Dyrepermen.Application.Dtos;

namespace Dyrepermen.Application.Extensions;

/// <summary>
/// Navnene pa delene av utskriften, slik de star i valgskjemaet. Samme
/// grunn som Behandlingsformat: etiketten skal sta ett sted.
/// </summary>
public static class Utskriftsformat
{
    /// <summary>Delene i den rekkefolgen de star pa arket.</summary>
    public static IReadOnlyList<Utskriftsdel> Deler { get; } =
        Enum.GetValues<Utskriftsdel>()
            .Where(d => d is not (Utskriftsdel.Ingen or Utskriftsdel.Alle))
            .ToList();

    public static string Navn(Utskriftsdel del) => del switch
    {
        Utskriftsdel.OmDyret => "Om dyret",
        Utskriftsdel.Forplan => "Fôrplan",
        Utskriftsdel.Vekt => "Vekt med graf",
        Utskriftsdel.Behandlinger => "Behandlinger",
        Utskriftsdel.Medisiner => "Medisiner",
        Utskriftsdel.Forsikring => "Forsikring",
        Utskriftsdel.Notater => "Notater om dyret",
        Utskriftsdel.FellesNotater => "Felles notater",
        _ => del.ToString()
    };
}
