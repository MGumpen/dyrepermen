namespace Dyrepermen.Application.Dtos;

/// <summary>
/// Hva som skal med pa utskriften.
///
/// <see cref="DyrIder"/> null betyr alle dyrene i husstanden. En tom liste
/// betyr ingen dyr - da blir bare fellesnotatene med, om de er valgt. Ider
/// fra en annen husstand filtreres bort av query-filteret, og gir ingenting.
/// </summary>
public sealed record Utskriftsvalg(
    IReadOnlyList<int>? DyrIder,
    Utskriftsdel Deler)
{
    /// <summary>Hele permen, alle dyrene.</summary>
    public static Utskriftsvalg Alt { get; } = new(null, Utskriftsdel.Alle);

    public bool Har(Utskriftsdel del) => Deler.HasFlag(del);
}
